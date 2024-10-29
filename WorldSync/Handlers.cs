using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy.PacketProcessor;
using Google.Protobuf;
using Packando;

namespace WorldSync;

public static class Handlers {
    [Handler(CmdID.CombatInvocationsNotify)]
    public static async ValueTask<PacketResult> HandleCombatInvocationsNotify(Session session, PacketHead _,
        CombatInvocationsNotify msg) {
        foreach (var entry in msg.InvokeList) {
            // For sanity reasons, we make sure to tell clients they should be receiving these packets.
            entry.ForwardType = ForwardType.ToAll;

            // Handle combat invocation.
            switch (entry.ArgumentType) {
                case CombatTypeArgument.CombatAnimatorParameterChanged: {
                    break;
                }
                case CombatTypeArgument.CombatAnimatorStateChanged: {
                    break;
                }
                case CombatTypeArgument.EntityMove: {
                    var moveInfo = entry.CombatData.ParseFrom<EntityMoveInfo>()!;
                    var moveEntityId = moveInfo.EntityId;

                    // Prevent the packet from being reliable.
                    moveInfo.IsReliable = false;
                    moveInfo.ReliableSeq = 0;
                    moveInfo.SceneTime = 0;
                    entry.CombatData = moveInfo.ToByteString();

                    // Check if the entity is affecting another one.
                    if (Sync.Entities.TryGetValue(moveEntityId, out var pair)) {
                        var (target, entityId) = pair;

                        var packet = new CombatInvocationsNotify();
                        packet.InvokeList.Add(new CombatInvokeEntry(entry) {
                            CombatData = new EntityMoveInfo(moveInfo) {
                                EntityId = entityId
                            }.ToByteString()
                        });

                        await target.SendClient(CmdID.CombatInvocationsNotify, packet, sequential: true);
                    }

                    break;
                }
            }
        }

        return PacketResult.Intercept;
    }
}
