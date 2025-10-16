using Common.Protocol;
using FreakyProxy;
using FreakyProxy.Events;
using FreakyProxy.PacketProcessor;
using WindbladeAPI = Windblade.Windblade;

namespace UIDHider;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    private const string Script = """CS.UnityEngine.GameObject.Find("/BetaWatermarkCanvas(Clone)/Panel/TxtUID"):GetComponent("Text").text = "{{REPLACE}}" """;

    private Config _config = new();

    public override void OnLoad() {
        _config = this.GetConfig(_config);

        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);

        Logger.Information("UIDHider plugin loaded.");
    }

    /// <summary>
    /// Sets the UID of the player to the config value.
    /// </summary>
    private void ReplaceUid(Session session) {
        WindbladeAPI.Execute(session,
            Script.Replace("{{REPLACE}}", _config.ReplaceWith));
    }

    private void OnReceivePacket(ReceivePacketEvent @event) {
        if (!_config.Enabled) return;

        var packet = @event.Packet;
        if (packet.CmdID != CmdID.PlayerLoginReq) return;

        // Send the UID replacement packet.
        ReplaceUid(@event.Session);
    }
}
