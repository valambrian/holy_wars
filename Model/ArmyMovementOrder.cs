/// <summary>
/// Representation of an army movement order (both combat and non-combat)
/// Contains the starting and the ending provinces and the list units to move
/// </summary>

using System;
using System.Collections.Generic;

public class ArmyMovementOrder
{
    public event EventHandler<EventArgs> Updated;

    private Province _origin;
    private Province _destination;
    private List<Unit> _units;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="origin">The province where the movement originates</param>
    /// <param name="destination">The province to which units are moving</param>
    /// <param name="units">List of moving units</param>
   public ArmyMovementOrder(Province origin, Province destination, List<Unit> units)
    {
        _origin = origin;
        _destination = destination;
        _units = units;
    }

    /// <summary>
    /// Will the movement result in a combat?
    /// </summary>
    /// <returns>Whether the move will result in a combat</returns>
    public bool IsCombatMove()
    {
        return _origin.GetOwnersFaction().GetId() != _destination.GetOwnersFaction().GetId();
    }

    /// <summary>
    /// Get the destinaiton province of the movement
    /// </summary>
    /// <returns>The destination province</returns>
    public Province GetDestination()
    {
        return _destination;
    }

    /// <summary>
    /// Get the origin province of the movement
    /// </summary>
    /// <returns>The origin province</returns>
    public Province GetOrigin()
    {
        return _origin;
    }

    /// <summary>
    /// Get list of moving units
    /// </summary>
    /// <returns>List of the moving units</returns>
    public List<Unit> GetUnits()
    {
        return _units;
    }

    /// <summary>
    /// Update the movement order by setting the list of units
    /// </summary>
    /// <param name="units">List of units to move</param>
    public void SetUnits(List<Unit> units)
    {
        _units = units;
        NotifyObservers();
    }

    /// <summary>
    /// Update the movement order by adding units to move
    /// </summary>
    /// <param name="units">List of units to add to the order</param>
    public void AddUnits(List<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            AddUnit(units[i]);
        }
        NotifyObservers();
    }

    /// <summary>
    /// Add a unit to the movement order
    /// </summary>
    /// <param name="unit">The unit to add to the order</param>
    private void AddUnit(Unit unit)
    {
        if (unit.GetQuantity() > 0)
        {
            int unitTypeId = unit.GetUnitType().GetId();
            for (int i = 0; i < _units.Count; i++)
            {
                if (unitTypeId == _units[i].GetUnitType().GetId())
                {
                    _units[i].AddQuantity(unit.GetQuantity());
                    return;
                }
            }
            _units.Add(unit);
        }
    }

    /// <summary>
    /// Get the number of moving units
    /// </summary>
    /// <returns>The number of moving units</returns>
    public int GetUnitsCount()
    {
        int result = 0;
        for (int i = 0; i < _units.Count; i++)
        {
            result += _units[i].GetQuantity();
        }

        return result;
    }

    /// <summary>
    /// Notify observers, if any, that the order was updated
    /// </summary>
    private void NotifyObservers()
    {
        if (Updated != null)
        {
            Updated(this, EventArgs.Empty);
        }
    }

}
