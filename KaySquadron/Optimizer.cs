using System;
using System.Collections.Generic;
using System.Linq;

namespace KaySquadron
{
    public class OptimizationResult
    {
        public List<SquadronMember> Team { get; set; } = new();

        /// <summary>The final squadron training total (banked + recommended) this result reaches.</summary>
        public Attributes BestTraining { get; set; }
        public int RequirementsMet { get; set; }
        public Attributes FinalStats { get; set; }

        /// <summary>Concrete Regimen Board courses to run, with counts, to reach BestTraining from
        /// whatever is already banked. Empty if no further training is needed.</summary>
        public List<CourseRecommendation> TrainingPlan { get; set; } = new();
        public int SessionsNeeded => TrainingPlan.Sum(c => c.Count);
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
                    for (int t = 0; t <= totalPoints - p - m; t += TrainingStep)
                    {
                        distributions.Add(new Attributes(p, m, t));
                    }
                }
            }

            _distributionCache[totalPoints] = distributions;
            return distributions;
        }

        /// <param name="maxTrainingPoints">
        /// Total board-training points available at the squadron's current Rank
        /// (200 at Rank 1, 300 at Rank 2, 400 at Rank 3). See SquadronData.GetMaxTrainingPoints.
        /// </param>
        /// <param name="currentTraining">
        /// Training bonus already banked on the squadron (read via GcArmyManager, or zero if
        /// not yet imported). Training only ever adds, so candidate targets below this are
        /// never considered, and candidates must be reachable from it in whole course sessions.
        /// </param>
        /// <param name="courses">The Regimen Board's training courses (SquadronData.GetTrainingCourses()).</param>
        public List<OptimizationResult> Optimize(List<SquadronMember> allMembers, SquadronMission mission, int maxTrainingPoints, Attributes currentTraining, List<TrainingCourse> courses)
        {
            var results = new List<OptimizationResult>();
            var byShape = BuildShapeLookup(courses);

            // Every training session adds exactly 40 points total (either 40 to one stat, or
            // 20+20 to two), so a target total is only reachable from currentTraining if it is
            // at least as large on every axis AND the total difference is a whole number of
            // sessions (a multiple of 40).
            //
            // currentTraining might exceed maxTrainingPoints if the player's selected Squadron
            // Rank doesn't match their actual banked total (e.g. still on the Rank 1 default
            // after actually ranking up) - in that case the candidate grid below would never
            // contain anything >= currentTraining and we'd silently (and wrongly) fall back to
            // "no further training needed" for every mission. Use whichever total is larger as
            // the search ceiling so currentTraining is always a valid candidate; we still never
            // recommend training BEYOND the user's selected rank unless they're already past it.
            int currentTotal = currentTraining.Total;
            int effectiveCap = Math.Max(maxTrainingPoints, currentTotal);
            var validTargets = GetTrainingDistributions(effectiveCap)
                .Where(t => t.Physical >= currentTraining.Physical
                         && t.Mental >= currentTraining.Mental
                         && t.Tactical >= currentTraining.Tactical
                         && (t.Total - currentTotal) % 40 == 0)
                .ToList();

            // Degenerate fallback (should not normally happen): if nothing lines up to a clean
            // multiple of 40 sessions from the current total, just allow the banked total itself
            // so the optimizer still returns a usable (if untrained-further) result.
            if (validTargets.Count == 0)
            {
                validTargets.Add(currentTraining);
            }

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

                foreach (var training in validTargets)
                {
                    var finalStats = baseStats + training;
                    int met = finalStats.RequirementsMetCount(mission.RequiredAttributes);

                    if (bestForThisTeam == null
                        || met > bestForThisTeam.RequirementsMet
                        || (met == bestForThisTeam.RequirementsMet && training.Total < bestForThisTeam.BestTraining.Total))
                    {
                        bestForThisTeam = new OptimizationResult
                        {
                            Team = team.ToList(),
                            BestTraining = training,
                            RequirementsMet = met,
                            FinalStats = finalStats
                        };
                    }
                }

                if (bestForThisTeam != null)
                {
                    bestForThisTeam.TrainingPlan = DecomposeTrainingDelta(bestForThisTeam.BestTraining - currentTraining, byShape);
                    results.Add(bestForThisTeam);
                }
            }

            // Return top results ordered by requirements met, then fewest training sessions needed
            return results
                .OrderByDescending(r => r.RequirementsMet)
                .ThenBy(r => r.SessionsNeeded)
                .Take(10)
                .ToList();
        }

        private static Dictionary<(bool p, bool m, bool t), TrainingCourse> BuildShapeLookup(List<TrainingCourse> courses)
        {
            var byShape = new Dictionary<(bool, bool, bool), TrainingCourse>();
            foreach (var course in courses)
            {
                bool p = course.Bonus.Physical > 0;
                bool m = course.Bonus.Mental > 0;
                bool t = course.Bonus.Tactical > 0;
                int axisCount = (p ? 1 : 0) + (m ? 1 : 0) + (t ? 1 : 0);
                if (axisCount == 1 || axisCount == 2)
                {
                    byShape[(p, m, t)] = course;
                }
            }
            return byShape;
        }

        /// <summary>
        /// Turns a non-negative training delta into the minimum number of Regimen Board
        /// sessions that reach it: each session contributes exactly 2 "units" of 20 points,
        /// either both to the same stat (Basic course) or one each to two different stats
        /// (Advanced course). Greedily pairs the two neediest stats each step.
        /// </summary>
        private static List<CourseRecommendation> DecomposeTrainingDelta(Attributes delta, Dictionary<(bool, bool, bool), TrainingCourse> byShape)
        {
            int[] units =
            {
                Math.Max(0, delta.Physical) / TrainingStep,
                Math.Max(0, delta.Mental) / TrainingStep,
                Math.Max(0, delta.Tactical) / TrainingStep,
            };

            var plan = new Dictionary<string, CourseRecommendation>();

            void RunSession(bool p, bool m, bool t)
            {
                if (!byShape.TryGetValue((p, m, t), out var course)) return;

                if (!plan.TryGetValue(course.Name, out var rec))
                {
                    rec = new CourseRecommendation { Course = course, Count = 0 };
                    plan[course.Name] = rec;
                }
                rec.Count++;
            }

            while (units[0] > 0 || units[1] > 0 || units[2] > 0)
            {
                int[] order = { 0, 1, 2 };
                Array.Sort(order, (x, y) => units[y].CompareTo(units[x]));
                int top = order[0];
                int second = order[1];

                if (units[top] == 0) break; // nothing left to allocate

                bool p = false, m = false, t = false;
                void SetAxis(int axis) { if (axis == 0) p = true; else if (axis == 1) m = true; else t = true; }

                if (units[second] > 0)
                {
                    // Pair the two neediest stats with the matching Advanced course.
                    SetAxis(top);
                    SetAxis(second);
                    units[top]--;
                    units[second]--;
                }
                else
                {
                    // Only one stat left needing points - use its Basic course (2 units/session).
                    SetAxis(top);
                    units[top] -= Math.Min(2, units[top]);
                }

                RunSession(p, m, t);
            }

            return plan.Values.OrderByDescending(c => c.Count).ToList();
        }

        private static IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetCombinations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
