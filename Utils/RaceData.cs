/// <summary>
/// Multiple factions can belong to the same race (especially minor factions)
/// This is serializable data describing a race
/// </summary>

[System.Serializable]
public class RaceData
{
    public int id;
    public string name;
    // per province per turn
    public int incomeBonus;
    // allows to train manpowerBonus more units per province per turn
    public int manpowerBonus;
    // per province per turn
    public int favorBonus;
    // units available for the race to train
    public int[] units;
    public IntIntPair[] magicUpgrades;
    public IntIntPair[] divineUpgrades;
    public int[] divineAdditions;
    public bool canBribe;

    public override string ToString()
    {
        string result = "Race, name: " + name + ", id: " + id + ", income bonus: " + incomeBonus + ", manpower bonus: " + manpowerBonus + ", favor bonus: " + favorBonus + ", units: [";
        if (units.Length > 0)
        {
            result += units[0];
        }
        for (int i = 1; i < units.Length; i++)
        {
            result += ", " + units[i];
        }
        result += "]";
        return result;
    }

}
