using Common.Protocol;
using Common.Protocol.Proto;
using Common.Util;
using FreakyProxy.Game;

namespace WorldSync;

public class SynchronizedWorld : World {
    /// <summary>
    /// Player UID -> Current Avatar Entity ID
    /// </summary>
    private readonly Dictionary<uint, uint> _avatars = new();

    /// <summary>
    /// Adds the player to the world.
    /// This also virtually adds them to other clients.
    /// </summary>
    public override async void AddPlayer(Player player) {
        try {
            base.AddPlayer(player); // Add the player to the world.

            // Skip the remaining steps if the world hasn't finished initializing.
            if (State != WorldState.Finished) return;

            await SyncWorldData(player); // Send the world data to the player.
            await SyncPlayerList(); // Synchronize the players in the world.
            // TODO: Spawn others all at the same time.
            await SpawnPlayer(player, true); // Spawn the player to other clients.
            await SyncTeam(); // Synchronize the team across clients.
        }
        catch (Exception ex) {
            Logger.Warning($"Failed to add player: {ex}");
        }
    }

    /// <summary>
    /// Invoked when the world ticks.
    /// </summary>
    public override async Task OnTick() {
        await base.OnTick();

        #region WorldPlayerLocationNotify

        var worldLocPacket = new WorldPlayerLocationNotify();

        // Add all players to the locations.
        foreach (var player in this) {
            var location = player.Location;

            // Add world location data.
            worldLocPacket.PlayerLocList.Add(location);
            worldLocPacket.PlayerWorldLocList.Add(new PlayerWorldLocationInfo {
                PlayerLoc = location,
                SceneId = player.SceneId
            });
        }

        // Send the packet to players.
        await BroadcastClients(CmdID.WorldPlayerLocationNotify, worldLocPacket);

        #endregion
    }

    /// <summary>
    /// Sends world data to the player.
    /// </summary>
    private async Task SyncWorldData(Player player) {
        #region WorldDataNotify

        var worldDataPacket = new WorldDataNotify {
            WorldPropMap = {
                {
                    1, new PropValue {
                        Ival = Host?.WorldLevel ?? 9,
                        Val = Host?.WorldLevel ?? 9,
                        Type = 1
                    }
                },
                {
                    2, new PropValue {
                        Ival = IsMultiplayer ? 1 : 0,
                        Val = IsMultiplayer ? 1 : 0,
                        Type = 2
                    }
                }
            }
        };

        await player.Session.SendClient(CmdID.WorldDataNotify, worldDataPacket);

        #endregion
    }

    /// <summary>
    /// Broadcasts the players in the world to clients.
    /// All clients are considered player 1.
    /// </summary>
    private async Task SyncPlayerList() {
        // Prepare the general player list.
        var listPacket = new WorldPlayerInfoNotify();
        Players.ForEach(p => {
            listPacket.PlayerUidList.Add(p.Uid);
            listPacket.PlayerInfoList.Add(p.Session.SocialModule.ToOnlinePlayer());
        });

        foreach (var player in this) {
            var session = player.Session;
            var infoPacket = new ScenePlayerInfoNotify();

            // Add the self player.
            infoPacket.PlayerInfoList.Add(new ScenePlayerInfo {
                Uid = player.Uid,
                Name = player.Nickname,
                PeerId = 1,
                SceneId = player.SceneId,
                OnlinePlayerInfo = session.SocialModule.ToOnlinePlayer()
            });

            // For the remaining players, we add them incrementally.
            uint nextPeerId = 2;
            foreach (var otherPlayer in this) {
                if (player.Equals(otherPlayer)) continue;

                infoPacket.PlayerInfoList.Add(new ScenePlayerInfo {
                    Uid = otherPlayer.Uid,
                    Name = otherPlayer.Nickname,
                    PeerId = nextPeerId++,
                    SceneId = otherPlayer.SceneId,
                    OnlinePlayerInfo = otherPlayer.Session.SocialModule.ToOnlinePlayer()
                });
            }

            // Send the info packet to the player.
            await session.SendClient(CmdID.ScenePlayerInfoNotify, infoPacket);
            await session.SendClient(CmdID.WorldPlayerInfoNotify, listPacket);
        }
    }

    /// <summary>
    /// Spawns this player to other clients.
    /// </summary>
    /// <param name="player">The player to spawn.</param>
    /// <param name="firstSpawn">Is this the first time the player is being spawned?</param>
    private async Task SpawnPlayer(Player player, bool firstSpawn = false) {
        var avatar = player.SelectedAvatar.NotNull("Player does not have an avatar selected");

        var spawnPacket = new SceneEntityAppearNotify {
            AppearType = firstSpawn ? VisionType.Born : VisionType.Replace,
            EntityList = { avatar.Info },
            Param = firstSpawn ? 0 : _avatars[player.Uid]
        };
        await BroadcastClients(CmdID.SceneEntityAppearNotify, spawnPacket, [player]);

        _avatars[player.Uid] = avatar.Id;
    }

    /// <summary>
    /// Re-sends a 'SceneTeamUpdateNotify' packet to all players in the world.
    /// </summary>
    private async Task SyncTeam() {
        // Each player has a unique SceneTeamUpdate notify packet.
        // The avatars for other players need to match with the peer IDs assigned.
        foreach (var player in Players) {
            var sceneTeamPacket = new SceneTeamUpdateNotify { IsInMp = true };

            // For the player's own avatars, they can be added without modification.
            foreach (var avatar in player.Session.AvatarModule.Avatars) {
                avatar.SceneEntityInfo.Avatar.PeerId = 1;
                sceneTeamPacket.SceneTeamAvatarList.Add(avatar);
            }

            // For remaining players, we will need to do peer ID synchronization.
            uint nextPeerId = 2;
            foreach (var other in this) {
                if (player.Equals(other)) continue;

                var peerId = nextPeerId++;
                foreach (var copy in other.Session.AvatarModule.Avatars
                             .Select(avatar => new SceneTeamAvatar(avatar))) {
                    copy.SceneEntityInfo.Avatar.PeerId = peerId;

                    sceneTeamPacket.SceneTeamAvatarList.Add(copy);
                }
            }

            await player.Session.SendClient(CmdID.SceneTeamUpdateNotify, sceneTeamPacket);
        }
    }
}
