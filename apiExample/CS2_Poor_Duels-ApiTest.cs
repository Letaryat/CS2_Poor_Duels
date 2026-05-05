using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Poor_Duels_Api;

namespace CS2_Poor_DuelsApiExample;

public class CS2_Poor_DuelsApiExample : BasePlugin
{
    public override string ModuleName => "CS2_Poor_Duels Api Example";

    public override string ModuleVersion => "0.0.1";
    public static PluginCapability<IPoorDuelsApi> Capability_SharedAPI { get; } = new("poor_duels_api:sharedapi");
    public static IPoorDuelsApi? SharedDuelsApi { get; private set; } = null;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        SharedDuelsApi = Capability_SharedAPI.Get();
    }
    public override void Load(bool hotReload)
    {
        Console.WriteLine("Api Test Plugin Loaded");

        AddCommand("css_apiAfk", "SetAfk", (p, i) =>
        {
            var player = p;
            if (player == null) return;

            if (SharedDuelsApi == null) return;

            SharedDuelsApi.PerformAFKAction(player, true);

            return;
        });

        AddCommand("css_apiUnAfk", "SetAfk", (p, i) =>
        {
            var player = p;
            if (player == null) return;

            if (SharedDuelsApi == null) return;

            SharedDuelsApi.PerformAFKAction(player, false);

            return;
        });

    }
}
