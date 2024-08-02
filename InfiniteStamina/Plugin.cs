// ReSharper disable UnusedType.Global

using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;

namespace InfiniteStamina;

public abstract class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static readonly Dictionary<ISession, bool> Enabled = new();

    /// <summary>
    /// Gets the enabled state for the specified session.
    /// </summary>
    public static bool GetEnabled(ISession session) {
        Enabled.TryAdd(session, false);
        return Enabled[session];
    }

    public override void OnLoad() {
        CommandProcessor.RegisterAllCommands("InfiniteStamina");
        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);

        Logger.Info("Infinite Stamina plugin loaded.");
    }

    private static void OnReceivePacket(ReceivePacketEvent @event) {
        var packet = @event.Packet;
        if (packet.CmdID != CmdID.PlayerPropNotify) return;

        if (!GetEnabled(@event.Session)) return;

        var msg = packet.Body.ParseFrom<PlayerPropNotify>()!;

        var modified = false;
        foreach (var (propId, propValue) in msg.PropMap) {
            if (propId != 10011) continue;

            modified = true;
            propValue.Val = 24000;
            propValue.Ival = 24000;
        }

        if (modified) {
            @event.SetBody(msg);
        }
    }
}
