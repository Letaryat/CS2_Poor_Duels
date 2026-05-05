
/*
    Pretty much this is this plugin implemented in Duels since I was to lazy to do it myself or creating a api or something like that.
    Please give some love to the original plugin as well:
    https://github.com/qstage/CS2-MutualScoringPlayers
*/

using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;

namespace CS2_Poor_Duels
{
    public class MutualScoring
    {
        public Dictionary<CCSPlayerController, int> Kills { get; set; } = [];
        public Dictionary<CCSPlayerController, int> KillsRow { get; set; } = [];

        public void Init(CCSPlayerController target)
        {
            Kills.TryAdd(target, 0);
            KillsRow.TryAdd(target, 0);
        }

        public void IncrementScore(CCSPlayerController target)
        {
            Kills[target]++;
            KillsRow[target]++;
        }

        public void ResetRow(CCSPlayerController target) => KillsRow[target] = 0;
    }

    public class MutualScoringFeature(CS2_Poor_DuelsPlugin plugin)
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        public readonly Dictionary<CCSPlayerController, MutualScoring> mutualScoring_ = [];

        public void MutualScoringOnDeath(CCSPlayerController attacker, CCSPlayerController victim)
        {
            if (attacker == null || victim == null || victim == attacker) return;
            mutualScoring_[victim].ResetRow(attacker);
            mutualScoring_[attacker].IncrementScore(victim);
        }

    }
}