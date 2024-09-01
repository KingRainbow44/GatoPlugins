using Common.Protocol;
using Common.Util;

namespace Visualizer.Commands;

public class Commands {
    private const string VisualizerUsage = "visualizer <save|add|remove|highlight|unhighlight|show> <name|id|all|highlighted>";

    [Command("visualizer", VisualizerUsage, "Change settings about the packet visualizer.")]
    public static async Task Visualizer(ICommandSender sender, string[] args) {
        if (args.Length < 2) {
            await sender.SendMessage(VisualizerUsage);
            return;
        }

        var action = args[0].ToLower();
        if (action == "show") {
            var status = args[1].ToLower();
            Plugin.HighlightedOnly = status switch {
                "all" => false,
                "highlighted" => true,
                _ => Plugin.HighlightedOnly
            };

            await sender.SendMessage($"Visualizer will now show {(Plugin.HighlightedOnly ? "highlighted" : "all")} packets.");
            return;
        }

        var value = GetCmdId(args[1]);
        switch (action) {
            case "save":
                Plugin.Instance?.Save();
                await sender.SendMessage("Configuration file saved!");
                return;
            case "add":
                Plugin.Blacklisted.Add(value);
                await sender.SendMessage($"Added {value} to the blacklist.");
                return;
            case "remove":
                Plugin.Blacklisted.Remove(value);
                await sender.SendMessage($"Removed {value} from the blacklist.");
                return;
            case "highlight":
                Plugin.Highlighted.Add(value);
                await sender.SendMessage($"Added {value} to the highlighted list.");
                return;
            case "unhighlight":
                Plugin.Highlighted.Remove(value);
                await sender.SendMessage($"Removed {value} from the highlighted list.");
                return;
            default:
                await sender.SendMessage(VisualizerUsage);
                return;
        }
    }

    /// <summary>
    /// Tries to parse a command ID from a string value.
    /// </summary>
    private static CmdID GetCmdId(string value) {
        // Check if the value is a number.
        if (ushort.TryParse(value, out var id)) {
            return (CmdID)id;
        }

        // Check if the value is a name.
        if (Enum.TryParse(typeof(CmdID), value, out var cmdId)) {
            return (CmdID)cmdId;
        }

        throw new Exception("Invalid command ID.");
    }
}
