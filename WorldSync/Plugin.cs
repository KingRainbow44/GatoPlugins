using FreakyProxy;

namespace WorldSync;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public override void OnLoad() {
        Logger.Info("WorldSync plugin loaded.");
    }
}
