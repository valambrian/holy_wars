/// <summary>
/// Tactical level AI
/// Selects defending units against enemy attacks
/// </summary>

using System.Collections.Generic;

public class Tactician
{
    private int _level;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="level">Number that determines the AI's sofistication</param>
    public Tactician(int level)
    {
        _level = level;
    }

    /// <summary>
    /// Select the unit stack which is going to soak enemy's attacks
    /// </summary>
    /// <param name="stacks">Available unit stacks</param>
    /// <param name="attacker">Attacking enemy unit stack</param>
    /// <param name="phase">Turn phase of the combat</param>
    /// <returns>Unit stack selected</returns>
    public UnitStack SelectDefendingUnitStack(List<UnitStack> stacks, UnitStack attacker, Combat.TurnPhase phase)
    {
        switch(_level)
        {
            case 1:
				// general-level AI takes into account successful defense odds
				// and selects the stack that has the lowest exectated replacement cost
                return SelectMinReplacementCostUnit(stacks, attacker, phase);
            default:
				// captain-level AI selects the cheapest unit stack
                return SelectCheapestUnit(stacks);
        }
    }

    /// <summary>
    /// Select the unit stack which has the lowest training cost
    /// </summary>
    /// <param name="stacks">Available unit stacks</param>
    /// <returns>Unit stack selected</returns>
    public UnitStack SelectCheapestUnit(List<UnitStack> stacks)
    {
        UnitStack result = null;
        int trainingCost = 0;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].GetTotalQty() == 0)
            {
                FileLogger.Trace("TAI", stacks[i].GetUnitType().GetName() + " is empty");
                continue;
            }
            if (result == null)
            {
                result = stacks[i];
                trainingCost = stacks[i].GetUnitType().GetTrainingCost();
                FileLogger.Trace("TAI", "Initial selection: " + stacks[i].GetUnitType().GetName() + ", training cost: " + trainingCost.ToString());
            }
            else
            {
                int alternativeTrainingCost = stacks[i].GetUnitType().GetTrainingCost();
                if (alternativeTrainingCost > 0 && alternativeTrainingCost < trainingCost)
                {
                    result = stacks[i];
                    trainingCost = alternativeTrainingCost;
                    FileLogger.Trace("TAI", "Current selection: " + stacks[i].GetUnitType().GetName() + ", training cost: " + trainingCost.ToString());
                }
                else
                {
                    FileLogger.Trace("TAI", stacks[i].GetUnitType().GetName() + " is more expensive to train: " + alternativeTrainingCost.ToString());
                }
            }
        }
        if (result != null)
        {
            FileLogger.Trace("TAI", "Easy Level AI selected  " + result.GetUnitType().GetName() + " as a target");
        }
        return result;
    }

    /// <summary>
    /// Select the unit stack which is going to be cheapest to re-train, taking into account odds of successful defense
    /// </summary>
    /// <param name="stacks">Available unit stacks</param>
    /// <param name="attacker">Attacking enemy unit stack</param>
    /// <param name="phase">Turn phase of the combat</param>
    /// <returns>Unit stack selected</returns>
    public UnitStack SelectMinReplacementCostUnit(List<UnitStack> stacks, UnitStack attacker, Combat.TurnPhase phase)
    {
        UnitStack result = null;
        double replacementCost = double.MaxValue;
        double altervative;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].GetTotalQty() > 0)
            {
                altervative = CombatHelper.Instance.EstimateStackAttacksDamage(attacker, stacks[i], phase) * stacks[i].GetUnitType().GetTrainingCost() / stacks[i].GetUnitType().GetHitPoints();
                if (altervative < replacementCost)
                {
                    replacementCost = altervative;
                    result = stacks[i];
                }
            }
        }
        if (result != null)
        {
            FileLogger.Trace("TAI", "Medium Level AI selected " + result.GetUnitType().GetName() + " as a target, expected replacement cost is " + replacementCost);
        }
        return result;
    }

}
