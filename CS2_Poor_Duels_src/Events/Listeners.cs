using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Extensions;

namespace CS2_Poor_Duels
{
    public partial class EventManager
    {
        public void RegisterListeners()
        {
            _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
            _plugin.RegisterListener<Listeners.OnTick>(OnTick);
            _plugin.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
            _plugin.RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
            
            /* HookMessages */
            _plugin.HookUserMessage(411, RemoveBloodSpatter, HookMode.Pre);

            //Commands Listeners:
            _plugin.AddCommandListener("changelevel", OnChangeLevelCommand, HookMode.Pre);
            _plugin.AddCommandListener("map", OnChangeLevelCommand, HookMode.Pre);
            _plugin.AddCommandListener("host_workshop_map", OnChangeLevelCommand, HookMode.Pre);
            _plugin.AddCommandListener("ds_workshop_changelevel", OnChangeLevelCommand, HookMode.Pre);
            _plugin.AddCommandListener("jointeam", OnJoinTeamListener, HookMode.Pre);


        }

        private HookResult OnJoinTeamListener(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if(player == null || !player.IsValid || player.IsHLTV || player.IsBot) return HookResult.Continue;
            if (!int.TryParse(commandInfo.GetArg(1), out int targetTeam))
                return HookResult.Continue;

            if((player.Team == CsTeam.Terrorist || player.Team == CsTeam.CounterTerrorist) && !_plugin.QueueManager!._AfkPlayers.Contains(player) && targetTeam != 1)
            {
                PluginExtensions.PlaySoundToClient(player, _plugin.Config.soundsPath.disabledSound);
                return HookResult.Stop;
            }
            return HookResult.Continue;
        }

        private void OnServerPrecacheResources(ResourceManifest manifest)
        {
            manifest.AddResource(_plugin.Config.soundsPath.enabledSound);
            manifest.AddResource(_plugin.Config.soundsPath.disabledSound);
            manifest.AddResource(_plugin.Config.soundsPath.saveWeaponSound);
            manifest.AddResource("agents/models/ctm_gendarmerie/ctm_gendarmerie_varianta.vmdl");
            manifest.AddResource("agents/models/tm_professional/tm_professional_vari.vmdl");
        }

        private void OnTick()
        {
            foreach (var duels in _plugin.DuelManager!.ActiveDuels)
            {
                if (duels.Arena!.showCenterHTML)
                {
                    var p1 = duels.player1;
                    var p2 = duels.player2;

                    if (p1 == null || p2 == null) continue;

                    p1.PrintToCenterHtml(
                        $"{_plugin.Localizer["NowFighting", p2.PlayerName,
                            _plugin.MutualScoring!.mutualScoring_[p1].Kills[p2],
                            _plugin.MutualScoring!.mutualScoring_[p2].Kills[p1]]}"
                    );
                    p2.PrintToCenterHtml(
                        $"{_plugin.Localizer["NowFighting", p1.PlayerName,
                            _plugin.MutualScoring!.mutualScoring_[p2].Kills[p1],
                            _plugin.MutualScoring!.mutualScoring_[p1].Kills[p2]]}"
                    );
                }

            }
        }


        private void OnClientPutInServer(int slot)
        {
            var player = Utilities.GetPlayerFromSlot(slot);

            if (player == null) return;

            OnClientPutInServer(player);
        }

        private void OnClientPutInServer(CCSPlayerController player)
        {
            _plugin.MutualScoring!.mutualScoring_[player] = new MutualScoring();

            foreach (var target in Utilities.GetPlayers())
            {
                if (target != player)
                {
                    _plugin.MutualScoring!.mutualScoring_.TryAdd(target, new());

                    _plugin.MutualScoring!.mutualScoring_[player].Init(target);
                    _plugin.MutualScoring!.mutualScoring_[target].Init(player);
                }
            }
        }

        private void OnMapStart(string mapName)
        {
            _plugin.AddTimer(0.2f, () =>
            {
                _plugin.CustomSpawnsManager!.InitializeCustomSpawnsManager();

                _plugin.PluginUtils!.KillServerCommandEnts();
                _plugin.PluginUtils!.ExecConfig();

                _plugin.ArenaManager!.SetupArenas();
                _plugin.ArenaManager.CacheAllIndexRounds();
            });
        }

        private HookResult RemoveBloodSpatter(UserMessage native)
        {
            native.Recipients.Clear();
            return HookResult.Continue;
        }


        private HookResult OnChangeLevelCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            _plugin.PluginExtensions!.DebugLogger("Clearing cache because of changing map command");

            _plugin.DuelManager!.ActiveDuels.Clear();
            _plugin.QueueManager!._AfkPlayers.Clear();
            _plugin.QueueManager!._WaitingQueue.Clear();
            _plugin.QueueManager._waitingPlayersOnArenas.Clear();

            _plugin.ArenaManager!._arenas.Clear();
            _plugin.ArenaManager!.RoundWeaponIndexCache.Clear();
            _plugin.MutualScoring!.mutualScoring_.Clear();
            _plugin.AdminToolsManager!._adminCustomSpawns.Clear();
            _plugin.AdminToolsManager!._adminPingTeleport.Clear();

            return HookResult.Continue;
        }

    }
}