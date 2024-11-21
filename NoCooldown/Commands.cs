using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy.Commands;

namespace NoCooldown.Commands;

public static class Commands {
    private const string CooldownUsage = "cooldown <on|off>";

    [Command("cooldown", CooldownUsage, "Enables or disables cooldowns.",
        Aliases = ["cd", "skillcd"])]
    public static async Task Cooldown(ICommandSender sender, string[] args) {
        var session = sender.AsPlayer().Session;

        if (args.Length < 1) {
            await sender.SendMessage($"Usage: {CooldownUsage}");
            return;
        }

        try {
            var value = args[0].ParseBool() ? 1 : 0;
            foreach (var avatar in session.AvatarModule.Avatars) {
                var packet = new AvatarFightPropUpdateNotify {
                    AvatarGuid = avatar.AvatarGuid
                };
                packet.FightPropMap.Add(80, value);

                await session.SendClient(CmdID.AvatarFightPropNotify, packet);
            }

            var message = value == 1 ? "enabled" : "disabled";
            await sender.SendMessage($"Avatar skill cooldowns are now {message}.");
        } catch (Exception) {
            await sender.SendMessage($"Invalid value. Usage: {CooldownUsage}");
        }
    }
}
