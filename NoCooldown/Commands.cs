using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy.Commands;

namespace NoCooldown.Commands;

public static class Commands {
    private const string CooldownUsage = "cooldown <on|off>";

    [Command("cooldown", CooldownUsage, "Enables or disables cooldowns.",
        Aliases = ["cd", "skillcd"])]
    public static Task Cooldown(ICommandSender sender, string[] args) {
        var player = sender.AsPlayer();
        var session = player.Session;

        if (args.Length < 1) {
            sender.SendMessage($"Usage: {CooldownUsage}");
            return Task.CompletedTask;
        }

        try {
            var value = args[0].ParseBool() ? 1 : 0;
            foreach (var avatar in player.TeamManager.SceneTeamAvatarList) {
                var packet = new AvatarFightPropUpdateNotify {
                    AvatarGuid = avatar.AvatarGuid
                };
                packet.FightPropMap.Add(80, value);

                session.SendClient(CmdID.AvatarFightPropNotify, packet);
            }

            var message = value == 1 ? "enabled" : "disabled";
            sender.SendMessage($"Avatar skill cooldowns are now {message}.");
        } catch (Exception) {
            sender.SendMessage($"Invalid value. Usage: {CooldownUsage}");
        }

        return Task.CompletedTask;
    }
}
