using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy.PacketProcessor;
using Packando;

namespace DeathSwap;

public static class Handlers {
    [Handler(CmdID.GetPlayerTokenReq, Inject = true)]
    public static ValueTask<PacketResult> GetPlayerTokenReq(Session session, PacketHead _, GetPlayerTokenReq msg) {
        if (Plugin.Override) {
            var @override = Plugin.Tokens[msg.AccountToken];
            msg.AccountUid = @override.AccountUid;
            msg.AccountToken = @override.AccountToken;
            msg.Ticket = @override.Ticket;
        }
        else {
            Plugin.Tokens[msg.AccountToken!] = msg;
            Plugin.Accounts[msg.AccountUid] = msg.AccountToken;
        }
        return ReturnValues.Intercept;
    }

    [Handler(CmdID.PlayerLoginReq)]
    public static ValueTask<PacketResult> PlayerLoginReq(Session session, PacketHead _, PlayerLoginReq msg) {
        if (Plugin.Override) {
            var @override = Plugin.Logins[msg.Token];
            msg.DeviceInfo = @override.DeviceInfo;
            msg.DeviceName = @override.DeviceName;
            msg.DeviceUuid = @override.DeviceUuid;
            msg.Platform = @override.Platform;
            msg.DeviceFp = @override.DeviceFp;
            msg.SystemVersion = @override.SystemVersion;
        }
        else {
            Plugin.Logins[msg.Token!] = msg;
        }

        return ReturnValues.Intercept;
    }
}
