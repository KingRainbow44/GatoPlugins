﻿using System.Reflection;
using Common.Protocol;
using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;
using FreakyProxy.PacketProcessor;
using Google.Protobuf;

namespace Packando;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    private readonly Dictionary<CmdID, Type> _packetMap = new();
    private readonly Dictionary<CmdID, List<(SendType, MethodInfo)>> _handlers = new();

    /// <summary>
    /// Returns a list of handlers for a packet.
    /// </summary>
    private List<(SendType, MethodInfo)> FindHandlers(CmdID packet) {
        if (_handlers.TryGetValue(packet, out var handlers))
            return handlers;

        // Add a new list if one doesn't exist.
        handlers = new List<(SendType, MethodInfo)>();
        _handlers[packet] = handlers;

        return handlers;
    }

    public override void OnLoad() {
        // Load all message types.
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

        PluginManager.AddEventListener<ReceivePacketEvent>(OnReceivePacket);
        PluginManager.AddEventListener<SendPacketEvent>(OnSendPacket);
    }

    public override void OnEnable() {
        // Register all packet handlers.
        AppDomain.CurrentDomain
            .GetAssemblies()
            .ToList()
            .ForEach(asm => asm
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.GetCustomAttributes<Handler>().Any()))
                .ToList()
                .ForEach(RegisterHandler));
    }

    #region Plugin APIs

    /// <summary>
    /// Internal method to register a packet handler.
    /// </summary>
    /// <param name="info">The method info of the packet handler method.</param>
    private void RegisterHandler(MethodInfo info) {
        var attribute = info.GetCustomAttribute<Handler>();
        if (attribute is null) return;

        RegisterHandler(attribute.ListenFor, attribute.PacketId, info);
    }

    /// <summary>
    /// Registers a packet handler.
    /// </summary>
    /// <param name="type">The type of packets to listen for.</param>
    /// <param name="packetId">The ID of the packet.</param>
    /// <param name="invoker">The method info of the packet handler method.</param>
    public void RegisterHandler(SendType type, CmdID packetId, MethodInfo invoker) {
        var handlers = FindHandlers(packetId);
        handlers.Add((type, invoker));

        _handlers[packetId] = handlers;
        Logger.Debug($"Registered handler for packet {packetId}.");
    }

    #endregion

    /// <summary>
    /// Invokes all packet handlers for a packet.
    /// </summary>
    private async ValueTask<(PacketResult, IMessage?)> InvokeHandlers(
        Session session, Packet packet, SendType type) {
        var result = PacketResult.Forward;

        // Parse the packet body.
        if (!_packetMap.TryGetValue(packet.CmdID, out var packetType)) {
            return (PacketResult.Forward, null);
        }
        var body = packet.Body.ParseFrom(packetType) as IMessage;

        // Invoke handlers.
        foreach (var (sendType, handler) in FindHandlers(packet.CmdID)) {
            if (sendType != SendType.All && sendType != type) continue;

            // The 'body' parameter is mutable between handlers.
            if (handler.Invoke(null, [session, packet.Metadata, body])
                is not ValueTask<PacketResult> invokeResult) continue;

            // Validate the result.
            switch (await invokeResult) {
                case PacketResult.Drop:
                    // We are ignoring the other handlers as someone chose to drop it.
                    return (PacketResult.Drop, null);
                case PacketResult.Intercept:
                    // The packet will be intercepted, it should not be changed.
                    if (result is PacketResult.Forward) {
                        result = PacketResult.Intercept;
                    }
                    break;
                case PacketResult.Forward:
                    // Nothing changes, we just continue to the next handler.
                    continue;
            }
        }

        return (result, body);
    }

    private async void OnReceivePacket(ReceivePacketEvent @event) {
        var (result, body) = await InvokeHandlers(
            @event.Session, @event.Packet, SendType.Server);

        switch (result) {
            case PacketResult.Drop:
                @event.Result = PacketResult.Drop;
                break;
            case PacketResult.Forward:
                // If all handlers agree to forward, we let the internal handler decide what to do.
                break;
            case PacketResult.Intercept when
                body is not null:
                @event.SetBody(body);
                break;
        }
    }

    private async void OnSendPacket(SendPacketEvent @event) {
        var (result, body) = await InvokeHandlers(
            @event.Session, @event.Packet, SendType.Proxy);

        switch (result) {
            case PacketResult.Drop:
                @event.Cancelled = true;
                break;
            case PacketResult.Intercept when
                body is not null:
                @event.Message = body;
                break;
        }
    }
}