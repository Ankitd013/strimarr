using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Gelato.Config;

namespace Gelato.Services;

/// <summary>Bounded, cached sample checks run only in background stream discovery.</summary>
internal static class CheckedStreamVersions
{
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectCallback = StreamHosts.ConnectAsync,
    });
    private static readonly SemaphoreSlim Slots = new(12);
    private static readonly ConcurrentDictionary<string, (DateTimeOffset Until, bool Good)> Cache = new();
    private static readonly Regex Resolution = new(@"\b(2160|1080|720|576|480|360|240)p\b", RegexOptions.IgnoreCase);

    internal static async Task<List<StremioStream?>> FilterAsync(
        List<StremioStream?> streams, PluginConfiguration cfg, CancellationToken ct, bool includeHidden = false)
    {
        var hosts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var urls = new HashSet<string>(StringComparer.Ordinal);
        var candidates = streams.OfType<StremioStream>()
            .Where(s => includeHidden || cfg.AllowedAudioLanguages.Length == 0 || cfg.AllowedAudioLanguages.Any(l =>
                Regex.IsMatch(s.Name ?? "", @"\b" + Regex.Escape(l) + @"\b", RegexOptions.IgnoreCase)))
            .OrderBy(s => (includeHidden || cfg.AllowedAudioLanguages.Length == 0) && (s.Description ?? s.Title ?? "").Contains("Original Audio", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(s => Rank(GetResolution(s)))
            .Where(s =>
            {
                if (!Uri.TryCreate(s.Url, UriKind.Absolute, out var uri) || !urls.Add(s.Url)) return false;
                var count = hosts.GetValueOrDefault(uri.IdnHost);
                if (count >= 4) return false;
                hosts[uri.IdnHost] = count + 1;
                return true;
            }).Take(24).ToList();
        var good = await Task.WhenAll(candidates.Select(async s =>
            !cfg.ValidateStreamLinks || await CheckAsync(s, ct).ConfigureAwait(false))).ConfigureAwait(false);
        var tiers = new Dictionary<string, int>();
        var result = new List<StremioStream?>();
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!good[i]) continue;
            var s = candidates[i]; var tier = GetResolution(s);
            var count = tiers.GetValueOrDefault(tier);
            if (count >= Math.Clamp(cfg.MaximumVersionsPerResolution, 1, 20)) continue;
            tiers[tier] = count + 1;
            Normalize(s); result.Add(s);
            if (result.Count >= Math.Clamp(cfg.MaximumVersions, 1, 50)) break;
        }
        return result;
    }

    private static string GetResolution(StremioStream s) => Resolution.Match(s.Name ?? "").Value.ToLowerInvariant();
    private static int Rank(string value) => value switch
    {
        "1080p" => 0, "720p" => 1, "2160p" => 2, "576p" => 3, "480p" => 4, "360p" => 5, _ => 6
    };

    private static async Task<bool> CheckAsync(StremioStream s, CancellationToken ct)
    {
        var headers = s.BehaviorHints?.ProxyHeaders?.Request;
        var identity = s.Url + string.Join(";", (headers ?? []).OrderBy(h => h.Key).Select(h => h.Key + "=" + h.Value));
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        if (Cache.TryGetValue(key, out var cached) && cached.Until > DateTimeOffset.UtcNow) return cached.Good;
        await Slots.WaitAsync(ct).ConfigureAwait(false);
        var good = false;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(7));
            using var request = new HttpRequestMessage(HttpMethod.Get, s.Url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0");
            if (headers is not null)
                foreach (var h in headers) { request.Headers.Remove(h.Key); request.Headers.TryAddWithoutValidation(h.Key, h.Value); }
            request.Headers.Range = new RangeHeaderValue(0, 1023);
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var mime = response.Content.Headers.ContentType?.MediaType ?? "";
            if (response.IsSuccessStatusCode && !mime.Contains("html", StringComparison.OrdinalIgnoreCase)
                && !mime.Contains("json", StringComparison.OrdinalIgnoreCase) && mime != "text/plain")
            {
                using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                var buffer = new byte[1024];
                var length = await body.ReadAtLeastAsync(buffer, 64, false, timeout.Token).ConfigureAwait(false);
                var prefix = Encoding.ASCII.GetString(buffer, 0, length).TrimStart();
                good = length >= 64 && !prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
                    && !prefix.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
        catch (HttpRequestException) { }
        catch (IOException) { }
        finally { Slots.Release(); }
        Cache[key] = (DateTimeOffset.UtcNow.AddSeconds(good ? 300 : 90), good);
        if (Cache.Count > 3000)
            foreach (var row in Cache.Where(r => r.Value.Until <= DateTimeOffset.UtcNow).Take(1000)) Cache.TryRemove(row.Key, out _);
        return good;
    }

    private static void Normalize(StremioStream s)
    {
        s.BehaviorHints ??= new();
        s.BehaviorHints.Filename ??= (s.Description ?? s.Title)?.Split('\n')[0];
        var detail = s.Description ?? s.Title ?? "";
        var audio = detail.Contains("Original Audio", StringComparison.OrdinalIgnoreCase) ? "Original audio (language unverified)"
            : detail.Contains("ptbr Audio", StringComparison.OrdinalIgnoreCase) ? "Portuguese (Brazil)"
            : detail.Contains("esla Audio", StringComparison.OrdinalIgnoreCase) ? "Spanish (Latin America)" : "Language unknown";
        var label = (s.Name ?? "Stream").Split('\n')[0];
        if (label.Contains("|  |")) label = label.Replace("|  |", "| " + audio + " |");
        if (s.BehaviorHints.VideoSize is > 0) label += $" | {s.BehaviorHints.VideoSize.Value / 1073741824d:0.##} GiB";
        var host = new Uri(s.Url).IdnHost;
        var hostLabel = host.EndsWith("googleusercontent.com", StringComparison.OrdinalIgnoreCase) ? "Google"
            : host.EndsWith("cloudflarestorage.com", StringComparison.OrdinalIgnoreCase) ? "R2"
            : host.EndsWith("workers.dev", StringComparison.OrdinalIgnoreCase) ? "Worker" : host;
        s.Name = label + " | " + hostLabel;
        s.Description = ""; s.Title = null;
    }
}
