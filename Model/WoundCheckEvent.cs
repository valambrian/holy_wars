/// <summary>
/// Event fired when a unit stack performs a wound check
/// </summary>

using System;

public class WoundCheckEvent : EventArgs
{
    public int _qty;
    public int _hp;
    public int _target;
    public int _roll;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="qty">The number of combatants in the unit stack</param>
    /// <param name="hp">Total hit points of combatants in the unit stack</param>
    /// <param name="target">Target number to roll for successful check</param>
    /// <param name="roll">Number rolled</param>
    public WoundCheckEvent(int qty, int hp, int target, int roll)
    {
        _qty = qty;
        _hp = hp;
        _target = target;
        _roll = roll;
    }
}
