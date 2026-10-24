using System;

namespace KaySquadron
{
    public struct Attributes
    {
        public int Physical;
        public int Mental;
        public int Tactical;

        public Attributes(int p, int m, int t)
        {
            Physical = p;
            Mental = m;
            Tactical = t;
        }

        public static Attributes operator +(Attributes a, Attributes b) 
            => new Attributes(a.Physical + b.Physical, a.Mental + b.Mental, a.Tactical + b.Tactical);

        public static Attributes operator -(Attributes a, Attributes b) 
            => new Attributes(a.Physical - b.Physical, a.Mental - b.Mental, a.Tactical - b.Tactical);

        public int Total => Physical + Mental + Tactical;

        public bool Meets(Attributes requirement) 
            => Physical >= requirement.Physical && Mental >= requirement.Mental && Tactical >= requirement.Tactical;

        public int RequirementsMetCount(Attributes requirement)
        {
            int count = 0;
            if (Physical >= requirement.Physical) count++;
            if (Mental >= requirement.Mental) count++;
            if (Tactical >= requirement.Tactical) count++;
            return count;
        }
    }

    public class SquadronMember
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Attributes BaseAttributes { get; set; }
        
        // Bonus attributes from Chemistry can be added here later
    }

    public class SquadronMission
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>The unprefixed mission name as shown in the native game UI (used to match
        /// against AgentGcArmyExpedition's live selection; Name adds a "[Lvl X]" prefix for
        /// display in our own dropdown).</summary>
        public string RawName { get; set; } = string.Empty;
        public int LevelRequirement { get; set; }
        public Attributes RequiredAttributes { get; set; }
        public bool IsFlagged { get; set; }
        public bool IsPriority { get; set; }
    }

    /// <summary>
    /// A single Regimen Board training course (GcArmyTraining sheet row). Each course adds a
    /// FIXED bonus when run (+40 to one stat, or +20/+20 to two) - training is not a freely
    /// distributable pool, it's picking one of these per session (up to 3 sessions/day).
    /// </summary>
    public readonly struct TrainingCourse
    {
        public string Name { get; }
        public Attributes Bonus { get; }

        public TrainingCourse(string name, Attributes bonus)
        {
            Name = name;
            Bonus = bonus;
        }
    }

    /// <summary>How many times a given training course should be run to reach a target.</summary>
    public class CourseRecommendation
    {
        public TrainingCourse Course { get; set; }
        public int Count { get; set; }
    }
}
