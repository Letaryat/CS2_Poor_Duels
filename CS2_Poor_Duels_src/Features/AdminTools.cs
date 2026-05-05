using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class AdminToolsManager(CS2_Poor_DuelsPlugin plugin)
    {
        public readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public Dictionary<CCSPlayerController, List<CustomSpawn>> _adminCustomSpawns = new();
        public List<CCSPlayerController> _adminPingTeleport = new();
        public int ifBeamsSpawned = 0;

        public void SavingCustomSpawn(CCSPlayerController player, Vector pos, QAngle angle)
        {
            if (!_adminCustomSpawns.ContainsKey(player))
            {
                _adminCustomSpawns[player] = new List<CustomSpawn>
                                            {
                                                new CustomSpawn { posX = pos.X, posY = pos.Y, posZ = pos.Z, angleX = angle.X, angleY = angle.Y, angleZ = angle.Z },
                                                new CustomSpawn()
                                            };

                player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_FirstSpawn"]}");
                _plugin.PluginUtils!.CreatePlayerEntity(new Vector(pos.X, pos.Y, pos.Z), new QAngle(angle.X, angle.Y, angle.Z));
                return;
            }
            else
            {
                _adminCustomSpawns[player][1] = new CustomSpawn { posX = pos.X, posY = pos.Y, posZ = pos.Z, angleX = angle.X, angleY = angle.Y, angleZ = angle.Z };

                try
                {
                    _plugin.CustomSpawnsManager!.PushCordsToFile(_adminCustomSpawns[player][0], _adminCustomSpawns[player][1]);
                    player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_SecondSpawn"]}");
                    _plugin.PluginUtils!.CreatePlayerEntity(new Vector(pos.X, pos.Y, pos.Z), new QAngle(angle.X, angle.Y, angle.Z), false);

                    _adminCustomSpawns.Remove(player);

                    _plugin.AddTimer(5.0f, () =>
                    {
                        Server.PrintToChatAll($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_FinishedCustomSpawns"]}");

                        _plugin.PluginUtils.RemoveAllPlayerModels();

                        _plugin.ArenaManager!.SetupArenas();
                    });



                }
                catch (Exception error)
                {
                    _plugin.PluginExtensions!.DebugLogger($"Something went wrong with saving custom spawns.\n{error}");
                    player.PrintToChat($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["Error"]}");
                }
            }
        }
        public void ShowCustomSpawns()
        {
            if (ifBeamsSpawned == 0)
            {
                foreach (var spawns in _plugin.ArenaManager!._arenas)
                {
                    _plugin.PluginUtils!.GenerateBeams(spawns.Spawn1, "Blue");
                    _plugin.PluginUtils!.GenerateBeams(spawns.Spawn2, "Red");
                }
                ifBeamsSpawned = 1;
                Server.PrintToChatAll($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_Beams"]}");
            }
            else
            {
                _plugin.PluginUtils!.RemoveAllBeams();
                ifBeamsSpawned = 0;
                Server.PrintToChatAll($"{_plugin.Localizer["Prefix"]}{_plugin.Localizer["AdminTools_BeamsRemove"]}");
            }
            return;
        }

        public void PrintCustomArenas(CCSPlayerController player)
        {
            foreach (var arenas in _plugin.CustomSpawnsManager!._cacheCustomArenas)
            {
                player.PrintToConsole($"---- Arena: {arenas.Id} ----");
                player.PrintToConsole($"Spawn 1: {arenas.Spawn1.posX} {arenas.Spawn1.posY} {arenas.Spawn1.posZ} | Angle 1: {arenas.Spawn1.angleX} {arenas.Spawn1.angleY} {arenas.Spawn1.angleZ}");
                player.PrintToConsole($"Spawn 1: {arenas.Spawn2.posX} {arenas.Spawn2.posY} {arenas.Spawn2.posZ} | Angle 1: {arenas.Spawn2.angleX} {arenas.Spawn2.angleY} {arenas.Spawn2.angleZ}");
            }
            player.PrintToConsole($"---- All Arenas: {_plugin.CustomSpawnsManager!._cacheCustomArenas.Count} ----");
            return;
        }

    }
}