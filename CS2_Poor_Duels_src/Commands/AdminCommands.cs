using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace CS2_Poor_Duels
{
    public partial class CommandManager
    {
        public void RegisterAdminCMDS()
        {
            if (!_plugin.Config.RegisterAdminCommands) return;

            _plugin.AddCommand("css_dsetspawns", "Setting custom spawns on this map", SetCustomSpawns);
            _plugin.AddCommand("css_dshowspawns", "Creating a Beam for each custom spawn", ShowCustomSpawns);
            _plugin.AddCommand("css_dprintarenas", "Print in console all custom arenas", PrintCustomArenas);
            _plugin.AddCommand("css_dtestarenas", "Testing custom arenas for this map", TestCustomArenaSpawns);
            _plugin.AddCommand("css_dpingtp", "Teleport via ping.", TeleportViaPing);
        }
        
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private void SetCustomSpawns(CCSPlayerController? player, CommandInfo commandInfo)
        {

            if (player == null || !player.IsValid) return;

            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Commands_NoAccess"]}");
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            _plugin.AdminToolsManager!.SavingCustomSpawn(player, pawn.AbsOrigin!, pawn.EyeAngles);

            return;
        }

        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        private void ShowCustomSpawns(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;
            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Commands_NoAccess"]}");
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            _plugin.AdminToolsManager!.ShowCustomSpawns();

            return;
        }

        private void PrintCustomArenas(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;
            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Commands_NoAccess"]}");
                return;
            }
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            _plugin.AdminToolsManager!.PrintCustomArenas(player);

            return;
        }

        private void TestCustomArenaSpawns(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;
            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Commands_NoAccess"]}");
                return;
            }
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            _plugin.MenuManager!.AllCustomArenasMenu(player);

            return;
        }

        private void TeleportViaPing(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;

            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Commands_NoAccess"]}");
                return;
            }

            if (!_plugin.AdminToolsManager!._adminPingTeleport.Contains(player))
            {
                _plugin.AdminToolsManager!._adminPingTeleport.Add(player);
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_TPPingOn"]}");

            }
            else
            {
                _plugin.AdminToolsManager!._adminPingTeleport.Remove(player);
                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_TPPingOff"]}");
            }

            return;
        }


    }
}