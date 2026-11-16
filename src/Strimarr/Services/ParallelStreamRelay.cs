using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Gelato.Services;

/// <summary>Loopback-only, bounded range read-ahead for slow progressive HTTP files.</summary>
internal static class ParallelStreamRelay
{
    internal const int ChunkSize = 1024 * 1024;
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        ConnectTimeout = TimeSpan.FromSeconds(8),
        MaxConnectionsPerServer = 12,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        AutomaticDecompression = DecompressionMethods.None,
        ConnectCallback = StreamHosts.ConnectAsync,
    }) { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly SemaphoreSlim Slots = new(12);
    private static readonly MemoryCache Chunks = new(new MemoryCacheOptions { SizeLimit = 64 * 1024 * 1024 });
    private static readonly ConcurrentDictionary<string, Lazy<Task<byte[]>>> Flights = new();
    private static readonly ConcurrentDictionary<string, Entry> Entries = new();
    private static readonly ConcurrentDictionary<string, string> Ids = new();
    private static readonly object RegistrationGate = new();

    internal sealed class Entry(string key, string url, Dictionary<string, string> headers, int connections)
    {
        internal readonly SemaphoreSlim Connections = new(connections);
        internal readonly string Key = key;
        internal readonly string Url = url;
        internal readonly Dictionary<string, string> Headers = headers;
        internal DateTime Used = DateTime.UtcNow;
        internal Task<Metadata>? MetadataTask;
        internal readonly object Gate = new();
    }
    internal sealed record Metadata(long Length, string Mime, string? ETag, bool Ranges, string Target);

    internal static string? Register(string url, Dictionary<string, string> headers, int connections = 4)
    {
        connections = Math.Clamp(connections, 1, 4);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connections + "\n" + url + "\n" +
            string.Join("\n", headers.OrderBy(h => h.Key).Select(h => h.Key + ":" + h.Value)))));
        lock (RegistrationGate)
        {
        if (Ids.TryGetValue(key, out var existing) && Entries.TryGetValue(existing, out var found))
        {
            found.Used = DateTime.UtcNow;
            return existing;
        }
        foreach (var row in Entries.Where(x => x.Value.Used < DateTime.UtcNow.AddHours(-2)))
            if (Entries.TryRemove(row.Key, out var expired)) Ids.TryRemove(expired.Key, out _);
        if (Entries.Count >= 1024) return null; // Keep the original path when registration is full.
        // Only short-lived opaque capabilities appear in the local URL, never signed upstream URLs.
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        Entries[id] = new Entry(key, url, new(headers, StringComparer.OrdinalIgnoreCase), connections);
        Ids[key] = id;
        foreach (var row in Entries.Where(x => x.Value.Used < DateTime.UtcNow.AddHours(-2)))
            if (Entries.TryRemove(row.Key, out var removed)) Ids.TryRemove(removed.Key, out _);
        return id;
        }
    }

    internal static bool IsLocal(IPAddress? address) => address is not null &&
        IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);

    private static HttpRequestMessage Request(Entry entry, long? start, long? end, string? etag = null, string? target = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, target ?? entry.Url);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0");
        foreach (var h in entry.Headers)
        {
            if (target is not null && new Uri(target).Authority != new Uri(entry.Url).Authority &&
                (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) || h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))) continue;
            if (h.Key.Equals("Range", StringComparison.OrdinalIgnoreCase) ||
                h.Key.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
            request.Headers.Remove(h.Key);
            request.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        request.Headers.AcceptEncoding.ParseAdd("identity");
        if (start is not null) request.Headers.Range = new RangeHeaderValue(start, end);
        if (etag is not null && !etag.StartsWith("W/", StringComparison.Ordinal))
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static async Task<Metadata> Inspect(Entry entry)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var request = Request(entry, 0, 0);
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        var range = response.Content.Headers.ContentRange;
        var ranges = response.StatusCode == HttpStatusCode.PartialContent && range?.From == 0 && range.To == 0 && range.Length > 0;
        return new Metadata(range?.Length ?? response.Content.Headers.ContentLength ?? 0,
            response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream", response.Headers.ETag?.ToString(), ranges, response.RequestMessage?.RequestUri?.AbsoluteUri ?? entry.Url);
    }

    private static async Task<byte[]> Fetch(Entry entry, Metadata metadata, long offset)
    {
        var end = Math.Min(offset + ChunkSize - 1, metadata.Length - 1);
        await entry.Connections.WaitAsync();
        await Slots.WaitAsync();
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                    using var request = Request(entry, offset, end, metadata.ETag, metadata.Target);
                    using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    response.EnsureSuccessStatusCode();
                    var range = response.Content.Headers.ContentRange;
                    if (response.StatusCode != HttpStatusCode.PartialContent || range?.From != offset || range.To != end || range.Length != metadata.Length)
                        throw new InvalidDataException("Upstream changed or did not honor the requested byte range.");
                    var bytes = new byte[checked((int)(end - offset + 1))];
                    using var body = await response.Content.ReadAsStreamAsync(timeout.Token);
                    await body.ReadExactlyAsync(bytes, timeout.Token);
                    return bytes;
                }
                catch (Exception ex) when (attempt == 0 && ex is HttpRequestException or IOException or OperationCanceledException)
                {
                    await Task.Delay(150);
                }
            }
        }
        finally { Slots.Release(); entry.Connections.Release(); }
    }

    private static async Task<byte[]> Chunk(Entry entry, Metadata metadata, long offset)
    {
        var key = entry.Key + ":" + offset;
        if (Chunks.TryGetValue(key, out byte[]? cached)) return cached!;
        var flight = Flights.GetOrAdd(key, _ => new Lazy<Task<byte[]>>(() => Fetch(entry, metadata, offset)));
        try
        {
            var bytes = await flight.Value;
            Chunks.Set(key, bytes, new MemoryCacheEntryOptions { Size = bytes.Length, SlidingExpiration = TimeSpan.FromMinutes(2) });
            return bytes;
        }
        finally { Flights.TryRemove(new KeyValuePair<string, Lazy<Task<byte[]>>>(key, flight)); }
    }

    internal static bool TryRange(string? value, long length, out long start, out long end)
    {
        start = 0; end = length - 1;
        if (string.IsNullOrEmpty(value)) return length > 0;
        if (!Microsoft.Net.Http.Headers.RangeHeaderValue.TryParse(value, out var range) ||
            range.Unit != "bytes" || range.Ranges.Count != 1) return false;
        var part = range.Ranges.Single();
        if (part.From is null)
        {
            if (part.To is null or <= 0) return false;
            start = Math.Max(0, length - part.To.Value);
        }
        else { start = part.From.Value; end = Math.Min(part.To ?? end, end); }
        return start >= 0 && start < length && end >= start;
    }

    private static async Task Direct(HttpContext context, Entry entry)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var request = Request(entry, null, null);
        if (context.Request.Headers.TryGetValue("Range", out var range)) request.Headers.TryAddWithoutValidation("Range", range.ToString());
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        timeout.CancelAfter(Timeout.InfiniteTimeSpan);
        if (response.StatusCode == HttpStatusCode.PartialContent &&
            response.Content.Headers.ContentRange is { Length: > 0 } actual &&
            TryRange(context.Request.Headers.Range.ToString(), actual.Length.Value, out var wantedStart, out var wantedEnd) &&
            (actual.From != wantedStart || actual.To != wantedEnd))
            throw new InvalidDataException("Upstream returned a different range.");
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        context.Response.ContentLength = response.Content.Headers.ContentLength;
        if (response.Content.Headers.ContentRange is { } cr) context.Response.Headers.ContentRange = cr.ToString();
        if (HttpMethods.IsHead(context.Request.Method)) return;
        using var body = await response.Content.ReadAsStreamAsync(context.RequestAborted);
        await body.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    internal static async Task Serve(HttpContext context, string id, int connections)
    {
        if (!IsLocal(context.Connection.RemoteIpAddress)) { context.Response.StatusCode = 403; return; }
        if (!Entries.TryGetValue(id, out var entry)) { context.Response.StatusCode = 404; return; }
        entry.Used = DateTime.UtcNow;
        try
        {
            Task<Metadata> inspect;
            lock (entry.Gate)
            {
                if (entry.MetadataTask is null || entry.MetadataTask.IsFaulted || entry.MetadataTask.IsCanceled)
                    entry.MetadataTask = Inspect(entry);
                inspect = entry.MetadataTask;
            }
            var metadata = await inspect.WaitAsync(context.RequestAborted);
            if (!metadata.Ranges) { await Direct(context, entry); return; }
            var range = context.Request.Headers.Range.ToString();
            if (!TryRange(range, metadata.Length, out var start, out var end))
            {
                // Preserve multipart/unsupported-range behavior through a normal upstream request.
                if (range.Contains(',')) { await Direct(context, entry); return; }
                context.Response.StatusCode = 416;
                context.Response.Headers.ContentRange = "bytes */" + metadata.Length;
                return;
            }
            context.Response.StatusCode = string.IsNullOrEmpty(range) ? 200 : 206;
            context.Response.ContentType = metadata.Mime;
            context.Response.ContentLength = end - start + 1;
            context.Response.Headers.AcceptRanges = "bytes";
            context.Response.Headers.CacheControl = "private, no-store";
            if (metadata.ETag is not null) context.Response.Headers.ETag = metadata.ETag;
            if (!string.IsNullOrEmpty(range)) context.Response.Headers.ContentRange = $"bytes {start}-{end}/{metadata.Length}";
            if (HttpMethods.IsHead(context.Request.Method)) return;
            var queue = new Queue<(long Offset, Task<byte[]> Task)>();
            var next = start / ChunkSize * ChunkSize;
            void Fill()
            {
                while (queue.Count < Math.Clamp(connections, 1, 4) * 2 && next <= end)
                {
                    var offset = next; next += ChunkSize;
                    var task = Chunk(entry, metadata, offset);
                    _ = task.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    queue.Enqueue((offset, task));
                }
            }
            Fill();
            while (queue.Count > 0)
            {
                var (offset, task) = queue.Dequeue();
                var bytes = await task.WaitAsync(context.RequestAborted);
                var first = checked((int)Math.Max(0, start - offset));
                var count = checked((int)(Math.Min(end + 1, offset + bytes.Length) - offset - first));
                await context.Response.Body.WriteAsync(bytes.AsMemory(first, count), context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
                Fill();
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception) when (!context.Response.HasStarted)
        {
            context.Response.Clear();
            try { await Direct(context, entry); }
            catch (Exception)
            {
                if (context.Response.HasStarted) context.Abort();
                else context.Response.StatusCode = 502;
            }
        }
        catch (Exception) { context.Abort(); }
    }
}
