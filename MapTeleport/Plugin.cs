// ReSharper disable UnusedType.Global

using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;
using static Common.Protocol.Proto.MarkMapReq.Types.Operation;

namespace MapTeleport;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public override void OnLoad() {
        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);

        Logger.Information("Map Teleport plugin loaded.");
    }

    private static void OnReceivePacket(ReceivePacketEvent @event) {
        var packet = @event.Packet;
        if (packet.CmdID != CmdID.MarkMapReq) return;

        var msg = packet.Body.ParseFrom<MarkMapReq>()!;
        if (msg.Op is not (Add or Mod)) return;

        var mark = msg.Mark;
        if (mark.PointType is not MapMarkPointType.FishPool) return;

        // Cancel the packet.
        @event.Result = PacketResult.Drop;

        // Teleport the player to the marker.
        var y = int.TryParse(mark.Name, out var output) ? output : 300;
        var position = new Vector {
            X = mark.Pos.X,
            Y = y,
            Z = mark.Pos.Z
        };

        var player = @event.Session.Player;
        if (player.SelectedAvatar is { } avatar) {
            // TODO: Implement cross-scene teleportation.
            avatar.Teleport(position);
        }
    }
}
