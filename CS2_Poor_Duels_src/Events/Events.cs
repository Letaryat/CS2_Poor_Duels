
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Extensions;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public partial class EventManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public void RegisterEvents()
        {
            RegisterListeners();

            _plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            _plugin.RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
            _plugin.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeamPre, HookMode.Pre);
            _plugin.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            _plugin.RegisterEventHandler<EventSwitchTeam>(OnSwitchTeam);
            _plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeathPre, HookMode.Pre);
            _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            _plugin.RegisterEventHandler<EventPlayerPing>(OnPlayerPing);

            _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            _plugin.RegisterEventHandler<EventItemPurchase>(OnItemPurchase, HookMode.Pre);

            VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);

        }

        private HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            var player = hook.GetParam<CCSPlayer_ItemServices>(0)?.Pawn.Value.Controller.Value!.As<CCSPlayerController>();
            if (player == null || !player.IsValid || !player.PawnIsAlive) return HookResult.Continue;

            var econ = hook.GetParam<CEconItemView>(1);
            if (econ == null) return HookResult.Continue;

            var aqquireMethod = hook.GetParam<AcquireMethod>(2);

            var vdata = VirtualFunctions
                .GetCSWeaponDataFromKeyFunc
                .Invoke(-1, econ.ItemDefinitionIndex.ToString());

            if (vdata == null) return HookResult.Continue;

            if (player.IsBot && (aqquireMethod == AcquireMethod.Buy || aqquireMethod == AcquireMethod.BuyWithCtrl))
            {
                return HookResult.Handled;
            }

            var playerArena = _plugin.ArenaManager!.GetArenaFromPlayer(player);

            RoundType roundType = null!;

            if (playerArena != null)
            {
                roundType = _plugin.Config.DuelRounds[playerArena!.roundType];
            }

            var classname = vdata.Name;
            if (string.IsNullOrWhiteSpace(classname)) return HookResult.Continue;

            if (playerArena != null && roundType != null)
            {
                if (roundType.primaryWeapon == classname || roundType.secondaryWeapon == classname)
                {
                    if (aqquireMethod == AcquireMethod.Buy || aqquireMethod == AcquireMethod.BuyWithCtrl)
                    {
                        if (WeaponModels.rifleItems.Contains(classname))
                        {
                            _plugin.PlayerManager!.SaveWeaponPreference(player, classname, 0);
                        }
                        else if (WeaponModels.pistolItems.Contains(classname))
                        {
                            _plugin.PlayerManager!.SaveWeaponPreference(player, classname, 1);
                        }
                    }

                    return HookResult.Continue; 
                }
            }
            if (WeaponModels.rifleItems.Contains(classname))
            {
                if (aqquireMethod == AcquireMethod.Buy || aqquireMethod == AcquireMethod.BuyWithCtrl)
                {
                    _plugin.PlayerManager!.SaveWeaponPreference(player, classname, 0);
                }

                if (playerArena == null) return HookResult.Continue;

                if (roundType != null && (roundType.forcePrimary || string.IsNullOrEmpty(roundType.primaryWeapon)))
                    return HookResult.Handled;

                return HookResult.Continue;
            }
            else if (WeaponModels.pistolItems.Contains(classname))
            {
                if (aqquireMethod == AcquireMethod.Buy || aqquireMethod == AcquireMethod.BuyWithCtrl)
                {
                    _plugin.PlayerManager!.SaveWeaponPreference(player, classname, 1);
                }

                if (playerArena == null) return HookResult.Continue;

                if (roundType != null && (roundType.forceSecondary || string.IsNullOrEmpty(roundType.secondaryWeapon)))
                    return HookResult.Handled;

                return HookResult.Continue;
            }
            else if (WeaponModels.sniperItems.Contains(classname))
            {
                if (aqquireMethod == AcquireMethod.Buy || aqquireMethod == AcquireMethod.BuyWithCtrl)
                {
                    if (classname == "weapon_awp")
                    {
                        int roundId = _plugin.DuelManager!.GetRoundIndexByName("AWP");
                        if (roundId != -1)
                        {
                            _plugin.PlayerManager!.SaveRoundPreference(player, roundId);
                        }
                    }
                    else if (classname == "weapon_ssg08")
                    {
                        int roundId = _plugin.DuelManager!.GetRoundIndexByName("Scout");
                        if (roundId != -1)
                        {
                            _plugin.PlayerManager!.SaveRoundPreference(player, roundId);
                        }
                    }

                    return HookResult.Handled;
                }
            }
            else if (WeaponModels.illegalWeapons.Contains(classname))
            {
                return HookResult.Handled;
            }

            return HookResult.Continue;
        }

        private HookResult OnItemPurchase(EventItemPurchase @event, GameEventInfo info)
        {
            var player = @event.Userid;
            player!.InGameMoneyServices!.Account = 6969;
            return HookResult.Stop;
        }

        private HookResult OnPlayerDeathPre(EventPlayerDeath @event, GameEventInfo info)
        {
            info.DontBroadcast = true;

            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid) return HookResult.Continue;

            @event.FireEventToClient(attacker);
            @event.FireEventToClient(victim);
            return HookResult.Continue;
        }

        private HookResult OnPlayerTeamPre(EventPlayerTeam @event, GameEventInfo info)
        {
            info.DontBroadcast = true;

            return HookResult.Continue;
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {

            _plugin.QueueManager!.ClearEveryQueue();
            _plugin.ArenaManager!.ResetEveryArena();

            if (_plugin.PluginUtils!.IfWarmup())
            {
                return HookResult.Continue;
            }

            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsHLTV) continue;

                if (!_plugin.QueueManager._WaitingQueue.Contains(p))
                {
                    _plugin.QueueManager!.AddPlayerToQueue(p);
                }
            }

            _plugin.PluginExtensions!.DebugArenaStatus();

            return HookResult.Continue;
        }


        private HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
        {
            info.DontBroadcast = true;

            return HookResult.Continue;
        }

        private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            info.DontBroadcast = true;

            var player = @event.Userid;
            if (player == null) return HookResult.Continue;

            if (_plugin.QueueManager!._usedAfkCMD.Contains(player)) return HookResult.Continue;

            if (@event.Team == (int)CsTeam.Spectator && !_plugin.QueueManager!._AfkPlayers.Contains(player))
            {
                _plugin.QueueManager!.MarkPlayerAsAfk(player);
            }
            else if (@event.Team != (int)CsTeam.Spectator && _plugin.QueueManager!._AfkPlayers.Contains(player))
            {
                _plugin.QueueManager!.UnSetPlayerAsAFK(player);
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;

            if (_plugin.PluginUtils!.IfWarmup())
            {
                _plugin.PluginExtensions!.DebugLogger("Warmup detected. Not doing anything. [PlayerSpawn]");
                return HookResult.Continue;
            }

            if (player.IsBot && !player.IsHLTV)
            {
                _plugin.QueueManager!.AddPlayerToQueue(player);
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;

            return HookResult.Continue;
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;

            if (_plugin.PluginUtils!.IfWarmup())
            {
                _plugin.PluginExtensions!.DebugLogger("Warmup detected. Not doing anything. [PlayerConnectFull]");
                return HookResult.Continue;
            }

            _plugin.PluginExtensions!.DebugLogger($"Adding to WaitingQueue: {player.PlayerName}");

            if (!player.IsHLTV)
            {
                _plugin.QueueManager!.AddPlayerToQueue(player);
            }

            var sid = player.SteamID;

            if (!player.IsBot || !player.IsHLTV)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _plugin.PlayerManager!.AddPlayerToPreferencesList(player, sid);
                    }
                    catch (Exception error)
                    {
                        _plugin.PluginExtensions.DebugLogger($"Error with connectfull {error}");
                    }
                });
            }


            return HookResult.Continue;
        }
        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn != null && pawn.IsValid)
            {
                pawn.Remove();
            }

            var sid = player.SteamID;

            var duel = _plugin.DuelManager!.ActiveDuels.FirstOrDefault(d => d.player1 == player || d.player2 == player);

            if (_plugin.PlayerManager!._playerPreferences.ContainsKey(sid))
            {
                Task.Run(async () =>
                {
                    await _plugin.DatabaseManager!.SavePlayerInformation(sid, _plugin.PlayerManager._playerPreferences[sid]);
                });

            }

            if (_plugin.QueueManager!._AfkPlayers.Contains(player)) _plugin.QueueManager._AfkPlayers.Remove(player);
            if (_plugin.QueueManager!._usedAfkCMD.Contains(player)) _plugin.QueueManager._usedAfkCMD.Remove(player);
            if (_plugin.QueueManager!._WaitingQueue.Contains(player)) _plugin.QueueManager._WaitingQueue.Remove(player);
            _plugin.QueueManager.RemoveWaitingPlayer(player);

            if (duel == null)
            {
                _plugin.PluginExtensions!.DebugLogger("OnPlayerDisconnect Duel not found");
                return HookResult.Continue;
            }

            _plugin.DuelManager.DuelEndPlayerDisconnect(duel, player);

            return HookResult.Continue;
        }
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            info.DontBroadcast = true;

            var victim = @event.Userid;
            var attacker = @event.Attacker;

            var duel = _plugin.DuelManager!.ActiveDuels.FirstOrDefault(d => d.player1 == victim || d.player2 == victim);
            if (duel == null)
            {
                _plugin.PluginExtensions!.DebugLogger("OnPlayerDeath Duel not found");
                return HookResult.Continue;
            }

            if (attacker != null || attacker != victim)
            {
                _plugin.MutualScoring!.MutualScoringOnDeath(attacker!, victim!);
            }

            _plugin.PluginExtensions!.DebugLogger($"PlayerDeath: Victim: {victim!.PlayerName}, Attacker: {attacker!.PlayerName}");

            _plugin.DuelManager.EndDuel(duel);

            return HookResult.Continue;
        }

        //Admin ping teleport:
        private HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return HookResult.Continue;

            if (_plugin.AdminToolsManager!._adminPingTeleport.Contains(player))
            {
                if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
                {
                    return HookResult.Continue;
                }

                pawn.Teleport(new Vector(@event.X, @event.Y, @event.Z));

                PluginExtensions.SendChatMessage(_plugin, player, "AdminTools_SuccessFullTP");
            }

            return HookResult.Continue;
        }


    }
}