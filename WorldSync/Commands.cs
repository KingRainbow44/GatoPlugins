using Common.Util;
using FreakyProxy.Commands;

namespace WorldSync.Commands;

public static class Commands {
    private const string SyncUsage = "sync <player>";

    [Command("sync", SyncUsage, "Synchronize your avatar with another player.")]
    public static async Task Sync(ICommandSender sender, string[] args) {
        var source = sender.AsPlayer();

        // Resolve the target player.
        if (args.Length == 0) {
            await sender.SendMessage($"Usage: {SyncUsage}");
            return;
        }
        var target = args[0].ParsePlayer();

        // Fetch the target's avatar.
        if (target.SelectedAvatar is not { } avatar) {
            await sender.SendMessage("Target player does not have an avatar selected.");
            return;
        }

        // Transition the host into a multiplayer world.
        var world = source.World.NotNull("Player is not in a world");
        if (!world.IsMultiplayer) {
            await world.ConvertToCoop();
        }

        // Add the target player to the source world.
        world.AddPlayer(target);

        await sender.SendMessage("Attempting to synchronize players...");

        // // Create the avatar clone.
        // var @new = Synced.CloneAvatar(player, target, avatar);
        //
        // // Update the team.
        // var avatars = player.Session.AvatarModule.Avatars;
        // var sceneTeamPacket = new SceneTeamUpdateNotify {
        //     IsInMp = true,
        //     SceneTeamAvatarList = { avatars }
        // };
        // sceneTeamPacket.SceneTeamAvatarList.Add(@new.ToTeamAvatar(true));
        //
        // await player.Session.SendClient(CmdID.SceneTeamUpdateNotify, sceneTeamPacket);
        //
        // // Spawn the avatar clone.
        // var scene = player.Scene.NotNull("You are not in a scene");
        // scene.AddEntity(@new);
    }
}
