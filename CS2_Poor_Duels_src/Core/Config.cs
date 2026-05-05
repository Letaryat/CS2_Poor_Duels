using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels
{
    public class DuelsConfig : BasePluginConfig
    {
        public DatabaseSetup DBSetup { get; set; } = new();
        public SoundsPath soundsPath { get; set; } = new();
        public CMDAlias cmdAlias { get; set; } = new();

        [JsonPropertyName("DuelRounds")]
        public List<RoundType> DuelRounds { get; set; } = new()
    {
        new RoundType
        {
            Name = "Rifle",
            primaryWeapon = "weapon_ak47",
            secondaryWeapon = "weapon_deagle",
            forcePrimary = false,
            forceSecondary = false,
            forceArmor = false,
            forceHelmet = false
        },

        new RoundType
        {
            Name = "Pistol",
            primaryWeapon = "",
            secondaryWeapon = "weapon_deagle",
            forcePrimary = true,
            forceSecondary = false,
            forceArmor = false,
            forceHelmet = false
        },

        new RoundType
        {
            Name = "AWP",
            primaryWeapon = "weapon_awp",
            secondaryWeapon = "weapon_deagle",
            forcePrimary = true,
            forceSecondary = false,
            forceArmor = false,
            forceHelmet = false
        },

        new RoundType
        {
            Name = "Scout",
            primaryWeapon = "weapon_ssg08",
            secondaryWeapon = "weapon_deagle",
            forcePrimary = true,
            forceSecondary = false,
            forceArmor = false,
            forceHelmet = false
        }
    };

        [JsonPropertyName("RegisterAdminCommands")]
        public bool RegisterAdminCommands { get; set; } = true;
        [JsonPropertyName("DebugMode")]
        public int DebugMode { get; set; } = 1;
        [JsonPropertyName("DetailedDebugMode")]
        public bool DetailedDebugMode { get; set; } = false;
    }
}