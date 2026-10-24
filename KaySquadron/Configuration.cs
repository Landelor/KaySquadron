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
