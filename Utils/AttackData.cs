[System.Serializable]
public class AttackData
{
    public enum Quality { DIVINE = 0, MAGIC, GUNPOWDER, RANGED, SKIRMISH, CHARGE, MELEE, AP, FIRE, LIGHTNING, ILLUSORY, NONE };

    public int quantity = 1;
    public int skill;
    public string damage;
    public Quality[] qualities;

    public override string ToString()
    {
        string result = "Attack, quantity: " + quantity + ", skill: " + skill + ", damage: " + damage;

        if (qualities != null && qualities.Length > 0)
        {
            result += ", qualities: [" + qualities[0];
            for (int i = 1; i < qualities.Length; i++)
            {
                result += ", " + qualities[i];
            }
            result += "]";
        }
        return result;
    }

}
