using System.Text.Json;
using System.Text.Json.Serialization;
using FreakyProxy;
using FreakyProxy.Data;

namespace Exporter;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static readonly Dictionary<uint, string> RelicSetNames = new();

    public static string OutputDirectory { get; private set; } = "";

    public override void OnLoad() {
        // Create the export data format.
        OutputDirectory = Path.Join(DataDirectory.FullName, "exports");
        if (!Directory.Exists(OutputDirectory)) {
            Directory.CreateDirectory(OutputDirectory);
        }

        // Load the text map.
        if (GameData.TextMap is null) {
            throw new Exception("Text map not specified or unable to load");
        }

        // Resolve relic set names.
        foreach (var entry in GameData.DisplayItemData.Values) {
            if (!entry.Icon.Contains("Relic")) continue;

            var setId = entry.Param;
            if (RelicSetNames.ContainsKey(setId)) continue;

            var name = GoodHelper.Convert(GameData.TextMap[entry.NameTextMapHash]);
            RelicSetNames[setId] = name;
        }

        Logger.Info("Exporter plugin loaded.");
    }
}
