using System;

[System.Serializable]
public class UnitTypeData
{
    public int id;
    public string name;
    public int[] spells;
    public AttackData[] attacks;
    public int defense;
    public int armor;
    public int shield;
    public int health;
    public int cost;
    public bool hero;
    public bool holy;
    public int[] spellLikes;
    public string info;

    public UnitTypeData(UnitTypeData other)
    {
        id = other.id;
        name = other.name;
        spells = null;
        attacks = null;
        defense = other.defense;
        armor = other.armor;
        shield = other.shield;
        health = other.health;
        cost = other.cost;
        hero = other.hero;
        holy = other.holy;
        spellLikes = null;

        if (other.spells != null)
        {
            spells = new int[other.spells.Length];
            Array.Copy(other.spells, spells, spells.Length);
        }

        if (other.attacks != null)
        {
            attacks = new AttackData[other.attacks.Length];
            Array.Copy(other.attacks, attacks, attacks.Length);
        }

        if (other.spellLikes != null)
        {
            spellLikes = new int[other.spellLikes.Length];
            Array.Copy(other.spellLikes, spellLikes, spellLikes.Length);
        }
    }

public override string ToString()
    {
        string result = "Unit type, name: " + name + ", id: " + id + ", cost: " + cost + ", defense: " + defense + ", armor: " + armor + ", shield: " + shield + ", health: " + health + ", attacks:\n";
        for (int i = 0; i < attacks.Length; i++)
        {
            result += (i + 1).ToString() + ". " + attacks[i] + "\n";
        }
        return result;
    }
}

