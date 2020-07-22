/// <summary>
/// Representation of a collection of army movement orders
/// </summary>

using System.Collections.Generic;

public class ArmyMovementOrdersCollection
{
    private List<ArmyMovementOrder> _orders;

    /// <summary>
    /// Class constructor
    /// </summary>
    public ArmyMovementOrdersCollection()
    {
        _orders = new List<ArmyMovementOrder>();
    }

    /// <summary>
    /// Add an army movement order to the collection
    /// </summary>
    /// <param name="order">Army movement order to add to the collection</param>
    /// <returns>True if the order was added, false if another order with the same origin and destination was found and updated</returns>
    public bool AddArmyMovementOrder(ArmyMovementOrder order)
    {
        for (int i = 0; i < _orders.Count; i++)
        {
            if (_orders[i].GetOrigin() == order.GetOrigin() && _orders[i].GetDestination() == order.GetDestination())
            {
                _orders[i].AddUnits(order.GetUnits());
                return false;
            }
        }
        _orders.Add(order);
        return true;
    }

    /// <summary>
    /// Clear the orders collection
    /// </summary>
    public void Clear()
    {
        _orders.Clear();
    }

    /// <summary>
    /// Does the colleciton include a combat move?
    /// </summary>
    /// <returns>Whether the collection includes a combat move</returns>
    public bool DoesIncludeCombatMoves()
    {
        for (int i = 0; i < _orders.Count; i++)
        {
            if (_orders[i].IsCombatMove())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Does the colleciton include a non-combat move?
    /// </summary>
    /// <returns>Whether the collection includes a non-combat move</returns>
    public bool DoesIncludeNonCombatMoves()
    {
        for (int i = 0; i < _orders.Count; i++)
        {
            if (!_orders[i].IsCombatMove())
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get all movement orders
    /// </summary>
    /// <returns>List of army movement orders</returns>
    public List<ArmyMovementOrder> GetAllOrders()
    {
        return _orders;
    }

    /// <summary>
    /// Remove movement orders with a specific destinaiton from the collection
    /// </summary>
    /// <param name="target">Destinaiton province of the movement orders</param>
    /// <returns>True if the order was added, false if another order with the same origin and destination was found and updated</returns>
    public void RemoveOrdersTargetingProvince(Province target)
    {
        for (int i = _orders.Count - 1; i > -1; i--)
        {
            if (_orders[i].GetDestination() == target)
            {
                _orders.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Remove all non-combat movement orders from the collection
    /// </summary>
    public void RemoveNonCombatMoveOrders()
    {
        _orders.RemoveAll(order => order.IsCombatMove() == false);
    }

}
