/// <summary>
/// Base class for in-game spells
/// </summary>

using System.Collections.Generic;

public class Spell
{
    public enum SpellType { UNIT_CREATION, DEFENSIVE, OFFENSIVE, RESTORATIVE };

    private SpellType _type;
    private string _name;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="name">Name of the spell</param>
    /// <param name="type">Type of the spell</param>
    public Spell(string name, SpellType type)
    {
        _name = name;
        _type = type;
    }

    /// <summary>
    /// Get spell type
    /// </summary>
    /// <returns>Type of the spell</returns>
    public SpellType GetSpellType()
    {
        return _type;
    }

    /// <summary>
    /// Get spell name
    /// </summary>
    /// <returns>Name of the spell</returns>
    public string GetName()
    {
        return _name;
    }

    /// <summary>
    /// Does the spell create new units?
    /// </summary>
    /// <returns>Whether the spell creates new units</returns>
    public bool IsCreatingUnits()
    {
        return _type == SpellType.UNIT_CREATION;
    }

    /// <summary>
    /// Is the spell defensive?
    /// </summary>
    /// <returns>Whether the spell is defensive</returns>
    public bool IsDefensive()
    {
        return _type == SpellType.DEFENSIVE;
    }

    /// <summary>
    /// Is the spell offensive?
    /// </summary>
    /// <returns>Whether the spell is offensive</returns>
    public bool IsOffensive()
    {
        return _type == SpellType.OFFENSIVE;
    }

    /// <summary>
    /// Finalize the spell
	/// To be overwritten by descendents
	/// Mostly needed for unit summoning spells
    /// </summary>
    /// <param name="unitTypes">Hash of unit type id => unit type, including all unit types in the game</param>
    /// <returns>Whether the operation was successful</returns>
    public virtual bool FinalizeSpell(Dictionary<int, UnitType> unitTypes)
    {
        return false;
    }

    /// <summary>
    /// Create a new unit stack
	/// To be overwritten by descendents
	/// Used by unit summoning spells
    /// </summary>
    /// <param name="existing">List of existing unit stacks</param>
    /// <returns>Unit stack created by the spell</returns>
    public virtual UnitStack Create(List<UnitStack> existing)
    {
        return null;
    }

    /// <summary>
    /// Cast the spell
	/// To be overwritten by descendents
    /// </summary>
    /// <param name="potentialTargets">List of unit stacks that are potential targets</param>
    public virtual void CastOn(List<UnitStack> potentialTargets)
    {
    }

}
