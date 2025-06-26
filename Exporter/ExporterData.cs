using System.Text.Json;
using Common.DataStructures;
using Common.Protocol.Proto;
using FreakyProxy.Data;
using FreakyProxy.Game;
using FreakyProxy.PacketProcessor;

namespace Exporter;

public class ExporterData(Player player, Session session) : IPlayerData {
    private readonly GoodFormat _data = new();
    public Player Player => player;
    public Session Session => session;


    /// <summary>
    /// Converts an 'AvatarInfo' message into a GOOD character object.
    /// </summary>
    public void AddAvatar(AvatarInfo avatar) {
        // Check if the list exists in the data object.
        _data.Characters ??= [];

        // Look-up avatar game data.
        var gameData = GameData.AvatarData[avatar.AvatarId];
        var skillData = GameData.AvatarSkillDepotData[gameData.SkillDepotId];

        // Resolve avatar info.
        var propMap = avatar.PropMap;
        var talentMap = avatar.SkillLevelMap;
         var @object = new Character {
            Key = GoodHelper.Convert(GameData.TextMap[gameData.NameTextMapHash]),
            Level = (uint) propMap[(uint) PlayerProperty.PROP_LEVEL].Val,
            Ascension = (ushort) propMap[(uint) PlayerProperty.PROP_BREAK_LEVEL].Val,
            Constellation = (ushort) avatar.TalentIdList.Count,
            Talent = {
                {"auto", (ushort) talentMap[skillData.Skills[0]]},
                {"skill", (ushort) talentMap[skillData.Skills[1]]},
                {"burst", (ushort) talentMap[skillData.EnergySkill]},
            }
        };

        _data.Characters.Add(@object);
        ResolveLocations(avatar, @object);
    }

    /// <summary>
    /// Resolves the item locations for an avatar.
    /// </summary>
    public void ResolveLocations(AvatarInfo avatar, Character character) {
        var key = character.Key;

        foreach (var guid in avatar.EquipGuidList) {
            if (_data.Artifacts is { } artifacts) {
                var artifact = artifacts.Find(a => a.Guid.Equals(guid));
                if (artifact is not null) {
                    artifact.Location = key;
                }
            }

            if (_data.Weapons is { } weapons) {
                var weapon = weapons.Find(w => w.Guid.Equals(guid));
                if (weapon is not null) {
                    weapon.Location = key;
                }
            }
        }
    }

    /// <summary>
    /// Automatic method to add an item to the GOOD database.
    /// </summary>
    public void AddItem(Item item) {
        if (item.Equip is { } equip) {
            if (equip.Reliquary is not null) {
                AddReliquary(equip, item.Guid, item.ItemId);
            } else if (equip.Weapon is not null) {
                AddWeapon(equip, item.Guid, item.ItemId);
            }
        } else if (item.Material is { } material) {
            AddMaterial(material, item.ItemId);
        }
    }

    /// <summary>
    /// Converts a reliquary into a GOOD artifact object.
    /// </summary>
    public void AddReliquary(Equip item, ulong guid, uint itemId) {
        // Check if the list exists in the data object.
        _data.Artifacts ??= [];

        // Look-up artifact game data.
        var relic = item.Reliquary;
        var gameData = GameData.ReliquaryData[itemId];
        var mainPropData = GameData.ReliquaryMainPropData[relic.MainPropId];

        // Skip < Epic artifacts.
        if (gameData.RankLevel < 3) return;

        // Resolve artifact info.
        var @object = new Artifact {
            SetKey = Plugin.RelicSetNames[gameData.SetId],
            SlotKey = GoodHelper.Convert(gameData.EquipType),
            Rarity = gameData.RankLevel,
            Level = relic.Level - 1,
            MainStatKey = GoodHelper.Convert(mainPropData.FightProp),
            Location = "",
            Guid = guid,
            Lock = item.IsLocked
        };

        // Resolve sub-stats info.
        var subStats = new Dictionary<string, float>();
        foreach (var propId in relic.AppendPropIdList) {
            var affixData = GameData.ReliquaryAffixData[propId];
            var propKey = GoodHelper.Convert(affixData.FightProp);

            var value = propKey.EndsWith('_') ? affixData.PropValue * 100 : affixData.PropValue;

            subStats.TryAdd(propKey, 0);
            subStats[propKey] += value;
        }

        foreach (var entry in subStats) {
            @object.Substats.Add(new SubStat {
                Key = entry.Key, Value = entry.Value
            });
        }

        _data.Artifacts.Add(@object);
    }

    /// <summary>
    /// Converts a weapon into a GOOD weapon object.
    /// </summary>
    public void AddWeapon(Equip item, ulong guid, uint itemId) {
        // Check if the list exists in the data object.
        _data.Weapons ??= [];

        // Look-up weapon game data.
        var gameData = GameData.WeaponData[itemId];

        // Resolve weapon info.
        var weapon = item.Weapon;

        var refinement = (ushort) 0;
        if (weapon.AffixMap.Count > 0) {
            refinement = (ushort)(Convert.ToUInt16(weapon.AffixMap.Values.First()) + 1);
        }

        var @object = new Weapon {
            Key = GoodHelper.Convert(GameData.TextMap[gameData.NameTextMapHash]),
            Level = weapon.Level,
            Ascension = (ushort) weapon.PromoteLevel,
            Refinement = refinement,
            Location = "",
            Guid = guid,
            Lock = item.IsLocked
        };

        _data.Weapons.Add(@object);
    }

    /// <summary>
    /// Adds a material into the GOOD materials dictionary.
    /// </summary>
    public void AddMaterial(Material material, uint itemId) {
        // Check if the list exists in the data object.
        _data.Materials ??= [];

        // Look-up the material name.
        var gameData = GameData.MaterialData[itemId];
        var key = GoodHelper.Convert(GameData.TextMap[gameData.NameTextMapHash]);

        // Set the count.
        _data.Materials[key] = material.Count;
    }

    /// <summary>
    /// Writes the GOOD data to a file.
    /// </summary>
    public async Task WriteToFile() {
        var fileInfo = new FileInfo($"{Plugin.OutputDirectory}/{player.Uid}.json");
        if (fileInfo.Exists) {
            File.Delete(fileInfo.FullName);
        }

        var serialized = JsonSerializer.Serialize(_data, Plugin.JsonOptions);
        await File.WriteAllTextAsync(fileInfo.FullName, serialized);
    }
}
