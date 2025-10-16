using Common.Util;
using FreakyProxy.Commands;

namespace InfiniteStamina.Commands;

public static class Commands {
    private const string StaminaUsage = "stamina <on|off>";

    [Command("stamina", StaminaUsage, "Enables or disables infinite stamina.")]
    public static Task Stamina(ICommandSender sender, string[] args) {
        var session = sender.AsPlayer().Session;

        try {
            if (args.Length == 0) throw new Exception();
            Plugin.Enabled[session] = args[0].ParseBool() switch {
                true => true,
                false => false
            };
        } catch (Exception) {
            // If we present an invalid value, or no value at all, we toggle the current state.
            Plugin.Enabled[session] = !Plugin.GetEnabled(session);
        }

        sender.SendMessage($"Infinite stamina is now {(Plugin.GetEnabled(session) ? "enabled" : "disabled")}.");
        return Task.CompletedTask;
    }
}
