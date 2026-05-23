namespace CS2_Poor_Duels.Models
{
    public class WeaponModels
    {
        // Maps CEconItemView.ItemDefinitionIndex -> weapon class name.
        // Used INSTEAD of VirtualFunctions.GetCSWeaponDataFromKeyFunc which crashes natively for some indexes.
        public static Dictionary<ushort, string> DefIndexToClassName = new()
        {
            { 1,  "weapon_deagle" },
            { 2,  "weapon_elite" },
            { 3,  "weapon_fiveseven" },
            { 4,  "weapon_glock" },
            { 7,  "weapon_ak47" },
            { 8,  "weapon_aug" },
            { 9,  "weapon_awp" },
            { 10, "weapon_famas" },
            { 11, "weapon_g3sg1" },
            { 13, "weapon_galilar" },
            { 14, "weapon_m249" },
            { 16, "weapon_m4a1" },
            { 17, "weapon_mac10" },
            { 19, "weapon_p90" },
            { 23, "weapon_mp5sd" },
            { 24, "weapon_ump45" },
            { 25, "weapon_xm1014" },
            { 26, "weapon_bizon" },
            { 27, "weapon_mag7" },
            { 28, "weapon_negev" },
            { 29, "weapon_sawedoff" },
            { 30, "weapon_tec9" },
            { 31, "weapon_taser" },
            { 32, "weapon_hkp2000" },
            { 33, "weapon_mp7" },
            { 34, "weapon_mp9" },
            { 35, "weapon_nova" },
            { 36, "weapon_p250" },
            { 38, "weapon_scar20" },
            { 39, "weapon_sg556" },
            { 40, "weapon_ssg08" },
            { 43, "weapon_flashbang" },
            { 44, "weapon_hegrenade" },
            { 45, "weapon_smokegrenade" },
            { 46, "weapon_molotov" },
            { 47, "weapon_decoy" },
            { 48, "weapon_incgrenade" },
            { 50, "item_assaultsuit" },
            { 51, "item_kevlar" },
            { 60, "weapon_m4a1_silencer" },
            { 61, "weapon_usp_silencer" },
            { 63, "weapon_cz75a" },
            { 64, "weapon_revolver" },
        };

        public static List<string> illegalWeapons =
        [
            "weapon_xm1014",
            "weapon_nova",
            "weapon_mag7",
            "weapon_sawedoff",
            "weapon_mac10",
            "weapon_mp9",
            "weapon_mp7",
            "weapon_p90",
            "weapon_mp5sd",
            "weapon_bizon",
            "weapon_ump45",
            "weapon_negev",
            "weapon_m249",
            "weapon_scar20",
            "weapon_g3sg1",
            "weapon_flashbang",
            "weapon_hegrenade",
            "weapon_smokegrenade",
            "weapon_decoy",
            "weapon_molotov",
            "weapon_incgrenade",
            "weapon_taser",
            "item_kevlar",
            "item_assaultsuit"
        ];
        public static List<string> rifleItems =
        [
            "weapon_ak47",
            "weapon_m4a1_silencer",
            "weapon_m4a1",
            "weapon_galilar",
            "weapon_famas",
            "weapon_sg556",
            "weapon_aug"
        ];

        public static List<string> sniperItems =
        [
            "weapon_awp",
            "weapon_ssg08"
            // Pod areny brakuje tu dwoch snajperek.
            /*
            "weapon_scar20",
            "weapon_g3sg1",
            */
        ];
        /*
        public static List<CsItem> shotgunItems =
        [
            CsItem.XM1014,
            CsItem.Nova,
            CsItem.MAG7,
            CsItem.SawedOff,
        ];
*/
        public static List<string> smgItems =
        [
            "weapon_mac10",
            "weapon_mp9",
            "weapon_mp7",
            "weapon_p90",
            "weapon_mp5sd",
            "weapon_bizon",
            "weapon_ump45"
        ];

        public static List<string> lmgItems =
        [
            "weapon_negev",
            "weapon_m249"
        ];

        public static List<string> pistolItems =
        [
            "weapon_deagle",
            "weapon_glock",
            "weapon_usp_silencer",
            "weapon_hkp2000",
            "weapon_elite",
            "weapon_tec9",
            "weapon_p250",
            "weapon_cz75a",
            "weapon_fiveseven",
            "weapon_revolver",
        ];
    }

}