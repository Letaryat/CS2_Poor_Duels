using System.Text.Json.Serialization;

namespace CS2_Poor_Duels.Models
{
    /// <summary>
    /// "Hud" in CS2_Poor_Duels.json. Every key is optional: a config file without them gets these defaults, and
    /// the plugin never writes them into the file. Unknown values fall back to the default.
    /// </summary>
    public class DuelsHudConfig
    {
        /// <summary>The info card and the toasts. False brings back every old center text and chat line unchanged.</summary>
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>"top": under the score (the Duels servers run without a radar). "low": under the crosshair,
        /// where the old center text sat.</summary>
        [JsonPropertyName("CardPosition")]
        public string CardPosition { get; set; } = "top";

        /// <summary>The "now fighting" card. "banner": weapon, opponent and score in one line. "faceoff": both
        /// names in team colors and a head-to-head bar, 452px wide.</summary>
        [JsonPropertyName("CardDesign")]
        public string CardDesign { get; set; } = "banner";

        /// <summary>"Duel won" / "Duel lost" with the new score on the card for 1.5s after a kill. The kill feed
        /// already shows it to both players, so it is off by default.</summary>
        [JsonPropertyName("ResultLine")]
        public bool ResultLine { get; set; } = false;

        /// <summary>"duels": the Duels toast, with weapon art and round chips. "service": ToastService's toasts
        /// (fixed icon set, no chips); the Duels toast is used whenever ToastService is not installed.</summary>
        [JsonPropertyName("ToastStyle")]
        public string ToastStyle { get; set; } = "duels";

        /// <summary>"left" or "right": the middle of which screen edge the toasts stack on.</summary>
        [JsonPropertyName("ToastSide")]
        public string ToastSide { get; set; } = "left";
    }
}
