using FreakyProxy;

namespace Template;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public override void OnLoad() {
        Logger.Information("Template plugin loaded.");
    }
}
