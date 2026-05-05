using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;
using CS2MenuManager.API.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Models;

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
    }
}