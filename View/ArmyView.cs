using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ArmyView : MonoBehaviour, IPointerClickHandler
{
    public event EventHandler<EventArgs> ImageClicked;
    public event EventHandler<EventArgs> ViewDragged;
    public event EventHandler<MouseUpEvent> MouseUp;

    [SerializeField]
    private MeshRenderer _armyImage;

    [SerializeField]
    private TextMesh _quantityField;

    private Province _province;
    private int _quantity;

    private Vector3 _originalPosition;
    private bool _isMovable = false;

    public void SetModel(Province province)
    {
        _province = province;
        _province.GarrisonUpdated += OnGarrisonUpdated;
        _originalPosition = transform.position;
        UpdateView();
    }

    public void UpdateView()
    {
        if (_armyImage == null)
        {
            Debug.Log("Army view image renderer destroyed in " + _province.GetName());
            return;
        }

        UnitType representative = GameUtils.GetRepresentative(_province.GetUnits());
        if (representative != null)
        {
            _armyImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(representative.GetName());
        }
        else
        {
            _armyImage.material.mainTexture = SpriteCollectionManager.GetTextureByName("empty");
        }
        _quantity = CountUnits();
        if (_quantity > 0)
        {
            _quantityField.text = _quantity.ToString();
        }
        else
        {
            _quantityField.text = "";
        }
    }

    private int CountUnits()
    {
        int result = 0;
        List<Unit> units = _province.GetUnits();
        for (int i = 0; i < units.Count; i++)
        {
            result += units[i].GetQuantity();
        }
        return result;
    }

    public Province GetModel()
    {
        return _province;
    }

    void OnMouseDrag()
    {
        if (_isMovable)
        {
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 9f);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            transform.position = worldPosition;

            if (ViewDragged != null)
            {
                ViewDragged(this, EventArgs.Empty);
            }
        }
    }

    void OnMouseUp()
    {
        if (MouseUp != null)
        {
            MouseUp(this, new MouseUpEvent(transform.position));
        }
        transform.position = _originalPosition;
    }

    public void AllowMovement(bool flag)
    {
        _isMovable = flag;
    }

    private void OnGarrisonUpdated(object sender, EventArgs args)
    {
        UpdateView();
    }

    void OnDestroy()
    {
        _province.GarrisonUpdated -= OnGarrisonUpdated;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ImageClicked != null)
        {
            ImageClicked(this, EventArgs.Empty);
        }
    }
}
