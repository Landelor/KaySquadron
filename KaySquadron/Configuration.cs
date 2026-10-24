using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

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
