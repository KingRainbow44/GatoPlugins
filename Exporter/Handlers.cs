using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy.PacketProcessor;
using Packando;

namespace Exporter;

public static class Handlers {
    [Handler(CmdID.PlayerStoreNotify)]
    public static ValueTask<PacketResult> PlayerStoreNotify(Session session, PacketHead _, PlayerStoreNotify msg) {
        var export = session.Data<ExporterData>();
        foreach (var item in msg.ItemList) {
            export.AddItem(item);
        }

        return ReturnValues.Forward;
    }

    [Handler(CmdID.AvatarDataNotify)]
    public static ValueTask<PacketResult> AvatarDataNotify(Session session, PacketHead _, AvatarDataNotify msg) {
        var export = session.Data<ExporterData>();
        foreach (var avatar in msg.AvatarList) {
            try {
                if (avatar is null) continue;
                export.AddAvatar(avatar);
            }
            catch (Exception) {
                // This exception is probably from the Traveler.
            }
        }

        return ReturnValues.Forward;
    }
}
