/// <summary>
/// Representation of an attack resoluton event
/// Includes all relevant data to be used by a view
/// </summary>

using System;

public class AttackResolutionEvent : EventArgs
{
    private AttackRollResult _attack;
    private UnitStack _target;
    private int _defensePositiveDieRoll;
    private int _defenseNegativeDieRoll;
    private int _defenseSkill;
    private int _shield;
    private int _armor;
    private int _damage;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="attack">Attack roll result</param>
    /// <param name="target">Target unit defending against the attack</param>
    /// <param name="defensePositiveDieRoll">Value of the defense's positive roll</param>
    /// <param name="defenseNegativeDieRoll">Value of the defense's negative roll</param>
    /// <param name="defenseSkill">Value of the skill used in defense</param>
    /// <param name="shield">Shield value used in the defense roll</param>
    public AttackResolutionEvent(AttackRollResult attack,
										UnitStack target,
										int defensePositiveDieRoll,
										int defenseNegativeDieRoll,
										int defenseSkill,
										int shield)
    {
        _attack = attack;
        _target = target;
        _defensePositiveDieRoll = defensePositiveDieRoll;
        _defenseNegativeDieRoll = defenseNegativeDieRoll;
        _defenseSkill = defenseSkill;
        _shield = shield;
    }

    /// <summary>
    /// Get attack roll result
    /// </summary>
    /// <returns>Result of the attack roll</returns>
    public AttackRollResult GetAttack()
    {
        return _attack;
    }

    /// <summary>
    /// Get defense positive roll
    /// </summary>
    /// <returns>Value of the positive roll</returns>
    public int GetDefensePositiveDieRoll()
    {
        return _defensePositiveDieRoll;
    }

    /// <summary>
    /// Get defense negative roll
    /// </summary>
    /// <returns>Value of the negative roll</returns>
    public int GetDefenseNegativeDieRoll()
    {
        return _defenseNegativeDieRoll;
    }

    /// <summary>
    /// Get defense roll
    /// </summary>
    /// <returns>Value of the defense roll</returns>
    public int GetDefenseRoll()
    {
        return _defensePositiveDieRoll - _defenseNegativeDieRoll;
    }

    /// <summary>
    /// Get defense skill
    /// </summary>
    /// <returns>Value of the skill used in defense</returns>
    public int GetDefenseSkill()
    {
        return _defenseSkill;
    }

    /// <summary>
    /// Get total defense value
    /// </summary>
    /// <returns>Total value of the defense</returns>
    public int GetTotalDefense()
    {
        return GetDefenseSkill() + GetShield() + GetDefenseRoll();
    }

    /// <summary>
    /// Get shield value
    /// </summary>
    /// <returns>Shield value used in the defense</returns>
    public int GetShield()
    {
        return _shield;
    }

    /// <summary>
    /// Get attack's target
    /// </summary>
    /// <returns>Unit stack defending against the attack</returns>
    public UnitStack GetTarget()
    {
        return _target;
    }

    /// <summary>
    /// Set armor value
	/// Based on target unit stack's armor, but modified by attack's qualities
    /// </summary>
    /// <param name="armor">Armor value to use for defense calculations</param>
    public void SetArmor(int armor)
    {
        _armor = armor;
    }

    /// <summary>
    /// Get armor value
    /// </summary>
    /// <returns>Armor value used in defense calculations</returns>
    public int GetArmor()
    {
        return _armor;
    }

    /// <summary>
    /// Set damage value
    /// </summary>
    /// <param name="armor">Damage received by the target unit stack</param>
    public void SetDamage(int damage)
    {
        _damage = damage;
    }

    /// <summary>
    /// Get damage value
    /// </summary>
    /// <returns>Damage received by the target unit stack</returns>
    public int GetDamage()
    {
        return _damage;
    }

}
