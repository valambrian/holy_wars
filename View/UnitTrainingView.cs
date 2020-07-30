using System;
using UnityEngine;

public class UnitTrainingView : MonoBehaviour
{
    public event EventHandler<EventArgs> QuantityIncreased;
    public event EventHandler<EventArgs> QuantityDecreased;
    public event EventHandler<EventArgs> ImageClicked;

    [SerializeField]
    private MeshRenderer _unitTypeImage;

    [SerializeField]
    private MouseClickListener _unitImageClickListener;

    [SerializeField]
    private TextMesh _costField;

    [SerializeField]
    private TextMesh _qtyField;

    [SerializeField]
    private MouseClickListener _upArrow;

    [SerializeField]
    private MouseClickListener _downArrow;

    [SerializeField]
    private UnitTypeView _unitTypePrefab;

    [SerializeField]
    private MouseClickListener _checkBoxClickListener;

    [SerializeField]
    private GameObject _checkMark;

    private UnitType _unitType;
    private int _quantity;
    private bool _isOrderStanding;
    private UnitTypeView _unitTypeView = null;

    private Vector3 UNIT_TYPE_VIEW_OFFSET = new Vector3(-8.92f, -2.3f, 0);

    public void SetModel(UnitType unitType, int quantity, bool isOrderStanding)
    {
        _unitType = unitType;
        _quantity = quantity;
        _isOrderStanding = isOrderStanding;
        _unitTypeImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(_unitType.GetName());
        _upArrow.MouseClickDetected += OnQtyIncreased;
        _downArrow.MouseClickDetected += OnQtyDecreased;
        _unitImageClickListener.MouseClickDetected += OnUnitImageClicked;
        _checkBoxClickListener.MouseClickDetected += OnCheckBoxClicked;
        _checkMark.gameObject.SetActive(isOrderStanding);
        UpdateView();
    }

    private void UpdateView()
    {
        _qtyField.text = _quantity.ToString();
        _costField.text = (_quantity * _unitType.GetTrainingCost()).ToString();
    }

    private void OnQtyIncreased(object sender, EventArgs args)
    {
        _quantity++;
        UpdateView();
        if (QuantityIncreased != null)
        {
            QuantityIncreased(this, new EventArgs());
        }
    }

    private void OnQtyDecreased(object sender, EventArgs args)
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

    private void OnUnitImageClicked(object sender, EventArgs args)
    {
        if (ImageClicked != null)
        {
            ImageClicked(this, EventArgs.Empty);
        }
    }

    private void OnCheckBoxClicked(object sender, EventArgs args)
    {
        _isOrderStanding = !_isOrderStanding;
        _checkMark.gameObject.SetActive(_isOrderStanding);
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
        DestroyUnitTypeView();

        _unitTypeView = (UnitTypeView)Instantiate(_unitTypePrefab,
            new Vector3(transform.position.x + UNIT_TYPE_VIEW_OFFSET.x, transform.position.y + UNIT_TYPE_VIEW_OFFSET.y, transform.position.z + UNIT_TYPE_VIEW_OFFSET.z),
            Quaternion.identity);
        _unitTypeView.SetModel(_unitType);
    }

    public void DestroyUnitTypeView()
    {
        if (_unitTypeView != null)
        {
            Destroy(_unitTypeView.gameObject);
            _unitTypeView = null;
        }
    }

    void OnDestroy()
    {
        DestroyUnitTypeView();
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

}
