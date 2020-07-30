/// <summary>
/// Unit type - the template of in-game units
/// </summary>

using System.Collections.Generic;

public class UnitType
{
    // serializable data
    private UnitTypeData _data;

    // cached data
    private List<Attack> _attacks;
    private List<Spell> _spells;
    // these are self-targeted spells
    private List<Spell> _spellLikeAbilities = new List<Spell>();

    private static string[] _names;
    private static Dictionary<string, UnitType> _availableUnitTypes = new Dictionary<string, UnitType>();

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Serialized unit type data</param>
    /// <param name="spells">Hash of spell id => spell</param>
    public UnitType(UnitTypeData data, Dictionary<Game.SpellId, Spell> spells)
    {
        _data = data;
        _attacks = new List<Attack>();
        if (data.attacks != null)
        {
            for (int i = 0; i < data.attacks.Length; i++)
            {
                _attacks.Add(new Attack(data.attacks[i]));
            }
        }
        _spells = new List<Spell>();
        if (data.spells != null)
        {
            for (int i = 0; i < data.spells.Length; i++)
            {
                _spells.Add(spells[(Game.SpellId)data.spells[i]]);
            }
        }
        RegisterUnitType(this);
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="other">The unit type to clone</param>
    public UnitType (UnitType other)
    {
        _data = new UnitTypeData(other._data);
        _attacks = new List<Attack>();
        // attacks can't be shared - they may get different qualities
        for (int i = 0; i < other._attacks.Count; i++)
        {
            _attacks.Add(new Attack(other._attacks[i].GetData()));
        }
        // spells and spell-like abilities can be shared (for now)
        _spells = new List<Spell>(other._spells);
        _spellLikeAbilities = new List<Spell>(other._spellLikeAbilities);
    }

    /// <summary>
    /// Add a unit type to the list of in-game unit types
    /// </summary>
    /// <param name="unitType">The unit type to add</param>
    /// <returns>Whether the unit type was successfully added</returns>
    public static bool RegisterUnitType(UnitType unitType)
    {
        if (_availableUnitTypes.ContainsKey(unitType._data.name))
        {
            return false;
        }
        _availableUnitTypes[unitType._data.name] = unitType;
        return true;
    }

    /// <summary>
    /// Get a unit type by its name
    /// </summary>
    /// <param name="name">Name of the unit type</param>
    /// <returns>Unit type with the name specified (or null if not found)</returns>
    public static UnitType GetUnitType(string name)
	{
		if (_availableUnitTypes.ContainsKey(name))
		{
			return _availableUnitTypes[name];
		}
		return null;
	}

    /// <summary>
    /// Get all unit type names
    /// </summary>
    /// <returns>An array of unit type names</returns>
    public static string[] GetAllUnitTypeNames()
    {
        if (_names == null)
        {
            _names = new string[_availableUnitTypes.Count];
            int index = 0;
            foreach (KeyValuePair<string, UnitType> entry in _availableUnitTypes)
            {
                _names[index] = entry.Key;
                index++;
            }
        }
        return _names;
    }

    /// <summary>
    /// Get unit type name
    /// </summary>
    /// <returns>Unit type name</returns>
    public string GetName()
    {
        return _data.name;
    }

    /// <summary>
    /// Get training cost of a single combatant of this unit type
    /// </summary>
    /// <returns>Training cost of a single combatant of this unit type</returns>
    public int GetTrainingCost()
    {
        return _data.cost;
    }

    /// <summary>
    /// Does the unit type represent a hero?
    /// </summary>
    /// <returns>Whether the unit type represents a hero</returns>
    public bool IsHero()
    {
        return _data.hero;
    }

    /// <summary>
    /// Is the unit type holy?
    /// </summary>
    /// <returns>Whether the unit type is holy</returns>
    public bool IsHoly()
    {
        return _data.holy;
    }

    /// <summary>
    /// Can this unit type be trained?
    /// </summary>
    /// <returns>Whether combatants of this unit type can be trained</returns>
    public bool IsTrainable()
    {
        return !_data.hero;
    }

    /// <summary>
    /// Get unit type id
    /// </summary>
    /// <returns>Unit type id</returns>
    public int GetId()
    {
        return _data.id;
    }

    /// <summary>
    /// Get hit points of a combatant of this unit type
    /// </summary>
    /// <returns>Hit points of a combatant of this unit type</returns>
    public int GetHitPoints()
    {
        return _data.health;
    }

    /// <summary>
    /// Set hit points of a combatant of this unit type
    /// </summary>
    /// <param name="value">The number of hit points for the unit type</param>
    public void SetHitPoints(int value)
    {
        _data.health = value;
    }

    /// <summary>
    /// Get unit type's attacks for a particular phase
    /// </summary>
    /// <param name="phase">Combat turn's phase</param>
    /// <returns>List of unit type's attacks for the phase</returns>
    public List<Attack> GetAttacksForPhase(Combat.TurnPhase phase)
    {
        List<Attack> result = new List<Attack>();
        AttackData.Quality quality = AttackData.Quality.NONE;
        switch (phase)
        {
            case Combat.TurnPhase.DIVINE:
                quality = AttackData.Quality.DIVINE;
                break;
            case Combat.TurnPhase.MAGIC:
                quality = AttackData.Quality.MAGIC;
                break;
            case Combat.TurnPhase.RANGED:
                quality = AttackData.Quality.RANGED;
                break;
            case Combat.TurnPhase.SKIRMISH:
                quality = AttackData.Quality.SKIRMISH;
                break;
            case Combat.TurnPhase.CHARGE:
                quality = AttackData.Quality.CHARGE;
                break;
            case Combat.TurnPhase.MELEE:
                quality = AttackData.Quality.MELEE;
                break;
        }

        if (quality != AttackData.Quality.NONE)
        {
            for (int i = 0; i < _attacks.Count; i++)
            {
                if (_attacks[i].HasQuality(quality))
                {
                    result.Add(_attacks[i]);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Get all unit type's attacks
    /// </summary>
    /// <returns>List of unit type's attacks</returns>
    public List<Attack> GetAllAttacks()
    {
        return _attacks;
    }

    /// <summary>
    /// Add an attack quality to all unit type's attacks
    /// </summary>
    /// <param name="quality">Attack quality to add</param>
    public void AddAttackQuality(AttackData.Quality quality)
    {
        for (int i = 0; i < _attacks.Count; i++)
        {
            _attacks[i].AddQuality(quality);
        }
    }

    /// <summary>
    /// Get unit type's melee defense
    /// </summary>
    /// <returns>Unit type's defense</returns>
    public int GetDefense()
    {
        return _data.defense;
    }

    /// <summary>
    /// Get unit type's shield value
    /// </summary>
    /// <returns>Unit type's shield value</returns>
    public int GetShield()
    {
        return _data.shield;
    }

    /// <summary>
    /// Set the shield value for the unit type
    /// </summary>
    /// <param name="value">Shield value to use</param>
    public void SetShield(int value)
    {
        _data.shield = value;
    }

    /// <summary>
    /// Get unit type's armor value
    /// </summary>
    /// <returns>Unit type's armor value</returns>
    public int GetArmor()
    {
        return _data.armor;
    }

    /// <summary>
    /// Set the armor value for the unit type
    /// </summary>
    /// <param name="value">Armor value to use</param>
    public void SetArmor(int value)
    {
        _data.armor = value;
    }

    /// <summary>
    /// Get unit type's spells of a specific type
    /// </summary>
    /// <param name="spellType">Spell type to search for</param>
    /// <returns>List of unit type's spells of this spell type</returns>
    public List<Spell> GetSpellsOfType(Spell.SpellType spellType)
    {
        List<Spell> result = new List<Spell>();

        for (int i = 0; i < _spells.Count; i++)
        {
            if (_spells[i].GetSpellType() == spellType)
            {
                result.Add(_spells[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// Get all unit type's spells
    /// </summary>
    /// <returns>List of unit type's spells</returns>
    public List<Spell> GetSpells()
    {
        return _spells;
    }

    /// <summary>
    /// Set up unit type's spell-like abilities
    /// </summary>
    /// <param name="spells">Hash of spell id => spell of all spells in the game</param>
    public void SetUpSpellLikeAbilities(Dictionary<Game.SpellId, Spell> spells)
    {
        if (_data.spellLikes != null)
        {
            for (int i = 0; i < _data.spellLikes.Length; i++)
            {
                _spellLikeAbilities.Add(spells[(Game.SpellId)_data.spellLikes[i]]);
            }
        }
    }

    /// <summary>
    /// Get all unit type's spell-like abilities
    /// </summary>
    /// <returns>List of unit type's spell-like abilities</returns>
    public List<Spell> GetSpellLikeAbilities()
    {
        return _spellLikeAbilities;
    }

    /// <summary>
    /// Get unit type's flavor text
    /// </summary>
    /// <returns>Unit type's flavor text</returns>
    public string GetFlavorText()
    {
        return _data.info;
    }
}

