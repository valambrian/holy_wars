/// <summary>
/// A training order for a province.
/// </summary>

[System.Serializable]
public class UnitTrainingOrderData
{
    // this is unit type id
    public int id;
    public int qty;
    public bool standing;

    public UnitTrainingOrderData(int unitTypeId, int quantity, bool isOrderStanding)
    {
        id = unitTypeId;
        qty = quantity;
        standing = isOrderStanding;
    }
}
