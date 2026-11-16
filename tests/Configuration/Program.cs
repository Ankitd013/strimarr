using Gelato.Config;

void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
var defaults = new PluginConfiguration();
Assert(defaults.AllowedAudioLanguages.Length == 0, "New installs should allow all languages");
Assert(!defaults.AllowLocalNetworkStreams && !defaults.RemuxDbContribute, "Sensitive options should require opting in");
var global = new PluginConfiguration
{
    Url = "https://addon.example.invalid/config/manifest.json",
    MetadataFirst = false, EnableParallelRelay = false, ParallelRelayConnections = 2,
    ParallelRelayBaseUrl = "http://127.0.0.1:9000/base", ValidateStreamLinks = false,
    AllowedAudioLanguages = ["Japanese"], MaximumVersions = 7, MaximumVersionsPerResolution = 2,
    AllowLocalNetworkStreams = true, EnableJavaScriptInjection = true,
    RefreshVersionsAfterProbe = false, LazyImages = true, BasePath = "/virtual/items",
    ManifestLifetimeHours = 3,
};
var userId = Guid.NewGuid();
global.UserConfigs.Add(new UserConfig { UserId = userId });
var effective = global.GetEffectiveConfig(userId);
foreach (var name in new[] { "MetadataFirst", "EnableParallelRelay", "ParallelRelayConnections", "ParallelRelayBaseUrl", "ValidateStreamLinks", "AllowedAudioLanguages", "MaximumVersions", "MaximumVersionsPerResolution", "AllowLocalNetworkStreams", "EnableJavaScriptInjection", "RefreshVersionsAfterProbe", "LazyImages", "BasePath", "ManifestLifetimeHours" })
{
    var property = typeof(PluginConfiguration).GetProperty(name)!;
    Assert(Equals(property.GetValue(global), property.GetValue(effective)), "User override dropped global " + name);
}
Assert(effective.Url == global.Url, "Empty addon override should use global addon");
Assert(ReferenceEquals(global.GetEffectiveConfig(Guid.NewGuid()), global), "Unconfigured user should use global settings");
Console.WriteLine("PASS safe defaults and global settings inherited by per-user overrides");
