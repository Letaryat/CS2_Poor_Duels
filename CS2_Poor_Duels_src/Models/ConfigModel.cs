using System.Text.Json.Serialization;

namespace CS2_Poor_Duels.Models
{
    public class DatabaseSetup
    {
        [JsonPropertyName("DBHost")]
        public string DBHost { get; set; } = "localhost";

        [JsonPropertyName("DBPort")]
        public uint DBPort { get; set; } = 3306;

        [JsonPropertyName("DBUsername")]
        public string DBUsername { get; set; } = "root";

        [JsonPropertyName("DBName")]
        public string DBName { get; set; } = "db_";

        [JsonPropertyName("DBPassword")]
        public string DBPassword { get; set; } = "123";
    }
    public class SoundsPath
    {
        [JsonPropertyName("SaveWeaponSound")]
        public string saveWeaponSound { get; set; } = "ui/csgo_ui_contract_type4";
        [JsonPropertyName("EnabledSound")]
        public string enabledSound { get; set; } = "ui/panorama/ping_alert_01";
        [JsonPropertyName("DisabledSound")]
        public string disabledSound { get; set; } = "ui/panorama/ping_alert_negative";

    }
    public class CMDAlias
    {
        [JsonPropertyName("AliasAFK")]
        public string[] aliasAFK { get; set; } = ["css_afk"];
        [JsonPropertyName("AliasRounds")]
        public string[] aliasRounds { get; set; } = ["css_rounds"];
        [JsonPropertyName("AliasGuns")]
        public string[] aliasGuns { get; set; } = ["css_guns"];
        [JsonPropertyName("AliasRifles")]
        public string[] aliasRifles { get; set; } = ["css_rifles"];
        [JsonPropertyName("AliasPistols")]
        public string[] aliasPistols { get; set; } = ["css_pistols"];
    }
}