using System.Collections.Concurrent;
namespace Gelato.Services;
internal static class HiddenVersions
{
    private static readonly ConcurrentDictionary<(Guid User, Guid Item), byte> Enabled = new();
    internal static bool Includes(Guid user, Guid item) => Enabled.ContainsKey((user,item));
    internal static void Set(Guid user, Guid item, bool enabled)
    {
        if (enabled) Enabled[(user,item)] = 0;
        else Enabled.TryRemove((user,item), out _);
    }
}
