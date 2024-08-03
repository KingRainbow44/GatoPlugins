using System.Drawing;
using Common.Util;
using FreakyProxy;

namespace Windblade.Commands;

public static class Commands {
    private const string WindyUsage = "windy <script>";

    [Command("windy", WindyUsage, "Executes a Lua script.",
        Aliases = ["lua", "exec", "eval"])]
    public static async Task Windy(ICommandSender sender, string[] args) {
        var session = sender.AsPlayer().Session;

        if (args.Length == 0) {
            await sender.SendMessage($"Usage: {WindyUsage}");
            return;
        }

        var script = string.Join(' ', args);
        if (await Windblade.ExecuteScript(session, script)) {
            await sender.SendMessage("Executed Lua script!".Colored(Color.Aquamarine));
        } else {
            await sender.SendMessage("Failed to execute Lua script.".Colored(Color.Red));
        }
    }
}
