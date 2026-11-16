using Gelato.Services;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();
var data = new byte[4 * 1024 * 1024 + 39];
new Random(7241).NextBytes(data);
var active = 0; var peak = 0; var requests = 0;
var redirects = 0;
var failures = new ConcurrentDictionary<string, int>();
app.MapMethods("/source/{mode}", ["GET", "HEAD"], async (HttpContext c, string mode) =>
{
    Interlocked.Increment(ref requests);
    if (mode == "redirect") { Interlocked.Increment(ref redirects); c.Response.Redirect("/source/normal"); return; }
    if (c.Request.Headers["X-Source-Test"] != "preserved") { c.Response.StatusCode = 401; return; }
    if (!ParallelStreamRelay.TryRange(c.Request.Headers.Range.ToString(), data.Length, out var start, out var end))
    { c.Response.StatusCode = 416; return; }
    var key = mode + ":" + start;
    if (mode == "retry" && end > 0 && failures.AddOrUpdate(key, 1, (_, n) => n + 1) == 1)
    { c.Response.StatusCode = 503; return; }
    if (mode == "ignore") { start = 0; end = data.Length - 1; }
    c.Response.ContentType = "application/octet-stream";
    c.Response.Headers.ETag = "\"test-video\"";
    c.Response.ContentLength = end - start + 1;
    if (mode != "ignore")
    {
        c.Response.StatusCode = 206;
        var headerStart = mode == "bad" && end > 0 ? start + 1 : start;
        c.Response.Headers.ContentRange = $"bytes {headerStart}-{end}/{data.Length}";
    }
    if (HttpMethods.IsHead(c.Request.Method)) return;
    var current = Interlocked.Increment(ref active);
    int previous;
    do { previous = peak; } while (current > previous && Interlocked.CompareExchange(ref peak, current, previous) != previous);
    try
    {
        for (var offset = start; offset <= end;)
        {
            var length = (int)Math.Min(32768, end - offset + 1);
            await c.Response.Body.WriteAsync(data.AsMemory((int)offset, length), c.RequestAborted);
            offset += length;
            await Task.Delay(3, c.RequestAborted);
        }
    }
    finally { Interlocked.Decrement(ref active); }
});
app.MapMethods("/relay/{id}", ["GET", "HEAD"], (HttpContext c, string id) => ParallelStreamRelay.Serve(c, id, 4));
await app.StartAsync();
var address = app.Urls.Single();
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
string Register(string mode) => ParallelStreamRelay.Register(address + "/source/" + mode,
    new() { ["X-Source-Test"] = "preserved" })!;
void Assert(bool okay, string message) { if (!okay) throw new Exception(message); }
async Task Check(string id, string? range, int expectedCode, int start, int length)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, address + "/relay/" + id);
    if (range is not null) request.Headers.TryAddWithoutValidation("Range", range);
    using var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsByteArrayAsync();
    Assert((int)response.StatusCode == expectedCode, range + " status " + response.StatusCode);
    Assert(body.AsSpan().SequenceEqual(data.AsSpan(start, length)), "Byte mismatch for " + range);
    Assert(response.Content.Headers.ContentLength == length, "Wrong Content-Length");
    Console.WriteLine("PASS " + (range ?? "full file") + ": status and byte-for-byte integrity");
}
try
{
    var registered = new ConcurrentBag<string>();
    Parallel.For(0, 256, _ => registered.Add(Register("normal")));
    Assert(registered.Distinct().Count() == 1, "Concurrent registration created duplicate capabilities");
    Console.WriteLine("PASS concurrent registration shares a single capability and connection limit");
    var id = registered.First();
    await Check(id, "bytes=12345-2000000", 206, 12345, 2000000 - 12345 + 1);
    var before = requests;
    await Check(id, "bytes=12345-2000000", 206, 12345, 2000000 - 12345 + 1);
    Assert(requests == before, "Repeated range did not use bounded shared cache");
    Console.WriteLine("PASS audio-switch range reuse without new upstream fetches");
    await Check(id, "bytes=-1234", 206, data.Length - 1234, 1234);
    await Check(id, "bytes=" + (data.Length - 500) + "-", 206, data.Length - 500, 500);
    await Check(id, null, 200, 0, data.Length);
    using (var head = new HttpRequestMessage(HttpMethod.Head, address + "/relay/" + id))
    using (var response = await client.SendAsync(head))
    { Assert(response.Content.Headers.ContentLength == data.Length && (await response.Content.ReadAsByteArrayAsync()).Length == 0, "HEAD failed"); }
    Console.WriteLine("PASS HEAD length and empty body");
    using (var invalid = new HttpRequestMessage(HttpMethod.Get, address + "/relay/" + id))
    {
        invalid.Headers.TryAddWithoutValidation("Range", "bytes=99999999-");
        using var response = await client.SendAsync(invalid);
        Assert(response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable, "Unsatisfiable range not rejected");
    }
    Console.WriteLine("PASS unsatisfiable range -> 416");
    await Check(Register("ignore"), "bytes=100-200", 200, 0, data.Length);
    Console.WriteLine("PASS upstream ignoring ranges falls back to direct fetching");
    await Check(Register("retry"), "bytes=500-900000", 206, 500, 900000 - 500 + 1);
    Console.WriteLine("PASS transient failures retried without corrupting bytes");
    await Check(Register("redirect"), "bytes=200-3000000", 206, 200, 3000000 - 200 + 1);
    Assert(redirects == 1, "Redirect repeated for each chunk instead of resolving once");
    Console.WriteLine("PASS redirects resolved once for all subsequent chunks");
    var disconnectId = Register("disconnect");
    using (var canceled = new CancellationTokenSource())
    {
        using var response = await client.GetAsync(address + "/relay/" + disconnectId, HttpCompletionOption.ResponseHeadersRead, canceled.Token);
        using var body = await response.Content.ReadAsStreamAsync();
        await body.ReadExactlyAsync(new byte[128]);
        canceled.Cancel();
    }
    await Check(disconnectId, "bytes=9000-900000", 206, 9000, 900000 - 9000 + 1);
    Console.WriteLine("PASS disconnect does not poison shared cache or block later requests");

    using (var response = await client.GetAsync(address + "/relay/" + Register("bad")))
        Assert(response.StatusCode == HttpStatusCode.BadGateway, "Mismatched ranges must never be returned as valid video");
    Console.WriteLine("PASS corrupt upstream Content-Range rejected");
    var remote = new DefaultHttpContext(); remote.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.42");
    await ParallelStreamRelay.Serve(remote, id, 4);
    Assert(remote.Response.StatusCode == 403, "Non-loopback client accepted");
    var unknown = new DefaultHttpContext(); unknown.Connection.RemoteIpAddress = IPAddress.Loopback;
    await ParallelStreamRelay.Serve(unknown, "missing", 4);
    Assert(unknown.Response.StatusCode == 404, "Unknown capability accepted");
    Console.WriteLine("PASS loopback-only access and unknown capability rejection");
    Gelato.GelatoPlugin.Instance.Configuration.AllowLocalNetworkStreams = false;
    Assert(StreamHosts.Blocked("untrusted.example", [IPAddress.Loopback]) is not null, "Private upstream address accepted");
    Assert(StreamHosts.Blocked("untrusted.example", [IPAddress.Parse("169.254.169.254")]) is not null, "Link-local upstream accepted");
    Assert(StreamHosts.Blocked("public.example", [IPAddress.Parse("1.1.1.1")]) is null, "Public upstream rejected");
    using (var policyTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
    {
        try
        {
            using var guardedClient = new HttpClient(new SocketsHttpHandler { ConnectCallback = StreamHosts.ConnectAsync });
            using var forbidden = await guardedClient.GetAsync(address + "/source/normal", policyTimeout.Token);
            throw new Exception("Connection-time policy allowed private upstream");
        }
        catch (HttpRequestException) { }
    }
    Console.WriteLine("PASS upstream private/link-local policy and connection-time enforcement");
    Assert(peak <= 12 && peak >= 2, "Concurrency bound incorrect: " + peak);
    Console.WriteLine("PASS parallel upstream concurrency observed, peak=" + peak);
}
finally { await app.StopAsync(); }
