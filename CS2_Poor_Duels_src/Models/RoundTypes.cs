namespace CS2_Poor_Duels.Models
{
    public class RoundType
    {
        public string? Name { get; set; } = "";
        public string? primaryWeapon { get; set; } = "";
        public string? secondaryWeapon { get; set; } = "";
        public bool forcePrimary { get; set; }
        public bool forceSecondary { get; set; }
        public bool forceArmor { get; set; }
        public bool forceHelmet { get; set; }
    }
}