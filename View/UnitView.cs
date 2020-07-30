using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    public event EventHandler<EventArgs> ImageClicked;
    public event EventHandler<EventArgs> QuantityChanged;

    [SerializeField]
    private MeshRenderer _unitImage;

    [SerializeField]
    private MouseClickListener _unitImageClickListener;

    [SerializeField]
    private TextMesh _quantityField;

    [SerializeField]
    private MouseClickListener _upArrow;

    [SerializeField]
    private MouseClickListener _downArrow;

    [SerializeField]
    private UnitTypeView _unitTypePrefab;

    private Unit _unit;
    private int _maxQuantity;
    private UnitTypeView _unitTypeView = null;

    private Vector3 UNIT_TYPE_VIEW_OFFSET = new Vector3(0, 6.1f, 0);

    public void SetModel(Province province, Unit unit, bool areButtonsActive)
    {
        if (areButtonsActive)
        {
            _upArrow.MouseClickDetected += OnQtyIncreased;
            _downArrow.MouseClickDetected += OnQtyDecreased;
        }
        else
        {
            _upArrow.gameObject.SetActive(false);
            _downArrow.gameObject.SetActive(false);
        }
        _unitImageClickListener.MouseClickDetected += OnImageClicked;

        SetUnit(province, unit);
    }

    public void SetUnit(Province province, Unit unit)
    {
        _unit = unit;
        _maxQuantity = unit.GetQuantity();

        UnitType unitType = _unit.GetUnitType();
        if (unitType != null)
        {
            _unitImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(unitType.GetName());
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
            _unitImage.material.mainTexture = SpriteCollectionManager.GetTextureByName("empty");
        }
        UpdateView();
    }


    private void UpdateView()
    {
        _quantityField.text = _unit.GetQuantity().ToString();
    }

    private void OnQtyIncreased(object sender, System.EventArgs args)
    {
        if (_unit.GetQuantity() < _maxQuantity)
        {
            _unit.AddQuantity(1);
            if(QuantityChanged!= null)
            {
                QuantityChanged(this, EventArgs.Empty);
            }
            UpdateView();
        }
    }

    private void OnQtyDecreased(object sender, System.EventArgs args)
    {
        if (_unit.GetQuantity() > 0)
        {
            _unit.AddQuantity(-1);
            if (QuantityChanged != null)
            {
                QuantityChanged(this, EventArgs.Empty);
            }
            UpdateView();
        }
    }

    private void OnImageClicked(object sender, EventArgs args)
    {
        if (ImageClicked != null)
        {
            ImageClicked(this, EventArgs.Empty);
        }
    }

    public void ToggleUnitTypeView()
    {
        if (_unitTypeView == null)
        {
            CreateUnitTypeView();
        }
        else
        {
            DestroyUnitTypeView();
        }
    }

    public void CreateUnitTypeView()
    {
        if (_unitTypeView != null)
        {
            DestroyUnitTypeView();

        }
        _unitTypeView = (UnitTypeView)Instantiate(_unitTypePrefab,
            new Vector3(transform.position.x + UNIT_TYPE_VIEW_OFFSET.x, transform.position.y + UNIT_TYPE_VIEW_OFFSET.y, transform.position.z + UNIT_TYPE_VIEW_OFFSET.z),
            Quaternion.identity);
        _unitTypeView.SetModel(_unit.GetUnitType());
    }

    public void DestroyUnitTypeView()
    {
        if (_unitTypeView != null)
        {
            Destroy(_unitTypeView.gameObject);
            _unitTypeView = null;
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
