using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Extensions;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class ArenaManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public List<Arena> _arenas = new List<Arena>();
        public Dictionary<string, int> RoundWeaponIndexCache = new();

        private static float _THRESHOLD_DISTANCE = 2000;

        public void SetupArenas()
        {
            _arenas.Clear();

            _plugin.PluginExtensions!.DebugLogger("Setting up arenas...");

            var spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                .Cast<CBaseEntity>()
                .Concat(
                    Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                        .Cast<CBaseEntity>()
                )
                .ToList();

            var teleportDestinations = Utilities.FindAllEntitiesByDesignerName<CInfoTeleportDestination>("info_teleport_destination").ToList();


            var allSpawns = spawnPoints
                .Concat(teleportDestinations)
                .ToList();

            var arenasFromFile = _plugin.CustomSpawnsManager!.LoadArenasFromFile();

            // Custom map Spawns

            if (arenasFromFile.Count > 0)
            {
                _plugin.CustomSpawnsManager.DisableAllSpawns(
                    Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                        .Concat(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist"))
                        .ToList()
                );
                foreach (var fileArena in arenasFromFile)
                {
                    var arena = Arena.FromFile(fileArena);
                    _plugin.CustomSpawnsManager.CreateCustomTeleportDestination(arena);
                    _arenas.Add(arena);
                }
                _plugin.PluginExtensions!.DebugLogger($"Loaded {_arenas.Count} arenas from file {_plugin.CustomSpawnsManager._mapName}");
                return;
            }

            // No spawns, plugin dead

            if (allSpawns.Count == 0)
            {
                _plugin.PluginExtensions!.DebugLogger("No spawns detected");
                return;
            }

            int id = 1;

            // Mapmaker spawns:

                // Cybershoke
            if (teleportDestinations.Count > 0)
            {
                _plugin.PluginExtensions.DebugLogger("Uzywamy aren teleportDestinations");

                _plugin.CustomSpawnsManager.DisableAllSpawns(
                    Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist")
                        .Concat(
                            Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist")
                        )
                        .ToList()
                );

                List<List<CInfoTeleportDestination>> grouped = GroupTeleportDestinationsByArena(teleportDestinations);

                foreach (var group in grouped)
                {
                    if (group.Count < 2)
                        continue;

                    if (group[0].AbsOrigin == null || group[1].AbsOrigin == null)
                        continue;

                    var ct = _plugin.CustomSpawnsManager.CreateSafeSpawn(group[0].AbsOrigin!, group[0].AbsRotation!, 0);
                    var t = _plugin.CustomSpawnsManager.CreateSafeSpawn(group[1].AbsOrigin!, group[1].AbsRotation!, 1);

                    _arenas.Add(new Arena(id++, ct.AbsOrigin!, t.AbsOrigin!, false, ct.AbsRotation, t.AbsRotation));
                }

                _plugin.PluginExtensions.DebugLogger(
                    $"Loaded {_arenas.Count} arenas from teleport destinations"
                );

            }
                // Normal ones
            else
            {
                List<List<CBaseEntity>> grouped = GroupSpawnsByArena(spawnPoints);
                _plugin.PluginExtensions.DebugLogger("We are using normal spawns");
                foreach (var group in grouped)
                {
                    if (group.Count < 2) continue;

                    var (spawn1, spawn2) = _plugin.CustomSpawnsManager.FindFarthestSpawns(group);

                    if (spawn1.AbsOrigin == null || spawn2.AbsOrigin == null) continue;

                    _arenas.Add(new Arena(id++, spawn1.AbsOrigin!, spawn2.AbsOrigin!, false, spawn1.AbsRotation, spawn2.AbsRotation));
                }
            }

            _plugin.PluginExtensions!.DebugLogger($"Created {_arenas.Count} arenas");
        }

        private static List<List<CBaseEntity>> GroupSpawnsByArena(List<CBaseEntity> spawns)
        {
            List<List<CBaseEntity>> arenas = new();

            foreach (var spawn in spawns)
            {
                if (spawn == null || spawn.AbsOrigin == null)
                    continue;

                bool added = false;

                foreach (var arena in arenas)
                {
                    var first = arena[0];

                    if (first.AbsOrigin == null)
                        continue;

                    if (Math.Abs(first.AbsOrigin.X - spawn.AbsOrigin.X) < _THRESHOLD_DISTANCE &&
                        Math.Abs(first.AbsOrigin.Y - spawn.AbsOrigin.Y) < _THRESHOLD_DISTANCE)
                    {
                        arena.Add(spawn);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    arenas.Add(new List<CBaseEntity> { spawn });
                }
            }

            return arenas;
        }

        private static List<List<CInfoTeleportDestination>> GroupTeleportDestinationsByArena(List<CInfoTeleportDestination> teleportDestinations)
        {
            List<List<CInfoTeleportDestination>> arenas = new();
            Dictionary<string, List<CInfoTeleportDestination>> grouped = new();

            foreach (var entity in teleportDestinations)
            {
                if (entity == null || entity.AbsOrigin == null)
                    continue;

                var targetName = entity.Entity!.Name;

                if (string.IsNullOrWhiteSpace(targetName))
                    continue;

                string lowered = targetName.ToLower();

                if (!lowered.Contains("ct_") && !lowered.Contains("t_"))
                    continue;

                if (lowered.Contains("nav_") || lowered.Contains("walkable"))
                    continue;

                int arenaIndex = lowered.IndexOf("_arena_");

                if (arenaIndex == -1)
                    continue;

                string arenaKey = lowered.Substring(arenaIndex + 1);

                if (!grouped.ContainsKey(arenaKey))
                {
                    grouped[arenaKey] = new List<CInfoTeleportDestination>();
                }

                grouped[arenaKey].Add(entity);
            }

            foreach (var kvp in grouped)
            {
                if (kvp.Value.Count >= 2)
                {
                    arenas.Add(kvp.Value);
                }
            }

            return arenas;
        }

        public void SpawnPlayerOnEmptyArena(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            _plugin.PluginExtensions!.DebugLogger($"SpawnPlayerOnEmptyArena: {player.PlayerName}");

            if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist)
                player.ChangeTeam(CsTeam.Terrorist);

            var arena = GetFreeArena();
            if (arena == null)
            {
                _plugin.PluginExtensions!.DebugLogger("No free arena for waiting player!");

                PluginExtensions.SendChatMessage(_plugin, player, "ArenaNotFound");
                _plugin.PluginExtensions!.SetPlayerClanTag(player, $"{_plugin.Localizer["WaitingTag"]}");
                player.ChangeTeam(CsTeam.Spectator);
                return;
            }

            arena.isReserved = true;
            _plugin.QueueManager!.RegisterWaitingPlayer(player, arena);

            if (!_plugin.QueueManager!._WaitingQueue.Contains(player))
            {
                _plugin.QueueManager!._WaitingQueue.Add(player);
                _plugin.PluginExtensions!.DebugLogger($"SpawnPlayerOnEmptyArena: Somehow player {player.UserId} | {player.PlayerName} Was not added to WaitingQueue, so we add him back.");
            }


            _plugin.AddTimer(0.3f, () =>
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid)
                {
                    player.Respawn();
                    pawn.Teleport(arena.Spawn1, arena.QAngle1);

                    _plugin.PlayerManager!.GivePlayerWeapon(player, 0);

                    player.PrintToCenterAlert(_plugin.Localizer["EmptyArenaNotifier"]);

                    _plugin.PluginExtensions!.SetPlayerClanTag(player, $"{_plugin.Localizer["WaitingTag"]}");
                    _plugin.PluginExtensions!.DebugLogger($"Player {player.PlayerName} waiting on arena {arena.Id}");
                }
            });
        }

        public Arena? GetFreeArena()
        {
            var freeArenas = _arenas.Where(a => !a.isBusy && !a.isReserved).ToList();

            if (freeArenas.Count == 0)
            {
                _plugin.PluginExtensions!.DebugLogger("No free arenas!");
                return null;
            }

            var random = new Random();
            var chosen = freeArenas[random.Next(freeArenas.Count)];

            _plugin.PluginExtensions!.DebugLogger($"Found free arena {chosen.Id} (randomly chosen from {freeArenas.Count} free arenas)");
            return chosen;
        }

        public Arena? GetArenaFromPlayer(CCSPlayerController player)
        {
            var duel = _plugin.DuelManager!.ActiveDuels.FirstOrDefault(d => d.player1 == player || d.player2 == player);

            if (duel == null) return null;

            return duel.Arena;
        }

        public void CacheAllIndexRounds()
        {
            RoundWeaponIndexCache.Clear();
            for (int i = 0; i < _plugin.Config.DuelRounds.Count(); i++)
            {
                var round = _plugin.Config.DuelRounds[i];
                if (!string.IsNullOrWhiteSpace(round.Name))
                {
                    RoundWeaponIndexCache[round.Name.ToLower()] = i;
                }
            }
            _plugin.PluginExtensions!.DebugLogger($"Rounds cached: {RoundWeaponIndexCache.Count()}");
        }


        public void ResetEveryArena()
        {
            foreach (var arena in _arenas)
            {
                arena.isBusy = false;
                arena.isReserved = false;
                arena.showCenterHTML = false;
                _plugin.DuelManager!.ActiveDuels.Clear();
            }
        }


    }
}