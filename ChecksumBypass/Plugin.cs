using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy;
using FreakyProxy.Events;
using Google.Protobuf;

namespace ChecksumBypass;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    private Config _config = new();

    public override void OnLoad() {
        _config = this.GetConfig(_config);

        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);

        Logger.Info("ChecksumBypass plugin loaded.");
    }

    private void OnReceivePacket(ReceivePacketEvent @event) {
        if (!_config.Enabled && _config.ReplaceWith.Length == 0) return;

        var packet = @event.Packet;
        if (packet.CmdID != CmdID.PlayerLoginReq) return;

        var empty = EmptyMessage.Parser.ParseFrom(packet.Body);
        var fields = empty.GetFields();

        int checksumField = 0;

        foreach (var field in fields) {
            if (field.Value is ByteString value) {
                try {
                    var hex = Convert.FromHexString(value.ToString(System.Text.Encoding.ASCII));
                    if (hex.Length > 20) {
                        checksumField = field.Key;
                        Logger.Debug($"Found checksum field at {checksumField}! Checksum:{value.ToString(System.Text.Encoding.ASCII)}");
                    }
                } catch { }
            }
        }

        empty.GetUnknownFieldSet()!.GetUnknownFieldObject(checksumField)!.SetLengthDelimited(ByteString.CopyFromUtf8(_config.ReplaceWith));
        @event.SetBody(empty);
    }
}
