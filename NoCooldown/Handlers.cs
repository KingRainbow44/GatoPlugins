using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy.PacketProcessor;
using Packando;

namespace NoCooldown;

public static class Handlers {
    [Handler(CmdID.SceneTeamUpdateNotify)]
    public static async ValueTask<PacketResult> OnSceneTeamUpdateNotify(
        Session session, PacketHead _, SceneTeamUpdateNotify msg) {
        if (!Plugin.Instance.Config.Enabled) {
            return PacketResult.Forward;
        }

        // For every avatar, remove the cooldown on all skills.
        foreach (var avatar in msg.SceneTeamAvatarList) {
            var packet = new AvatarFightPropNotify {
                AvatarGuid = avatar.AvatarGuid
            };
            packet.FightPropMap.Add(80, 1);

            await session.SendClient(CmdID.AvatarFightPropNotify, packet);
        }

        return PacketResult.Forward;
    }
}
