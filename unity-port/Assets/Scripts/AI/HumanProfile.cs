// Lügen — HumanProfile.cs
// Translated from runState.humanProfile + the predictor helpers
// (_recordHumanPlay / _recordHumanChallengeOpp / predictHumanChallengeRate /
// posteriorBluffRateAtCount / etc.) in beta.js.
//
// The profile is what Lugen and the 'prophet' personality READ to predict
// the human's next move. It's a heuristic tracker, not a Bayesian model —
// runs are too short for a true posterior to converge.

using System.Collections.Generic;

namespace Lugen.AI
{
    [System.Serializable]
    public class CountBucket { public int plays; public int bluffs; }

    [System.Serializable]
    public class HumanProfile
    {
        // Plays bucketed by play size (1-4).
        public Dictionary<int, CountBucket> playsByCount = new Dictionary<int, CountBucket>();

        // Plays bucketed by Jack count in hand at play time.
        public Dictionary<int, CountBucket> playsByJacks = new Dictionary<int, CountBucket>();

        // Challenge stats.
        public int challengeOps;
        public int challengesFired;

        // Sliding window of last 20 wasBluff flags.
        public Queue<bool> recentBluffs = new Queue<bool>();

        // Rolling sum of claim counts (for predicting size of next play).
        public int sumClaimCount;

        // "I HAD to dump everything" plays — high-bluff-rate.
        public int emptyHandPlays;
        public int emptyHandPlaysBluff;

        public HumanProfile()
        {
            for (int c = 1; c <= 4; c++) playsByCount[c] = new CountBucket();
        }

        public void RecordPlay(int count, bool wasBluff, int jackCountAtPlay, bool becameEmptyHand)
        {
            int c = System.Math.Max(1, System.Math.Min(4, count));
            if (!playsByCount.ContainsKey(c)) playsByCount[c] = new CountBucket();
            playsByCount[c].plays++;
            if (wasBluff) playsByCount[c].bluffs++;

            int jk = System.Math.Max(0, System.Math.Min(4, jackCountAtPlay));
            if (!playsByJacks.ContainsKey(jk)) playsByJacks[jk] = new CountBucket();
            playsByJacks[jk].plays++;
            if (wasBluff) playsByJacks[jk].bluffs++;

            sumClaimCount += c;
            recentBluffs.Enqueue(wasBluff);
            while (recentBluffs.Count > 20) recentBluffs.Dequeue();
            if (becameEmptyHand)
            {
                emptyHandPlays++;
                if (wasBluff) emptyHandPlaysBluff++;
            }
        }

        public void RecordChallengeOpp(bool fired)
        {
            challengeOps++;
            if (fired) challengesFired++;
        }

        // Predicted "if a LIAR opportunity comes up, will the human fire?"
        // Bayesian-ish blend: observed rate * (n / (n + k)) + prior * (k / (n + k))
        // with prior=0.30 and k=8 pseudo-ops.
        public double PredictChallengeRate()
        {
            const double prior = 0.30, k = 8.0;
            double observed = challengeOps == 0 ? prior : (double)challengesFired / challengeOps;
            return (observed * challengeOps + prior * k) / (challengeOps + k);
        }

        // Posterior bluff rate at a specific play size — same Bayesian-ish blend.
        public double PosteriorBluffRateAt(int count)
        {
            const double prior = 0.30, k = 6.0;
            int c = System.Math.Max(1, System.Math.Min(4, count));
            if (!playsByCount.TryGetValue(c, out var bucket) || bucket.plays == 0) return prior;
            double observed = (double)bucket.bluffs / bucket.plays;
            return (observed * bucket.plays + prior * k) / (bucket.plays + k);
        }
    }
}
