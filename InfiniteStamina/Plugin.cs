// ReSharper disable UnusedType.Global

using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;
using FreakyProxy.PacketProcessor;

namespace InfiniteStamina;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static readonly Dictionary<Session, bool> Enabled = new();
    private static Plugin? Instance;

    private Config _config = new();

    /// <summary>
    /// Gets the enabled state for the specified session.
    /// </summary>
    public static bool GetEnabled(Session session) {
        Enabled.TryAdd(session, Instance?._config.Enabled ?? false);
        return Enabled[session];
    }

    public override void OnLoad() {
        Instance = this;
        _config = this.GetConfig(_config);

        CommandProcessor.RegisterAllCommands("InfiniteStamina");
        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);

        Logger.Info("Infinite Stamina plugin loaded.");
    }

    public override void OnUnload() {
        Instance = null;
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
