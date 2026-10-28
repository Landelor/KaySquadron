using Dalamud.Game;
using System.Collections.Generic;

namespace KaySquadron
{
    public static class Loc
    {
        private static ClientLanguage _currentLanguage = ClientLanguage.English;

        public static void Initialize(ClientLanguage language)
        {
            _currentLanguage = language;
        }

        private static readonly Dictionary<string, string> EnglishStrings = new()
        {
            // General
            { "PluginName", "KaySquadron - GC Squadron Optimizer" },
            { "SelectMission", "1. Choose a mission" },
            { "CurrentMissionPlaceholder", "Select a mission..." },
            { "OptimizeBtn", "Optimize Squadron" },
            { "ResetBtn", "Reset Training" },
            
            // Tabs
            { "OptimizationTab", "Optimization" },
            { "MembersTab", "Members" },
            
            // Optimization Results
            { "ImportPrompt", "Please import your squadron from the game first." },
            { "StatsNotMet", "No configuration found that meets 100% of the mission requirements." },
            { "BestMatches", "Best recommendations:" },
            { "SuccessRatio", "Success: {0}/3 - [{1}]" },
            { "BoardTrainingLabel", "Board Training ({0} pts):" },
            { "Physical", "Physical" },
            { "Mental", "Mental" },
            { "Tactical", "Tactical" },
            { "StatsFinalVsRequired", "Final Stats vs Required:" },
            { "OK", "OK" },
            { "Insufficient", "Insufficient" },
            
            // Members Tab
            { "ImportGameBtn", "Import from current Squadron" },
            { "ManageMembersLabel", "Manage your 8 squadron members here." },
            
            // Error handling
            { "ErrInit", "Window initialized." },
            { "ErrNotFound", "Error: Mission list not loaded." },
            { "ErrNoMembers", "Error: No members imported." },
            { "ErrPointer", "Error: Memory pointer not found." },
            { "ErrLuminaLoad", "Error loading missions: {0}" },
            { "MissionNameFallback", "Lumina load error" },

            // Optimization tab intro / status
            { "OptimizationIntro1", "Select your target mission and the calculator will find" },
            { "OptimizationIntro2", "exactly how to split training for a 100% success rate." },
            { "CurrentTrainingStatus", "Current training: {0}  {1}  {2}" },

            // Import debug messages
            { "ImportStart", "Starting auto-import via GcArmyManager..." },
            { "ImportErrNullManager", "Error: GcArmyManager or Data is null. (Are you in the barracks?)" },
            { "ImportTrainingRead", "Training read: {0}/{1}/{2}" },
            { "ImportMemberLine", "Imported: {0} (Lvl {1}, {2})" },
            { "ImportComplete", "Import complete. {0} recruits found!" },
            { "ImportErrGeneric", "GcArmyManager error: {0}" },
            { "RecruitPlaceholder", "Recruit {0}" },
            { "AutoOpenToggle", "Auto-open with the Squadron Missions window" },
            { "SquadronRankLabel", "Squadron Rank (training point pool)" },
            { "RankOption1", "Rank 1 - Trainee (200 pts)" },
            { "RankOption2", "Rank 2 (300 pts)" },
            { "RankOption3", "Rank 3 - Max (400 pts)" },
            { "TrainingPlanLabel", "Recommended training plan ({0} sessions):" },
            { "TrainingPlanDays", "~{0} day(s) at 3 sessions/day" },
            { "TrainingPlanNone", "Already there - no further training needed!" },
            { "MissionNamePrefix", "[Lvl. {0}] {1}" },
            { "RankMismatchWarning", "⚠ Your banked training ({0} pts) exceeds the selected Rank's cap ({1} pts) - check the Squadron Rank setting above." },
            { "TrainingPlanStuck", "⚠ No reachable training allocation within a {0}-pt cap meets this mission. If your Squadron Rank setting looks right, this mission may be out of reach for this roster - try a different team or level up your recruits." }
        };

        private static readonly Dictionary<string, string> FrenchStrings = new()
        {
            // General
            { "PluginName", "KaySquadron - Optimiseur d'Escouade GC" },
            { "SelectMission", "1. Choisissez une mission" },
            { "CurrentMissionPlaceholder", "Sélectionnez une mission..." },
            { "OptimizeBtn", "Optimiser l'Escouade" },
            { "ResetBtn", "Réinitialiser l'entraînement" },
            
            // Tabs
            { "OptimizationTab", "Optimisation" },
            { "MembersTab", "Membres" },
            
            // Optimization Results
            { "ImportPrompt", "Commencez par importer votre escouade depuis le jeu." },
            { "StatsNotMet", "Aucune configuration ne permet d'atteindre 100% des prérequis pour cette mission." },
            { "BestMatches", "Meilleures recommandations :" },
            { "SuccessRatio", "Succès : {0}/3 - [{1}]" },
            { "BoardTrainingLabel", "Entraînement au tableau ({0} pts) :" },
            { "Physical", "Physique" },
            { "Mental", "Mental" },
            { "Tactical", "Tactique" },
            { "StatsFinalVsRequired", "Statistiques Finales vs Requises :" },
            { "OK", "OK" },
            { "Insufficient", "Insuffisant" },
            
            // Members Tab
            { "ImportGameBtn", "Importer de l'Escouade actuelle" },
            { "ManageMembersLabel", "Gérez vos 8 membres d'escouade ici." },
            
            // Error handling
            { "ErrInit", "Initialisation de la fenêtre." },
            { "ErrNotFound", "Erreur : la liste des missions n'est pas chargée." },
            { "ErrNoMembers", "Erreur : aucun membre importé." },
            { "ErrPointer", "Erreur : Pointeur mémoire introuvable." },
            { "ErrLuminaLoad", "Erreur lors du chargement des missions : {0}" },
            { "MissionNameFallback", "Erreur de chargement de Lumina" },

            // Optimization tab intro / status
            { "OptimizationIntro1", "Sélectionnez votre mission cible et le calculateur trouvera" },
            { "OptimizationIntro2", "exactement comment répartir l'entraînement pour 100% de réussite." },
            { "CurrentTrainingStatus", "Entraînement actuel : {0}  {1}  {2}" },

            // Import debug messages
            { "ImportStart", "Début de l'auto-importation via GcArmyManager..." },
            { "ImportErrNullManager", "Erreur : GcArmyManager ou Data est null. (Êtes-vous dans la caserne ?)" },
            { "ImportTrainingRead", "Entraînement lu : {0}/{1}/{2}" },
            { "ImportMemberLine", "Importé : {0} (Niv {1}, {2})" },
            { "ImportComplete", "Import terminé. {0} recrues trouvées !" },
            { "ImportErrGeneric", "Erreur GcArmyManager : {0}" },
            { "RecruitPlaceholder", "Recrue {0}" },
            { "AutoOpenToggle", "Ouvrir automatiquement avec la fenêtre des Missions d'Escouade" },
            { "SquadronRankLabel", "Rang de l'escouade (réserve de points d'entraînement)" },
            { "RankOption1", "Rang 1 - Recrue (200 pts)" },
            { "RankOption2", "Rang 2 (300 pts)" },
            { "RankOption3", "Rang 3 - Max (400 pts)" },
            { "TrainingPlanLabel", "Plan d'entraînement recommandé ({0} séances) :" },
            { "TrainingPlanDays", "~{0} jour(s) à 3 séances/jour" },
            { "TrainingPlanNone", "Déjà atteint - aucun entraînement supplémentaire requis !" },
            { "MissionNamePrefix", "[Niv. {0}] {1}" },
            { "RankMismatchWarning", "⚠ Votre entraînement banqué ({0} pts) dépasse le plafond du Rang sélectionné ({1} pts) - vérifiez le réglage du Rang d'Escouade ci-dessus." },
            { "TrainingPlanStuck", "⚠ Aucune répartition d'entraînement possible avec un plafond de {0} pts ne permet d'atteindre cette mission. Si votre réglage de Rang semble correct, cette mission est peut-être hors de portée pour cette escouade - essayez une autre équipe ou montez vos recrues en niveau." }
        };

        public static string T(string key)
        {
            var dict = _currentLanguage == ClientLanguage.French ? FrenchStrings : EnglishStrings;
            
            if (dict.TryGetValue(key, out var val))
            {
                return val;
            }

            // Fallback to English if key is missing in French
            if (EnglishStrings.TryGetValue(key, out var fallback))
            {
                // Optionally log missing translation here if needed
                return fallback;
            }

            return key; // return the key itself if not found anywhere
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}
