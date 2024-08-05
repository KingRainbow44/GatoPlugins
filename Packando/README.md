# Packando

A packet interception library for GatoProxy.

# Usage

Similar to the internal proxy, Packando has attributes which can be applied to methods to receive and modify packets.

```csharp
using Packando;
using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy.PacketProcessor;

using static FreakyProxy.PacketProcessor.ReturnValues;

public static class MyPacketHandlers
{
    [Handler(CmdID.GetPlayerTokenReq)]
    public static ValueTask<PacketResult> HandleGetPlayerTokenReq(Session session, PacketHead _, GetPlayerTokenReq msg)
    {
        Console.WriteLine($"Received GetPlayerTokenReq: {msg}");
        return Forward;
    }
}
```
