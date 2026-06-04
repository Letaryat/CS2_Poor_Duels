using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;

namespace CS2_Poor_Duels
{
    public partial class CommandManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;

        public void RegisterCommands()
        {
            RegisterAdminCMDS();

            foreach (var cmd in _plugin.Config.cmdAlias.aliasAFK)
            {
                _plugin.AddCommand(cmd, "Duel AFK CMD", OnAfkCommand);
            }
            foreach (var cmd in _plugin.Config.cmdAlias.aliasRounds)
            {
                _plugin.AddCommand(cmd, "Select rounds that you want to play.", OnRoundsCommand);
            }
            foreach (var cmd in _plugin.Config.cmdAlias.aliasGuns)
            {
                _plugin.AddCommand(cmd, "Select favorite guns you would like to play.", OnGunsCommand);
            }
            foreach (var cmd in _plugin.Config.cmdAlias.aliasRifles)
            {
                _plugin.AddCommand(cmd, "Select your favorite rifle.", OnRifleCommand);
            }
            foreach (var cmd in _plugin.Config.cmdAlias.aliasPistols)
            {
                _plugin.AddCommand(cmd, "Select your favorite pistol.", OnPistolCommand);
            }
            foreach (var cmd in _plugin.Config.cmdAlias.aliasDuel)
            {
                _plugin.AddCommand(cmd, "Start a duel.", OnDuelCommand);
            }


            /*
            _plugin.AddCommand("css_scout", "Scout round preference", OnScoutCommand);
            _plugin.AddCommand("css_awp", "AWP round preference", OnAWPCommand);
            */
        }

        private void OnGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;
            _plugin.MenuManager!.GunMenu(player);
            return;
        }

        private void OnRoundsCommand(CCSPlayerController? p, CommandInfo commandInfo)
        {
            if (p == null || !p.IsValid) return;
            _plugin.MenuManager!.SelectRoundPreference(p);
            return;
        }

        private void OnRifleCommand(CCSPlayerController? p, CommandInfo commandInfo)
        {
            if (p == null || !p.IsValid) return;
            _plugin.MenuManager!.SelectWeaponPreference(p, 0);
        }
        private void OnPistolCommand(CCSPlayerController? p, CommandInfo commandInfo)
        {
            if (p == null || !p.IsValid) return;
            _plugin.MenuManager!.SelectWeaponPreference(p, 1);
        }

        private void OnDuelCommand(CCSPlayerController? p, CommandInfo commandInfo)
        {
            if (p == null || !p.IsValid) return;
            Server.PrintToChatAll("TUTAJ MENU DO DUEL CZELENDZ");
            _plugin.MenuManager!.DuelChallengeSettings(p);
        }

        private void OnAfkCommand(CCSPlayerController? p, CommandInfo commandInfo)
        {
            if (p == null || !p.IsValid) return;

            _plugin.QueueManager!._usedAfkCMD.Add(p);

            if (!_plugin.QueueManager!._AfkPlayers.Contains(p))
            {
                _plugin.QueueManager!.MarkPlayerAsAfk(p);

                p.CommitSuicide(false, true);

                p.ChangeTeam(CsTeam.Spectator);
            }
            else
            {
                _plugin.QueueManager!.UnSetPlayerAsAFK(p);
                p.ChangeTeam(CsTeam.Terrorist);
            }

            _plugin.AddTimer(0.2f, () =>
            {
                _plugin.QueueManager!._usedAfkCMD.Remove(p);
            });

        }

    }
}