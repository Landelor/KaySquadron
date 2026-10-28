using System;
using System.Collections.Generic;

namespace KaySquadron
{
    public static class SquadronData
    {
        public enum SquadronClass
        {
            Gladiator = 1,
            Pugilist = 2,
            Marauder = 3,
            Lancer = 4,
            Archer = 5,
            Conjurer = 6,
            Thaumaturge = 7,
            Arcanist = 26,
            Rogue = 29
        }

        /// <summary>
        /// Each squad member's actual Physical/Mental/Tactical contribution, read live from the
        /// game's own per-class-per-level growth table (GcArmyMemberGrow.MemberParams[level]),
        /// matched to the member's class via its ClassJob link. This replaces an earlier
        /// screenshot-derived percentage estimate that didn't match real recruits closely enough
        /// to reliably find training plans. Falls back to that old estimate only if the sheet
        /// can't be read for some reason (should not normally happen).
        ///
        /// Index is the level itself, NOT level-1: index 0 is an unused placeholder (always
        /// zero), not "level 1". Verified live against 4 real level-16 members across 4
        /// different classes (Conjurer/Archer/Marauder/Pugilist) - all 4 matched index 16
        /// exactly, not index 15.
        /// </summary>
        public static Attributes GetBaseStats(SquadronClass job, int level)
        {
            try
            {
                var sheet = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyMemberGrow>();
                if (sheet != null)
                {
                    foreach (var row in sheet)
                    {
                        if (row.ClassJob.RowId != (uint)job) continue;

                        // MemberParams has 60 slots (indices 0-59) and index == level, so a
                        // level-60 (max) member technically has no exact slot and falls back to
                        // the level-59 row - a known, minor edge case affecting only the single
                        // top level, where growth has already nearly flattened out anyway.
                        int index = Math.Clamp(level, 0, 59);
                        var p = row.MemberParams[index];
                        return new Attributes(p.Physical, p.Mental, p.Tactical);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Error($"Failed to read live GcArmyMemberGrow stats, falling back to estimate: {ex}");
            }

            // Fallback estimate (screenshot-derived, not exact) - only reached if the live sheet
            // lookup above fails outright.
            int total = 96 + (level - 1) * 2;
            
            return job switch
            {
                SquadronClass.Gladiator or SquadronClass.Marauder => new Attributes((int)(total * 0.60), (int)(total * 0.15), (int)(total * 0.25)),
                SquadronClass.Conjurer => new Attributes((int)(total * 0.15), (int)(total * 0.66), (int)(total * 0.19)),
                SquadronClass.Archer or SquadronClass.Rogue or SquadronClass.Lancer => new Attributes((int)(total * 0.23), (int)(total * 0.15), (int)(total * 0.62)),
                SquadronClass.Thaumaturge or SquadronClass.Arcanist => new Attributes((int)(total * 0.15), (int)(total * 0.57), (int)(total * 0.28)),
                SquadronClass.Pugilist => new Attributes((int)(total * 0.50), (int)(total * 0.20), (int)(total * 0.30)),
                _ => new Attributes(total / 3, total / 3, total / 3)
            };
        }

        /// <summary>
        /// Total board-training points available at a given Squadron Rank.
        /// Rank 1 (Trainee, starting rank) = 200, Rank 2 = 300, Rank 3 (max) = 400.
        /// </summary>
        public static int GetMaxTrainingPoints(int squadronRank) => squadronRank switch
        {
            1 => 200,
            2 => 300,
            _ => 400,
        };

        /// <summary>
        /// The Regimen Board's 6 stat-affecting training courses (GcArmyTraining sheet rows
        /// 1-6), read live so names match the game's display language. Row 0 is empty and
        /// row 7 ("Comprehensive Training Course") grants 3000 XP but no stat change, so both
        /// are excluded here since they're irrelevant to stat-optimization math.
        /// </summary>
        public static List<TrainingCourse> GetTrainingCourses()
        {
            var list = new List<TrainingCourse>();

            try
            {
                var sheet = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyTraining>();
                if (sheet != null)
                {
                    foreach (var row in sheet)
                    {
                        if (row.RowId == 0) continue; // empty placeholder row
                        if (row.PhysicalBonus == 0 && row.MentalBonus == 0 && row.TacticalBonus == 0) continue; // Comprehensive Training Course

                        string name = row.Name.ToString();
                        if (string.IsNullOrEmpty(name)) continue;

                        list.Add(new TrainingCourse(name, new Attributes(row.PhysicalBonus, row.MentalBonus, row.TacticalBonus)));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Error(Loc.T("ErrLuminaLoad", ex.Message));
            }

            // Fallback if the sheet fails to load - values confirmed against GcArmyTraining.
            if (list.Count == 0)
            {
                list.Add(new TrainingCourse("Basic Training: Physical", new Attributes(40, 0, 0)));
                list.Add(new TrainingCourse("Basic Training: Mental", new Attributes(0, 40, 0)));
                list.Add(new TrainingCourse("Basic Training: Tactical", new Attributes(0, 0, 40)));
                list.Add(new TrainingCourse("Advanced Training: Physical & Mental", new Attributes(20, 20, 0)));
                list.Add(new TrainingCourse("Advanced Training: Physical & Tactical", new Attributes(20, 0, 20)));
                list.Add(new TrainingCourse("Advanced Training: Mental & Tactical", new Attributes(0, 20, 20)));
            }

            return list;
        }

        public static List<SquadronMission> GetMissions()
        {
            var list = new List<SquadronMission>();
            const int expeditionParamsIndex = ExpeditionRequirementSlot;

            try
            {
                var sheet = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyExpedition>();
                var typeSheet = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyExpeditionType>();
                
                if (sheet != null)
                {
                    foreach (var row in sheet)
                    {
                        if (row.RequiredLevel == 0) continue; // Skip empty rows

                        string name = "Mission " + row.RowId;
                        
                        // Try to get the real mission name
                        try 
                        {
                            string realName = row.Name.ToString();
                            if (!string.IsNullOrEmpty(realName))
                            {
                                name = realName;
                            }
                        } catch { }
                        
                        var tier = row.ExpeditionParams[expeditionParamsIndex];
                        list.Add(new SquadronMission 
                        { 
                            Id = (uint)row.RowId, 
                            Name = Loc.T("MissionNamePrefix", row.RequiredLevel, name),
                            RawName = name,
                            LevelRequirement = (int)row.RequiredLevel, 
                            RequiredAttributes = new Attributes(
                                tier.RequiredPhysical, 
                                tier.RequiredMental, 
                                tier.RequiredTactical)
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Error(Loc.T("ErrLuminaLoad", ex.Message));
            }

            // Fallback if sheet fails or is empty
            if (list.Count == 0)
            {
                list.Add(new SquadronMission { Id = 1, Name = Loc.T("MissionNameFallback"), LevelRequirement = 1, RequiredAttributes = new Attributes(145, 140, 140) });
            }

            return list;
        }

        /// <summary>
        /// GcArmyExpedition.ExpeditionParams has 6 slots per mission that all sum to the same
        /// total difficulty but weight Physical/Mental/Tactical differently. The native UI
        /// shows only ONE of them. This was originally assumed to be Grand-Company-dependent
        /// (slots looked like they formed 3 GC-themed pairs), but that hypothesis was disproven
        /// live: Maelstrom and Immortal Flames players both confirmed the exact same values
        /// (slot index 2) for the same mission (Lvl 15 Pest Eradication, P320/M145/T225).
        /// With 2 of the 3 GCs confirmed identical, slot 2 is treated as a GC-independent
        /// constant - whatever the other 5 slots represent, they are not shown to players here.
        /// Not yet confirmed for Order of the Twin Adder specifically, but given 2/2 agreement
        /// so far there's no reason to expect it to differ.
        /// </summary>
        private const int ExpeditionRequirementSlot = 2;
    }
}
