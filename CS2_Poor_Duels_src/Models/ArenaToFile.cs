using System.Text.Json.Serialization;

namespace CS2_Poor_Duels.Models
{
    public class ArenaToFile
    {
        public int Id { get; set; }

        [JsonPropertyName("Spawn1")]
        public CustomSpawn Spawn1 { get; set; } = new();

        [JsonPropertyName("Spawn2")]
        public CustomSpawn Spawn2 { get; set; } = new();
    }
}
