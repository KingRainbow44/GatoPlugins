// ReSharper disable UnusedType.Global

using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Protocol;
using Common.Util;
using Fleck;
using FreakyProxy;
using FreakyProxy.Events;
using Google.Protobuf;
using Newtonsoft.Json;
using ProtoUntyped;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Visualizer;

public struct VisualizerMessage {
    [JsonProperty(PropertyName = "packetId")]
    public uint PacketId;

    [JsonProperty(PropertyName = "data")]
    public object PacketData;
}

public struct PacketData {
    [JsonProperty(PropertyName = "time")] public long Time;

    [JsonProperty(PropertyName = "source")]
    public string Source;

    [JsonProperty(PropertyName = "packetId")]
    public ushort PacketId;

    [JsonProperty(PropertyName = "packetName")]
    public string PacketName;

    [JsonProperty(PropertyName = "length")]
    public uint Length;

    [JsonProperty(PropertyName = "data")]
    public string Data;
}

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    private static readonly JsonSerializerOptions _options = new();

    private static readonly Dictionary<CmdID, Type> _packetMap = new();
    private static readonly Dictionary<string, CmdID> _nameMap = new();

    private static readonly List<IWebSocketConnection> _connections = [];
    private static readonly JsonFormatter _formatter = new(JsonFormatter.Settings.Default);

    public static bool HighlightedOnly = false;
    public static readonly List<CmdID> Highlighted = [];
    public static readonly List<CmdID> Blacklisted = [];

    private WebSocketServer? _server;

    public override void OnLoad() {
        _options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;

        // Initialize the packet map.
        AppDomain.CurrentDomain
            .GetAssemblies()
            .First(t => t.GetName().Name == "Common").GetTypes()
            .Where(t => t.Namespace == "Common.Protocol.Proto" && (
                t.Name.EndsWith("Req") ||
                t.Name.EndsWith("Notify") ||
                t.Name.EndsWith("Rsp")))
            .ToList()
            .ForEach(t => {
                if (Enum.TryParse(typeof(CmdID), t.Name, out var id)) {
                    _packetMap.Add((CmdID)id, t);
                }
            });

        foreach (var name in Enum.GetNames<CmdID>()) {
            var value = (CmdID)Enum.Parse(typeof(CmdID), name);
            _nameMap.Add(name, value);
        }

        // Read the configuration file.
        var config = this.GetConfig(new Config());
        Blacklisted.AddRange(config.Blacklisted
            .Select(name => _nameMap[name])
            .ToList());

        // Start the web socket server
        FleckLog.LogAction = (level, message, _) => {
            if (!config.EnableLogger) return;

            switch (level) {
                case LogLevel.Debug:
                    Logger.Debug(message);
                    break;
                case LogLevel.Warn:
                    Logger.Warn(message);
                    break;
                case LogLevel.Error:
                    Logger.Error(message);
                    break;
                case LogLevel.Info:
                    Logger.Info(message);
                    break;
                default:
                    throw new Exception("Unknown logging level.");
            }
        };
        _server = new WebSocketServer($"ws://{config.BindAddress}:{config.BindPort}");
        _server.Start(OnClientConnected);

        // Register commands.
        CommandProcessor.RegisterAllCommands("Visualizer");
        // Register event listeners.
        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);
        PluginManager.AddEventListener<SendPacketEvent>(OnSendPacket);

        Logger.Info("Visualizer plugin loaded.");
    }

    public override void OnUnload() {
        _server?.ListenerSocket.Close();
        _server?.Dispose();
    }

    /// <summary>
    /// Invoked when a client connects to the websocket server.
    /// </summary>
    private void OnClientConnected(IWebSocketConnection connection) {
        connection.OnOpen = () => {
            Logger.Info("Client connected.");
            _connections.Add(connection);
        };
        connection.OnClose = () => {
            Logger.Info("Client disconnected.");
            _connections.Remove(connection);
        };
        connection.OnMessage = message => OnMessage(connection, message);
    }

    /// <summary>
    /// Invoked when a message is received from a client.
    /// </summary>
    private static async void OnMessage(IWebSocketConnection connection, string message) {
        var decoded = JsonConvert.DeserializeObject<VisualizerMessage>(message);
        if (decoded.PacketId != 0) return;

        // Send back a handshake message.
        var handshake = JsonConvert.SerializeObject(new VisualizerMessage {
            PacketId = 0, PacketData = Utils.CurrentTime()
        });
        await connection.Send(handshake);
    }

    private static void OnReceivePacket(ReceivePacketEvent @event) {
        var packet = @event.Packet;

        // Check if the packet should be shown.
        if (!ShowPacket(packet)) return;

        // Check if the message can be parsed.
        string serialized;
        if (_packetMap.TryGetValue(packet.CmdID, out var type)) {
            var decoded = packet.Body.ParseFrom(type) as IMessage;
            serialized = _formatter.Format(decoded) ?? "{}";
        } else {
            var decoded = ProtoObject.Decode(packet.Body);
            serialized = JsonSerializer.Serialize(decoded.ToFieldDictionary(), _options);
        }

        // Send the message to all connected clients.
        var packetData = new PacketData {
            Time = Utils.CurrentTime(),
            Source = packet.Source.AsString(),
            PacketId = (ushort)packet.CmdID,
            PacketName = packet.CmdID.ToString(),
            Length = (uint)packet.Body.Length,
            Data = serialized
        };
        var message = JsonConvert.SerializeObject(new VisualizerMessage {
            PacketId = 1, PacketData = packetData
        });
        _connections.ForEach(c => c.Send(message));
    }

    private static void OnSendPacket(SendPacketEvent @event) {
        var packet = @event.Packet;

        // Check if the packet should be shown.
        if (!ShowPacket(packet)) return;

        var data = @event.Message;
        var serialized = _formatter.Format(data) ?? "{}";

        // Send the message to all connected clients.
        var packetData = new PacketData {
            Time = Utils.CurrentTime(),
            Source = packet.Source.AsString(),
            PacketId = (ushort)packet.CmdID,
            PacketName = packet.CmdID.ToString(),
            Length = (uint)packet.Body.Length,
            Data = serialized
        };
        var message = JsonConvert.SerializeObject(new VisualizerMessage {
            PacketId = 1, PacketData = packetData
        });
        _connections.ForEach(c => c.Send(message));
    }

    /// <summary>
    /// Checks to see if a packet should be visualized.
    /// </summary>
    private static bool ShowPacket(Packet packet) {
        if (HighlightedOnly && !Highlighted.Contains(packet.CmdID)) return false;
        return !Blacklisted.Contains(packet.CmdID);
    }
}
