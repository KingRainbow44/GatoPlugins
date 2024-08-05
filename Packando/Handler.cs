using Common.Protocol;

namespace Packando;

[AttributeUsage(AttributeTargets.Method)]
public class Handler(CmdID packetId) : Attribute {
    public CmdID PacketId { get; init; } = packetId;
}
