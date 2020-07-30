using System;
using System.Collections;
using UnityEngine;

public class AttackResolutionEventView : MonoBehaviour
{
    public event EventHandler<EventArgs> DisplayCompleted;

    [SerializeField]
    private MeshRenderer _defenderImage;

    [SerializeField]
    private TextMesh _defenderQuantityField;

    [SerializeField]
    private TextMesh _defenderHealthField;

    [SerializeField]
    private TextMesh _defenderRoll;

    [SerializeField]
    private TextMesh _hitOrMissMessage;

    [SerializeField]
    private TextMesh _defenderArmor;

    [SerializeField]
    private MeshRenderer _attackerImage;

    [SerializeField]
    private TextMesh _attackerRoll;

    [SerializeField]
    private TextMesh _damageRoll;

    [SerializeField]
    private TextMesh _finalDamage;

    private AttackResolutionEvent _model;

    public void SetModel(AttackResolutionEvent eventData)
    {
        _model = eventData;
        _defenderImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(_model.GetTarget().GetUnitType().GetName());
        _attackerImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(_model.GetAttack().GetUnitType().GetName());
        _attackerRoll.text = "";
        _defenderRoll.text = "";
        _hitOrMissMessage.text = "";
        _damageRoll.text = "";
        _defenderArmor.text = "";
        _finalDamage.text = "";
        _defenderQuantityField.text = "";
        _defenderHealthField.text = "";
        StartCoroutine("UpdateView");
    }

    private IEnumerator UpdateView()
    {
        yield return new WaitForSeconds(.2f);
        if (_model.GetAttack().AttackRoll >= 0)
        {
            _attackerRoll.text = _model.GetAttack().AttackSkill.ToString() + " + " + _model.GetAttack().AttackRoll.ToString();
        }
        else
        {
            _attackerRoll.text = _model.GetAttack().AttackSkill.ToString() + " - " + (-1 * _model.GetAttack().AttackRoll).ToString();
        }
        int defense = _model.GetDefenseSkill() + _model.GetShield();
        if (_model.GetDefenseRoll() > 0)
        {
            _defenderRoll.text = defense.ToString() + " + " + _model.GetDefenseRoll().ToString();
        }
        else
        {
            _defenderRoll.text = defense.ToString() + " - " + (-1 * _model.GetDefenseRoll()).ToString();
        }
        yield return new WaitForSeconds(.5f);
        int attack = _model.GetAttack().AttackSkill + _model.GetAttack().AttackRoll;
        defense += _model.GetDefenseRoll();
        _attackerRoll.text = attack.ToString();
        _defenderRoll.text = defense.ToString();
        yield return new WaitForSeconds(.2f);
        if (_model.GetAttack().IsCritical)
        {
            _hitOrMissMessage.color = Color.red;
            _hitOrMissMessage.text = "CRIT!";
        }
        else
        {
            if (attack > defense)
            {
                _hitOrMissMessage.color = Color.red;
                _hitOrMissMessage.text = "HIT";
            }
            else
            {
                _hitOrMissMessage.color = Color.green;
                _hitOrMissMessage.text = "MISS";
            }
        }
        _damageRoll.text = _model.GetAttack().FullDamage.ToString();
        _defenderArmor.text = _model.GetArmor().ToString();
        yield return new WaitForSeconds(.5f);
        _finalDamage.text = _model.GetDamage().ToString();
        _defenderQuantityField.text = _model.GetTarget().GetTotalQty().ToString();
        _defenderHealthField.text = _model.GetTarget().GetTotalHealth().ToString();
        yield return new WaitForSeconds(.5f);
        if (DisplayCompleted != null)
        {
            DisplayCompleted(this, EventArgs.Empty);
        }
    }

    public AttackResolutionEvent GetModel()
    {
        return _model;
    }

}
