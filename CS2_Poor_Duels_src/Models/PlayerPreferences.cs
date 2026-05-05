namespace CS2_Poor_Duels.Models
{

    public class PlayerPreferences
    {
        public string? RifleWeapon { get; set; }
        public string? PistolWeapon { get; set; }
        public List<int> RoundPreferences { get; set; } = new();
    }
    public class PlayerPreferencesDB
    {
        public string? RifleWeapon { get; set; }
        public string? PistolWeapon { get; set; }
        public string? RoundPreferences { get; set; }
    }
}