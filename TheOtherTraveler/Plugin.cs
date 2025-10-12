using Common.DataStructures;
using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy;
using FreakyProxy.Data;
using FreakyProxy.PacketProcessor;
using Packando;

namespace TheOtherTraveler;

public class Plugin(PluginInfo info) : FreakyProxy.Plugin(info) {
    private static Plugin? _instance;

    private const uint
        TravelerMale = 10000005,
        TravelerFemale = 10000007;

    private static readonly uint[] Travelers = [TravelerMale, TravelerFemale];

    public override void OnLoad() {
        _instance = this;
        Logger.Info("The Other Traveler plugin loaded.");
    }

    public override void OnUnload() {
        _instance = null;
    }

    /// <summary>
    /// Translates all avatar info to the new avatar.
    /// </summary>
    private static void UpdateAvatarInfo(AvatarInfo info) {
        var oldAvatarId = info.AvatarId;
        if (!Travelers.Contains(oldAvatarId)) return;

        info.ExcelInfo = null;

        // Determine the new avatar ID.
        var newAvatarId = info.AvatarId = oldAvatarId switch {
            TravelerMale => TravelerFemale,
            TravelerFemale => TravelerMale,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Get current skill depot index.
        var oldAvatarConfig = GameData.AvatarData[oldAvatarId];

        var oldDepotId = info.SkillDepotId;
        var depotIndex = oldAvatarConfig.CandSkillDepotIds.IndexOf(oldDepotId);

        // Now that we have the index for the old avatar, we need to find the new one.
        // We do this by getting the candidate skill depot IDs for the new avatar and
        // selecting the one at the same index as the old one.
        var newDepotId = GameData.AvatarData[newAvatarId].CandSkillDepotIds[depotIndex];
        var skillDepot = GameData.AvatarSkillDepotData[newDepotId];

        // Update all skills.
        var promoteLevel = info.PropMap[(uint)PlayerProperty.PROP_BREAK_LEVEL];
        if (promoteLevel is null) {
            _instance?.Logger.Error($"Promote (ascension) level was not found for {info.Guid}.");
            return;
        }

        skillDepot.AllSkills().ForEach(id =>
            info.SkillLevelMap[id] = 1);

        info.InherentProudSkillList.Clear();
        skillDepot.PassiveSkills
            .Where(data => data.ProudSkillGroupId > 0)
            .Where(data => data.NeedAvatarPromoteLevel < promoteLevel.Val)
            .Select(data => data.ProudSkillGroupId * 100 + 1)
            .ToList()
            .ForEach(id => info.InherentProudSkillList.Add(id));

        info.TalentIdList.Clear();
        info.TalentIdList.AddRange(skillDepot.Talents);

        info.SkillLevelMap.Clear();
        info.ProudSkillExtraLevelMap.Clear();
        skillDepot.AllSkills().ForEach(skill => {
            info.SkillLevelMap[skill] = 11;
        });
    }

    /// <summary>
    /// Translates all avatar info to the new avatar.
    /// </summary>
    private static void UpdateSceneAvatarInfo(SceneAvatarInfo info) {
        var oldAvatarId = info.AvatarId;
        if (!Travelers.Contains(oldAvatarId)) return;

        info.ExcelInfo = null;

        // Determine the new avatar ID.
        var newAvatarId = info.AvatarId = oldAvatarId switch {
            TravelerMale => TravelerFemale,
            TravelerFemale => TravelerMale,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Get current skill depot index.
        var oldAvatarConfig = GameData.AvatarData[oldAvatarId];

        var oldDepotId = info.SkillDepotId;
        var depotIndex = oldAvatarConfig.CandSkillDepotIds.IndexOf(oldDepotId);
        if (depotIndex == -1) {
            _instance?.Logger.Error($"Skill depot ID {oldDepotId} not found for avatar {oldAvatarId}.");
            return;
        }

        // Now that we have the index for the old avatar, we need to find the new one.
        // We do this by getting the candidate skill depot IDs for the new avatar and
        // selecting the one at the same index as the old one.
        var newDepotId = GameData.AvatarData[newAvatarId].CandSkillDepotIds[depotIndex];
        var skillDepot = GameData.AvatarSkillDepotData[newDepotId];

        // Update all skills.

        skillDepot.AllSkills().ForEach(id =>
            info.SkillLevelMap[id] = 1);

        info.InherentProudSkillList.Clear();
        skillDepot.PassiveSkills
            .Where(data => data.ProudSkillGroupId > 0)
            .Select(data => data.ProudSkillGroupId * 100 + 1)
            .ToList()
            .ForEach(id => info.InherentProudSkillList.Add(id));

        info.TalentIdList.Clear();
        info.TalentIdList.AddRange(skillDepot.Talents);

        info.SkillLevelMap.Clear();
        info.ProudSkillExtraLevelMap.Clear();
        skillDepot.AllSkills().ForEach(skill => {
            info.SkillLevelMap[skill] = 11;
        });
    }

    [Handler(CmdID.SceneEntityAppearNotify)]
    public static PacketResult HandleSceneEntityAppearNotify(Session session, PacketHead _,
        SceneEntityAppearNotify msg) {
        foreach (var entity in msg.EntityList) {
            if (entity.EntityType != ProtEntityType.ProtEntityAvatar) continue;

            var info = entity.Avatar;
            if (info is null) continue;

            UpdateSceneAvatarInfo(info);
        }

        return PacketResult.Intercept;
    }

    [Handler(CmdID.SceneTeamUpdateNotify)]
    public static PacketResult HandleSceneTeamUpdateNotify(Session session, PacketHead _,
        SceneTeamUpdateNotify msg) {
        foreach (var avatar in msg.SceneTeamAvatarList) {
            var avatarInfo = avatar?.SceneEntityInfo?.Avatar;
            if (avatarInfo is null) continue;

            UpdateSceneAvatarInfo(avatarInfo);
        }
        return PacketResult.Intercept;
    }

    [Handler(CmdID.AvatarDataNotify)]
    public static PacketResult HandleAvatarDataNotify(Session session, PacketHead _,
        AvatarDataNotify msg) {
        foreach (var avatar in msg.AvatarList) {
            var avatarId = avatar.AvatarId;
            if (!Travelers.Contains(avatarId)) continue;

            UpdateAvatarInfo(avatar);
        }

        return PacketResult.Intercept;
    }
}
