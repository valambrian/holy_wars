/// <summary>
/// Representation of a unit training plan
/// Contains the target province, the list of army movement orders, and derived data items
/// </summary>

public class UnitTrainingPlan
{
    public enum Reason { COUNTER, CORE, FODDER };

    private Reason _reason;
    private int _targetUnitTypeId;
    private int _targetProvinceId;
    private UnitTrainingOrder _unitTrainingOrder;

    public UnitTrainingPlan(UnitTrainingOrder order, Reason reason, int targetUnitTypeId = 0, int targetProvinceId = 0)
    {
        _unitTrainingOrder = order;
        _reason = reason;
        _targetUnitTypeId = targetUnitTypeId;
        _targetProvinceId = targetProvinceId;
    }

    public UnitTrainingOrder GetUnitTrainingOrder()
    {
        return _unitTrainingOrder;
    }

    public int GetManpowerCost()
    {
        return _unitTrainingOrder.GetQuantity();
    }

    public int GetCost()
    {
        return _unitTrainingOrder.GetCost();
    }

    public int GetUnitTypeTrainingCost()
    {
        return _unitTrainingOrder.GetUnitType().GetTrainingCost();
    }

    public Reason GetReason()
    {
        return _reason;
    }

    public int GetCounteredUnitTypeId()
    {
        return _targetUnitTypeId;
    }

    public int GetCounteredUnitProvinceId()
    {
        return _targetProvinceId;
    }

    public void DecreaseQuantity()
    {
        _unitTrainingOrder.DecreaseQuantity();
    }


}
