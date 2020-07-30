/// <summary>
/// Class representing an in-game event
/// </summary>

public class GameEvent
{
    protected string _text;
    protected int _numberOfOptions;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="text">Event test to be displayed to the player</param>
    /// <param name="numberOfOptions">Number of reactions to choose from</param>
    public GameEvent(string text, int numberOfOptions)
    {
        _text = text;
        _numberOfOptions = numberOfOptions;
    }

	/// <summary>
	/// Get event text`
	/// </summary>
    /// <returns>The event text</returns>
    public string GetText()
    {
        return _text;
    }

	/// <summary>
	/// Get number of response options
	/// </summary>
    /// <returns>The number of response options</returns>
    public int GetNumberOfOptions()
    {
        return _numberOfOptions;
    }
}
