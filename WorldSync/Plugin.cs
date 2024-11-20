using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;

namespace WorldSync;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public override void OnLoad() {
        CommandProcessor.RegisterAllCommands("WorldSync");

        PluginManager.AddEventListener<WorldCreationEvent>(OnWorldCreation);

        Logger.Info("WorldSync plugin loaded.");
    }

    /// <summary>
    /// Invoked when a player's world is created.
    /// </summary>
    private static void OnWorldCreation(WorldCreationEvent @event) {
        @event.WorldType = typeof(SynchronizedWorld);
    }
}
