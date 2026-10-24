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

        public static Attributes GetBaseStats(SquadronClass job, int level)
        {
            // Derived from screenshots: 
            // Level 49 total = 192, Level 38 total = 170.
            // Growth = 2 points per level. Base at level 1 = 96.
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
                        
                        list.Add(new SquadronMission 
                        { 
                            Id = (uint)row.RowId, 
                            Name = Loc.T("MissionNamePrefix", row.RequiredLevel, name),
                            RawName = name,
                            LevelRequirement = (int)row.RequiredLevel, 
                            RequiredAttributes = new Attributes(
                                row.ExpeditionParams[5].RequiredPhysical, 
                                row.ExpeditionParams[5].RequiredMental, 
                                row.ExpeditionParams[5].RequiredTactical)
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
    }
}
