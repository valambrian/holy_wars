/// <summary>
/// Class implementing dice rolls
/// Includes only static functionality
/// </summary>

using System;
using System.Collections.Generic;

public class Dice
{
	/// chances to roll a result from 4 to 24
    private static int[] _4d6 = { 1, 4, 10, 20, 35, 56, 80, 104, 125, 140, 146, 140, 125, 104, 80, 56, 35, 20, 10, 4, 1 };
	/// the denominator to calculate probability based on chances to roll a result
    private static int _4d6Total = 1296;

	/// <summary>
	/// Get probability to get a number by rolling 4 d6 dice
	/// </summary>
    /// <param name="outcome">Rolled result</param>
    /// <returns>Probability to get the outcome by rolling 4 d6 dice</returns>
    public static float Get4D6Probability(int outcome)
    {
        if (outcome < 4 || outcome > 24)
            throw new Exception("Rolling 4d6 can't yield " + outcome.ToString());
        return (float)_4d6[outcome] / _4d6Total;
    }

	/// <summary>
	/// Roll a number of d6 dice
	/// </summary>
    /// <param name="numberOfDice">Number of dice to roll</param>
    /// <returns>Rolled result</returns>
    public static int Roll(int numberOfDice)
    {
        int result = 0;
        for (int i = 0; i < numberOfDice; i++)
        {
            if (UnityEngine.Random.Range(0.0f, 1.0f) >= 0.5f)
            {
                result += 1;
            }
        }
        return result;
    }

	/// <summary>
	/// Generate a result of rolling dice from a pool
	/// </summary>
    /// <param name="pool">Dice pool: number of sides => number of dice</param>
    /// <param name="rerollThreshold">Threshold to trigger reroll of an open roll (0 for closed roll)</param>
    /// <returns>Rolled result</returns>
    public static int Roll(Dictionary<int, int> pool, int rerollThreshold = 0)
    {
        int result = 0;
        foreach (KeyValuePair<int, int> pair in pool)
        {
            result += RollPool(pair.Key, pair.Value, rerollThreshold);
        }
        return result;
    }

	/// <summary>
	/// Roll a die
	/// </summary>
    /// <param name="sides">Number of the die's sides</param>
    /// <param name="rerollThreshold">Threshold to trigger reroll of an open roll (0 for closed roll)</param>
    /// <returns>Rolled result</returns>
    public static int RollDie(int sides, int rerollThreshold = 0)
    {
        int result = UnityEngine.Random.Range(1, sides + 1);
        if (rerollThreshold == 0 || result < rerollThreshold)
            return result;
        return result + RollDie(sides, rerollThreshold);
    }

	/// <summary>
	/// Generate a result of rolling dice from a pool
	/// </summary>
    /// <param name="sides">Number of the die's sides</param>
    /// <param name="size">Number of the dice to roll</param>
    /// <param name="rerollThreshold">Threshold to trigger reroll of an open roll (0 for closed roll)</param>
    /// <returns>Rolled result</returns>
    private static int RollPool(int sides, int size, int rerollThreshold)
    {
        if (sides < 0)
            return -RollPool(-sides, size, rerollThreshold);
        if (sides == 0)
            throw new Exception("Illegal number of die sides: zero.");
        int result = 0;
        for (int i = 0; i < size; i++)
        {
            result += RollDie(sides, rerollThreshold);
        }
        return result;
    }

}

