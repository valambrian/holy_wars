/// <summary>
/// Multiple factions can belong to the same race (especially minor factions)
/// </summary>

using System.Collections.Generic;

public class Race
{
    // serializable data
    private RaceData _data;

    // cached data
    // by default, a province with dwellers of this race can train these units
    private List<UnitType> _racialUnits;
    // old unit type => new unit type upon reaching age of magic
    private Dictionary<UnitType, UnitType> _magicUpgrades;
    // old unit type => new unit type upon reaching age of divine
    private Dictionary<UnitType, UnitType> _divineUpgrades;
    // a unit for each of these unit types is added upon reaching age of divine
    private List<UnitType> _divineAdditions;

    // used as a string identifier
    private string _lowercaseName;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Serialized race data</param>
    /// <param name="unitTypes">Hash of unit type id => unit type of racial units</param>
    public Race(RaceData data, Dictionary<int, UnitType> unitTypes)
    {
        _data = data;
        _lowercaseName = _data.name.ToLower();

        _racialUnits = new List<UnitType>();
        for (int i = 0; i < _data.units.Length; i++)
        {
            UnitType unitType = unitTypes[_data.units[i]];
            if (unitType != null)
            {
                _racialUnits.Add(unitType);
            }
        }
        _magicUpgrades = new Dictionary<UnitType, UnitType>();
        if (_data.magicUpgrades != null)
        {
            for (int i = 0; i < _data.magicUpgrades.Length; i++)
            {
                IntIntPair upgrade = _data.magicUpgrades[i];
                _magicUpgrades[unitTypes[upgrade.first]] = unitTypes[upgrade.second];
            }
        }

        _divineAdditions = new List<UnitType>();
        if (_data.divineAdditions != null)
        {
            for (int i = 0; i < _data.divineAdditions.Length; i++)
            {
                int newUnit = _data.divineAdditions[i];
                _divineAdditions.Add(unitTypes[newUnit]);
            }
        }

        _divineUpgrades = new Dictionary<UnitType, UnitType>();
        if (_data.divineUpgrades != null)
        {
            for (int i = 0; i < _data.divineUpgrades.Length; i++)
            {
                IntIntPair upgrade = _data.divineUpgrades[i];
                _divineUpgrades[unitTypes[upgrade.first]] = unitTypes[upgrade.second];
            }
        }
    }

    /// <summary>
    /// Get race id
    /// </summary>
    /// <returns>Race id</returns>
    public int GetId()
    {
        return _data.id;
    }

    /// <summary>
    /// Get race name converted to lower case
    /// </summary>
    /// <returns>Lowercased race name</returns>
    public string GetNameLowercased()
    {
        return _lowercaseName;
    }

    /// <summary>
    /// Get race's bonus to province income
    /// </summary>
    /// <returns>Racial bonus to income</returns>
    public int GetIncomeBonus()
    {
        return _data.incomeBonus;
    }

    /// <summary>
    /// Get race's bonus to province manpower
    /// </summary>
    /// <returns>Racial bonus to province manpower</returns>
    public int GetManpowerBonus()
    {
        return _data.manpowerBonus;
    }

    /// <summary>
    /// Get race's bonus to number of divine favor points collected from a province
    /// </summary>
    /// <returns>Racial bonus to province divine favors</returns>
    public int GetFavorBonus()
    {
        return _data.favorBonus;
    }

    /// <summary>
    /// Get a list of unit type the race can produce
    /// </summary>
    /// <returns>List of racial unit types</returns>
    public List<UnitType> GetRacialUnits()
    {
        return _racialUnits;
    }

    /// <summary>
    /// Get a hash of unit type upgrades to apply on reaching the Age of Magic
    /// </summary>
    /// <returns>Hash of existing unit type => upgraded unit type for the Age of Magic</returns>
    public Dictionary<UnitType, UnitType> GetAgeOfMagicUnitTypeUpgrades()
    {
        return _magicUpgrades;
    }

    /// <summary>
    /// Get a hash of unit type upgrades to apply on reaching the Age of Divine
    /// </summary>
    /// <returns>Hash of existing unit type => upgraded unit type for the Age of Divine</returns>
    public Dictionary<UnitType, UnitType> GetAgeOfDivineUnitTypeUpgrades()
    {
        return _divineUpgrades;
    }

    /// <summary>
    /// Get a hash of unit type to appear on reaching the Age of Divine
    /// </summary>
    /// <returns>List of unit types joining factions of this race upon reaching the Age of Divine</returns>
    public List<UnitType> GetUnitTypesJoiningForAgeOfDivine()
    {
        return _divineAdditions;
    }

    /// <summary>
    /// Can a faction of this race bribe units of opposing factions?
    /// </summary>
    /// <returns>Whether a faction of this race can bribe units of opposing factions</returns>
    public bool CanBribe()
    {
        return _data.canBribe;
    }
}
