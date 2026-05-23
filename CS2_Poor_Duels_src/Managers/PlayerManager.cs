using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Extensions;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class PlayerManager(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;

        public Dictionary<ulong, PlayerPreferences> _playerPreferences = new();

        // Takes only the steamid (no CCSPlayerController): this method runs on a background thread via
        // Task.Run in OnPlayerConnectFull, and CCSPlayerController properties must only be accessed on
        // the main game thread. All player-state checks happen before Task.Run.
        public async Task AddPlayerToPreferencesList(ulong steamid)
        {
            if (steamid == 0) return;
            if (_playerPreferences.ContainsKey(steamid)) return;

            var playerInformation = await _plugin.DatabaseManager!.GetPlayerInformation(steamid);
            if (playerInformation == null) return;

            _playerPreferences[steamid] = playerInformation;
        }
        public void ChangePlayerTeam(CCSPlayerController player1, CCSPlayerController player2)
        {
            var player1Pawn = player1.PlayerPawn.Value;
            if (player1Pawn == null || !player1Pawn.IsValid) return;
            var player2Pawn = player2.PlayerPawn.Value;
            if (player2Pawn == null || !player2Pawn.IsValid) return;

            if (player2.PawnIsAlive)
            {
                _plugin.PluginUtils!.ChangeToOppositeTeam(player1, player2);
            }
            else if (player1.PawnIsAlive)
            {
                _plugin.PluginUtils!.ChangeToOppositeTeam(player2, player1);
            }
            else
            {
                _plugin.PluginUtils!.ChangeToTeamsIDontFuckingCareAnyLonger(player1, player2);
            }
        }

        public void GivePlayerWeapon(CCSPlayerController player, int roundTypeId)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            player.RemoveWeapons();
            player.GiveNamedItem(CsItem.Knife);

            if (roundTypeId < 0 || roundTypeId >= _plugin.Config.DuelRounds.Count)
            {
                return;
            }

            var round = _plugin.Config.DuelRounds[roundTypeId];

            string primary;
            string secondary;

            if(round.forceArmor)
            {
                pawn.ArmorValue = 100;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
            }
            
            if (round.forceHelmet){
                if(pawn.ItemServices != null)
                {
                    new CCSPlayer_ItemServices(pawn.ItemServices.Handle).HasHelmet = true;
                    Utilities.SetStateChanged(pawn, "CCSPlayer_ItemServices", "m_bHasHelmet");
                }
            }

            if (round.forceSecondary == true)
            {
                secondary = round.secondaryWeapon!;
                Server.NextFrame(() =>
                {
                    NativeAPI.IssueClientCommand(player.Slot, "slot2");
                });
            }
            else
            {
                secondary = CheckPlayerWeaponPreference(player, round.secondaryWeapon!, false);
                Server.NextFrame(() =>
                {
                    NativeAPI.IssueClientCommand(player.Slot, "slot2");
                });
            }

            if (round.forcePrimary == true)
            {
                primary = round.primaryWeapon!;
                Server.NextFrame(() =>
                {
                    NativeAPI.IssueClientCommand(player.Slot, "slot1");
                });
            }
            else
            {
                primary = CheckPlayerWeaponPreference(player, round.primaryWeapon!, true);
                Server.NextFrame(() =>
                {
                    NativeAPI.IssueClientCommand(player.Slot, "slot1");
                });
            }

            if (!string.IsNullOrEmpty(primary))
                player.GiveNamedItem(primary);

            if (!string.IsNullOrEmpty(secondary))
                player.GiveNamedItem(secondary);

            //_plugin.PluginExtensions!.DebugLogger($"Gave weapons to {player.PlayerName}: {primary}, {secondary}");
        }

        private string CheckPlayerWeaponPreference(CCSPlayerController player, string defaultWeapon, bool isPrimary)
        {
            if (player == null || !player.IsValid)
                return defaultWeapon;

            var sid = player.SteamID;
            if (!_playerPreferences.TryGetValue(sid, out var pref) || pref == null)
                return defaultWeapon;

            string? weapon = isPrimary ? pref.RifleWeapon : pref.PistolWeapon;

            if (!string.IsNullOrEmpty(weapon))
                return weapon;

            return defaultWeapon;
        }

        public void SaveWeaponPreference(CCSPlayerController p, string weapon, int _type)
        {
            var player = p;
            if (player == null || !player.IsValid) return;

            var sid = player.SteamID;

            if (sid == 0) return;

            if (!_playerPreferences.ContainsKey(sid))
            {
                _playerPreferences[sid] = new PlayerPreferences();
            }

            string newWeapon = weapon.Split("weapon_")[1].ToString()!.ToUpper();

            if (_type == 0)
            {
                _playerPreferences[sid].RifleWeapon = weapon;
                PluginExtensions.SendChatMessage(_plugin, player, "RifleChanged", newWeapon);
            }
            else if (_type == 1)
            {
                _playerPreferences[sid].PistolWeapon = weapon;
                PluginExtensions.SendChatMessage(_plugin, player, "PistolChanged", newWeapon);
            }
            PluginExtensions.PlaySoundToClient(player, _plugin.Config.soundsPath.saveWeaponSound);

        }

        public void SaveRoundPreference(CCSPlayerController p, int roundType)
        {
            /* roundTypes
            0 - Rifles, 1 - Pistols, 2 - AWP, 3 - Scout
            */
            var player = p;
            if (player == null || !player.IsValid) return;
            var sid = player.SteamID;

            if (sid == 0) return;

            var name = _plugin.Config.DuelRounds[roundType].Name;
            if (name == null)
            {
                name = "";
            }

            if (_playerPreferences[sid].RoundPreferences.Contains(roundType))
            {
                _playerPreferences[sid].RoundPreferences.Remove(roundType);
                PluginExtensions.SendChatMessage(_plugin, player, $"RoundPreferenceDisabled", [_plugin.Localizer["Disabled"], name.ToUpper()]);
                PluginExtensions.PlaySoundToClient(player, _plugin.Config.soundsPath.disabledSound);
            }
            else
            {
                _playerPreferences[sid].RoundPreferences.Add(roundType);
                PluginExtensions.SendChatMessage(_plugin, player, $"RoundPreferenceEnabled", [_plugin.Localizer["Enabled"], name.ToUpper()]);
                PluginExtensions.PlaySoundToClient(player, _plugin.Config.soundsPath.enabledSound);
            }
        }

        public bool IfPlayerHaveRoundEnabled(CCSPlayerController p, int roundType)
        {
            var player = p;
            if (player == null || !player.IsValid) return false;
            var sid = player.SteamID;

            if (sid == 0) return false;
            if (_playerPreferences[sid].RoundPreferences.Contains(roundType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task SaveAllPlayersAsync()
        {
            try
            {
                foreach (var player in _playerPreferences)
                {
                    await _plugin.DatabaseManager!.SavePlayerInformation(player.Key, player.Value);
                }
            }
            catch (Exception error)
            {
                _plugin.PluginExtensions!.DebugLogger($"Problem with saving players information: \n{error}");
            }

        }

        public PlayerPreferences GetOrCreatePlayerPreferences(ulong steamId)
        {
            if (!_playerPreferences.ContainsKey(steamId))
            {
                _playerPreferences[steamId] = new PlayerPreferences();
            }

            return _playerPreferences[steamId];
        }


        public int GetRandomSharedRound(CCSPlayerController player1, CCSPlayerController player2)
        {
            if (player1 == null || player2 == null)
                return 0;

            var p1pref = GetOrCreatePlayerPreferences(player1.SteamID);
            var p2pref = GetOrCreatePlayerPreferences(player2.SteamID);
            if (p1pref.RoundPreferences == null || p2pref.RoundPreferences == null)
                return 0;

            var sharedRounds = p1pref.RoundPreferences
                .Intersect(p2pref.RoundPreferences)
                .ToList();

            if (sharedRounds.Count == 0)
                return 0;

            var random = new Random();
            int randomIndex = random.Next(sharedRounds.Count);

            return sharedRounds[randomIndex];
        }

        public int GetRandomRound()
        {
            var sharedRounds = _plugin.Config.DuelRounds.ToList();

            if (sharedRounds.Count == 0)
                return 0;

            var random = new Random();
            int randomIndex = random.Next(sharedRounds.Count);

            return randomIndex;
        }
    }
}