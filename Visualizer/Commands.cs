using Common.Protocol;
using Common.Util;

namespace Visualizer.Commands;

public class Commands {
    private const string VisualizerUsage = "visualizer <save|add|remove|highlight|unhighlight|show> <name|id|all|highlighted>";

    [Command("visualizer", VisualizerUsage, "Change settings about the packet visualizer.")]
    public static Task Visualizer(ICommandSender sender, string[] args) {
        if (args.Length < 1) {
            sender.SendMessage(VisualizerUsage);
            return Task.CompletedTask;
        }

        var action = args[0].ToLower();
        switch (action) {
            case "show": {
                var status = args[1].ToLower();
                Plugin.HighlightedOnly = status switch {
                    "all" => false,
                    "highlighted" => true,
                    _ => Plugin.HighlightedOnly
                };

                sender.SendMessage($"Visualizer will now show {(Plugin.HighlightedOnly ? "highlighted" : "all")} packets.");
                return Task.CompletedTask;
            }
            case "save": {
                Plugin.Instance?.Save();
                sender.SendMessage("Configuration file saved!");
                return Task.CompletedTask;
            }
            case "reload": {
                Plugin.LoadConfig();
                sender.SendMessage("Reloaded the configuration!");
                return Task.CompletedTask;
            }
            default: {
                var value = GetCmdId(args[1]);
                switch (action) {
                    case "add":
                        Plugin.Blacklisted.Add(value);
                        sender.SendMessage($"Added {value} to the blacklist.");
                        return Task.CompletedTask;
                    case "remove":
                        Plugin.Blacklisted.Remove(value);
                        sender.SendMessage($"Removed {value} from the blacklist.");
                        return Task.CompletedTask;
                    case "highlight":
                        Plugin.Highlighted.Add(value);
                        sender.SendMessage($"Added {value} to the highlighted list.");
                        return Task.CompletedTask;
                    case "unhighlight":
                        Plugin.Highlighted.Remove(value);
                        sender.SendMessage($"Removed {value} from the highlighted list.");
                        return Task.CompletedTask;
                    default:
                        sender.SendMessage(VisualizerUsage);
                        return Task.CompletedTask;
                }
            }
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
