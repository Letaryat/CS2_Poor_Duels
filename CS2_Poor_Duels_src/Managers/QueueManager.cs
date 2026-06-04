using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Extensions;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class QueueManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public List<CCSPlayerController> _WaitingQueue = new List<CCSPlayerController>();
        public List<CCSPlayerController> _AfkPlayers = new List<CCSPlayerController>();
        public List<CCSPlayerController> _usedAfkCMD = new List<CCSPlayerController>();
        public readonly Dictionary<CCSPlayerController, Arena> _waitingPlayersOnArenas = new();

        public void AddPlayerToQueue(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.IsHLTV) return;

            if(player.Connected != PlayerConnectedState.Connected) return;

            if (_AfkPlayers.Contains(player))
            {
                _plugin.PluginExtensions!.DebugLogger($"Ignored AddToQueue: {player.PlayerName} (spectator/afk)");
                return;
            }

            if (_plugin.DuelManager!.ActiveDuels.Any(d => d.player1 == player || d.player2 == player))
            {
                _plugin.PluginExtensions!.DebugLogger($"Ignored AddToQueue: {player.PlayerName} (active duel)");
                return;
            }

            var challenge = _plugin.DuelManager.ActiveChallenges.FirstOrDefault(c => c.Challenger == player || c.Target == player);
            if(challenge != null)
            {
                _plugin.PluginExtensions!.DebugLogger($"Ignored AddToQueue: {player.PlayerName} (pending challenge - starting now)");
                _plugin.DuelManager.PreStartDuelChallenge(challenge);
                return;
            }

            if (!_WaitingQueue.Contains(player))
            {
                _WaitingQueue.Add(player);
                _plugin.PluginExtensions!.DebugLogger($"Added {player.PlayerName} to queue (total: {_WaitingQueue.Count})");

                PluginExtensions.SendChatMessage(_plugin, player, "AddedToQueue");

                _plugin.AddTimer(0.15f, () =>
                {
                    if (player.IsValid)
                    {
                        _plugin.DuelManager!.TryStartDuel();
                    }
                });
            }
        }

        public void RegisterWaitingPlayer(CCSPlayerController player, Arena arena)
        {
            _waitingPlayersOnArenas[player] = arena;
            _plugin.PluginExtensions!.DebugLogger($"Registered {player.PlayerName} waiting on arena {arena.Id}");
        }

        public void RemovePlayerFromQueue(CCSPlayerController player)
        {
            if (_WaitingQueue.Contains(player))
            {
                _WaitingQueue.Remove(player);
                _plugin.PluginExtensions!.DebugLogger($"Removed {player.UserId} | {player.SteamID} from queue");
            }
        }

        public void RemoveWaitingPlayer(CCSPlayerController player)
        {
            if (_waitingPlayersOnArenas.TryGetValue(player, out var arena))
            {
                _waitingPlayersOnArenas.Remove(player);

                if (arena.isReserved)
                {
                    arena.isReserved = false;
                    _plugin.PluginExtensions!.DebugLogger($"Freed reserved arena {arena.Id} - {player.PlayerName} no longer waiting");
                }
                else
                {
                    _plugin.PluginExtensions!.DebugLogger($"Skipped freeing arena {arena.Id} - it was in duel or already free");
                }
            }
        }
        public void MarkPlayerAsAfk(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || player.IsHLTV) return;
            if (_AfkPlayers.Contains(player))
                return;

            _AfkPlayers.Add(player);

            RemovePlayerFromQueue(player);
            RemoveWaitingPlayer(player);

            var duel = _plugin.DuelManager!.ActiveDuels
                .FirstOrDefault(d => d.player1 == player || d.player2 == player);

            if (duel != null)
            {
                _plugin.DuelManager!.DuelEndPlayerDisconnect(duel, player);
            }

            _plugin.PluginExtensions!.SetPlayerClanTag(player, $"{_plugin.Localizer["AFKTag"]}");

            PluginExtensions.SendChatMessage(_plugin, player, "AFK");

            _plugin.PluginExtensions!.DebugLogger($"Marked {player.PlayerName} as AFK (spectator)");
        }


        public void UnSetPlayerAsAFK(CCSPlayerController player)
        {
            if (player.IsHLTV) return;
            if (!_AfkPlayers.Contains(player))
                return;

            _AfkPlayers.Remove(player);
            _plugin.PluginExtensions!.SetPlayerClanTag(player, "");
            PluginExtensions.SendChatMessage(_plugin, player, "NotAFK");

            AddPlayerToQueue(player);
            _plugin.PluginExtensions!.DebugLogger($"Unmarked {player.PlayerName} as AFK (joined queue)");
        }
        public void ClearEveryQueue()
        {
            _WaitingQueue.Clear();
            _AfkPlayers.Clear();
            _waitingPlayersOnArenas.Clear();
            _plugin.PluginExtensions!.DebugLogger("Cleared queue");
        }

    }
}