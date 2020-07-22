/// <summary>
/// In-game model of an attack
/// </summary>

using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Attack
{
    // serializable data
    private AttackData _data;

    // fields used by damage calculations
    // essentially, a cache - all of them are contained in data.damage formula
    private List<int> _dicePool;
    private List<int> _rollMultipliers;
    private int _rollModifier;
    // this is also a cache, but for supporting phase logic
	// rather than damage calculations
    private List<AttackData.Quality> _qualities;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">AttackData object containing the information</param>
    public Attack(AttackData data)
    {
        _data = data;
        _qualities = new List<AttackData.Quality>();
        if (_data.qualities != null)
        {
            for (int i = 0; i < _data.qualities.Length; i++)
            {
                _qualities.Add(_data.qualities[i]);
            }
        }

        ParseDamageExpression();
    }

    /// <summary>
    /// Get the underlying AttackData object
    /// </summary>
    /// <returns>Attack's underlying data</returns>
    public AttackData GetData()
    {
        return _data;
    }

    /// <summary>
    /// Convert the damage expression string into workable components
	/// like dice faces and modifiers
    /// </summary>
    private void ParseDamageExpression()
    {
        _dicePool = new List<int>();
        _rollMultipliers = new List<int>();
        _rollModifier = 0;

        string pattern = "\\-";
        string replacement = "+-";
        Regex rgx = new Regex(pattern);

        string modifiedDamageExpression = rgx.Replace(_data.damage, replacement);

        string[] diceExpressions = modifiedDamageExpression.Split('+');

        for (int i = 0; i < diceExpressions.Length; i++)
        {
            string[] diceElements = diceExpressions[i].Split('d');
            if (diceElements.Length == 2)
            {
                if (diceElements[0] == "")
                {
                    _rollMultipliers.Add(1);
                }
                else
                {
                    _rollMultipliers.Add(int.Parse(diceElements[0]));
                }
                _dicePool.Add(int.Parse(diceElements[1]));
            }
            if (diceElements.Length == 1)
            {
                _rollModifier = int.Parse(diceElements[0]);
            }
        }
    }

    /// <summary>
    /// Get a random damage number from the range of possible outcomes
    /// </summary>
    /// <returns>Damage rolled</returns>
    public int RollDamage()
    {
        int result = 0;
        for (int i = 0; i < _dicePool.Count; i++)
        {
            int sign = _rollMultipliers[i] > 0 ? 1 : -1;
            int abs = sign * _rollMultipliers[i];
            for (int j = 0; j < abs; j++)
            {
                result += sign*Dice.Roll(_dicePool[i]);
            }
        }
        result += _rollModifier;
        return result;
    }

    /// <summary>
    /// Get the upper threshold of the possible damage range
    /// </summary>
    /// <returns>Maximum possible damage</returns>
    public int GetMaxDamage()
    {
        int result = 0;
        for (int i = 0; i < _dicePool.Count; i++)
        {
            int bestResult = _rollMultipliers[i] > 0 ? _rollMultipliers[i] * _dicePool[i] : _rollMultipliers[i];
            result += bestResult;
        }
        result += _rollModifier;
        return result;
    }

    /// <summary>
    /// Get the lower threshold of the possible damage range
    /// </summary>
    /// <returns>Minimum possible damage</returns>
    public int GetMinDamage()
    {
        int result = 0;
        for (int i = 0; i < _dicePool.Count; i++)
        {
            int worstResult = _rollMultipliers[i] > 0 ? _rollMultipliers[i] : _rollMultipliers[i] * _dicePool[i];
            result += worstResult;
        }
        result += _rollModifier;
        return result;
    }

    /// <summary>
    /// Get the skill component of the damage formula
    /// </summary>
    /// <returns>Values for the skill component</returns>
    public int GetSkill()
    {
        return _data.skill;
    }

    /// <summary>
    /// Get the number of attacks
    /// </summary>
    /// <returns>Number of attacks</returns>
    public int GetNumberOfAttacks()
    {
        return _data.quantity;
    }

    /// <summary>
    /// Does the attack has the quality in question?
    /// </summary>
    /// <param name="quality">Quality to check</param>
    /// <returns>Whether the attack has the quality in question</returns>
    public bool HasQuality(AttackData.Quality quality)
    {
        return _qualities.Contains(quality);
    }

    /// <summary>
    /// Is this an armor-piercing attack?
    /// </summary>
    /// <returns>Whether this is an armor-piercing attack</returns>
    public bool IsArmorPiercing()
    {
        return HasQuality(AttackData.Quality.AP);
    }

    /// <summary>
    /// Is this a gunpowder attack?
    /// </summary>
    /// <returns>Whether this is a gunpowder attack</returns>
    public bool IsGunpowderAttack()
    {
        return HasQuality(AttackData.Quality.GUNPOWDER);
    }

    /// <summary>
    /// Is this this a ranged attack?
    /// </summary>
    /// <returns>Whether this is a ranged attack</returns>
    public bool IsRangedAttack()
    {
        return HasQuality(AttackData.Quality.RANGED);
    }

    /// <summary>
    /// Is this a skirmish attack?
    /// </summary>
    /// <returns>Whether this is a skirmish attack</returns>
    public bool IsSkirmishAttack()
    {
        return HasQuality(AttackData.Quality.SKIRMISH);
    }

    /// <summary>
    /// Is this a fire attack?
    /// </summary>
    /// <returns>Whether this is a fire attack</returns>
    public bool IsFireAttack()
    {
        return HasQuality(AttackData.Quality.FIRE);
    }

    /// <summary>
    /// Add a new quality to the attack
    /// </summary>
    /// <param name="quality">The attack quality to add</param>
    public void AddQuality(AttackData.Quality quality)
    {
        _qualities.Add(quality);
    }

    /// <summary>
    /// Get all qualities
    /// </summary>
    /// <returns>List of attack qualities</returns>
    public List<AttackData.Quality> GetQualities()
    {
        return _qualities;
    }

    /// <summary>
    /// Get damage expression
    /// </summary>
    /// <returns>String representation of the damage formula</returns>
    public string GetDamageExpression()
    {
        return _data.damage;
    }

}
