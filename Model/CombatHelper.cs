/// <summary>
/// Utility class for running combat calculations
/// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatHelper : MonoBehaviour
{
    private static CombatHelper instance;

    private const int _diceSides = 6;
    private const double _damageScale = 0.0007716049; // 1 over 6^4 - must be a constant, so precalculated here
    private const bool _useCriticals = true;
    private Dictionary<int, Dictionary<int, Dictionary<int, double>>> _expectedDamage = new Dictionary<int, Dictionary<int, Dictionary<int, double>>>();

	/// <summary>
	/// Initialize singleton
	/// </summary>
    void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else
        {
            if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

	/// <summary>
	/// Access singleton
	/// </summary>
    /// <returns>Static instance of the class</returns>
    public static CombatHelper Instance
    {
        get { return instance; }
    }

	/// <summary>
	/// Calculate mathematical expectation of the weapon damage value
	/// </summary>
    /// <param name="attackRollResult">Result of the attack roll</param>
    /// <param name="defender">Defending unit stack</param>
    /// <param name="totalDefense">Defender's total defense against the attack</param>
    /// <param name="armor">Defender's armor value against the attack</param>
    /// <param name="maxValue">Max value the damage can reach</param>
    /// <returns>Mathematical expectation of the weapon damage value</returns>
    public double EstimateWeaponDamage(AttackRollResult attackRollResult, UnitStack defender, int totalDefense, int armor, int maxValue)
    {
        double result = 0;
        if ((IsUsingCriticals() && attackRollResult.IsCritical) || (attackRollResult.AttackRoll + attackRollResult.AttackSkill > totalDefense))
        {
            result = 0.5 * (attackRollResult.Attack.GetMinDamage() + attackRollResult.Attack.GetMaxDamage());
            if (IsUsingCriticals() && attackRollResult.IsCritical)
            {
                result *= 2;
            }
            if (armor > 0)
            {
                result -= armor;
            }
            if (result < 0)
            {
                result = 0;
            }
            if (result > maxValue)
            {
                result = maxValue;
            }
        }
        return result;
    }

	/// <summary>
	/// Calculate mathematical expectation of the attack damage value
	/// </summary>
    /// <param name="attackRollResult">Attack roll result object representing the attack</param>
    /// <param name="defender">Defending unit stack</param>
    /// <returns>Mathematical expectation of the attack damage value</returns>
    public int EstimateAttackDamage(AttackRollResult attackRoll, UnitStack defender)
    {
        int result = 0;
        double totalDamage = 0;
        int maxHP = defender.GetUnitType().GetHitPoints();

        for (int attackerPlusDie = 0; attackerPlusDie < _diceSides; attackerPlusDie++)
        {
            for (int attackerMinusDie = 0; attackerMinusDie < _diceSides; attackerMinusDie++)
            {
                AttackRollResult attackRollResult = CreateAnAttackRollResult(attackRoll.UnitStack, attackRoll.Attack,
                    attackerPlusDie + 1, attackerMinusDie + 1);

                int defensiveSkill = CalculateDefensiveSkill(attackRollResult.Attack, defender);
                int shield = CalculateShieldValue(attackRollResult.Attack, defender);
                int armor = CalculateArmorValue(attackRollResult.Attack, defender, attackRollResult.IsCritical);

                for (int defenderPlusDie = 0; defenderPlusDie < _diceSides; defenderPlusDie++)
                {
                    for (int defenderMinusDie = 0; defenderMinusDie < _diceSides; defenderMinusDie++)
                    {
                        // both defenderPlusDie and defenderMinusDie would add +1 to them,
                        // so the +1s would cancel each other
                        int defenseRoll = defenderPlusDie - defenderMinusDie;
                        int totalDefense = defenseRoll + defensiveSkill + shield;
                        totalDamage += EstimateWeaponDamage(attackRollResult, defender, totalDefense, armor, maxHP);
                    }
                }
            }
        }

        totalDamage *= _damageScale;
        result = Convert.ToInt32(Math.Floor(totalDamage));
        totalDamage -= result;

        System.Random rando = new System.Random();
        if (totalDamage > rando.NextDouble())
        {
            result++;
        }
        return result;
    }

	/// <summary>
	/// Estimate results of a unit stack attacking another during a specified turn phase
	/// </summary>
    /// <param name="attacker">Attacking unit stack</param>
    /// <param name="defender">Defending unit stack</param>
    /// <param name="phase">Turn phase</param>
    /// <returns>Mathematical expectation of the attack damage value</returns>
    public double EstimateStackAttacksDamage(UnitStack attacker, UnitStack defender, Combat.TurnPhase phase)
    {
        double result = GetEstimatedDamage(attacker, defender, phase);
        if (result >= 0)
        {
            return result;
        }
        result = 0;
        int maxHP = defender.GetUnitType().GetHitPoints();
        List<Attack> attacks = attacker.GetUnitType().GetAttacksForPhase(phase);
        for (int i = 0; i < attacks.Count; i++)
        {
            int numberOfAttacks = attacks[i].GetNumberOfAttacks();
            for (int attackerPlusDie = 0; attackerPlusDie < _diceSides; attackerPlusDie++)
            {
                for (int attackerMinusDie = 0; attackerMinusDie < _diceSides; attackerMinusDie++)
                {
                    AttackRollResult attackRollResult = CreateAnAttackRollResult(attacker, attacks[i],
                        attackerPlusDie + 1, attackerMinusDie + 1);

                    int defensiveSkill = CalculateDefensiveSkill(attackRollResult.Attack, defender);
                    int shield = CalculateShieldValue(attackRollResult.Attack, defender);
                    int armor = CalculateArmorValue(attackRollResult.Attack, defender, attackRollResult.IsCritical);

                    for (int defenderPlusDie = 0; defenderPlusDie < _diceSides; defenderPlusDie++)
                    {
                        for (int defenderMinusDie = 0; defenderMinusDie < _diceSides; defenderMinusDie++)
                        {
                            // both defenderPlusDie and defenderMinusDie would add +1 to them,
                            // so the +1s would cancel each other
                            int defenseRoll = defenderPlusDie - defenderMinusDie;
                            int totalDefense = defenseRoll + defensiveSkill + shield;
                            result += numberOfAttacks * EstimateWeaponDamage(attackRollResult, defender, totalDefense, armor, maxHP);
                        }
                    }
                }
            }
        }
        result *= _damageScale;
        SetEstimatedDamage(attacker, defender, phase, result);
        return result;
    }

	/// <summary>
	/// Calculate defender's skill value against the attack
	/// </summary>
    /// <param name="attack">Attack under consideration</param>
    /// <param name="defender">Defending unit stack</param>
    /// <returns>Defensive skill modified by attack's qualities</returns>
    public int CalculateDefensiveSkill(Attack attack, UnitStack defender)
    {
        // RANGED attacks ignore defense skill
        // SKIRMISH or MAGIC attacks halve it
        int result = defender.GetUnitType().GetDefense();
        if (attack.IsRangedAttack())
        {
            result = 0;
        }
        else
        {
            if (attack.IsSkirmishAttack() || attack.HasQuality(AttackData.Quality.MAGIC))
            {
                result /= 2;
            }
        }
        return result;
    }

	/// <summary>
	/// Calculate defender's shield value against the attack
	/// </summary>
    /// <param name="attack">Attack under consideration</param>
    /// <param name="defender">Defending unit stack</param>
    /// <returns>Defender's shield value modified by attack's qualities</returns>
    public int CalculateShieldValue(Attack attack, UnitStack defender)
    {
        // Shields perform at 150% against SKIRMISH attacks
        // They perform at 200% against regular RANGED attacks
        // GUNPOWDER attacks ignore shields
        // MAGIC and LIGHTNING attacks halve the shield value
        // MAGIC SHIELD spell adds 2 to the shield value after the rules above were applied
        // ILLUSORY attacks ignore shields, including those created by MAGIC SHIELD spell
        int shieldRating = defender.GetUnitType().GetShield();
        int result = shieldRating;
        if (attack.IsSkirmishAttack())
        {
            result += shieldRating / 2;
        }

        if (attack.IsRangedAttack())
        {
            result = 2 * shieldRating;
        }

        if (attack.HasQuality(AttackData.Quality.MAGIC) || attack.HasQuality(AttackData.Quality.LIGHTNING))
        {
            result = shieldRating / 2;
        }
        if (attack.HasQuality(AttackData.Quality.GUNPOWDER))
        {
            result = 0;
        }
        if (defender.IsAffectedBy("Magic Shield"))
        {
            result += 2;
        }
        if (attack.HasQuality(AttackData.Quality.ILLUSORY))
        {
            result = 0;
        }
        return result;
    }

	/// <summary>
	/// Calculate defender's armor value against the attack
	/// </summary>
    /// <param name="attack">Attack under consideration</param>
    /// <param name="defender">Defending unit stack</param>
    /// <param name="isCritical">Whether the attack was a critical success</param>
    /// <returns>Defender's armor value modified by attack's qualities</returns>
    public int CalculateArmorValue(Attack attack, UnitStack defender, bool isCritical)
    {
        // AP (armor piercing), GUNPOWDER and LIGHTNING attacks halve armor rating
        // FIRE attacks ignore armor
        // Critical hits ignore armor
        // STONE SKIN spell adds 2 to the armor value after the above rules were applied
        // ILLUSORY attacks ignore armor and ignore effects of STONE SKIN spell
        int result = defender.GetUnitType().GetArmor();
        if (attack.IsArmorPiercing() || attack.IsGunpowderAttack() || attack.HasQuality(AttackData.Quality.LIGHTNING))
        {
            result /= 2;
        }
        if (isCritical && IsUsingCriticals())
        {
            result = 0;
        }
        if (attack.IsFireAttack())
        {
            result = 0;
        }
        if (defender.IsAffectedBy("Stone Skin"))
        {
            result += 2;
        }
        if (attack.HasQuality(AttackData.Quality.ILLUSORY))
        {
            result = 0;
        }

        return result;
    }

	/// <summary>
	/// Calculate damage inflicted by an attack
	/// </summary>
    /// <param name="attackRollResult">Attack roll result</param>
    /// <param name="defender">Defending unit stack</param>
    /// <param name="totalDefense">Defender's total defense value</param>
    /// <param name="armor">Defender's armor value</param>
    /// <returns>Damage inflicted by the attack to the defender</returns>
    public int CalculateDamage(AttackRollResult attackRollResult, UnitStack defender, int totalDefense, int armor)
    {
        int result = 0;
        if ((IsUsingCriticals() && attackRollResult.IsCritical) || (attackRollResult.AttackRoll + attackRollResult.AttackSkill > totalDefense))
        {
            result = attackRollResult.FullDamage;
            if (armor > 0)
            {
                result = Mathf.Max(0, result - armor);
            }
        }
        return result;
    }

	/// <summary>
	/// Do combat calculations use rules for critical successes?
	/// </summary>
    /// <returns>Whether combat calculations use rules for critical successes</returns>
    public bool IsUsingCriticals()
    {
        return _useCriticals;
    }

	/// <summary>
	/// Does the die roll represent a critical success?
	/// </summary>
    /// <param name="dieRoll">Die roll</param>
    /// <returns>Whether the die roll represents a critical success</returns>
    public bool IsCritical(int dieRoll)
    {
        return IsUsingCriticals() && dieRoll >= 5;
    }

	/// <summary>
	/// Generate an attack roll result
	/// </summary>
    /// <param name="attacker">Unit stack representing the attacker</param>
    /// <param name="attack">Attack object for roll result generation</param>
    /// <param name="plusDie">Value of the positive die's roll (pass zero to actually roll)</param>
    /// <param name="minusDie">Value of the negative die's roll (pass zero to actually roll)</param>
    /// <returns>Whether the die roll represents a critical success</returns>
    public AttackRollResult CreateAnAttackRollResult(UnitStack attacker, Attack attack, int plusDie = 0, int minusDie = 0)
    {
        bool isCritical = false;
        int positiveDieRoll = plusDie == 0 ? Dice.RollDie(_diceSides) : plusDie;
        int negativeDieRoll = minusDie == 0 ? Dice.RollDie(_diceSides) : minusDie;
        int dieRoll = positiveDieRoll - negativeDieRoll;
        int bonusDamage = 0;
        if (IsCritical(dieRoll))
        {
            isCritical = true;
            bonusDamage = attack.RollDamage();
        }
        AttackRollResult result = new AttackRollResult(attacker, attack, positiveDieRoll, negativeDieRoll, attack.GetSkill(), attack.RollDamage(), bonusDamage, isCritical);
        return result;
    }

    public int RollDie()
    {
        return Dice.RollDie(_diceSides);
    }

    private double GetEstimatedDamage(UnitStack attacker, UnitStack defender, Combat.TurnPhase phase)
    {
        int intPhase = (int)phase;
        if (attacker.GetAffectingSpells().Count == 0 && defender.GetAffectingSpells().Count == 0)
        {
            int attackerTypeId = attacker.GetUnitType().GetId();
            if (_expectedDamage.ContainsKey(attackerTypeId))
            {
                Dictionary <int, Dictionary <int, double>> attackerRecords = _expectedDamage[attackerTypeId];
                int defenderTypeId = defender.GetUnitType().GetId();
                if (attackerRecords.ContainsKey(defenderTypeId))
                {
                    Dictionary<int, double> defenderRecords = attackerRecords[defenderTypeId];
                    if (defenderRecords.ContainsKey(intPhase))
                    {
                        return defenderRecords[intPhase];
                    }
                }
            }
        }

        // "not found"
        return -1;
    }

    private void SetEstimatedDamage(UnitStack attacker, UnitStack defender, Combat.TurnPhase phase, double damage)
    {
        if (attacker.GetAffectingSpells().Count == 0 && defender.GetAffectingSpells().Count == 0)
        {
            int attackerTypeId = attacker.GetUnitType().GetId();
            if (!_expectedDamage.ContainsKey(attackerTypeId))
            {
                _expectedDamage[attackerTypeId] = new Dictionary<int, Dictionary<int, double>>();
            }
            int defenderTypeId = defender.GetUnitType().GetId();
            if (!_expectedDamage[attackerTypeId].ContainsKey(defenderTypeId))
            {
                _expectedDamage[attackerTypeId][defenderTypeId] = new Dictionary<int, double>();
            }
            _expectedDamage[attackerTypeId][defenderTypeId][(int)phase] = damage;
        }
        FileLogger.Trace("ESTIMATE", "Estimated damage of " + attacker.GetUnitType().GetName() +
                                        " vs " + defender.GetUnitType().GetName() + " during " +
                                        phase + " phase is " + damage);
    }

    public Unit GetAttackingCounter(Unit target, List<Unit> candidates)
    {
        double largestEstimatedDamage = 0;
        int bestMatchIndex = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].GetUnitType().IsHero())
            {
                double esimatedDamage = EstimateUnitDamage(candidates[i], target);
                if (esimatedDamage > largestEstimatedDamage)
                {
                    largestEstimatedDamage = esimatedDamage;
                    bestMatchIndex = i;
                }
            }
        }
        if (bestMatchIndex > -1 && 2 * largestEstimatedDamage >= target.GetUnitType().GetHitPoints())
        {
            return candidates[bestMatchIndex];
        }
        return null;
    }

    public Unit GetDefendingCounter(Unit target, List<Unit> candidates)
    {
        // damage ratio of 0.5 is borderline acceptable
        double smallestDamageRatio = 0.5;
        int bestMatchIndex = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].GetUnitType().IsHero())
            {
                double damageRatio = EstimateUnitDamage(target, candidates[i]) / candidates[i].GetUnitType().GetHitPoints();
                if (damageRatio < smallestDamageRatio)
                {
                    smallestDamageRatio = damageRatio;
                    bestMatchIndex = i;
                }
            }
        }
        if (bestMatchIndex > -1)
        {
            return candidates[bestMatchIndex];
        }
        return null;
    }

    private double EstimateUnitDamage(Unit attacker, Unit defender)
    {
        Combat.TurnPhase phase = Combat.TurnPhase.MAGIC;
        double epsilon = 0.05;
        while (phase < Combat.TurnPhase.CLEANUP)
        {
            UnitStack attackerStack = new UnitStack(new Unit(attacker.GetUnitType(), 1), null);
            UnitStack defenderStack = new UnitStack(new Unit(defender.GetUnitType(), 1), null);
            double attackerDamage = EstimateStackAttacksDamage(attackerStack, defenderStack, phase);
            attackerStack = new UnitStack(new Unit(attacker.GetUnitType(), 1), null);
            defenderStack = new UnitStack(new Unit(defender.GetUnitType(), 1), null);
            double defenderDamage = EstimateStackAttacksDamage(defenderStack, attackerStack, phase);
            phase++;
            // read as "attacker deals significantly less damage than defender
            // during this phase"
            if (attackerDamage + epsilon < defenderDamage)
            {
                return 0;
            }
            // "defender deals significantly less damage than attacker
            // during this phase"
            if (attackerDamage > defenderDamage + epsilon)
            {
                return attackerDamage;
            }
        }
        return 0;
    }

}
