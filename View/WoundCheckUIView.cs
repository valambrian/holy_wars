using UnityEngine;
using UnityEngine.UI;

public class WoundCheckUIView : MonoBehaviour
{
    [SerializeField]
    private Image _unitImage;

    [SerializeField]
    private Text _target;

    [SerializeField]
    private Text _roll;

    [SerializeField]
    private Text _result;

    [SerializeField]
    private Text _unitQuantityField;

    [SerializeField]
    private Text _unitHealthField;

    private WoundCheckEvent _model;

    public void SetModel(WoundCheckEvent eventData, UnitType unitType)
    {
        _model = eventData;
        _unitImage.sprite = SpriteCollectionManager.GetSpriteByName(unitType.GetName());
        _target.text = "Need to Roll:";
        _roll.text = "Rolling 1d" + unitType.GetHitPoints() + ":";
        _result.text = "";
        _unitQuantityField.text = "";
        _unitHealthField.text = "";
    }

    public void DisplayTargetNumber()
    {
        _target.text += " " + _model._target.ToString() + " or less";
    }

    public void DisplayRoll()
    {
        _roll.text += " " + _model._roll.ToString();
    }

    public void DisplayUnitNumbers()
    {
        if (_model._roll <= _model._target)
        {
            _result.color = Color.green;
            _result.text = "SUCCESS";
        }
        else
        {
            _result.color = Color.red;
            _result.text = "FAILURE";
        }
        _unitQuantityField.text = _model._qty.ToString();
        _unitHealthField.text = _model._hp.ToString();
    }

}
