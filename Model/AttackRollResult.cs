/// <summary>
/// Representation of an attack roll
/// </summary>

public class AttackRollResult
{
    public UnitStack UnitStack { get; private set; }
    public Attack Attack { get; private set;  }
    public int PositiveDieRoll { get; set; }
    public int NegativeDieRoll { get; set; }
    public int AttackRoll { get { return PositiveDieRoll - NegativeDieRoll; } }
    public int AttackSkill { get; set; }
    public int TotalAttack { get { return AttackSkill + AttackRoll; } }
    public int BaseDamage { get; private set; }
    public int BonusDamage { get; private set; }
    public int FullDamage { get { return BaseDamage + BonusDamage; } }
    public bool IsCritical { get; private set; }

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="unitStack">Attacking unit stack</param>
    /// <param name="attack">Attack object</param>
    /// <param name="positiveDieRoll">Value of the attack's positive roll</param>
    /// <param name="negativeDieRoll">Value of the attack's negative roll</param>
    /// <param name="attackSkill">Attack skill value</param>
    /// <param name="baseDamage">Based damage value</param>
    /// <param name="bonusDamage">Bonus damage value</param>
    /// <param name="isCritical">Whether the attack is a critical hit</param>
    public AttackRollResult(UnitStack unitStack,
							Attack attack,
							int positiveDieRoll,
							int negativeDieRoll,
							int attackSkill,
							int baseDamage,
							int bonusDamage,
							bool isCritical)
    {
        UnitStack = unitStack;
        Attack = attack;
        PositiveDieRoll = positiveDieRoll;
        NegativeDieRoll = negativeDieRoll;
        AttackSkill = attackSkill;
        BaseDamage = baseDamage;
        BonusDamage = bonusDamage;
        IsCritical = isCritical;
    }

    /// <summary>
    /// Get attacker's unit type
    /// </summary>
    /// <returns>Unit type of the attacker</returns>
    public UnitType GetUnitType()
    {
        return UnitStack.GetUnitType();
    }

}
