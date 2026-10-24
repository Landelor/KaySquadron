using System;
using System.Collections.Generic;
using System.Linq;

namespace KaySquadron
{
    public class OptimizationResult
    {
        public List<SquadronMember> Team { get; set; } = new();
        public Attributes BestTraining { get; set; }
        public int RequirementsMet { get; set; }
        public Attributes FinalStats { get; set; }
    }

    public class Optimizer
    {
        private const int TrainingStep = 20;

        // Cached per total-points pool (200/300/400) so repeated Optimize() calls at the
        // same Squadron Rank don't regenerate the same distribution list every time.
        private static readonly Dictionary<int, List<Attributes>> _distributionCache = new();

        private static List<Attributes> GetTrainingDistributions(int totalPoints)
        {
            if (_distributionCache.TryGetValue(totalPoints, out var cached))
            {
                return cached;
            }

            var distributions = new List<Attributes>();
            for (int p = 0; p <= totalPoints; p += TrainingStep)
            {
                for (int m = 0; m <= totalPoints - p; m += TrainingStep)
                {
                    int t = totalPoints - p - m;
                    distributions.Add(new Attributes(p, m, t));
                }
            }

            _distributionCache[totalPoints] = distributions;
            return distributions;
        }

        /// <param name="maxTrainingPoints">
        /// Total board-training points available at the squadron's current Rank
        /// (200 at Rank 1, 300 at Rank 2, 400 at Rank 3). See SquadronData.GetMaxTrainingPoints.
        /// </param>
        public List<OptimizationResult> Optimize(List<SquadronMember> allMembers, SquadronMission mission, int maxTrainingPoints)
        {
            var results = new List<OptimizationResult>();
            var trainingDistributions = GetTrainingDistributions(maxTrainingPoints);

            // Combinations of 4 members from 8
            var combinations = GetCombinations(allMembers, 4);

            foreach (var team in combinations)
            {
                var baseStats = new Attributes(0, 0, 0);
                foreach (var member in team)
                {
                    baseStats += member.BaseAttributes;
                }

                OptimizationResult? bestForThisTeam = null;

                foreach (var training in trainingDistributions)
                {
                    var finalStats = baseStats + training;
                    int met = finalStats.RequirementsMetCount(mission.RequiredAttributes);

                    if (bestForThisTeam == null || met > bestForThisTeam.RequirementsMet)
                    {
                        bestForThisTeam = new OptimizationResult
                        {
                            Team = team.ToList(),
                            BestTraining = training,
                            RequirementsMet = met,
                            FinalStats = finalStats
                        };
                    }

                    if (met == 3) break; // Perfect match found for this team
                }

                if (bestForThisTeam != null)
                {
                    results.Add(bestForThisTeam);
                }
            }

            // Return top results ordered by requirements met, then maybe total surplus
            return results.OrderByDescending(r => r.RequirementsMet).Take(10).ToList();
        }

        private static IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetCombinations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
