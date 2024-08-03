using FreakyProxy;

namespace Template;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public override void OnLoad() {
        Logger.Info("Template plugin loaded.");
    }
}
