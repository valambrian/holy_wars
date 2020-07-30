/// <summary>
/// Event fired when a unit stack was chosen as a target
/// </summary>

using System;


public class UnitStackAttackedEvent : EventArgs
{
    AttackRollResultsCollection _attacks;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="attacks">Collection of attack roll results</param>
    public UnitStackAttackedEvent(AttackRollResultsCollection attacks)
    {
        _attacks = attacks;
    }

    /// <summary>
    /// Get the collection of attack roll results
    /// </summary>
    /// <returns>Attack roll results</returns>
    public AttackRollResultsCollection GetAttacks()
    {
        return _attacks;
    }
}
