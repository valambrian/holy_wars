/// <summary>
/// A training order representation
/// </summary>

using System.Collections.Generic;

public class UnitTrainingOrder
{
    private UnitTrainingOrderData _data;
    private UnitType _unitType;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="unitType">The type of the unit to train</param>
    /// <param name="quantity">The number of combatants to train</param>
    /// <param name="isOrderStanding">Whether the order should be executed every turn</param>
    public UnitTrainingOrder(UnitType unitType, int quantity, bool isOrderStanding)
    {
        _unitType = unitType;
        _data = new UnitTrainingOrderData(unitType.GetId(), quantity, isOrderStanding);
    }

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Serialized unit training order data</param>
    /// <param name="unitTypes">Hash of unit type id => unit type of in-game units</param>
    public UnitTrainingOrder(UnitTrainingOrderData data, Dictionary<int, UnitType> allUnitTypes)
    {
        _data = data;
        _unitType = allUnitTypes[_data.id];
    }

    /// <summary>
    /// Get the number of combatants planned to train
    /// </summary>
    /// <returns>The number of combatants in the order</returns>
    public int GetQuantity()
    {
        return _data.qty;
    }

    /// <summary>
    /// Should the order be executed every turn?
    /// </summary>
    /// <returns>Whether the order should be executed every turn</returns>
    public bool IsStanding()
    {
        return _data.standing;
    }

    /// <summary>
    /// Get the cost of training the unit
    /// </summary>
    /// <returns>The cost of training the unit</returns>
    public int GetCost()
    {
        return _unitType.GetTrainingCost() * GetQuantity();
    }

    /// <summary>
    /// Get the unit type the order is about
    /// </summary>
    /// <returns>The type of the unit to train</returns>
    public UnitType GetUnitType()
    {
        return _unitType;
    }

    /// <summary>
    /// Decrease the number of combatants planned to train by one
    /// </summary>
    public void DecreaseQuantity()
    {
        if (_data.qty > 0)
        {
            _data.qty--;
        }
    }

}
