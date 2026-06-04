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

        // CHALLANGE SYSTEM
        public Dictionary<CCSPlayerController, DuelChallenge> PendingChallenges = new();
        public List<DuelChallenge> ActiveChallenges = new();

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

        public void StartDuel(CCSPlayerController p1, CCSPlayerController p2, Arena arena, bool isChallenge = false)
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
                Arena = arena,
                IsChallengeDuel = isChallenge
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


        public void ForceEndDuel(CCSPlayerController player)
        {
            var duel = ActiveDuels.FirstOrDefault(d =>
                d.player1 == player ||
                d.player2 == player);

            if (duel == null) return;

            if (duel.Arena == null) return;

            var opponent = duel.player1 == player ? duel.player2 : duel.player1;

            duel.Arena.isBusy = false;
            duel.Arena.showCenterHTML = false;

            ActiveDuels.Remove(duel);

            if (opponent != null)
            {
                _plugin.AddTimer(1.0f, () =>
                {
                    PreparePlayer(opponent);
                });
                Server.NextFrame(() =>
                {
                    TryStartDuel();
                });
            }

            _plugin.PluginExtensions!.DebugLogger(
                $"Force ended duel arena {duel.Arena.Id}"
            );
        }

        public int GetRoundIndexByName(string roundName)
        {
            if (string.IsNullOrWhiteSpace(roundName)) return -1;
            _plugin.ArenaManager!.RoundWeaponIndexCache.TryGetValue(roundName.ToLower(), out int roundId);
            return roundId;
        }


        /* CHALLENGE SYSTEM (WIP) */
        public void PreStartDuelChallenge(DuelChallenge challenge)
        {
            var challenger = challenge.Challenger;
            var target = challenge.Target;

            if (challenger == null || target == null)
                return;

            _plugin.QueueManager!.RemovePlayerFromQueue(challenger);
            _plugin.QueueManager.RemovePlayerFromQueue(target);

            _plugin.QueueManager.RemoveWaitingPlayer(challenger);
            _plugin.QueueManager.RemoveWaitingPlayer(target);

            ForceEndDuel(challenger);
            ForceEndDuel(target);

            var arena = _plugin.ArenaManager!.GetFreeArena();

            if (arena == null)
            {
                challenger.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["ArenaNotFound"]}");
                target.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["ArenaNotFound"]}");
                return;
            }

            challenge.Arena = arena;

            ActiveChallenges.Add(challenge);

            StartDuel(
                challenger,
                target,
                arena,
                isChallenge: true
            );

            PendingChallenges.Remove(challenger);
        }

        public void EndDuelChallenge(DuelChallenge challenge)
        {
            if (challenge == null) return;

            var challenger = challenge.Challenger;
            var target = challenge.Target;

            if (challenger == null || target == null) return;
            if (challenge.Arena == null) return;

            ActiveDuels.RemoveAll(d =>
                (d.player1 == challenger && d.player2 == target) ||
                (d.player1 == target && d.player2 == challenger));

            challenge.Arena.isBusy = false;
            challenge.Arena.showCenterHTML = false;

            ActiveChallenges.Remove(challenge);

            var winner = challenge.ChallengerWins > challenge.TargetWins ? challenger : target;
            var loser = winner == challenger ? target : challenger;
            var winnerKills = winner == challenger ? challenge.ChallengerWins : challenge.TargetWins;
            var loserKills  = loser == challenger ? challenge.ChallengerWins : challenge.TargetWins;

            Server.PrintToChatAll($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["DuelWonServerMessage", winner.PlayerName, loser.PlayerName, winnerKills, loserKills]}");

            _plugin.AddTimer(1.0f, () =>
            {
                PreparePlayer(challenger);
                PreparePlayer(target);
            });

            Server.NextFrame(() => TryStartDuel());
        }

        public void RestartChallengeDuel(DuelChallenge challenge, PlayerDuelData duel)
        {
            var p1 = duel.player1;
            var p2 = duel.player2;
            var oldArena = duel.Arena;

            if (p1 == null || p2 == null || oldArena == null) return;

            int wins1 = challenge.Challenger == p1 ? challenge.ChallengerWins : challenge.TargetWins;
            int wins2 = challenge.Challenger == p2 ? challenge.ChallengerWins : challenge.TargetWins;

            oldArena.isBusy = false;
            oldArena.isReserved = false;
            oldArena.showCenterHTML = false;
            ActiveDuels.Remove(duel);

            _plugin.AddTimer(1.0f, () =>
            {
                if (p1 == null || !p1.IsValid || p2 == null || !p2.IsValid) return;

                var newArena = _plugin.ArenaManager!.GetFreeArena();
                if (newArena == null)
                {
                    _plugin.PluginExtensions!.DebugLogger("RestartChallengeDuel: No free arena found");
                    newArena = oldArena;
                }

                challenge.Arena = newArena;

                StartDuel(p1, p2, newArena, isChallenge: true);
            });
        }

        public void ChallengeEndPlayerDisconnect(DuelChallenge challenge, CCSPlayerController disconnectedPlayer)
        {
            if (challenge == null) return;

            var remainingPlayer = challenge.Challenger == disconnectedPlayer
                ? challenge.Target
                : challenge.Challenger;

            _plugin.PluginExtensions!.DebugLogger(
                $"ChallengeEndPlayerDisconnect: {disconnectedPlayer.PlayerName} left. Remaining: {remainingPlayer?.PlayerName ?? "none"}");

            if (challenge.Arena != null)
            {
                challenge.Arena.isBusy = false;
                challenge.Arena.isReserved = false;
                challenge.Arena.showCenterHTML = false;
            }

            ActiveDuels.RemoveAll(d =>
                (d.player1 == challenge.Challenger && d.player2 == challenge.Target) ||
                (d.player1 == challenge.Target && d.player2 == challenge.Challenger));

            if (PendingChallenges.ContainsKey(challenge.Challenger))
                PendingChallenges.Remove(challenge.Challenger);

            ActiveChallenges.Remove(challenge);

            Server.NextFrame(() =>
            {
                if (remainingPlayer != null && remainingPlayer.IsValid &&
                    remainingPlayer.Connected == PlayerConnectedState.Connected)
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


    }
}
