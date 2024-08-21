using Common.Util;
using FreakyProxy;

namespace NoCooldown;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static Plugin Instance = null!;
    public Config Config { get; private set; }

    public override void OnLoad() {
        Instance = this;
        Config = this.GetConfig(new Config());

        CommandProcessor.RegisterAllCommands("NoCooldown");

        Logger.Info("No Cooldown plugin loaded.");
    }

    public override void OnUnload() {
        Instance = null!;
    }
}
