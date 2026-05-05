namespace CS2_Poor_Duels.Models
{
    public class WeaponModels
    {
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