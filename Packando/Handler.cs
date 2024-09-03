using Common.Protocol;

namespace Packando;

public enum SendType {
    All,
    Server,
    Proxy
}

[AttributeUsage(AttributeTargets.Method)]
public class Handler(CmdID packetId) : Attribute {
    public CmdID PacketId { get; init; } = packetId;
    public SendType ListenFor { get; set; } = SendType.All;
    public bool Inject { get; set; }
}
