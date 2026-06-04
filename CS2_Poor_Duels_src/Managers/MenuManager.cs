using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;
using CS2MenuManager.API.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Models;
using CounterStrikeSharp.API;

namespace CS2_Poor_Duels
{
    public class MenuManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;

        public void AllCustomArenasMenu(CCSPlayerController player)
        {
            if (player == null) return;
            WasdMenu menu = new($"{_plugin.Localizer["CustomArenaMenuTitle"]}", _plugin);
            foreach (var arena in _plugin.CustomSpawnsManager!._cacheCustomArenas)
            {
                menu.AddItem($"Arena: {arena.Id}", (p, o) =>
                {
                    ArenaSpawnsMenu(player, arena.Id, menu);
                });
            }
            menu.Display(player, 0);
        }

        public void ArenaSpawnsMenu(CCSPlayerController player, int _arenaId, WasdMenu parentMenu)
        {
            if (player == null) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;
            WasdMenu menu = new($"Arena {_arenaId}", _plugin);
            menu.PrevMenu = parentMenu;
            var arena = _plugin.CustomSpawnsManager!._cacheCustomArenas[_arenaId];
            menu.AddItem($"{_plugin.Localizer["TeleportToArenaTitle"]} 1", (p, o) =>
            {
                pawn.Teleport(new Vector(arena.Spawn1.posX, arena.Spawn1.posY, arena.Spawn1.posZ), new QAngle(arena.Spawn1.angleX, arena.Spawn1.angleY, arena.Spawn1.angleZ));
                o.PostSelectAction = CS2MenuManager.API.Enum.PostSelectAction.Nothing;
            });

            menu.AddItem($"{_plugin.Localizer["TeleportToArenaTitle"]} 2", (p, o) =>
            {
                pawn.Teleport(new Vector(arena.Spawn2.posX, arena.Spawn2.posY, arena.Spawn2.posZ), new QAngle(arena.Spawn2.angleX, arena.Spawn2.angleY, arena.Spawn2.angleZ));
                o.PostSelectAction = CS2MenuManager.API.Enum.PostSelectAction.Nothing;
            });

            menu.Display(player, 0);
        }

        public void GunMenu(CCSPlayerController player)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer[$"GunPreferenceMenuTitle"], _plugin);
            menu.AddItem(_plugin.Localizer[$"WeaponPreferenceMenuTitle_0"], (p, o) =>
            {
                SelectWeaponPreference(player, 0, menu);
            });
            menu.AddItem(_plugin.Localizer[$"WeaponPreferenceMenuTitle_1"], (p, o) =>
            {
                SelectWeaponPreference(player, 1, menu);
            });
            menu.Display(player, 0);
        }

        public void SelectWeaponPreference(CCSPlayerController player, int type, ChatMenu prevMenu = null!)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer[$"WeaponPreferenceMenuTitle_{type}"], _plugin);

            if (prevMenu != null)
            {
                menu.PrevMenu = prevMenu;
            }

            var weapons = type == 0
                ? WeaponModels.rifleItems
                : WeaponModels.pistolItems;

            foreach (var weapon in weapons)
            {
                menu.AddItem($"{weapon.Split("weapon_")[1].ToUpper()}", (p, o) =>
                {
                    switch (type)
                    {
                        case 0:
                            _plugin.PlayerManager!.SaveWeaponPreference(p, weapon, 0);
                            break;
                        case 1:
                            _plugin.PlayerManager!.SaveWeaponPreference(p, weapon, 1);
                            break;
                    }
                });
            }
            menu.Display(player, 0);
        }

        public void SelectRoundPreference(CCSPlayerController player)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer["RoundPreferenceMenuTitle"], _plugin);
            for (int i = 0; i < _plugin.Config.DuelRounds.Count; i++)
            {
                int index = i;
                var roundType = _plugin.Config.DuelRounds[i];
                var playerContains = _plugin.PlayerManager!.IfPlayerHaveRoundEnabled(player, i);

                var ifEnabled = playerContains
                    ? $"{_plugin.Localizer["Enabled"]}"
                    : $"{_plugin.Localizer["Disabled"]}";

                menu.AddItem($"{roundType.Name!.ToUpper()} - {ifEnabled}", (p, o) =>
                {
                    _plugin.PlayerManager!.SaveRoundPreference(player, index);
                });
            }
            menu.Display(player, 0);
        }


        /* CHALLENGE SYSTEM (WIP) */

        public void DuelChallengeSettings(CCSPlayerController player)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer["DuelChallengeSettingsMenuTitle"], _plugin);

            if (!_plugin.DuelManager!.PendingChallenges.ContainsKey(player))
            {
                var challenge = new DuelChallenge
                {
                    Challenger = player,
                    RoundType = 0,
                    MaxRounds = 5
                };

                _plugin.DuelManager!.PendingChallenges[player] = challenge;
            }


            menu.AddItem($"{(_plugin.DuelManager!.PendingChallenges[player].Target != null ? $"{_plugin.Localizer["DuelChallengePlayer"]} {_plugin.DuelManager!.PendingChallenges[player].Target.PlayerName}" : $"{_plugin.Localizer["DuelChallengePlayerSelect"]}")}", (pl, o) =>
            {
                DuelChallengePlayerMenu(player, menu);
            });

            // ToDo: What Round + How many round

            menu.AddItem($"{(_plugin.Config.DuelRounds[_plugin.DuelManager!.PendingChallenges[player].RoundType].Name != null ? $"{_plugin.Localizer["DuelChallengeRound"]} {_plugin.Config.DuelRounds[_plugin.DuelManager!.PendingChallenges[player].RoundType].Name}" : $"{_plugin.Localizer["DuelChallengeRoundSelect"]}")}", (pl, o) =>
            {
                DuelChallengeRoundType(player, menu);
            });

            menu.AddItem($"{(_plugin.DuelManager!.PendingChallenges[player].MaxRounds != 0 ? $"{_plugin.Localizer["DuelChallengeNumberRounds"]} {_plugin.DuelManager!.PendingChallenges[player].MaxRounds}" : $"{_plugin.Localizer["DuelChallengeNumberRoundsSelect"]}")}", (pl, o) =>
            {
                DuelChallengeSelectNumberRounds(player, menu);
            });
            menu.AddItem($"{_plugin.Localizer["DuelChallengeChallenge"]}", (pl, o) =>
            {
                if (_plugin.DuelManager!.PendingChallenges[player].Target != null)
                {
                    DuelChallengeSendInvitation(_plugin.DuelManager!.PendingChallenges[player].Target, player);
                }
            }, _plugin.DuelManager!.PendingChallenges[player].Target == null ? CS2MenuManager.API.Enum.DisableOption.DisableShowNumber : CS2MenuManager.API.Enum.DisableOption.None);

            menu.Display(player, 0);
        }

        public void DuelChallengePlayerMenu(CCSPlayerController player, ChatMenu prevMenu)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer["DuelChallengeSelectPlayerTitle"], _plugin);

            var availablePlayers = Utilities.GetPlayers()
                .Where(p =>
                    p != null &&
                    p != player &&
                    !p.IsHLTV &&
                    p.IsValid &&
                    p.Connected == PlayerConnectedState.Connected)
                .ToList();

            if (availablePlayers.Count == 0)
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["NoPlayersInList"]}");
                return;
            }

            foreach (var p in availablePlayers)
            {
                menu.AddItem($"{p.PlayerName}", (pl, o) =>
                {
                    _plugin.DuelManager!.PendingChallenges[player].Target = p;
                    o.PostSelectAction = CS2MenuManager.API.Enum.PostSelectAction.Close;
                    DuelChallengeSettings(player);
                });
            }
            menu.PrevMenu = prevMenu;
            menu.Display(player, 0);
        }

        private void DuelChallengeRoundType(CCSPlayerController player, ChatMenu prevMenu)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer["DuelChallengeRoundTypeTitle"], _plugin);

            for (int i = 0; i < _plugin.Config.DuelRounds.Count; i++)
            {
                int index = i;
                var roundType = _plugin.Config.DuelRounds[i];

                menu.AddItem($"{roundType.Name!.ToUpper()}", (p, o) =>
                {
                    _plugin.DuelManager!.PendingChallenges[player].RoundType = index;
                    o.PostSelectAction = CS2MenuManager.API.Enum.PostSelectAction.Close;
                    DuelChallengeSettings(player);
                });
            }

            menu.PrevMenu = prevMenu;
            menu.Display(player, 0);
        }

        private void DuelChallengeSelectNumberRounds(
            CCSPlayerController player,
            ChatMenu prevMenu = null!)
        {
            if (player == null) return;

            ChatMenu menu = new(
                _plugin.Localizer["DuelChallengeNumberRoundsTitle"],
                _plugin);


            for (int i = 5; i <= 30; i += 5)
            {
                int rounds = i;

                menu.AddItem($"{rounds}", (pl, o) =>
                {
                    _plugin.DuelManager!
                        .PendingChallenges[player]
                        .MaxRounds = rounds;
                    o.PostSelectAction = CS2MenuManager.API.Enum.PostSelectAction.Close;
                    DuelChallengeSettings(player);
                });
            }

            menu.PrevMenu = prevMenu;
            menu.Display(player, 0);
        }

        private void DuelChallengeSendInvitation(CCSPlayerController player, CCSPlayerController challenger)
        {
            if (player == null) return;
            ChatMenu menu = new(_plugin.Localizer["DuelChallengeInvitation", challenger], _plugin);

            
            if(player.IsBot)
            {
                _plugin.DuelManager!.PendingChallenges[challenger].Accepted = true;
                _plugin.DuelManager.PreStartDuelChallenge(_plugin.DuelManager!.PendingChallenges[challenger]);
                return;
            }
            

            menu.AddItem($"{_plugin.Localizer["InviteAccept"]}", (pl, o) =>
            {
                _plugin.DuelManager!.PendingChallenges[challenger].Accepted = true;
                _plugin.DuelManager.PreStartDuelChallenge(_plugin.DuelManager!.PendingChallenges[challenger]);
            });

            menu.AddItem($"{_plugin.Localizer["InviteDecline"]}", (pl, o) =>
            {
                _plugin.DuelManager!.PendingChallenges[challenger].Accepted = false;
                if (_plugin.DuelManager.PendingChallenges.ContainsKey(challenger))
                {
                    _plugin.DuelManager.PendingChallenges.Remove(challenger);
                }
            });

            menu.Display(player, 15);

            _plugin.AddTimer(15.0f, () =>
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["InviteTimeEnd"]}");
                challenger.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["InviteTimeEnd"]}");

                if (_plugin.DuelManager!.PendingChallenges.ContainsKey(challenger))
                {
                    _plugin.DuelManager.PendingChallenges.Remove(challenger);
                }

            });


        }


    }
}