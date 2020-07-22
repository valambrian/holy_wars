/// <summary>
/// Class representing a dungeon exploration event
/// </summary>

using System.Collections.Generic;

public class ExpeditionEvent : GameEvent
{
    private KeyValuePair<Unit, Province> _whoWhere;
    private int _cost;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="whoWhere">The hero involved in the expedition and its location</param>
    /// <param name="cost">Cost of funding the expedition</param>
    public ExpeditionEvent(KeyValuePair<Unit, Province> whoWhere, int cost) : base("", 2)
    {
        _whoWhere = whoWhere;
        _cost = cost;

        _text = "My lord, would you like our heroic " + whoWhere.Key.GetUnitType().GetName() +
                " to lead a dungeon exploration expedition? It would cost us " + cost.ToString() +
                " gold pieces, but could allow us to rediscover secrets of magic.";
    }

	/// <summary>
	/// Get info about the explorer and the location of the expedition
	/// </summary>
    /// <returns>Tuple containing the hero involved in the expedition and its location</returns>
    public KeyValuePair<Unit, Province> GetExpeditionDetails()
    {
        return _whoWhere;
    }

	/// <summary>
	/// Get the cost of funding the expedition
	/// </summary>
    /// <returns>Cost of funding the expedition in gold coins</returns>
    public int GetCost()
    {
        return _cost;
    }
}
