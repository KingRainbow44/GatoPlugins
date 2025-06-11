using System.Runtime.InteropServices;
using System.Text;
using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy;
using FreakyProxy.PacketProcessor;
using Google.Protobuf;

namespace Windblade;

public static class Windblade {
    [DllImport("gc-windy", CharSet = CharSet.Ansi)]
    private static extern int compile(string script, nuint output);

    /// <summary>
    /// These bytes always appear at the beginning of a Lua bytecode file.
    /// </summary>
    public static readonly byte[] LuaHeader = [0x1b, 0x4c, 0x75, 0x61, 0x53];

    /// <summary>
    /// Compares the first few bytes of a script to the Lua bytecode header.
    /// </summary>
    public static bool IsBytecode(byte[] script) {
        if (script.Length < LuaHeader.Length)
            return false;

        return !LuaHeader.Where((t, i) => script[i] != t).Any();
    }

    /// <summary>
    /// Executes a script from a file path.
    /// </summary>
    public static async Task<bool> ExecuteScript(Session session, string path) {
        try {
            var scriptPath = Plugin.Instance!.FilePath(path);
            var scriptFile = await File.ReadAllBytesAsync(scriptPath);

            // Determine if the script is a Lua script or a bytecode.
            var bytecode = scriptFile;
            if (!IsBytecode(scriptFile)) {
                bytecode = Compile(Encoding.UTF8.GetString(scriptFile));
            }

            // Execute the script.
            var packet = new PlayerNormalLuaShellNotify {
                Luashell = ByteString.CopyFrom(bytecode)
            };
            await session.SendClient(CmdID.PlayerNormalLuaShellNotify, packet);

            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    /// Executes a script from a string.
    /// </summary>
    public static bool Execute(Session session, string script) {
        try {
            var bytecode = Compile(script);
            Execute(session, bytecode);

            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    /// Executes a Lua script.
    /// </summary>
    public static async void Execute(Session session, byte[] payload) {
        var packet = new PlayerNormalLuaShellNotify {
            Luashell = ByteString.CopyFrom(payload)
        };
        await session.SendClient(CmdID.PlayerNormalLuaShellNotify, packet);
    }

    /// <summary>
    /// Compiles a Lua script into Lua bytecode.
    /// </summary>
    public static byte[] Compile(string lua) {
        var output = Marshal.AllocHGlobal(4096);
        var length = compile(lua, (nuint)output);

        var bytecode = new byte[length];
        Marshal.Copy(output, bytecode, 0, length);
        Marshal.FreeHGlobal(output);

        return bytecode;
    }
}

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static Plugin? Instance;

    public override void OnLoad() {
        Instance = this;

        CommandProcessor.RegisterAllCommands("Windblade");

        Logger.Info("Windblade plugin loaded.");
    }

    public override void OnUnload() {
        Instance = null;
    }
}
