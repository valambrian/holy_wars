/// <summary>
/// Collection of attack rolls results
/// </summary>

using System;
using System.Collections.Generic;

public class AttackRollResultsCollection
{
    public event EventHandler<EventArgs> Updated;

    private List<AttackRollResult> _model;
    // this is just for convenience - think of it as a cached value
    private UnitStack _unitStack;

    /// <summary>
    /// Class constructor
    /// </summary>
    public AttackRollResultsCollection()
    {
        _model = new List<AttackRollResult>();
    }

    /// <summary>
    /// Add another attack roll result to the collection
    /// </summary>
    /// <param name="result">Attack roll result to add</param>
    /// <returns>Whether the operation was successful</returns>
    public bool AddAttackRollResult(AttackRollResult result)
    {
        if (_model.Count > 0 && result.UnitStack != _unitStack)
        {
            return false;
        }
        _model.Add(result);
        _unitStack = result.UnitStack;
        if (Updated != null)
        {
            Updated(this, EventArgs.Empty);
        }
        return true;
    }

    /// <summary>
    /// Remove an attack roll result from the collection
    /// </summary>
    /// <param name="result">Attack roll result to remove</param>
    /// <returns>Whether the operation was successful</returns>
    public bool RemoveAttackRollResult(AttackRollResult result)
    {
        bool success = _model.Remove(result);
        if (success && Updated != null)
        {
            Updated(this, EventArgs.Empty);
        }
        return success;
    }

    /// <summary>
    /// Get attacking unit stack
    /// </summary>
    /// <returns>Attacking unit stack</returns>
    public UnitStack GetUnitStack()
    {
        return _unitStack;
    }

    /// <summary>
    /// Get attacking unit stack's type
    /// </summary>
    /// <returns>Attacking stack's unit type</returns>
    public UnitType GetUnitType()
    {
        return _unitStack.GetUnitType();
    }

    /// <summary>
    /// Get number of elements in the collection
    /// </summary>
    /// <returns>Number of elements in the collection</returns>
    public int Count
    {
        get
        {
            return _model.Count;
        }
    }

    /// <summary>
    /// Get a collection element by index
    /// </summary>
    /// <param name="i">Element's index in the collection</param>
    /// <returns>Attack roll result corresponding to the index</returns>
    public AttackRollResult GetAt(int i)
    {
        return _model[i];
    }

    /// <summary>
    /// Clear the collection
    /// </summary>
    public void Clear()
    {
        _model.Clear();
        if (Updated != null)
        {
            Updated(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Check if the collection contains an attack roll result
    /// </summary>
    /// <param name="attackRoll">Attack roll result to check</param>
    /// <returns>Whether the collection contains this attack roll result</returns>
    public bool Contains(AttackRollResult attackRoll)
    {
        return _model.Contains(attackRoll);
    }
}
