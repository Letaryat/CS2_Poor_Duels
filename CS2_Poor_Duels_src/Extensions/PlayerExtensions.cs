using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CS2_Poor_Duels.Extensions
{
    public partial class PluginExtensions
    {
        public void SetPlayerClanTag(CCSPlayerController player, string clanTag)
        {
            player.Clan = clanTag;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
            new EventNextlevelChanged(false).FireEventToClient(player);
        }

    }
}