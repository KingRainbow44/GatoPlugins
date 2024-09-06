using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;

namespace DeathSwap;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static bool Override = false;

    public static readonly Dictionary<string, string> Accounts = new();
    public static readonly Dictionary<string, GetPlayerTokenReq> Tokens = new();
    public static readonly Dictionary<string, PlayerLoginReq> Logins = new();

    public override void OnLoad() {
        CommandProcessor.RegisterAllCommands("DeathSwap");

        #region Register Events

        PluginManager.AddEventListener<PlayerCreationEvent>(OnPlayerCreationEvent);

        #endregion

        Logger.Info("DeathSwap plugin loaded.");
    }

    /// <summary>
    /// Invoked when the player is created.
    /// We change the player type to our own.
    /// </summary>
    private static void OnPlayerCreationEvent(PlayerCreationEvent @event) {
        @event.PlayerType = typeof(DeathSwapPlayer);
    }
}
