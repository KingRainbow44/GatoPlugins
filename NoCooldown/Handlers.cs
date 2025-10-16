using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy.PacketProcessor;
using Packando;

namespace NoCooldown;

public static class Handlers {
    [Handler(CmdID.SceneTeamUpdateNotify)]
    public static PacketResult OnSceneTeamUpdateNotify(
        Session session, PacketHead _, SceneTeamUpdateNotify msg) {
        if (!Plugin.Instance.Config.Enabled) {
            return PacketResult.Forward;
        }

        // For every avatar, remove the cooldown on all skills.
        foreach (var avatar in msg.SceneTeamAvatarList) {
            var packet = new AvatarFightPropUpdateNotify {
                AvatarGuid = avatar.AvatarGuid,
                FightPropMap = { {80, 1} }
            };

            session.SendClient(CmdID.AvatarFightPropUpdateNotify, packet);
        }

        return PacketResult.Forward;
    }
}
