using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using System.IO;

namespace KaySquadron
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static IDataManager Data { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static ICondition Condition { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;

        public string Name => "KaySquadron";
        private const string CommandName = "/ksquad";

        public Configuration Configuration { get; init; } = null!;
        public WindowSystem WindowSystem { get; init; } = null!;
        public KaySquadronWindow MainWindow { get; init; } = null!;

        public Plugin()
        {
            try {
                this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
                this.Configuration.Initialize(PluginInterface);

                Loc.Initialize(ClientState.ClientLanguage);

                this.WindowSystem = new WindowSystem("KaySquadron");
                this.MainWindow = new KaySquadronWindow();
                this.WindowSystem.AddWindow(this.MainWindow);

                CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
                {
                    HelpMessage = "Open UI KaySquadron."
                });

                PluginInterface.UiBuilder.Draw += DrawUI;
                PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            } catch (System.Exception ex) {
                Log.Error($"Failed to initialize: {ex}");
            }
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(CommandName);
            this.WindowSystem.RemoveAllWindows();
        }

        private void OnCommand(string command, string args)
        {
            this.MainWindow.IsOpen = !this.MainWindow.IsOpen;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            this.MainWindow.IsOpen = true;
        }
    }
}
