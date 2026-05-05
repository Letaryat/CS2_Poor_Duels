using Dapper;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Models;
using MySqlConnector;

namespace CS2_Poor_Duels
{
    public class DatabaseManager(CS2_Poor_DuelsPlugin plugin)
    {
        public readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        private string _connectionString = string.Empty;
        public void InitializeDBConnection()
        {
            var config = _plugin.Config;
            if (string.IsNullOrEmpty(config.DBSetup.DBHost) ||
                string.IsNullOrEmpty(config.DBSetup.DBName) ||
                string.IsNullOrEmpty(config.DBSetup.DBPassword) ||
                string.IsNullOrEmpty(config.DBSetup.DBUsername))
            {
                _plugin.PluginExtensions!.DebugLogger("MySQL database configuration is incomplete!");
                return;
            }

            MySqlConnectionStringBuilder builder = new()
            {
                Server = config.DBSetup.DBHost,
                UserID = config.DBSetup.DBUsername,
                Port = config.DBSetup.DBPort,
                Password = config.DBSetup.DBPassword,
                Database = config.DBSetup.DBName,
            };

            _connectionString = builder.ConnectionString;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                _plugin.PluginExtensions!.DebugLogger("Connected to Database.");
                string createTablesQuery = @"
                CREATE TABLE IF NOT EXISTS PoorDuels_PlayerPreferences(
                    SteamID VARCHAR(255),
                    rifleWeapon VARCHAR(255),
                    pistolWeapon VARCHAR(255),
                    roundPreferences VARCHAR(255)
                );";

                connection.Execute(createTablesQuery);
            }
            catch (Exception error)
            {
                _plugin.PluginExtensions!.DebugLogger($"Error while connecting to database: {error}");
            }
        }
        public async Task<PlayerPreferences?> GetPlayerInformation(ulong steamId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return null;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                var playerInfo = await connection.QueryFirstOrDefaultAsync<PlayerPreferencesDB>(
                    "SELECT rifleWeapon, pistolWeapon, roundPreferences FROM PoorDuels_PlayerPreferences WHERE SteamID = @steamId",
                    new { steamId });

                if (playerInfo == null)
                {
                    return new PlayerPreferences
                    {
                        RifleWeapon = null,
                        PistolWeapon = null,
                        RoundPreferences = Enumerable.Range(0, _plugin.Config.DuelRounds.Count).ToList()
                    };
                }

                var roundIds = new List<int>();
                if (!string.IsNullOrEmpty(playerInfo.RoundPreferences))
                {
                    roundIds = playerInfo.RoundPreferences
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out var id) ? id : -1)
                        .Where(id => id >= 0)
                        .ToList();
                }

                return new PlayerPreferences
                {
                    RifleWeapon = playerInfo.RifleWeapon ?? null,
                    PistolWeapon = playerInfo.PistolWeapon ?? null,
                    RoundPreferences = roundIds
                };
            }
            catch (Exception error)
            {
                _plugin.PluginExtensions!.DebugLogger($"Error with fetching player data. {error}");
                return null;
            }
        }

        private async Task<bool> CheckIfPlayerExist(MySqlConnection connection, ulong steamId)
        {
            try
            {
                string sql = "SELECT COUNT(1) FROM PoorDuels_PlayerPreferences WHERE SteamID = @steamId";
                var exists = await connection.ExecuteScalarAsync<bool>(sql, new { steamId });
                return exists;
            }
            catch (Exception error)
            {
                _plugin.PluginExtensions!.DebugLogger($"Error with checking if player exists.\n {error}");
                return false;
            }
        }

        public async Task SavePlayerInformation(ulong steamId, PlayerPreferences playerData)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                _plugin.PluginExtensions!.DebugLogger($"Saving to the database {steamId}");
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var exists = await CheckIfPlayerExist(connection, steamId);
                string roundPrefs = string.Join(",", playerData.RoundPreferences);

                if (exists)
                {
                    await connection.ExecuteAsync(@"
                UPDATE PoorDuels_PlayerPreferences 
                SET rifleWeapon = @rifle, 
                    pistolWeapon = @pistol, 
                    roundPreferences = @roundpref 
                WHERE SteamID = @steamId",
                    new
                    {
                        steamId,
                        rifle = playerData.RifleWeapon,
                        pistol = playerData.PistolWeapon,
                        roundpref = roundPrefs
                    });
                }
                else
                {
                    await connection.ExecuteAsync(@"
                INSERT INTO PoorDuels_PlayerPreferences 
                    (SteamID, rifleWeapon, pistolWeapon, roundPreferences)
                VALUES (@steamId, @rifle, @pistol, @roundpref)",
                    new
                    {
                        steamId,
                        rifle = playerData.RifleWeapon,
                        pistol = playerData.PistolWeapon,
                        roundpref = roundPrefs
                    });
                }
            }
            catch (Exception error)
            {
                _plugin.PluginExtensions!.DebugLogger($"Error with Saving player information \n {error}");
            }
        }



    }
}