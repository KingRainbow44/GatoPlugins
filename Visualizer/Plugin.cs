// ReSharper disable UnusedType.Global

using System.Collections;
using System.Text;
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

    [JsonProperty(PropertyName = "binary")]
    public string RawData;
}

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    private static readonly JsonSerializerOptions _options = new() {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private static readonly Dictionary<CmdID, Type> _packetMap = new();
    private static readonly Dictionary<string, CmdID> _nameMap = new();

    private static readonly ArrayList _connections = ArrayList.Synchronized([]);
    private static readonly JsonFormatter _formatter = new(JsonFormatter.Settings.Default);

    public static Plugin? Instance;
    public static bool HighlightedOnly, Obfuscated;
    public static readonly List<CmdID> Highlighted = [];
    public static readonly List<CmdID> Blacklisted = [];

    private Config _config = new();
    private WebSocketServer? _server;

    public override void OnLoad() {
        Instance = this;

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
        _config = this.GetConfig(new Config());
        Blacklisted.AddRange(_config.Blacklisted
            .Select(name => _nameMap[name])
            .ToList());
        Highlighted.AddRange(_config.Highlighted
            .Select(name => _nameMap[name])
            .ToList());
        HighlightedOnly = _config.HighlightedOnly;
        Obfuscated = _config.IsObfuscated;

        // Start the web socket server
        FleckLog.LogAction = (level, message, _) => {
            if (!_config.EnableLogger) return;

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
        _server = new WebSocketServer($"ws://{_config.BindAddress}:{_config.BindPort}");
        _server.Start(OnClientConnected);

        // Register commands.
        CommandProcessor.RegisterAllCommands("Visualizer");
        // Register event listeners.
        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);
        PluginManager.AddEventListener<SendPacketEvent>(OnSendPacket);

        Logger.Info("Visualizer plugin loaded.");
    }

    public override void OnUnload() {
        Instance = null;

        _server?.ListenerSocket.Close();
        _server?.Dispose();
    }

    /// <summary>
    /// Saves the configuration file.
    /// </summary>
    public void Save() {
        // Update the config values.
        _config.HighlightedOnly = HighlightedOnly;
        _config.Highlighted = Highlighted
            .Select(id => id.ToString())
            .ToList();
        _config.Blacklisted = Blacklisted
            .Select(id => id.ToString())
            .ToList();

        // Save the config to the file.
        this.SaveConfig(_config);
    }

    /// <summary>
    /// Invoked when a client connects to the websocket server.
    /// </summary>
    private void OnClientConnected(IWebSocketConnection connection) {
        connection.OnOpen = () => {
            Logger.Debug("Client connected.");
            _connections.Add(connection);
        };
        connection.OnClose = () => {
            Logger.Debug("Client disconnected.");
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
        if (!Obfuscated && _packetMap.TryGetValue(packet.CmdID, out var type)) {
            var decoded = packet.Body.ParseFrom(type) as IMessage;
            serialized = _formatter.Format(decoded) ?? "{}";
        } else {
            serialized = SerializeUnknown(packet.Body);
        }

        // Send the message to all connected clients.
        var packetId = (ushort)packet.CmdID;
        var packetData = new PacketData {
            Time = Utils.CurrentTime(),
            Source = packet.Source.AsString(),
            PacketId = packetId,
            PacketName = Obfuscated ?
                PacketUtils.NamePacket(packetId) :
                packet.CmdID.ToString(),
            Length = (uint)packet.Body.Length,
            Data = serialized,
            RawData = Convert.ToBase64String(packet.Body)
        };
        var message = JsonConvert.SerializeObject(new VisualizerMessage {
            PacketId = 1, PacketData = packetData
        });

        foreach (var connection in _connections) {
            if (connection is IWebSocketConnection c) c.Send(message);
        }
    }

    private static void OnSendPacket(SendPacketEvent @event) {
        var packet = @event.Packet;

        // Check if the packet should be shown.
        if (!ShowPacket(packet)) return;

        var data = @event.Message;
        var serialized = _formatter.Format(data) ?? "{}";

        // Send the message to all connected clients.
        var packetId = (ushort)packet.CmdID;
        var packetData = new PacketData {
            Time = Utils.CurrentTime(),
            Source = packet.Source.AsString(),
            PacketId = packetId,
            PacketName = Obfuscated ?
                PacketUtils.NamePacket(packetId) :
                packet.CmdID.ToString(),
            Length = (uint)packet.Body.Length,
            Data = serialized,
            RawData = Convert.ToBase64String(packet.Body)
        };
        var message = JsonConvert.SerializeObject(new VisualizerMessage {
            PacketId = 1, PacketData = packetData
        });

        foreach (var connection in _connections) {
            if (connection is IWebSocketConnection c) c.Send(message);
        }
    }

    /// <summary>
    /// Checks to see if a packet should be visualized.
    /// </summary>
    private static bool ShowPacket(Packet packet) {
        if (HighlightedOnly && !Highlighted.Contains(packet.CmdID)) return false;
        return !Blacklisted.Contains(packet.CmdID);
    }

    /// <summary>
    /// Serializes an unknown packet.
    /// </summary>
    private static string SerializeUnknown(byte[] data) {
        var decoded = ProtoObject.Decode(data);
        return JsonSerializer.Serialize(decoded.ToFieldDictionary(), _options);
    }
}

internal static class PacketUtils {
    private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Generates a unique name for the packet.
    /// </summary>
    /// <param name="packetId">The packet's numerical identifier.</param>
    /// <returns>A unique name for the packet.</returns>
    public static string NamePacket(ushort packetId) {
        var name = new StringBuilder();
        var random = new Random(packetId);

        for (var i = 0; i < 11; i++) {
            name.Append(CHARS[random.Next(0, CHARS.Length)]);
        }

        var idName = "";
        var idStr = packetId.ToString();
        foreach (var c in idStr) {
            var numerical = int.Parse(c.ToString());
            var letter = 'A';
            for (var i = 0; i < numerical; i++) {
                letter = CHARS[random.Next(0, CHARS.Length)];
            }

            name.Append(letter);

            // Append a direct translation of the ID -> name.
            idName += CHARS[numerical];
        }

        name.Append(idName);

        // Shorten the name to the last 11 characters.
        return name.Length > 11 ?
            name.ToString()[(name.Length - 11)..] :
            name.ToString();
    }
}
