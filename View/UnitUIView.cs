using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitUIView : MonoBehaviour
{
    public event EventHandler<EventArgs> MouseOver;
    public event EventHandler<EventArgs> MouseOut;

    [SerializeField]
    private Image _unitImage;

    [SerializeField]
    private Text _quantityField;

    [SerializeField]
    private Button _upArrow;

    [SerializeField]
    private Button _downArrow;

    private Unit _unit;
    private int _maxQuantity;

    public void SetModel(Province province, Unit unit, bool areButtonsActive)
    {
        if (areButtonsActive)
        {
            _upArrow.gameObject.SetActive(true);
            _downArrow.gameObject.SetActive(true);
        }
        else
        {
            _upArrow.gameObject.SetActive(false);
            _downArrow.gameObject.SetActive(false);
        }

        SetUnit(province, unit);
    }

    public void SetUnit(Province province, Unit unit)
    {
        _unit = unit;
        _maxQuantity = unit.GetQuantity();

        UnitType unitType = _unit.GetUnitType();
        if (unitType != null)
        {
            _unitImage.sprite = SpriteCollectionManager.GetSpriteByName(unitType.GetName());
            List<Unit> garrison = province.GetUnits();
            for (int i = 0; i < garrison.Count; i++)
            {
                if (garrison[i].GetUnitType() == unitType)
                {
                    _maxQuantity += garrison[i].GetQuantity();
                    break;
                }
            }
        }
        else
        {
            _unitImage.sprite = SpriteCollectionManager.GetSpriteByName("empty");
        }
        UpdateView();
    }


    private void UpdateView()
    {
        _quantityField.text = _unit.GetQuantity().ToString();
    }

    public void IncreaseQuantity()
    {
        if (_unit.GetQuantity() < _maxQuantity)
        {
            _unit.AddQuantity(1);
            UpdateView();
        }
    }

    public void DecreaseQuantity()
    {
        if (_unit.GetQuantity() > 0)
        {
            _unit.AddQuantity(-1);
            UpdateView();
        }
    }

    public void OnMouseOver()
    {
        if (MouseOver != null)
        {
            MouseOver(this, EventArgs.Empty);
        }
    }

    public void OnMouseOut()
    {
        if (MouseOut != null)
        {
            MouseOut(this, EventArgs.Empty);
        }
    }

    public int GetQuantity()
    {
        return _unit.GetQuantity();
    }

    public Unit GetUnit()
    {
        return _unit;
    }

    public UnitType GetUnitType()
    {
        return _unit.GetUnitType();
    }

}
