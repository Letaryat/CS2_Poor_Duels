using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Extensions;
using Poor_Duels_Api;

namespace CS2_Poor_Duels
{
    public class ApiManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        
        public static PluginCapability<IPoorDuelsApi> Capability_SharedAPI { get; } = 
            new("poor_duels_api:sharedapi");

        public void InitializeAPI()
        {
            Capabilities.RegisterPluginCapability(Capability_SharedAPI, () => new PoorDuelsApiImpl(_plugin));
        }
    }

    public class PoorDuelsApiImpl : IPoorDuelsApi  
    {
        private readonly CS2_Poor_DuelsPlugin _plugin;

        public PoorDuelsApiImpl(CS2_Poor_DuelsPlugin plugin)
        {
            _plugin = plugin;
        }

        public void PerformAFKAction(CCSPlayerController player, bool afk)
        {
            if (afk)
            {
                if (!_plugin.QueueManager!._AfkPlayers.Contains(player))
                {
                    _plugin.QueueManager!._usedAfkCMD.Add(player);
                    _plugin.QueueManager!.MarkPlayerAsAfk(player);
                    player.CommitSuicide(false, true);
                    player.ChangeTeam(CsTeam.Spectator);
                }
            }
            else
            {
                if (_plugin.QueueManager!._AfkPlayers.Contains(player))
                {
                    _plugin.QueueManager!.UnSetPlayerAsAFK(player);
                    player.ChangeTeam(CsTeam.Terrorist);
                }
            }
            
            _plugin.AddTimer(0.2f, () =>
            {
                _plugin.QueueManager!._usedAfkCMD.Remove(player);
            });
        }

        public bool IsAFK(CCSPlayerController player)
        {
            return _plugin.QueueManager!._AfkPlayers.Contains(player);
        }

        public void ForceTryStartDuel()
        {
            _plugin.DuelManager!.TryStartDuel();
        }
    }
}