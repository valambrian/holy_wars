/// <summary>
/// A number of combatants of the same unit type
/// </summary>

public class Unit
{
    private UnitData _data;
    private UnitType _unitType;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Serialized unit data</param>
    /// <param name="unitType">Type of the unit</param>
    public Unit(UnitData data, UnitType unitType)
    {
        _data = new UnitData(data);
        _unitType = unitType;
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="original">The unit to clone</param>
    public Unit(Unit original)
    {
        _data = new UnitData(original._data);
        _unitType = original._unitType;
    }

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="unitType">Type of the unit</param>
    /// <param name="quantity">The number of combatants in the unit</param>
    public Unit(UnitType unitType, int quantity)
    {
        _data = new UnitData(unitType.GetId(), quantity);
        _unitType = unitType;
    }

    /// <summary>
    /// Get unit type
    /// </summary>
    /// <returns>Unit type</returns>
    public UnitType GetUnitType()
    {
        return _unitType;
    }

    /// <summary>
    /// Get the number of combatants in the unit
    /// </summary>
    /// <returns>The number of combatants in the unit</returns>
    public int GetQuantity()
    {
        return _data.qty;
    }

    /// <summary>
    /// Get the cost of training the unit
    /// </summary>
    /// <returns>The cost of training the unit</returns>
    public int GetTrainingCost()
    {
        return GetQuantity() * _unitType.GetTrainingCost();
    }

    /// <summary>
    /// Increase the number of combatants in the unit
    /// Or decrease it if the parameter is negative
    /// </summary>
    /// <param name="delta">The additional number of combatants</param>
    public void AddQuantity(int delta)
    {
        _data.qty += delta;
    }

    /// <summary>
    /// Set the number of combatants in the unit
    /// </summary>
    /// <param name="quantity">The number of combatants</param>
    public void SetQuantity(int quantity)
    {
        _data.qty = quantity;
    }

    /// <summary>
    /// Set the unit type
    /// </summary>
    /// <param name="unitType">The unit type</param>
    public void SetUnitType(UnitType unitType)
    {
        _data.id = unitType.GetId();
        _unitType = unitType;
    }

}

