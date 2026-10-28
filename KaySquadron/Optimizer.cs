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

        private readonly record struct VisitInfo(int Sessions, Attributes? Prev, TrainingCourse? Via);

        /// <param name="maxTrainingPoints">
        /// Total board-training points available at the squadron's current Rank
        /// (200 at Rank 1, 300 at Rank 2, 400 at Rank 3). See SquadronData.GetMaxTrainingPoints.
        /// </param>
        /// <param name="currentTraining">
        /// Training bonus already banked on the squadron (read via GcArmyManager, or zero if
        /// not yet imported).
        /// </param>
        /// <param name="courses">The Regimen Board's training courses (SquadronData.GetTrainingCourses()).</param>
        public List<OptimizationResult> Optimize(List<SquadronMember> allMembers, SquadronMission mission, int maxTrainingPoints, Attributes currentTraining, List<TrainingCourse> courses)
        {
            var results = new List<OptimizationResult>();
            var statCourses = courses
                .Where(c => AxisCount(c.Bonus) == 1 || AxisCount(c.Bonus) == 2)
                .ToList();

            // Guard against a stale/mismatched Squadron Rank selection: never let the search
            // ceiling fall below what's already banked (see the 1.4.1.0 fix).
            int cap = Math.Max(maxTrainingPoints, currentTraining.Total);

            // Explore every training state reachable from the banked total, in the minimum
            // number of sessions, via BFS over the 6 stat-affecting courses. This is the same
            // for every team (it only depends on currentTraining/cap/courses), so it's computed
            // once and reused, rather than per team.
            var reachable = ExploreReachableStates(currentTraining, statCourses, cap);

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
                int bestSessions = int.MaxValue;

                foreach (var pair in reachable)
                {
                    var state = pair.Key;
                    var info = pair.Value;
                    var finalStats = baseStats + state;
                    int met = finalStats.RequirementsMetCount(mission.RequiredAttributes);

                    if (bestForThisTeam == null
                        || met > bestForThisTeam.RequirementsMet
                        || (met == bestForThisTeam.RequirementsMet && info.Sessions < bestSessions))
                    {
                        bestForThisTeam = new OptimizationResult
                        {
                            Team = team.ToList(),
                            BestTraining = state,
                            RequirementsMet = met,
                            FinalStats = finalStats
                        };
                        bestSessions = info.Sessions;
                    }
                }

                if (bestForThisTeam != null)
                {
                    bestForThisTeam.TrainingPlan = ReconstructPlan(bestForThisTeam.BestTraining, reachable);
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

        private static int AxisCount(Attributes bonus)
            => (bonus.Physical > 0 ? 1 : 0) + (bonus.Mental > 0 ? 1 : 0) + (bonus.Tactical > 0 ? 1 : 0);

        /// <summary>
        /// Applies one Regimen Board session's effect, modeling the game's actual mechanic:
        /// the squadron's total bonus (P+M+T) can never exceed the rank cap. A course grants its
        /// full nominal bonus to its target stat(s) ONLY if the stat(s) it does NOT target hold
        /// enough combined value to fund the resulting excess; if they don't, the ENTIRE course
        /// is wasted - no change at all, not a partial effect, and it never dips into its own
        /// target stat(s) to make up the difference.
        ///
        /// Confirmed live: starting at 100/40/60 (cap 200), running Basic Training: Physical
        /// gives 140/20/40 (the 40-point cost split across Mental and Tactical, both of which
        /// had enough). Running Advanced Training: Physical & Tactical from THAT state produces
        /// literally no change at all - its only untouched stat, Mental, has just 20 banked but
        /// the course needs 40 to fund its +20/+20 gain, so it fails outright rather than only
        /// partially applying or encroaching on Physical/Tactical. Running the courses in the
        /// opposite order (Advanced first, then Basic) succeeds for both and reaches a higher
        /// total, since Advanced-first only ever needs to draw from Mental while it still has
        /// the full 40 to give.
        /// </summary>
        public static Attributes ApplyCourse(Attributes current, TrainingCourse course, int cap)
        {
            var afterGain = current + course.Bonus;
            int excess = Math.Max(0, afterGain.Total - cap);
            if (excess == 0) return afterGain;

            var untouchedAxes = new List<int>();
            if (course.Bonus.Physical == 0) untouchedAxes.Add(0);
            if (course.Bonus.Mental == 0) untouchedAxes.Add(1);
            if (course.Bonus.Tactical == 0) untouchedAxes.Add(2);

            int[] afterVals = { afterGain.Physical, afterGain.Mental, afterGain.Tactical };
            int availableInUntouched = untouchedAxes.Sum(a => afterVals[a]);

            // Not enough banked in the untouched stat(s) to fund this course's deduction -
            // the whole course is wasted, unchanged from `current`.
            if (availableInUntouched < excess) return current;

            return RemoveUnits(afterGain, untouchedAxes, excess / TrainingStep);
        }

        /// <summary>Removes `units` x 20 points total from the given axes, each unit taken from
        /// whichever eligible axis currently holds the most (floor-clamped at 0 per axis).</summary>
        private static Attributes RemoveUnits(Attributes stats, List<int> axes, int units)
        {
            int[] vals = { stats.Physical, stats.Mental, stats.Tactical };
            for (int u = 0; u < units; u++)
            {
                int best = -1, bestVal = 0;
                foreach (var axis in axes)
                {
                    if (vals[axis] > bestVal) { bestVal = vals[axis]; best = axis; }
                }
                if (best == -1) break; // nothing left to take from any eligible axis
                vals[best] -= TrainingStep;
            }
            return new Attributes(vals[0], vals[1], vals[2]);
        }

        private static Dictionary<Attributes, VisitInfo> ExploreReachableStates(Attributes start, List<TrainingCourse> courses, int cap)
        {
            var visited = new Dictionary<Attributes, VisitInfo> { [start] = new VisitInfo(0, null, null) };
            var queue = new Queue<Attributes>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int sessions = visited[current].Sessions;

                foreach (var course in courses)
                {
                    var next = ApplyCourse(current, course, cap);
                    if (!visited.ContainsKey(next))
                    {
                        visited[next] = new VisitInfo(sessions + 1, current, course);
                        queue.Enqueue(next);
                    }
                }
            }

            return visited;
        }

        /// <summary>
        /// Walks the BFS path from `target` back to the banked start, then reverses it so the
        /// returned list is in the EXACT order the sessions must be run. Order is not cosmetic:
        /// once at the rank cap, a course only succeeds if its untouched stat(s) hold enough to
        /// fund it (see ApplyCourse) - running the same two courses in a different order can
        /// genuinely produce a worse (or completely different) result, confirmed live. Only
        /// directly-adjacent identical courses are merged into a single "xN" entry; anything
        /// non-adjacent stays as separate, order-preserving steps.
        /// </summary>
        private static List<CourseRecommendation> ReconstructPlan(Attributes target, Dictionary<Attributes, VisitInfo> reachable)
        {
            var steps = new List<TrainingCourse>();
            var cursor = target;

            while (reachable.TryGetValue(cursor, out var info) && info.Prev.HasValue && info.Via.HasValue)
            {
                steps.Add(info.Via.Value);
                cursor = info.Prev.Value;
            }

            steps.Reverse(); // was target->start, now start->target (the order to actually run them in)

            var plan = new List<CourseRecommendation>();
            foreach (var course in steps)
            {
                if (plan.Count > 0 && plan[^1].Course.Name == course.Name)
                {
                    plan[^1].Count++;
                }
                else
                {
                    plan.Add(new CourseRecommendation { Course = course, Count = 1 });
                }
            }

            return plan;
        }

        private static IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetCombinations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
