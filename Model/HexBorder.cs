/// <summary>
/// Support for flat-top hexagons' borders
/// HexBorder describes the border between a hex and one of its neighbors
/// </summary>

public class HexBorder
{
    public enum Direction { LOWER_LEFT = 0, UPPER_LEFT, UP, UPPER_RIGHT, LOWER_RIGHT, BOTTOM };

    private static HexBorder[] _evenColumnCellBorders = new HexBorder[6]
    {
        new HexBorder(-1, -1, Direction.LOWER_LEFT),
        new HexBorder(-1,  0, Direction.UPPER_LEFT),
        new HexBorder( 0,  1, Direction.UP),
        new HexBorder( 1,  0, Direction.UPPER_RIGHT),
        new HexBorder( 1, -1, Direction.LOWER_RIGHT),
        new HexBorder( 0, -1, Direction.BOTTOM)
    };

    private static HexBorder[] _oddColumnCellBorders = new HexBorder[6]
    {
        new HexBorder(-1,  0, Direction.LOWER_LEFT),
        new HexBorder(-1,  1, Direction.UPPER_LEFT),
        new HexBorder( 0,  1, Direction.UP),
        new HexBorder( 1,  1, Direction.UPPER_RIGHT),
        new HexBorder( 1,  0, Direction.LOWER_RIGHT),
        new HexBorder( 0, -1, Direction.BOTTOM)
    };

    private int _deltaX;
    private int _deltaY;
    private Direction _direction;

    /// <summary>
    /// Class constructor
	/// (deltaX, deltaY) represents coordinates offset for the adjacent hex
	/// Direction is the movement direction to the adjacent hex
    /// </summary>
    /// <param name="deltaX">X coordinate offset for the adjacent hex</param>
    /// <param name="deltaY">Y coordinate offset for the adjacent hex</param>
    /// <param name="direction">Movement direction to the adjacent hex</param>
    public HexBorder(int deltaX, int deltaY, Direction direction)
    {
        _deltaX = deltaX;
        _deltaY = deltaY;
        _direction = direction;
    }

    /// <summary>
    /// Given the adjacent hex's coordinate offset, get direction's index in the list of enumerator's values
    /// </summary>
    /// <param name="deltaX">X coordinate offset for the adjacent hex</param>
    /// <param name="deltaY">Y coordinate offset for the adjacent hex</param>
    /// <param name="oddColumn">Whether the current column is odd or even (this is important for flat-top hexes)</param>
    /// <returns>Related direction's index in the list of enumerator's values</returns>
    public static int ConvertDeltaToBorderIndex(int deltaX, int deltaY, bool oddColumn)
    {
        HexBorder[] borders = GetBorderDirections(oddColumn);
        for (int i = 0; i < borders.Length; i++)
        {
            if (borders[i]._deltaX == deltaX && borders[i]._deltaY == deltaY)
            {
                return i;
            }
        }
        string messageTail = oddColumn ? ") for odd column." : ") for even column.";
        throw new System.ArgumentOutOfRangeException("Invalid map cell delta: (" + deltaX + ", " + deltaY + messageTail);
    }

    /// <summary>
    /// Get a list of possible hex borders
    /// </summary>
    /// <param name="oddColumn">Whether the current column is odd or even (this is important for flat-top hexes)</param>
    /// <returns>The list of possible hex borders</returns>
    public static HexBorder[] GetBorderDirections(bool oddColumn)
    {
        return oddColumn ? _oddColumnCellBorders : _evenColumnCellBorders;
    }

    /// <summary>
    /// Get X coordinate offset
    /// </summary>
    /// <returns>X coordinate offset</returns>
    public int GetDeltaX()
    {
        return _deltaX;
    }

    /// <summary>
    /// Get Y coordinate offset
    /// </summary>
    /// <returns>Y coordinate offset</returns>
    public int GetDeltaY()
    {
        return _deltaY;
    }

}
