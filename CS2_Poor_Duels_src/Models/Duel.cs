
using CounterStrikeSharp.API.Core;

namespace CS2_Poor_Duels.Models
{
    public class PlayerDuelData
    {
        public CCSPlayerController? player1 { get; set; }
        public CCSPlayerController? player2 { get; set; }
        public Arena? Arena { get; set; }
    }
}