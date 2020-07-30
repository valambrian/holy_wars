using System;
using UnityEngine;
using UnityEngine.UI;

public class UnitTrainingUIView : MonoBehaviour
{
    public event EventHandler<EventArgs> QuantityIncreased;
    public event EventHandler<EventArgs> QuantityDecreased;
    public event EventHandler<EventArgs> MouseOver;
    public event EventHandler<EventArgs> MouseOut;

    [SerializeField]
    private Image _unitTypeImage;

    [SerializeField]
    private Text _costField;

    [SerializeField]
    private Text _perUnitCostField;

    [SerializeField]
    private Text _qtyField;

    [SerializeField]
    private Image _checkMark;

    private UnitType _unitType;
    private int _quantity;
    private int _remainingManpower;
    private bool _isOrderStanding;

    public void SetModel(UnitType unitType, int quantity, bool isOrderStanding)
    {
        _unitType = unitType;
        _quantity = quantity;
        _remainingManpower = 0;
        _isOrderStanding = isOrderStanding;
        _unitTypeImage.sprite = SpriteCollectionManager.GetSpriteByName(_unitType.GetName());
        _checkMark.gameObject.SetActive(isOrderStanding);
        UpdateView();
    }

    private void UpdateView()
    {
        _qtyField.text = _quantity.ToString();
        _costField.text = (_quantity * _unitType.GetTrainingCost()).ToString();
        _perUnitCostField.text = _unitType.GetTrainingCost().ToString();
    }

    public void IncreaseQuantity()
    {
        if (_remainingManpower > 0)
        {
            _quantity++;
            UpdateView();
            if (QuantityIncreased != null)
            {
                QuantityIncreased(this, new EventArgs());
            }
        }
    }

    public void DecreaseQuantity()
    {
        if (_quantity > 0)
        {
            _quantity--;
            UpdateView();
            if (QuantityDecreased != null)
            {
                QuantityDecreased(this, new EventArgs());
            }
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

    public void OnCheckBoxClicked()
    {
        _isOrderStanding = !_isOrderStanding;
        _checkMark.gameObject.SetActive(_isOrderStanding);
    }

    public int GetQuanitity()
    {
        return _quantity;
    }

    public UnitType GetUnitType()
    {
        return _unitType;
    }

    public bool IsOrderStanding()
    {
        return _isOrderStanding;
    }

    public void ResetQuantity()
    {
        _quantity = 0;
    }

    public void SetRemainingManpower(int remainingManpower)
    {
        _remainingManpower = remainingManpower;
    }

}
