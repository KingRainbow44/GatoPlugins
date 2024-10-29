using FreakyProxy.Game;
using Synced = WorldSync.Sync;

namespace WorldSync;

public class SynchronizedPlayer(Player handle) {
    public readonly Dictionary<uint, uint> EntityMap = new();
}

public static class PlayerExtensions {
    /// <summary>
    /// Fetches the player with its synchonized state.
    /// </summary>
    public static SynchronizedPlayer Sync(this Player player) {
        return Synced.GetOrCreate(player);
    }
}
