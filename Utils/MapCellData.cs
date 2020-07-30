[System.Serializable]
public class MapCellData
{
    public int x;
    public int y;
    public int provinceId;
    public bool center;

    public MapCellData(int xCoord, int yCoord, int province, bool isCenter = false)
    {
        x = xCoord;
        y = yCoord;
        provinceId = province;
        center = isCenter;
    }

    public override string ToString()
    {
        return "Map cell at x = " + x + " and y = " + y + " belonging to province " + provinceId;
    }

}
