using FreakyProxy;

namespace Template;

public class Template(PluginInfo info) : FreakyProxy.Plugin(info) {
    public override void OnLoad() {
        Logger.Info("Template plugin loaded.");
    }
}
