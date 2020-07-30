/// <summary>
/// A representation of a unit used in combat
/// Tracks current hit points of combatants and the province
/// for attackers to retreat to
/// </summary>

using System;
using System.Collections.Generic;

public class UnitStack
{
    public event EventHandler<WoundCheckEvent> WoundCheckMade;

    private Unit _baseUnit;
    private Province _provinceToRetreat;
    private int[] _hitPointsDistribution;
    private List<Spell> _affectingSpells;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="unit">The unit on which to base the unit stack</param>
    /// <param name="province">The province to which an attacking unit stack will retreat</param>
    public UnitStack(Unit unit, Province province)
    {
        _baseUnit = unit;
        _provinceToRetreat = province;
        _hitPointsDistribution = new int[_baseUnit.GetUnitType().GetHitPoints()];
        _hitPointsDistribution[_baseUnit.GetUnitType().GetHitPoints() - 1] = _baseUnit.GetQuantity();
        _affectingSpells = new List<Spell>();
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="stack">The unit stack to clone</param>
    public UnitStack(UnitStack stack)
    {
        _baseUnit = new Unit(stack.GetBaseUnit());
        _provinceToRetreat = stack.GetProvinceToRetreat();
        _hitPointsDistribution = new int[_baseUnit.GetUnitType().GetHitPoints()];
        for (int i = 0; i < _hitPointsDistribution.Length; i++)
        {
            _hitPointsDistribution[i] = stack._hitPointsDistribution[i];
        }
        _affectingSpells = new List<Spell>();
    }

    /// <summary>
    /// Get stack's unit type
    /// </summary>
    /// <returns>Unit type of the unit stack</returns>
    public UnitType GetUnitType()
    {
        return _baseUnit.GetUnitType();
    }

    /// <summary>
    /// Get stack's unit
    /// </summary>
    /// <returns>Unit on which the unit stack is based</returns>
    public Unit GetBaseUnit()
    {
        return _baseUnit;
    }

    /// <summary>
    /// Get the province to which an attacking unit stack will retreat
    /// </summary>
    /// <returns>The province to which an attacking unit stack will retreat</returns>
    public Province GetProvinceToRetreat()
    {
        return _provinceToRetreat;
    }

    /// <summary>
    /// Get the index of a random combatant in the stack
    /// </summary>
    /// <returns>The index of a random combatant in the stack</returns>
    public int GetRandomIndex()
    {
        int index = 0;
        int random = Dice.RollDie(GetTotalQty());
        for (index = 0; index < _hitPointsDistribution.Length; index++)
        { 
            random -= _hitPointsDistribution[index];
            if (random <= 0)
            {
                break;
            }
        }

        return index;
    }

    /// <summary>
    /// Add fresh combatants to the unit stack
    /// </summary>
    /// <param name="quantity">The number of combatants to add</param>
    public void ReinforceStack(int quantity)
    {
        _baseUnit.AddQuantity(quantity);
        _hitPointsDistribution[_baseUnit.GetUnitType().GetHitPoints() - 1] += quantity;
    }

    /// <summary>
    /// Get the current number of combatants
    /// </summary>
    /// <returns>The current number of combatants</returns>
    public int GetTotalQty()
    {
        int result = 0;
        for (int i = 0; i < _hitPointsDistribution.Length; i++)
        {
            result += _hitPointsDistribution[i];
        }

        return result;
    }

    /// <summary>
    /// Get the current number of hit points of all combatants
    /// </summary>
    /// <returns>The current number of hit points of all combatants</returns>
    public int GetTotalHealth()
    {
        int result = 0;
        for (int i = 0; i < _hitPointsDistribution.Length; i++)
        {
            result += _hitPointsDistribution[i] * (i + 1);
        }

        return result;
    }

    /// <summary>
    /// Get the current number of wound points of all combatants
	/// This is the difference between the max number of hit points
	/// and the current number of hit points combatants have
    /// </summary>
    /// <returns>The current number of wound points of all combatants</returns>
    public int GetWoundPoints()
    {
        int result = 0;
        int maxHealth = _baseUnit.GetUnitType().GetHitPoints();
        for (int i = 0; i < _hitPointsDistribution.Length; i++)
        {
            result += _hitPointsDistribution[i] * (maxHealth - i - 1);
        }
        return result;
    }

    /// <summary>
    /// Heal the unit stack, bringing the number of hit points to the max
	/// Note that this doesn't resurrect dead combatants, only heals wounded
    /// </summary>
    public void Heal()
    {
        int healthyIndex = _baseUnit.GetUnitType().GetHitPoints() - 1;
        for (int i = 0; i < _hitPointsDistribution.Length - 1; i++)
        {
            _hitPointsDistribution[healthyIndex] += _hitPointsDistribution[i];
            _hitPointsDistribution[i] = 0;
        }
    }

    /// <summary>
    /// Assign damage to a random combatant in the stack
    /// </summary>
    /// <param name="damage">Damage amount</param>
    public void TakeDamage(int damage)
    {
        if (damage > 0)
        {
            int currentHPIndex = GetRandomIndex();
            int newHPIndex = currentHPIndex - damage;
            _hitPointsDistribution[currentHPIndex]--;
            if (newHPIndex >= 0 && newHPIndex < _hitPointsDistribution.Length)
            {
                _hitPointsDistribution[newHPIndex]++;
                FileLogger.Trace("COMBAT", "One " + GetUnitType().GetName() + " is wounded - " + GetTotalQty().ToString() + " left");
            }
            else
            {
                FileLogger.Trace("COMBAT", "One " + GetUnitType().GetName() + " is dead - " + GetTotalQty().ToString() + " left");
            }
        }
    }

    /// <summary>
    /// Perform 'live or die' check
	/// It's made at the end of a combat
	/// The more damaged a combatant is, the more difficult is to pass the check
    /// </summary>
    /// <param name="woundCheckBonus">A bonus to check difficulty</param>
    public void PerformWoundChecks(int woundCheckBonus)
    {
        int hitPoints = GetTotalHealth();
        int qty = GetTotalQty();
        _baseUnit.SetQuantity(_hitPointsDistribution[_hitPointsDistribution.Length - 1]);
        int maxHealth = _baseUnit.GetUnitType().GetHitPoints();
        for (int i = 0; i < _hitPointsDistribution.Length - 1; i++)
        {
            int target = i + 1 + woundCheckBonus;
            for (int j = 0; j < _hitPointsDistribution[i]; j++)
            {
                int roll = Dice.RollDie(maxHealth);
                if (roll <= target)
                {
                    _baseUnit.AddQuantity(1);
                    hitPoints += maxHealth - target;
                    FileLogger.Trace("COMBAT", GetUnitType().GetName() + ": target = " + target + ", rolled " + roll + ": success!");
                }
                else
                {
                    qty -= 1;
                    hitPoints -= target;
                    FileLogger.Trace("COMBAT", GetUnitType().GetName() + ": target = " + target + ", rolled " + roll + ": failure.");
                }

                if (WoundCheckMade != null)
                {
                    WoundCheckMade(this, new WoundCheckEvent(qty, hitPoints, target, roll));
                }
            }
            // keep hit points distribution honest
            // we need it to be accurate in order to remove empty stacks
            _hitPointsDistribution[i] = 0;
        }
        _hitPointsDistribution[_hitPointsDistribution.Length - 1] = _baseUnit.GetQuantity();
    }

    /// <summary>
    /// Store the fact of being affected by a spell
    /// </summary>
    /// <param name="spell">The spell cast on the unit stack</param>
    public void AffectBySpell(Spell spell)
    {
        if (!_affectingSpells.Contains(spell))
        {
            _affectingSpells.Add(spell);
        }
    }

    /// <summary>
    /// Is the unit stack affected by the spell?
    /// </summary>
    /// <param name="spell">The spell to check</param>
    /// <returns>Whether the unit stack is affected by the spell</returns>
    public bool IsAffectedBy(Spell spell)
    {
        return _affectingSpells.Contains(spell);
    }

    /// <summary>
    /// Is the unit stack affected by a spell of this type?
    /// </summary>
    /// <param name="spellType">The spell type to check</param>
    /// <returns>Whether the unit stack is affected by a spell of a particular type</returns>
    public bool IsAffectedBy(Spell.SpellType spellType)
    {
        for (int i = 0; i < _affectingSpells.Count; i++)
        {
            if (_affectingSpells[i].GetSpellType() == spellType)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Is the unit stack affected by the spell?
    /// </summary>
    /// <param name="spellName">The name of the spell</param>
    /// <returns>Whether the unit stack is affected by the spell with this name</returns>
    public bool IsAffectedBy(string spellName)
    {
        for (int i = 0; i < _affectingSpells.Count; i++)
        {
            if (_affectingSpells[i].GetName() == spellName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get the list of spells affecting the unit stack
    /// </summary>
    /// <returns>The list of spells affecting the unit stack</returns>
    public List<Spell> GetAffectingSpells()
    {
        return _affectingSpells;
    }

    /// <summary>
    /// Remove a spell from the list of spells affecting the unit stack
    /// </summary>
    /// <param name="spell">The spell to remove</param>
    public void RemoveSpell(Spell spell)
    {
        if (_affectingSpells.Contains(spell))
        {
            _affectingSpells.Remove(spell);
        }
    }

    /// <summary>
    /// Remove all spells affecting the unit stack
    /// </summary>
    public void RemoveAllSpells()
    {
        _affectingSpells.Clear();
    }

    /// <summary>
    /// Set the province to which the unit stack has to retreat
    /// </summary>
    /// <param name="province">The province to which the unt stack has to retreat</param>
    /// <returns>Whether the province to retreat to was successfully assigned</returns>
    public bool SetProvinceToRetreat(Province province)
    {
        if (_provinceToRetreat != null)
        {
            return false;
        }
        _provinceToRetreat = province;
        return true;
    }

    /// <summary>
    /// Activate unit stack's spell-like abilities (based on the unit type)
    /// </summary>
    public void ActivateSpellLikeAbilities()
    {
        List<Spell> abilities = GetUnitType().GetSpellLikeAbilities();
        for (int i = 0; i < abilities.Count; i++)
        {
            // spell-like abilities are always self-targeted (for now, at least)
            AffectBySpell(abilities[i]);
        }
    }

    /// <summary>
    /// Create a clone of a list of unit stacks
    /// </summary>
    /// <param name="toClone">The list of unit stacks to clone</param>
    /// <returns>The clone of the passed list of unit stacks</returns>
    public static List<UnitStack> Clone(List<UnitStack> toClone)
    {
        List<UnitStack> result = new List<UnitStack>();
        for (int i = 0; i < toClone.Count; i++)
        {
            result.Add(new UnitStack(toClone[i]));
        }
        return result;
    }

}
