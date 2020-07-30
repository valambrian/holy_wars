/// <summary>
/// "Players" in the game
/// </summary>

[System.Serializable]
public class FactionData
{
    // lesson: serializable classes don't get along with properties
    public int id;
    public string name;
    // race id
    public int race;
    // will train new units / seek expansion / etc ?
    public bool isMajor;
    // can be controlled by the player?
    public bool isPlayable;
    // is controlled by the player?
    public bool isPC;
    public int capital;
    public int money;
    public int favors;
    public int expeditions;
    public bool rediscoveredMagic;
    public bool reachedDivine;
    public int level;

    public override string ToString()
    {
        return "Faction " + name + ", id = " + id + ", is major: " + isMajor + ", is playable: " + isPlayable;
    }

}
