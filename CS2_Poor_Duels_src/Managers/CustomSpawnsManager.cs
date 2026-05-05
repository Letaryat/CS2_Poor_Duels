using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class CustomSpawnsManager(CS2_Poor_DuelsPlugin plugin)
    {
        public readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public string? _mapName;
        private string? _mapFilePath;
        public readonly List<ArenaToFile> _cacheCustomArenas = [];
        private static readonly object _fileLock = new();

        public void InitializeCustomSpawnsManager()
        {
            GetMapInfo();
            GenerateJsonFile();
        }

        public void GetMapInfo()
        {
            var map = Server.MapName;
            _mapName = map;
            _mapFilePath = Path.Combine(_plugin.ModuleDirectory, "maps", $"{map}.json");
        }
        public void GenerateJsonFile()
        {
            string directoryPath = Path.Combine(_plugin.ModuleDirectory, "maps");
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _plugin.PluginExtensions!.DebugLogger($"Created directory at: {directoryPath}");
                }
                if (!File.Exists(_mapFilePath))
                {
                    File.WriteAllText(_mapFilePath!, "[]");
                    _plugin.PluginExtensions!.DebugLogger($"Created file {_mapFilePath}");
                }
            }
            catch (Exception error)
            {
                _plugin.PluginExtensions!.DebugLogger($"Something went wrong with creating a Json file.\n{error}");
            }
        }

        public void PushCordsToFile(CustomSpawn spawn1, CustomSpawn spawn2)
        {
            lock (_fileLock)
            {
                if (spawn1 == null || spawn2 == null)
                    return;

                int newId = _cacheCustomArenas.Count;

                var arena = new ArenaToFile
                {
                    Id = newId,
                    Spawn1 = spawn1,
                    Spawn2 = spawn2
                };

                _cacheCustomArenas.Add(arena);

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_mapFilePath!, JsonSerializer.Serialize(_cacheCustomArenas, options));

                _plugin.PluginExtensions!.DebugLogger($"Saved arena #{newId} with spawn1: {spawn1.posX}, {spawn1.posY}, {spawn1.posZ}");
                Server.PrintToChatAll("[CS2_Poor_Duels] Custom spawn points saved!");
            }
        }

        public List<ArenaToFile> LoadArenasFromFile()
        {
            if (!File.Exists(_mapFilePath))
            {
                _plugin.PluginExtensions!.DebugLogger($"No custom arenas file found for this map.");
                return new List<ArenaToFile>();
            }

            try
            {
                string json = File.ReadAllText(_mapFilePath!);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<ArenaToFile>();

                var arenas = JsonSerializer.Deserialize<List<ArenaToFile>>(json) ?? new List<ArenaToFile>();
                _plugin.PluginExtensions!.DebugLogger($"Loaded {arenas.Count} arenas from JSON file.");

                _cacheCustomArenas.Clear();
                _cacheCustomArenas.AddRange(arenas);

                return arenas;
            }
            catch (Exception ex)
            {
                _plugin.PluginExtensions!.DebugLogger($"Error while loading arenas: {ex.Message}");
                return new List<ArenaToFile>();
            }
        }

        public void DisableAllSpawns(List<SpawnPoint> Spawns)
        {
            foreach (var s in Spawns)
            {
                s.AcceptInput("SetDisabled");
            }
            _plugin.PluginExtensions!.DebugLogger("All map spawns are disabled. Reading now spawns from file.");
        }

        public void CreateCustomTeleportDestination(Arena arena)
        {
            if (arena.Spawn1 == null || arena.Spawn2 == null) return;

            SpawnPoint? CTentity = Utilities.CreateEntityByName<SpawnPoint>("info_player_counterterrorist");
            if (CTentity == null || !CTentity.IsValid) return;

            CTentity.Teleport(arena.Spawn1, arena.QAngle1);
            CTentity.DispatchSpawn();

            SpawnPoint? Tentity = Utilities.CreateEntityByName<SpawnPoint>("info_player_terrorist");
            if (Tentity == null || !Tentity.IsValid) return;

            Tentity.Teleport(arena.Spawn2, arena.QAngle2);
            Tentity.DispatchSpawn();

        }
        public SpawnPoint CreateSafeSpawn(Vector pos, QAngle rot, int team)
        {
            SpawnPoint entity;
            if(team == 0)
            {
                entity = Utilities.CreateEntityByName<SpawnPoint>("info_player_counterterrorist")!;
            }
            else
            {
                entity = Utilities.CreateEntityByName<SpawnPoint>("info_player_terrorist")!;
            }
            
            if (entity == null || !entity.IsValid) return null!;

            entity.Teleport(pos, rot);
            entity.DispatchSpawn();

            return entity;
        }


        public (CBaseEntity, CBaseEntity) FindFarthestSpawns(List<CBaseEntity> spawns)
        {
            if (spawns == null || spawns.Count < 2)
                throw new ArgumentException("At least 2 spawns are required.");

            if (spawns.Count == 2)
                return (spawns[0], spawns[1]);

            float maxDistance = 0f;

            CBaseEntity farthest1 = spawns[0];
            CBaseEntity farthest2 = spawns[1];

            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i] == null || spawns[i].AbsOrigin == null)
                    continue;

                for (int j = i + 1; j < spawns.Count; j++)
                {
                    if (spawns[j] == null || spawns[j].AbsOrigin == null)
                        continue;

                    var pos1 = spawns[i].AbsOrigin!;
                    var pos2 = spawns[j].AbsOrigin!;

                    float dx = pos1.X - pos2.X;
                    float dy = pos1.Y - pos2.Y;
                    float dz = pos1.Z - pos2.Z;

                    float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        farthest1 = spawns[i];
                        farthest2 = spawns[j];
                    }
                }
            }

            return (farthest1, farthest2);
        }


    }
}