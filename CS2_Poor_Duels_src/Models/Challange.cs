using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Models;

public class DuelChallenge
{
    public CCSPlayerController Challenger { get; set; } = null!;
    public CCSPlayerController Target { get; set; } = null!;
    public Arena? Arena { get; set; }
    public int RoundType { get; set; }
    public int MaxRounds { get; set; }

    public int ChallengerWins { get; set; }
    public int TargetWins { get; set; }

    public bool Accepted { get; set; }
}