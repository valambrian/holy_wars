using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackResolutionUIView : MonoBehaviour
{
    public event EventHandler<EventArgs> DisplayCompleted;

    [SerializeField]
    private Image _defenderImage;

    [SerializeField]
    private Text _defenderNameField;

    [SerializeField]
    private Text _defenderQuantityField;

    [SerializeField]
    private Text _defenderHealthField;

    [SerializeField]
    private Text _defense;

    [SerializeField]
    private Text _defenderRoll;

    [SerializeField]
    private Text _totalDefense;

    [SerializeField]
    private Text _hitOrMissMessage;

    [SerializeField]
    private Text _defenderArmor;

    [SerializeField]
    private Image _attackerImage;

    [SerializeField]
    private Text _attackerNameField;

    [SerializeField]
    private Text _attack;

    [SerializeField]
    private Text _attackerRoll;

    [SerializeField]
    private Text _totalAttack;

    [SerializeField]
    private Text _damageRoll;

    [SerializeField]
    private Text _finalDamage;

    private AttackResolutionEvent _model;

    public void SetModel(AttackResolutionEvent eventData)
    {
        _model = eventData;
        _attackerNameField.text = _model.GetAttack().GetUnitType().GetName();
        _defenderNameField.text = _model.GetTarget().GetUnitType().GetName();
        _defenderImage.sprite = SpriteCollectionManager.GetSpriteByName(_defenderNameField.text);
        _attackerImage.sprite = SpriteCollectionManager.GetSpriteByName(_attackerNameField.text);
        _attack.text = "";
        _attackerRoll.text = "";
        _totalAttack.text = "";
        _defense.text = "";
        _defenderRoll.text = "";
        _totalDefense.text = "";
        _hitOrMissMessage.text = "VS";
        _hitOrMissMessage.color = Color.black;
        _damageRoll.text = "";
        _defenderArmor.text = "";
        _finalDamage.text = "";
        _defenderQuantityField.text = "";
        _defenderHealthField.text = "";
        StartCoroutine("UpdateView");
    }

    private IEnumerator UpdateView()
    {
        yield return new WaitForSeconds(.3f);
        int defense = _model.GetDefenseSkill() + _model.GetShield();
        _attack.text = _model.GetAttack().AttackSkill.ToString();
        _defense.text = defense.ToString();
        yield return new WaitForSeconds(.3f);
        if (_model.GetAttack().AttackRoll >= 0)
        {
            _attackerRoll.text =  "+ " + _model.GetAttack().AttackRoll.ToString();
        }
        else
        {
            _attackerRoll.text = "- " + (-1 * _model.GetAttack().AttackRoll).ToString();
        }
        if (_model.GetDefenseRoll() >= 0)
        {
            _defenderRoll.text = "+ " + _model.GetDefenseRoll().ToString();
        }
        else
        {
            _defenderRoll.text = "- " + (-1 * _model.GetDefenseRoll()).ToString();
        }
        yield return new WaitForSeconds(.3f);
        int attack = _model.GetAttack().TotalAttack;
        defense = _model.GetTotalDefense();
        _totalAttack.text = attack.ToString();
        _totalDefense.text = defense.ToString();
        yield return new WaitForSeconds(.3f);
        if (_model.GetAttack().IsCritical)
        {
            _hitOrMissMessage.color = Color.red;
            _hitOrMissMessage.text = "CRITS!";
        }
        else
        {
            if (attack > defense)
            {
                _hitOrMissMessage.color = Color.red;
                _hitOrMissMessage.text = "HITS";
            }
            else
            {
                _hitOrMissMessage.color = Color.green;
                _hitOrMissMessage.text = "MISSES";
            }
        }
        yield return new WaitForSeconds(.3f);
        _damageRoll.text = _model.GetAttack().FullDamage.ToString();
        yield return new WaitForSeconds(.3f);
        _defenderArmor.text = _model.GetArmor().ToString();
        yield return new WaitForSeconds(.3f);
        _finalDamage.text = _model.GetDamage().ToString();
        yield return new WaitForSeconds(.3f);
        _defenderQuantityField.text = _model.GetTarget().GetTotalQty().ToString();
        _defenderHealthField.text = _model.GetTarget().GetTotalHealth().ToString();
        yield return new WaitForSeconds(.7f);
        if (DisplayCompleted != null)
        {
            DisplayCompleted(this, EventArgs.Empty);
        }
    }

    public AttackResolutionEvent GetModel()
    {
        return _model;
    }

    public void StopAnimation()
    {
        FileLogger.Trace("COMBAT VIEW", "AttackResolutionUIView: stopping the animation");
        StopCoroutine("UpdateView");
        if (DisplayCompleted != null)
        {
            DisplayCompleted(this, EventArgs.Empty);
        }
    }

}
