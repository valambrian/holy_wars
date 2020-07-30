/// <summary>
/// Represents data for a map cell
/// </summary>

using UnityEngine;

public class MapCell
{

    // serializable data
    private MapCellData _data;

    // cached data
    private Province _province;
    // this is a number that specifies whether this cell's neighbors belong to a different province
    // see HexBorder.Direction for the order of the directions
    // see HexBorder.ConvertDeltaToBorderIndex for calculations
    private int _provinceBordersIndex;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Map cell data loaded from a save</param>
    /// <param name="province">The province including the map cell</param>
    public MapCell(MapCellData data, Province province)
    {
        _data = data;
        _province = province;
        if (_data.provinceId != province.GetId())
        {
            Debug.Log("Bad province data for map cell at " + _data.x + ", " + _data.y + ": " + _data.provinceId+ " is changed to " + province.GetId());
            _data.provinceId = province.GetId();
        }
        // assume that all neighbors belong to a different province or provinces
        _provinceBordersIndex = 63;
    }

	/// <summary>
	/// What race lives here?
	/// </summary>
    /// <returns>Race of the map cell dwellers</returns>
    public Race GetDwellersRace()
    {
        return _province.GetDwellersRace();
    }

	/// <summary>
	/// What faction owns this place?
	/// </summary>
    /// <returns>Faction owning the map cell</returns>
    public Faction GetOwnersFaction()
    {
        return _province.GetOwnersFaction();
    }

    /// <summary>
    /// Establish a relation with a neighboring map cell
    /// </summary>
    /// <param name="neighbor">Neighboring map cell</param>
    /// <param name="deltaX">X coordinate offset to the neighbor</param>
    /// <param name="deltaY">Y coordinate offset to the neighbor</param>
    public void SetNeighbor(MapCell neighbor, int deltaX, int deltaY)
    {
        if (neighbor != null)
        {
            int borderIndex = HexBorder.ConvertDeltaToBorderIndex(deltaX, deltaY, _data.x % 2 == 1);
            if (neighbor.GetProvinceId() == GetProvinceId())
            {
				// if the neighboring map cell belongs to the same province,
				// there should be no border between them, so
                // set the corresponding bit to zero
                int index = 1 << borderIndex;
                _provinceBordersIndex = _provinceBordersIndex ^ index;
            }
            else
            {
                _province.AddNeighbor(neighbor.GetProvince());
                neighbor.GetProvince().AddNeighbor(_province);
            }
        }
    }

	/// <summary>
	/// Get id of the province that includes this map cell
	/// </summary>
    /// <returns>Id of the province that includes this map cell</returns>
    public int GetProvinceId()
    {
        return _data.provinceId;
    }

	/// <summary>
	/// Get the province that includes this map cell
	/// </summary>
    /// <returns>The province that includes this map cell</returns>
    public Province GetProvince()
    {
        return _province;
    }

	/// <summary>
	/// Does the map cell have a border in the specified direction?
	/// </summary>
    /// <returns>Whether the map cell has a border in the specified direction</returns>
    public bool HasBorder(HexBorder.Direction direction)
    {
        int index = (int)(direction);
        return (1 & _provinceBordersIndex >> index) != 0;
    }

	/// <summary>
	/// Get X coordinate of the map cell
	/// </summary>
    /// <returns>X coordinate of the map cell</returns>
    public int GetX()
    {
        return _data.x;
    }

	/// <summary>
	/// Get Y coordinate of the map cell
	/// </summary>
    /// <returns>Y coordinate of the map cell</returns>
    public int GetY()
    {
        return _data.y;
    }

	/// <summary>
	/// Is the map cell located in the center of its province?
	/// </summary>
    /// <returns>Whether the map cell is the center of its provinec</returns>
    public bool IsCenterOfProvince()
    {
        return _data.center;
    }

}
