using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;
using Microsoft.Extensions.Logging;

namespace CS2_Poor_Duels.Extensions
{
    public partial class PluginExtensions
    {
        private readonly CS2_Poor_DuelsPlugin _plugin;

        public PluginExtensions(CS2_Poor_DuelsPlugin plugin)
        {
            _plugin = plugin;
        }

        public static void SendChatMessage(CS2_Poor_DuelsPlugin plugin, CCSPlayerController player, string key, params object[] args)
        {
            if (player == null || !player.IsValid)
                return;

            var message = plugin.Localizer[key, args];
            if (!message.ResourceNotFound)
            {
                player.PrintToChat($"{plugin.Localizer["Prefix"]}{message}");
            }
        }
        public void DebugLogger(string message)
        {
            if (_plugin.Config.DebugMode == 1)
            {
                _plugin.Logger.LogInformation($"[CS2_Poor_Duels] {message}");
            }
            else if (_plugin.Config.DebugMode == 2)
            {
                Console.WriteLine($"[CS2_Poor_Duels] {message}");
            }

        }
        public void DebugArenaStatus()
        {
            if (_plugin.Config.DetailedDebugMode)
            {
                if (_plugin.Config.DebugMode == 1)
                {
                    foreach (var arena in _plugin.ArenaManager!._arenas)
                    {
                        bool waiting = _plugin.QueueManager!._waitingPlayersOnArenas.Values.Contains(arena);
                        bool duel = _plugin.DuelManager!.ActiveDuels.Any(d => d.Arena == arena);
                        DebugLogger($"[ARENA {arena.Id}] Busy={arena.isBusy}, Waiting={waiting}, Duel={duel}");
                    }
                }
                else if (_plugin.Config.DebugMode == 2)
                {
                    foreach (var arena in _plugin.ArenaManager!._arenas)
                    {
                        bool waiting = _plugin.QueueManager!._waitingPlayersOnArenas.Values.Contains(arena);
                        bool duel = _plugin.DuelManager!.ActiveDuels.Any(d => d.Arena == arena);
                        Console.WriteLine($"[ARENA {arena.Id}] Busy={arena.isBusy}, Waiting={waiting}, Duel={duel}");
                    }
                }
            }

        }

        public static void PlaySoundToClient(CCSPlayerController player, string soundPath)
        {
            if (player == null || !player.IsValid) return;
            if (string.IsNullOrEmpty(soundPath) || string.IsNullOrWhiteSpace(soundPath)) return;
            player.ExecuteClientCommand($"play {soundPath}");
        }

    }
}