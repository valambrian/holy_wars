using System.Collections.Generic;
using UnityEngine;

public class UnitTypeView : MonoBehaviour
{
    [SerializeField]
    private TextMesh _name;

    [SerializeField]
    private TextMesh[] _labels;

    [SerializeField]
    private TextMesh[] _lines;

    [SerializeField]
    private TextMesh _defense;

    [SerializeField]
    private TextMesh _shield;

    [SerializeField]
    private TextMesh _armor;

    [SerializeField]
    private TextMesh _health;

    private UnitType _unitType;

    public void SetModel(UnitType unitType)
    {
        _unitType = unitType;
        _name.text = _unitType.GetName();
        if (_unitType.IsHero())
        {
            _name.text += " (hero)";
        }
        _defense.text = _unitType.GetDefense().ToString();
        _shield.text = _unitType.GetShield().ToString();
        _armor.text = _unitType.GetArmor().ToString();
        _health.text = _unitType.GetHitPoints().ToString();
        List<Attack> attacks = _unitType.GetAllAttacks();
        List<Spell> spells = _unitType.GetSpells();
        List<Spell> abilities = _unitType.GetSpellLikeAbilities();

        int currentIndex = 0;
        string currentLabel = "";
        if (attacks.Count == 1)
        {
            currentLabel = "Attack:";
            DisplayAttack(currentIndex, currentLabel, attacks[0]);
            // attacks take 2 lines to display
            currentIndex += 2;
        }
        else if (attacks.Count > 1)
        {
            currentLabel = "Attacks:";
            int maxLinesAllowed = Mathf.Min(_labels.Length - currentIndex, attacks.Count);
            for (int i = 0; i < maxLinesAllowed; i++)
            {
                DisplayAttack(currentIndex, currentLabel, attacks[i]);
                currentIndex += 2;
                currentLabel = "";
            }
        }

        if (currentIndex < _labels.Length)
        {
            if (spells.Count == 1)
            {
                currentLabel = "Spell:";
                DisplaySpell(currentIndex, currentLabel, spells[0]);
                currentIndex++;
            }
            else if (spells.Count > 1)
            {
                currentLabel = "Spells:";
                int maxLinesAllowed = Mathf.Min(_labels.Length - currentIndex, spells.Count);
                for (int i = 0; i < maxLinesAllowed; i++)
                {
                    DisplaySpell(currentIndex, currentLabel, spells[i]);
                    currentIndex++;
                    currentLabel = "";
                }
            }
        }

        if (currentIndex < _labels.Length)
        {
            if (abilities.Count == 1)
            {
                currentLabel = "Ability:";
                DisplaySpell(currentIndex, currentLabel, abilities[0]);
                currentIndex++;
            }
            else if (abilities.Count > 1)
            {
                currentLabel = "Abilities:";
                int maxLinesAllowed = Mathf.Min(_labels.Length - currentIndex, abilities.Count);
                for (int i = 0; i < maxLinesAllowed; i++)
                {
                    DisplaySpell(currentIndex, currentLabel, abilities[i]);
                    currentIndex++;
                    currentLabel = "";
                }
            }
        }

        for (int i = currentIndex; i < _labels.Length; i++)
        {
            DisplayEmpty(i);
        }
    }

    private string FormatAttackDataStringPart1(Attack attack)
    {
        string result = "";
        List<AttackData.Quality> qualities = attack.GetQualities();
        if (attack.GetNumberOfAttacks() > 1)
        {
            result = attack.GetNumberOfAttacks().ToString() + " x ";
        }
        for (int i = 0; i < qualities.Count; i++)
        {
            result += qualities[i] + ", ";
        }
        return result;
    }

    private string FormatAttackDataStringPart2(Attack attack)
    {
        return "skill: " + attack.GetSkill() + ", damage: " + attack.GetDamageExpression();
    }

    private void DisplayAttack(int index, string label, Attack attack)
    {
        _labels[index].text = label;
        _lines[index].text = FormatAttackDataStringPart1(attack);
        _labels[index + 1].text = "";
        _lines[index + 1].text = FormatAttackDataStringPart2(attack);
    }

    private void DisplaySpell(int index, string label, Spell spell)
    {
        _labels[index].text = label;
        _lines[index].text = spell.GetName();
    }

    private void DisplayEmpty(int index)
    {
        _labels[index].text = "";
        _lines[index].text = "";
    }

}
