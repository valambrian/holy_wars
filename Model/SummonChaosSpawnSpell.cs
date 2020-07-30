/// <summary>
/// Implementation of the summon chaos spawn spell
/// </summary>

using System.Collections.Generic;

public class SummonChaosSpawnSpell : Spell
{
    private List<KeyValuePair<int, int>> _outcomes = new List<KeyValuePair<int, int>>()
    {
        { new KeyValuePair<int, int>(26, 3)  }, { new KeyValuePair<int, int>(27, 4)  }, { new KeyValuePair<int, int>(28, 3)  }
    };
    private List<KeyValuePair<UnitType, int>> _convertedOutcomes = new List<KeyValuePair<UnitType, int>>();
    private int _totalWeight = 0;


    public SummonChaosSpawnSpell() : base("Summon Chaos Spawn", SpellType.UNIT_CREATION)
    {
    }

    /// <summary>
    /// Finalize the spell
	/// Update summoned unit types based on the stored ids
    /// </summary>
    /// <param name="unitTypes">Hash of unit type id => unit type, including all unit types in the game</param>
    /// <returns>Whether the operation was successful</returns>
    public override bool FinalizeSpell(Dictionary<int, UnitType> unitTypes)
    {
        for (int i = 0; i < _outcomes.Count; i++)
        {
            UnitType unitType = unitTypes[_outcomes[i].Key];
            if (unitType != null)
            {
                _convertedOutcomes.Add(new KeyValuePair<UnitType, int>(unitType, _outcomes[i].Value));
                _totalWeight += _outcomes[i].Value;
            }
        }
        return true;
    }

    /// <summary>
    /// Create a new unit stack with one of the chaos spawn version
    /// </summary>
    /// <param name="summoners">List of unit stacks capable of summoning chaos spawns</param>
    /// <returns>Unit stack created by the spell</returns>
    public override UnitStack Create(List<UnitStack> summoners)
    {
        if (_totalWeight == 0)
        {
            return null;
        }
        int randomNumber = Dice.RollDie(_totalWeight) - 1;
        UnitType unitType = null;
        for (int i = 0; i < _convertedOutcomes.Count; i++)
        {
            if (randomNumber <= _convertedOutcomes[i].Value)
            {
                unitType = _convertedOutcomes[i].Key;
                break;
            }
            else
            {
                randomNumber -= _convertedOutcomes[i].Value;
            }
        }
        if (unitType != null)
        {
            UnitStack stack = new UnitStack(new Unit(unitType, 1), null);
            stack.AffectBySpell(this);

            FileLogger.Trace("SPAWN", unitType.GetName() + " is spawned.");

            return stack;
        }
        return null;
    }

}
