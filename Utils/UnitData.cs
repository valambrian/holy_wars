[System.Serializable]
public class UnitData
{
    // this is id of the type, not of the unit
    public int id;
    public int qty;

    public UnitData(int unitTypeId, int quantity)
    {
        id = unitTypeId;
        qty = quantity;
    }

    public UnitData(UnitData original)
    {
        id = original.id;
        qty = original.qty;
    }

    public override string ToString()
    {
        return "Unit, type id: " + id + ", quantity: " + qty;
    }
}

