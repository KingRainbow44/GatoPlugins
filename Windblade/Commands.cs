using System.Drawing;
using Common.Util;
using FreakyProxy.Commands;

namespace Windblade.Commands;

public static class Commands {
    private const string WindyUsage = "windy <script>";

    [Command("windy", WindyUsage, "Executes a Lua script.",
        Aliases = ["lua", "exec", "eval"])]
    public static async Task Windy(ICommandSender sender, string[] args) {
        var session = sender.AsPlayer().Session;

        if (args.Length == 0) {
            sender.SendMessage($"Usage: {WindyUsage}");
            return;
        }

        var script = string.Join(' ', args);
        if (await Windblade.ExecuteScript(session, script)) {
            sender.SendMessage("Executed Lua script!".Colored(Color.Aquamarine));
        } else {
            sender.SendMessage("Failed to execute Lua script.".Colored(Color.Red));
        }
    }
}
