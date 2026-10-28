using Dalamud.Interface.Windowing;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImGuiCond = Dalamud.Bindings.ImGui.ImGuiCond;
using ImGuiCol = Dalamud.Bindings.ImGui.ImGuiCol;
using ImGuiStyleVar = Dalamud.Bindings.ImGui.ImGuiStyleVar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace KaySquadron
{
    public class KaySquadronWindow : Window
    {
        private List<SquadronMission> _missions;
        private List<SquadronMember> _members;
        private List<TrainingCourse> _trainingCourses;
        private SquadronMission? _selectedMission;
        private List<OptimizationResult> _results = new();
        private Optimizer _optimizer = new();
        private Attributes _currentTraining = new(0, 0, 0);
        private readonly Configuration _configuration;
        private int _lastSyncedMissionRow = -1;

        private void AddDebug(string msg)
        {
            Plugin.Log.Info(msg);
        }

        public KaySquadronWindow(Configuration configuration) : base(Loc.T("PluginName"))
        {
            _configuration = configuration;
            Size = new Vector2(600, 600);
            SizeCondition = ImGuiCond.FirstUseEver;
            
            _missions = SquadronData.GetMissions();
            _trainingCourses = SquadronData.GetTrainingCourses();

            if (_configuration.SavedMembers.Count > 0)
            {
                // Restore the roster (and banked training) from the last successful import.
                _members = _configuration.SavedMembers;
                _currentTraining = _configuration.SavedTraining;
            }
            else
            {
                _members = new List<SquadronMember>();

                // Default members for testing
                for (int i = 0; i < 8; i++)
                {
                    _members.Add(new SquadronMember 
                    { 
                        Name = Loc.T("RecruitPlaceholder", i + 1), 
                        Level = 50, 
                        ClassId = (int)SquadronData.SquadronClass.Marauder,
                        ClassName = SquadronData.SquadronClass.Marauder.ToString(),
                        BaseAttributes = SquadronData.GetBaseStats(SquadronData.SquadronClass.Marauder, 50)
                    });
                }
            }
            AddDebug(Loc.T("ErrInit"));
        }

        /// <summary>Persists the current roster and banked training so they survive restarts.</summary>
        private void PersistMembers()
        {
            _configuration.SavedMembers = _members;
            _configuration.SavedTraining = _currentTraining;
            _configuration.Save();
        }

        private void PushKayTheme()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.08f, 0.08f, 0.1f, 0.95f))); // Dark glass
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.06f, 0.06f, 0.08f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.15f, 0.18f, 1.0f))); // Subtle cyan tint
            
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.18f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.7f, 0.75f, 1.0f))); // Kay Cyan Hover
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1.0f))); // Kay Cyan Logo Exact
            
            ImGui.PushStyleColor(ImGuiCol.Header, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.3f, 0.35f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.6f, 0.65f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1.0f)));
            
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.16f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.21f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.25f, 0.26f, 1.0f)));

            ImGui.PushStyleColor(ImGuiCol.CheckMark, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.7f, 0.75f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1.0f)));
            
            ImGui.PushStyleColor(ImGuiCol.Tab, ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.12f, 0.14f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.6f, 0.65f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.TabActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.5f, 0.55f, 1.0f)));
            
            // TabUnfocused and TabUnfocusedActive are not in this Dalamud ImGui version enum, ignoring them to fix build
            
            ImGui.PushStyleColor(ImGuiCol.Separator, ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
        }

        private void PopKayTheme()
        {
            ImGui.PopStyleColor(19);
            ImGui.PopStyleVar(5);
        }

        public override void Draw()
        {
            SyncSelectedMissionFromGame();

            PushKayTheme();
            if (ImGui.BeginTabBar("SquadronTabs"))
            {
                if (ImGui.BeginTabItem(Loc.T("OptimizationTab")))
                {
                    DrawOptimizationTab();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem(Loc.T("MembersTab")))
                {
                    DrawMembersTab();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
            
            PopKayTheme();
        }




        /// <summary>
        /// Mirrors the mission row currently highlighted in the native Squadron Missions
        /// window (AgentGcArmyExpedition.SelectedRow) into our own mission dropdown, so
        /// clicking a mission there auto-selects it here. Best-effort: silently does nothing
        /// if the agent/addon isn't open or the native name can't be matched.
        /// </summary>
        private unsafe void SyncSelectedMissionFromGame()
        {
            try
            {
                var agent = AgentGcArmyExpedition.Instance();
                if (agent == null || agent->ExpeditionData == null) return;

                int row = agent->SelectedRow;
                if (row == _lastSyncedMissionRow) return;
                _lastSyncedMissionRow = row;

                if (row < 0 || row >= agent->ExpeditionData->NumEntries) return;

                string nativeName = agent->ExpeditionData->MissionInfo[row].Name.ToString();
                if (string.IsNullOrWhiteSpace(nativeName)) return;

                var match = _missions.FirstOrDefault(m => m.RawName == nativeName);
                if (match != null)
                {
                    _selectedMission = match;
                }
                else
                {
                    AddDebug($"Mission sync: no match for native row name '{nativeName}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Mission sync error: {ex}");
            }
        }

        private void DrawOptimizationTab()
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("OptimizationIntro1"));
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("OptimizationIntro2"));
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 0.5f, 1f), Loc.T("CurrentTrainingStatus", $"💪 {_currentTraining.Physical}", $"🧠 {_currentTraining.Mental}", $"🎯 {_currentTraining.Tactical}"));
            if (_currentTraining.Total > SquadronData.GetMaxTrainingPoints(_configuration.SquadronRank))
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.1f, 1f), Loc.T("RankMismatchWarning", _currentTraining.Total, SquadronData.GetMaxTrainingPoints(_configuration.SquadronRank)));
            }
            ImGui.Spacing();
            
            ImGui.TextColored(new Vector4(0.01f, 0.95f, 1f, 1f), Loc.T("SelectMission"));
            
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##MissionCombo", _selectedMission?.Name ?? Loc.T("CurrentMissionPlaceholder")))
            {
                foreach (var mission in _missions)
                {
                    if (ImGui.Selectable($"{mission.Name}##{mission.Id}", _selectedMission?.Id == mission.Id))
                    {
                        _selectedMission = mission;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.01f, 0.95f, 1f, 1f), Loc.T("SquadronRankLabel"));
            int squadronRank = _configuration.SquadronRank;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##SquadronRankCombo", Loc.T($"RankOption{squadronRank}")))
            {
                for (int rank = 1; rank <= 3; rank++)
                {
                    if (ImGui.Selectable(Loc.T($"RankOption{rank}"), squadronRank == rank))
                    {
                        _configuration.SquadronRank = rank;
                        _configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            int maxTrainingPoints = SquadronData.GetMaxTrainingPoints(_configuration.SquadronRank);

            if (ImGui.Button(Loc.T("OptimizeBtn"), new Vector2(ImGui.GetContentRegionAvail().X / 2 - 4, 40)) && _selectedMission != null)
            {
                _results = _optimizer.Optimize(_members, _selectedMission, maxTrainingPoints, _currentTraining, _trainingCourses);
            }
            ImGui.SameLine();
            if (ImGui.Button(Loc.T("ResetBtn"), new Vector2(ImGui.GetContentRegionAvail().X, 40)))
            {
                _results.Clear();
                _selectedMission = null;
            }

            if (_results.Any() && _selectedMission != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f), Loc.T("BestMatches"));

                foreach (var res in _results)
                {
                    bool isPerfect = res.RequirementsMet == 3;
                    Vector4 headerColor = isPerfect ? new Vector4(0.01f, 0.95f, 1f, 1f) : new Vector4(0.8f, 0.8f, 0.2f, 1f);
                    
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(headerColor));
                    bool expanded = ImGui.CollapsingHeader($"{Loc.T("SuccessRatio", res.RequirementsMet, string.Join(", ", res.Team.Select(m => m.Name.Split(' ')[0])))}##{res.GetHashCode()}");
                    ImGui.PopStyleColor();
                    
                    if (expanded)
                    {
                        ImGui.Indent();

                        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("BoardTrainingLabel", maxTrainingPoints));
                        ImGui.TextColored(new Vector4(1f, 1f, 1f, 1f), $" 💪 {Loc.T("Physical")} : {res.BestTraining.Physical}");
                        ImGui.SameLine(150);
                        ImGui.TextColored(new Vector4(1f, 1f, 1f, 1f), $" 🧠 {Loc.T("Mental")} : {res.BestTraining.Mental}");
                        ImGui.SameLine(300);
                        ImGui.TextColored(new Vector4(1f, 1f, 1f, 1f), $" 🎯 {Loc.T("Tactical")} : {res.BestTraining.Tactical}");

                        ImGui.Spacing();

                        if (res.TrainingPlan.Count > 0)
                        {
                            ImGui.TextColored(new Vector4(0.01f, 0.95f, 1f, 1f), Loc.T("TrainingPlanLabel", res.SessionsNeeded));
                            foreach (var rec in res.TrainingPlan)
                            {
                                ImGui.BulletText($"{rec.Course.Name}  x{rec.Count}  ({DescribeCourseBonus(rec.Course)})");
                            }
                            int days = (int)Math.Ceiling(res.SessionsNeeded / 3.0);
                            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), Loc.T("TrainingPlanDays", days));
                        }
                        else if (isPerfect)
                        {
                            ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f), Loc.T("TrainingPlanNone"));
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(1f, 0.6f, 0.1f, 1f), Loc.T("TrainingPlanStuck", maxTrainingPoints));
                        }

                        ImGui.Spacing();
                        
                        // Compare final stats to required stats
                        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("StatsFinalVsRequired"));
                        
                        DrawStatComparison(Loc.T("Physical"), res.FinalStats.Physical, _selectedMission.RequiredAttributes.Physical, new Vector4(0.01f, 0.95f, 1f, 1f));
                        DrawStatComparison(Loc.T("Mental"), res.FinalStats.Mental, _selectedMission.RequiredAttributes.Mental, new Vector4(0.1f, 0.7f, 0.9f, 1f));
                        DrawStatComparison(Loc.T("Tactical"), res.FinalStats.Tactical, _selectedMission.RequiredAttributes.Tactical, new Vector4(0.3f, 1f, 0.8f, 1f));

                        ImGui.Unindent();
                        ImGui.Spacing();
                    }
                }
            }
        }

        private static string DescribeCourseBonus(TrainingCourse course)
        {
            var parts = new List<string>();
            if (course.Bonus.Physical > 0) parts.Add($"+{course.Bonus.Physical} {Loc.T("Physical")}");
            if (course.Bonus.Mental > 0) parts.Add($"+{course.Bonus.Mental} {Loc.T("Mental")}");
            if (course.Bonus.Tactical > 0) parts.Add($"+{course.Bonus.Tactical} {Loc.T("Tactical")}");
            return string.Join(", ", parts);
        }

        private void DrawStatComparison(string label, int actual, int required, Vector4 color)
        {
            float ratio = required > 0 ? (float)Math.Min(actual, required) / required : 1f;
            ImGui.Text($"{label, -10}: {actual}/{required}");
            ImGui.SameLine(180);
            
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ImGui.ColorConvertFloat4ToU32(color));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1f)));
            ImGui.ProgressBar(ratio, new Vector2(-1, 14), actual >= required ? Loc.T("OK") : Loc.T("Insufficient"));
            ImGui.PopStyleColor(2);
        }

        private void DrawMembersTab()
        {
            ImGui.Spacing();

            bool autoOpen = _configuration.AutoOpenWithSquadronWindow;
            if (ImGui.Checkbox(Loc.T("AutoOpenToggle"), ref autoOpen))
            {
                _configuration.AutoOpenWithSquadronWindow = autoOpen;
                _configuration.Save();
            }

            ImGui.Spacing();
            
            // Primary Action Button styling
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.5f, 0.55f, 1f)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.7f, 0.75f, 1f)));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1f)));
            if (ImGui.Button(Loc.T("ImportGameBtn"), new Vector2(-1, 40)))
            {
                ImportFromGame();
            }
            ImGui.PopStyleColor(3);
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.BeginChild("MembersList", new Vector2(-1, -1), false);
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T("ManageMembersLabel"));
            ImGui.Spacing();
            
            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                ImGui.PushID(i);
                
                ImGui.BeginGroup();
                
                ImGui.SetNextItemWidth(140);
                string name = m.Name;
                if (ImGui.InputText("##Name", ref name, 32)) { m.Name = name; PersistMembers(); }
                
                ImGui.SameLine();
                ImGui.SetNextItemWidth(70);
                int level = m.Level;
                if (ImGui.InputInt("##Niv", ref level, 0, 0)) { m.Level = Math.Clamp(level, 1, 60); PersistMembers(); }
                
                ImGui.SameLine();
                ImGui.SetNextItemWidth(140);
                if (ImGui.BeginCombo("##Class", m.ClassName))
                {
                    foreach (SquadronData.SquadronClass job in Enum.GetValues(typeof(SquadronData.SquadronClass)))
                    {
                        if (ImGui.Selectable(job.ToString(), m.ClassId == (int)job))
                        {
                            m.ClassId = (int)job;
                            m.ClassName = job.ToString();
                            PersistMembers();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.EndGroup();
                ImGui.Spacing();

                // Update attributes if level or class changed
                m.BaseAttributes = SquadronData.GetBaseStats((SquadronData.SquadronClass)m.ClassId, m.Level);
                ImGui.PopID();
            }
            ImGui.EndChild();
        }

        private unsafe void ImportFromGame()
        {
            AddDebug(Loc.T("ImportStart"));
            
            try 
            {
                var manager = FFXIVClientStructs.FFXIV.Client.Game.GcArmyManager.Instance();
                if (manager == null || manager->Data == null)
                {
                    AddDebug(Loc.T("ImportErrNullManager"));
                    return;
                }
                
                var members = manager->Data->Members;
                
                // Read current training
                _currentTraining = new Attributes(
                    manager->Data->BonusPhysical,
                    manager->Data->BonusMental,
                    manager->Data->BonusTactical
                );
                AddDebug(Loc.T("ImportTrainingRead", _currentTraining.Physical, _currentTraining.Mental, _currentTraining.Tactical));

                // Sheets for real names
                var gcSheet = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.GcArmyMember>();
                var enpcSheet = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.ENpcResident>();
                
                _members.Clear();
                
                for (int i = 0; i < 8; i++)
                {
                    var m = members[i];
                    byte* mPtr = (byte*)&m;
                    
                    uint baseId = *(uint*)mPtr;      // Offset 0x00: BaseId
                    byte classId = *(mPtr + 0x0A);   // Offset 0x0A: ClassJob
                    byte level = *(mPtr + 0x0B);     // Offset 0x0B: Level
                    
                    if (baseId == 0) continue; // Empty slot
                    
                    string name = Loc.T("RecruitPlaceholder", i + 1);
                    try 
                    {
                        if (gcSheet != null && enpcSheet != null) 
                        {
                            var gcRow = gcSheet.GetRowOrDefault(baseId);
                            if (gcRow.HasValue) 
                            {
                                // In Lumina 1.x / Dalamud 9+, accessing the reference directly:
                                // usually GcArmyMember has a single ENpcResident reference Column 0
                                uint enpcId = gcRow.Value.RowId; // Fallback
                                
                                // Through reflection to safely grab the ENpcResident column value if properties changed
                                var val = gcRow.Value.GetType().GetProperty("ENpcResident")?.GetValue(gcRow.Value);
                                if (val != null)
                                {
                                    // Usually it's a LazyRow or uint depending on Lumina version
                                    dynamic lazyVal = val;
                                    try { enpcId = (uint)lazyVal.RowId; } catch { 
                                        try { enpcId = (uint)lazyVal.Value.RowId; } catch { }
                                    }
                                }

                                var npc = enpcSheet.GetRowOrDefault(enpcId);
                                if (npc.HasValue && !string.IsNullOrEmpty(npc.Value.Singular.ToString()))
                                {
                                    name = npc.Value.Singular.ToString();
                                }
                                else 
                                {
                                    // Let's hardcode the +1016923 logic as an ultimate fallback if reflection fails
                                    // Cecily = ID:3 -> 1016926. So ENpcResidentID = BaseID + 1016923
                                    var hardNpc = enpcSheet.GetRowOrDefault(baseId + 1016923);
                                    if (hardNpc.HasValue && !string.IsNullOrEmpty(hardNpc.Value.Singular.ToString()))
                                    {
                                        name = hardNpc.Value.Singular.ToString();
                                    }
                                }
                            }
                        }
                    } 
                    catch { /* Fallback to default name if sheet fails */ }
                    
                    var job = (SquadronData.SquadronClass)classId;
                    
                    _members.Add(new SquadronMember
                    {
                        Name = name,
                        Level = level,
                        ClassId = classId,
                        ClassName = job.ToString(),
                        BaseAttributes = SquadronData.GetBaseStats(job, level)
                    });
                    
                    AddDebug(Loc.T("ImportMemberLine", name, level, job.ToString()));
                }
                AddDebug(Loc.T("ImportComplete", _members.Count));
                PersistMembers();
            }
            catch (Exception ex)
            {
                AddDebug(Loc.T("ImportErrGeneric", ex.Message));
            }
        }

    }
}
