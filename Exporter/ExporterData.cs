using System.Text.Json;
using Common.DataStructures;
using Common.Protocol.Proto;
using FreakyProxy.Data;
using FreakyProxy.Game;
using FreakyProxy.PacketProcessor;

namespace Exporter;

public class ExporterData(Player player, Session _) : IPlayerData {
    private readonly GoodFormat _data = new();

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
            Constellation = 0, // TODO: Resolve constellation level.
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
                artifacts
                    .Where(a => a.Guid.Equals(guid))
                    .ToList()
                    .ForEach(a => a.Location = key);
            }

            if (_data.Weapons is { } weapons) {
                weapons
                    .Where(w => w.Guid.Equals(guid))
                    .ToList()
                    .ForEach(w => w.Location = key);
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
        var gameData = GameData.ReliquaryData[itemId];
        var mainPropData = GameData.ReliquaryMainPropData[gameData.MainPropDepotId];

        // Skip < Epic artifacts.
        if (gameData.RankLevel < 3) return;

        // Resolve artifact info.
        var relic = item.Reliquary;
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
        var @object = new Weapon {
            Key = GoodHelper.Convert(GameData.TextMap[gameData.NameTextMapHash]),
            Level = weapon.Level,
            Ascension = (ushort) weapon.PromoteLevel,
            Refinement = (ushort) (Convert.ToUInt16(weapon.AffixMap[0]) + 1),
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

        var stream = new FileStream(fileInfo.FullName, FileMode.Create);
        await JsonSerializer.SerializeAsync(stream, _data, Plugin.JsonOptions);
    }
}
