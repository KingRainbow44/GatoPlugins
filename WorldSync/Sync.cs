using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy.Game;
using FreakyProxy.Game.Entities;
using FreakyProxy.PacketProcessor;

namespace WorldSync;

public static class Sync {
    private static readonly Dictionary<Player, SynchronizedPlayer> _players = new();

    /// <summary>
    /// A collection of entities to be synchronized.
    /// </summary>
    public static readonly Dictionary<uint, (Session, uint)> Entities = new();

    /// <summary>
    /// Gets or creates a synchronized player instance.
    /// </summary>
    public static SynchronizedPlayer GetOrCreate(Player player) {
        return _players.TryGetValue(player, out var value) ?
            value :
            _players[player] = new SynchronizedPlayer(player);
    }

    /// <summary>
    /// Creates an avatar clone for a player.
    /// </summary>
    public static Avatar CloneAvatar(Player receiver, Player owner, Avatar avatar) {
        var world = receiver.World.NotNull("Player does not have a world");
        var scene = receiver.Scene.NotNull("Player is not in a scene");

        // Generate avatar IDs.
        var copyAvatarId = world.NextEntityId(ProtEntityType.ProtEntityAvatar);
        var copyWeaponId = world.NextEntityId(ProtEntityType.ProtEntityWeapon);

        // Generate GUIDs.
        var copyAvatarGuid = receiver.NextGuid();
        var copyWeaponGuid = receiver.NextGuid();

        // Create an entity copy.
        var avatarInfo = avatar.AvatarInfo;

        var weaponCopy = new SceneWeaponInfo(avatarInfo.Weapon) {
            EntityId = copyWeaponId,
            Guid = copyWeaponGuid
        };
        var avatarCopy = new SceneAvatarInfo(avatarInfo) {
            Guid = copyAvatarGuid,
            BornTime = (uint)owner.SceneTime,
            PeerId = owner.PeerId,
            Uid = owner.Uid,
            Weapon = weaponCopy
        };
        var entityCopy = new SceneEntityInfo(avatar.Info) {
            Avatar = avatarCopy,
            EntityId = copyAvatarId
        };

        // Spawn the entity.
        var newAvatar = new Avatar(entityCopy);
        scene.AddEntity(newAvatar);

        return newAvatar;
    }
}
