using Common.Util;
using FreakyProxy.Commands;

namespace Exporter.Commands;

public static class Commands {
    [Command("export", "export", "Export your saved GOOD data.")]
    public static async Task Export(ICommandSender sender, string[] _) {
        var player = sender.AsPlayer();
        var session = player.Session;
        var export = session.Data<ExporterData>();

        await export.WriteToFile();
        sender.SendMessage("Your data has been saved!");
    }
}
