using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class DuelManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public List<PlayerDuelData> ActiveDuels = new List<PlayerDuelData>();
        public readonly Dictionary<ulong, bool> _lastSpawnSide = new();

        public void TryStartDuel()
        {
            var players = string.Join(", ", _plugin.QueueManager!._WaitingQueue.Select(p => p.PlayerName));
            _plugin.PluginExtensions!.DebugLogger($"TryStartDuel called. Queue: {_plugin.QueueManager!._WaitingQueue.Count} : {players}");

            if (_plugin.QueueManager!._WaitingQueue.Count >= 2)
            {
                _plugin.AddTimer(1.0f, () =>
                {
                    var random = new Random();

                    var queue = _plugin.QueueManager._WaitingQueue
                        .Where(x =>
                            x != null &&
                            x.IsValid &&
                            x.Connected == PlayerConnectedState.Connected &&
                            !_plugin.QueueManager._AfkPlayers.Contains(x))
                        .ToList();

                    if (queue.Count < 2)
                        return;

                    var selectedPlayers = queue
                        .OrderBy(x => random.Next())
                        .Take(2)
                        .ToList();

                    var p1 = selectedPlayers[0];
                    var p2 = selectedPlayers[1];

                    var freeArena = _plugin.ArenaManager!.GetFreeArena();
                    if (freeArena == null)
                    {
                        _plugin.PluginExtensions!.DebugLogger("No free arena - players stay in queue");
                        return;
                    }

                    _plugin.QueueManager.RemovePlayerFromQueue(p1);
                    _plugin.QueueManager.RemovePlayerFromQueue(p2);

                    if (_plugin.QueueManager._waitingPlayersOnArenas.ContainsKey(p1))
                        _plugin.QueueManager.RemoveWaitingPlayer(p1);

                    if (_plugin.QueueManager._waitingPlayersOnArenas.ContainsKey(p2))
                        _plugin.QueueManager.RemoveWaitingPlayer(p2);

                    StartDuel(p1, p2, freeArena);

                    Server.NextFrame(() =>
                    {
                        TryStartDuel();
                    });
                });
            }
            else if (_plugin.QueueManager._WaitingQueue.Count == 1)
            {
                var p = _plugin.QueueManager._WaitingQueue[0];

                if (!_plugin.QueueManager._waitingPlayersOnArenas.ContainsKey(p))
                {
                    _plugin.ArenaManager!.SpawnPlayerOnEmptyArena(p);
                }
                else
                {
                    //_plugin.PluginExtensions!.DebugLogger($"{p.SteamID} already waiting on arena");
                }
            }
        }

        private void StartDuel(CCSPlayerController p1, CCSPlayerController p2, Arena arena)
        {

            arena.isReserved = false;
            arena.isBusy = true;

            _plugin.PlayerManager!.ChangePlayerTeam(p1, p2);

            if (p1.IsBot || p2.IsBot)
            {
                arena.roundType = _plugin.PlayerManager.GetRandomRound();
            }
            else
            {
                var randomizedRound = _plugin.PlayerManager.GetRandomSharedRound(p1, p2);
                arena.roundType = randomizedRound;
            }

            ActiveDuels.Add(new PlayerDuelData
            {
                player1 = p1,
                player2 = p2,
                Arena = arena
            });

            bool swapSpawns = new Random().Next(0, 2) == 0;

            Vector spawnP1 = swapSpawns ? arena.Spawn2 : arena.Spawn1;
            Vector spawnP2 = swapSpawns ? arena.Spawn1 : arena.Spawn2;

            
            QAngle? angleP1 = swapSpawns ? arena.QAngle2 : arena.QAngle1;
            QAngle? angleP2 = swapSpawns ? arena.QAngle1 : arena.QAngle2;
            
            /*
            QAngle angleP1 = CalculateAngleFacingTarget(spawnP1, spawnP2);
            QAngle angleP2 = CalculateAngleFacingTarget(spawnP2, spawnP1);
            */

            arena.showCenterHTML = true;

            _plugin.AddTimer(0.1f, () =>
            {
                var p1Pawn = p1.PlayerPawn.Value;
                var p2Pawn = p2.PlayerPawn.Value;

                if (p1Pawn == null || p2Pawn == null || !p1Pawn.IsValid || !p2Pawn.IsValid) return;

                if (p1.Connected == PlayerConnectedState.Connected) p1.Respawn();
                if (p2.Connected == PlayerConnectedState.Connected) p2.Respawn();

                p1Pawn?.Teleport(new Vector(spawnP1.X, spawnP1.Y, spawnP1.Z + 5), angleP1);
                p2Pawn?.Teleport(new Vector(spawnP2.X, spawnP2.Y, spawnP2.Z + 5), angleP2);

                Server.NextFrame(() =>
                {
                    _plugin.PlayerManager.GivePlayerWeapon(p1, arena.roundType);
                    _plugin.PlayerManager.GivePlayerWeapon(p2, arena.roundType);
                });

                _plugin.PluginExtensions!.SetPlayerClanTag(p1, "");
                _plugin.PluginExtensions!.SetPlayerClanTag(p2, "");

            });

            _plugin.AddTimer(3.0f, () =>
            {
                arena.showCenterHTML = false;
            });

        }


        private QAngle CalculateAngleFacingTarget(Vector from, Vector to)
        {
            // Oblicz różnicę pozycji
            float deltaX = to.X - from.X;
            float deltaY = to.Y - from.Y;

            // Oblicz yaw (obrót w poziomie) w radianach, potem konwertuj na stopnie
            float yaw = (float)(Math.Atan2(deltaY, deltaX) * (180.0 / Math.PI));

            // Opcjonalnie: oblicz pitch (kąt w pionie) dla bardziej precyzyjnego celowania
            float deltaZ = to.Z - from.Z;
            float horizontalDistance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            float pitch = (float)(Math.Atan2(-deltaZ, horizontalDistance) * (180.0 / Math.PI));

            // QAngle(pitch, yaw, roll)
            // Roll pozostawiamy 0 (brak przechylenia na boki)
            return new QAngle(pitch, yaw, 0);
        }

        public void EndDuel(PlayerDuelData duel)
        {
            _plugin.PluginExtensions!.DebugLogger($"EndDuel on arena {duel.Arena!.Id}");

            _plugin.AddTimer(0.5f, () =>
            {
                duel.Arena.isBusy = false;

                duel.Arena.showCenterHTML = false;

                var p1 = duel.player1;
                var p2 = duel.player2;

                Server.NextFrame(() =>
                {
                    ActiveDuels.Remove(duel);

                    if (p1 != null && p1.IsValid)
                    {
                        PreparePlayer(p1);
                    }

                    if (p2 != null && p2.IsValid)
                    {
                        PreparePlayer(p2);
                    }
                });
            });
        }

        private void PreparePlayer(CCSPlayerController p)
        {
            if (p == null || !p.IsValid) return;

            if (p.Connected != PlayerConnectedState.Connected) return;

            if (_plugin.QueueManager!._AfkPlayers.Contains(p)) return;

            if (p.Team == CsTeam.Spectator || p.Team == CsTeam.None)
            {
                p.ChangeTeam(CsTeam.Terrorist);
            }

            _plugin.QueueManager!.AddPlayerToQueue(p);
        }

        public void DuelEndPlayerDisconnect(PlayerDuelData duel, CCSPlayerController player)
        {

            var players = string.Join(", ", _plugin.QueueManager!._WaitingQueue.Select(p => p.PlayerName));
            _plugin.PluginExtensions!.DebugLogger($"TryStartDuel called. Queue: {_plugin.QueueManager!._WaitingQueue.Count} : {players}");

            if (duel.Arena == null) return;
            var remainingPlayer = duel.player1 == player ? duel.player2 : duel.player1;

            _plugin.PluginExtensions!.DebugLogger($"DuelEndPlayerDisconnect: {player.PlayerName}. Remaining player: {remainingPlayer}");

            duel.Arena.isBusy = false;
            duel.Arena.showCenterHTML = false;

            Server.NextFrame(() =>
            {
                ActiveDuels.Remove(duel);

                if (remainingPlayer != null)
                {
                    remainingPlayer.PrintToCenterAlert($"{_plugin.Localizer["EnemyLeft"]}");
                    _plugin.AddTimer(1.0f, () =>
                    {
                        PreparePlayer(remainingPlayer);
                    });
                    Server.NextFrame(() =>
                    {
                        TryStartDuel();
                    });
                }

            });

        }

        public int GetRoundIndexByName(string roundName)
        {
            if (string.IsNullOrWhiteSpace(roundName)) return -1;
            _plugin.ArenaManager!.RoundWeaponIndexCache.TryGetValue(roundName.ToLower(), out int roundId);
            return roundId;
        }

    }
}
