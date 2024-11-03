using System.Text.RegularExpressions;
using Common.DataStructures;

namespace Exporter;

/// <summary>
/// A helper utility for converting internal values to GOOD values.
/// </summary>
public static partial class GoodHelper {
    /// <summary>
    /// Converts a name to pascal case.
    /// </summary>
    public static string Convert(string name) {
        return string.Concat(NameRegex().Matches(name));
    }

    /// <summary>
    /// Converts a 'FightProperty' into a GOOD key.
    /// </summary>
    public static string Convert(FightProperty property) {
        return property switch {
            FightProperty.HP => "hp",
            FightProperty.HPPercent => "hp_",
            FightProperty.Attack => "atk",
            FightProperty.AttackPercent => "atk_",
            FightProperty.Defense => "def",
            FightProperty.DefensePercent => "def_",
            FightProperty.ElementMastery => "eleMas",
            FightProperty.ChargeEfficiency => "enerRech_",
            FightProperty.Critical => "critRate_",
            FightProperty.CriticalHurt => "critDMG_",
            FightProperty.PhysicalAddHurt => "physical_dmg_",
            FightProperty.WindAddHurt => "anemo_dmg_",
            FightProperty.RockAddHurt => "geo_dmg_",
            FightProperty.ElecAddHurt => "electro_dmg_",
            FightProperty.WaterAddHurt => "hydro_dmg_",
            FightProperty.FireAddHurt => "pyro_dmg_",
            FightProperty.IceAddHurt => "cryo_dmg_",
            FightProperty.GrassAddHurt => "dendro_dmg_",
            FightProperty.HealAdd => "heal_",
            _ => throw new Exception("Property is not mapped to a GOOD key")
        };
    }

    /// <summary>
    /// Converts an equip slot into a GOOD key.
    /// </summary>
    public static string Convert(EquipType equip) {
        return equip switch {
            EquipType.Bracer => "flower",
            EquipType.Necklace => "plume",
            EquipType.Shoes => "sands",
            EquipType.Ring => "goblet",
            EquipType.Dress => "circlet",
            _ => "unknown"
        };
    }

    [GeneratedRegex("[A-z]")]
    private static partial Regex NameRegex();
}

/// <summary>
/// The 'GOOD' or 'Genshin Open Object Description' format uses PascalCase.
/// </summary>
public class GoodFormat {
    /// <summary>
    /// This is a constant which is always 'GOOD'.
    /// </summary>
    public readonly string Format = "GOOD";

    /// <summary>
    /// This is a constant which is always 'GatoProxy'.
    /// </summary>
    public readonly string Source = "GatoProxy";

    /// <summary>
    /// This is a constant which is always '2'.
    /// </summary>
    public readonly uint Version = 2;

    /// <summary>
    /// A list of avatar/character objects.
    /// </summary>
    public List<Character>? Characters;

    /// <summary>
    /// A list of relic/artifact objects.
    /// </summary>
    public List<Artifact>? Artifacts;

    /// <summary>
    /// A list of weapon objects.
    /// </summary>
    public List<Weapon>? Weapons;

    /// <summary>
    /// A dictionary of materials.
    /// Key = Material Name
    /// Value = Quantity
    /// </summary>
    public Dictionary<string, uint>? Materials;
}

public struct Character {
    /// <summary>
    /// The avatar name.
    /// </summary>
    public string Key = "Rosaria";

    /// <summary>
    /// The avatar's level.
    /// </summary>
    public uint Level = 1;

    /// <summary>
    /// The avatar's constellation level.
    /// </summary>
    public ushort Constellation = 0;

    /// <summary>
    /// The avatar's ascension level.
    /// </summary>
    public ushort Ascension = 0;

    /// <summary>
    /// The avatar's talent levels.
    /// See default value for potential keys.
    /// Value is inclusive 1-15.
    /// Does not include constellation boosts.
    /// </summary>
    public readonly Dictionary<string, ushort> Talent = new() {
        {"auto", 1},
        {"skill", 1},
        {"burst", 1}
    };

    public Character() { }
}

public struct SubStat {
    /// <summary>
    /// The statistic key.
    /// </summary>
    public string Key = "critDMG_";

    /// <summary>
    /// The statistic value.
    /// </summary>
    public float Value = 19.4f;

    public SubStat() { }
}

public struct Artifact {
    /// <summary>
    /// The artifact set type.
    /// </summary>
    public string SetKey = "GladiatorsFinale";

    /// <summary>
    /// The artifact slot type.
    /// </summary>
    public string SlotKey = "plume";

    /// <summary>
    /// The artifact's level.
    /// </summary>
    public uint Level = 0;

    /// <summary>
    /// The artifact's rarity.
    /// Value is inclusive 1-5.
    /// </summary>
    public ushort Rarity = 1;

    /// <summary>
    /// The artifact's main statistic type.
    /// </summary>
    public string MainStatKey = "critDMG_";

    /// <summary>
    /// The avatar which this artifact is equipped to.
    /// An empty string correlates to not equipped.
    /// </summary>
    public string Location = "";

    /// <summary>
    /// An internal value used by the game for differentiating game objects.
    /// </summary>
    public ulong Guid = 0;

    /// <summary>
    /// Whether the artifact is locked.
    /// </summary>
    public bool Lock = false;

    /// <summary>
    /// A list of the artifact's other statistics.
    /// </summary>
    public List<SubStat> Substats = [];

    public Artifact() { }
}

public struct Weapon {
    /// <summary>
    /// The weapon name.
    /// </summary>
    public string Key = "CrescentPike";

    /// <summary>
    /// The weapon's level.
    /// </summary>
    public uint Level = 1;

    /// <summary>
    /// The weapon's ascension level.
    /// </summary>
    public ushort Ascension = 0;

    /// <summary>
    /// The weapon's refinement level.
    /// </summary>
    public ushort Refinement = 1;

    /// <summary>
    /// The avatar which this weapon is equipped to.
    /// An empty string correlates to not equipped.
    /// </summary>
    public string Location = "";

    /// <summary>
    /// An internal value used by the game for differentiating game objects.
    /// </summary>
    public ulong Guid = 0;

    /// <summary>
    /// Whether the weapon is locked.
    /// </summary>
    public bool Lock = false;

    public Weapon() { }
}
