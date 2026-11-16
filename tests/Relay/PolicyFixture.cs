// Minimal configuration host for exercising the production socket policy without a Jellyfin server.
namespace Gelato.Config
{
    public class PluginConfiguration
    {
        public bool AllowLocalNetworkStreams { get; set; } = true;
        public string Url { get; set; } = "";
        public List<UserConfig> UserConfigs { get; set; } = [];
    }
    public class UserConfig { public string Url { get; set; } = ""; }
}
namespace Gelato
{
    public class GelatoPlugin
    {
        public static GelatoPlugin Instance { get; } = new();
        public Config.PluginConfiguration Configuration { get; } = new();
    }
}
