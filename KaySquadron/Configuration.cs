using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace KaySquadron
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // Add config fields here
        public bool ShowSuccessProbability { get; set; } = true;
        public bool AutoOpenWithSquadronWindow { get; set; } = true;

        /// <summary>
        /// Current Squadron Rank (1 = Trainee/200 pts, 2 = 300 pts, 3 = max/400 pts).
        /// Manually set by the player since Dalamud has no confirmed way to read this
        /// from the game's memory/sheets yet; bump it when your squadron ranks up.
        /// </summary>
        public int SquadronRank { get; set; } = 1;

        /// <summary>Persisted squadron roster from the last successful in-game import,
        /// so members survive plugin/game restarts without needing to re-import.</summary>
        public List<SquadronMember> SavedMembers { get; set; } = new();

        /// <summary>Training bonus already banked on the squadron, from the last import.</summary>
        public Attributes SavedTraining { get; set; } = new(0, 0, 0);

        [NonSerialized]
        private IDalamudPluginInterface? _pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this._pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this._pluginInterface?.SavePluginConfig(this);
        }
    }
}
