using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy;
using FreakyProxy.Events;
using Timer = System.Timers.Timer;

namespace DeathSwap;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    public static bool Running;

    public static DeathSwapPlayer? Source, Target;

    public static readonly Dictionary<string, string> Accounts = new();
    public static readonly Dictionary<string, GetPlayerTokenReq> Tokens = new();
    public static readonly Dictionary<string, PlayerLoginReq> Logins = new();

    private static Config _config;

    public override void OnLoad() {
        _config = this.GetConfig(new Config());
        CommandProcessor.RegisterAllCommands("DeathSwap");
        PluginManager.AddEventListener<PlayerCreationEvent>(OnPlayerCreationEvent);

        Logger.Info("DeathSwap plugin loaded.");
    }

    public override void OnEnable() {
        RunTask();
    }

    public override void OnUnload() {
        Running = false;
    }

    /// <summary>
    /// Starts a repeated task for the death swap game.
    /// </summary>
    public static void RunTask() {
        Task.Run(async () => {
            // Wait a random duration of time.
            var span = TimeSpan.FromMilliseconds(
                Utils.Random(_config.MinInterval, _config.MaxInterval));
            await Task.Delay(span);

            if (Running &&
                Source is not null &&
                Target is not null) {
                // Run the swap function.
                await Source.SwapWith(Target);

                // Clear the source and target.
                // These should be re-populated by the next interval.
                Source = null;
                Target = null;
            }

            // Re-run the loop.
            RunTask();
        });
    }

    /// <summary>
    /// Invoked when the player is created.
    /// We change the player type to our own.
    /// </summary>
    private static void OnPlayerCreationEvent(PlayerCreationEvent @event) {
        @event.PlayerType = typeof(DeathSwapPlayer);
    }
}
