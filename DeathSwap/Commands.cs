using Common.Util;
using FreakyProxy.Commands;

namespace DeathSwap.Commands;

public static class Commands {
    private const string SwapUsage = "swap <start|stop> <with>";

    [Command("swap", SwapUsage, "Death Swap game command.")]
    public static async Task Swap(ICommandSender sender, string[] args) {
        var player = sender.IntoPlayer<DeathSwapPlayer>();

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

                // Get the target player.
                var target = args[1].ParsePlayer().As<DeathSwapPlayer>();

                await player.SendMessage("Sending token in 2.5s...");
                await Task.Delay(2500);

                // Swap the players.
                Plugin.Override = true;
                await player.SwapWith(target);
                return;
            }
            case "stop": {
                Plugin.Override = false;
                return;
            }
            default: {
                await sender.SendMessage($"Usage: {SwapUsage}");
                return;
            }
        }
    }
}
