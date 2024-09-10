using Common.Util;
using FreakyProxy.Commands;

namespace DeathSwap.Commands;

public static class Commands {
    private const string SwapUsage = "swap <start|stop> <with>";

    [Command("swap", SwapUsage, "Death Swap game command.")]
    public static async Task Swap(ICommandSender sender, string[] args) {
        if (args.Length == 0) {
            await sender.SendMessage($"Usage: {SwapUsage}");
            return;
        }

        switch (args[0]) {
            case "start": {
                if (args.Length < 2) {
                    await sender.SendMessage($"Usage: {SwapUsage}");
                    return;
                }

                // Set the swap target.
                Plugin.Source = sender.IntoPlayer<DeathSwapPlayer>();
                Plugin.Target = args[1].ParsePlayer().As<DeathSwapPlayer>();
                Plugin.Running = true;

                await sender.SendMessage("The game has now started!");
                return;
            }
            case "stop": {
                Plugin.Running = false;

                await sender.SendMessage("The game has stopped!");
                return;
            }
            default: {
                await sender.SendMessage($"Usage: {SwapUsage}");
                return;
            }
        }
    }
}
