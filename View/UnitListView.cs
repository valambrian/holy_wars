using UnityEngine;
using System.Collections.Generic;
using System;

public class UnitListView : MonoBehaviour
{
    public event EventHandler<EventArgs> WindowClosed;

    [SerializeField]
    private UnitView _unitViewPrefab;

    [SerializeField]
    private GameObject[] _spawnPoints;

    [SerializeField]
    private MouseClickListener _leftArrow;

    [SerializeField]
    private MouseClickListener _rightArrow;

    [SerializeField]
    private MouseClickListener _closeButton;

    private List<Unit> _units;
    private Province _province;
    private List<UnitView> _unitViews;
    private bool _areButtonsActive;
    private int _offset = 0;

    public void SetModel(Province province, List<Unit> unitList, bool areButtonsActive)
    {
        _units = new List<Unit>();
        for (int i = 0; i < unitList.Count; i++)
        {
            _units.Add(new Unit(unitList[i]));
        }
        _province = province;
        _areButtonsActive = areButtonsActive;

        int slots = Mathf.Min(_spawnPoints.Length, unitList.Count);
        _unitViews = new List<UnitView>();
        for (int i = 0; i < slots; i++)
        {
            UnitView view = (UnitView)Instantiate(_unitViewPrefab, _spawnPoints[i].transform.position, Quaternion.identity);
            view.transform.parent = gameObject.transform;
            view.ImageClicked += OnUnitImageClicked;
            view.QuantityChanged += OnUnitQuantityChanged;
            view.SetModel(province, _units[i], areButtonsActive);
            _unitViews.Add(view);
        }
        if (_units.Count > _spawnPoints.Length)
        {
            _leftArrow.MouseClickDetected += OnArrowButtonClicked;
            _rightArrow.MouseClickDetected += OnArrowButtonClicked;
        }
        else
        {
            _leftArrow.gameObject.SetActive(false);
            _rightArrow.gameObject.SetActive(false);
        }
        _closeButton.MouseClickDetected += OnCloseWindowButtonClicked;
    }

    private void OnCloseWindowButtonClicked(object sender, EventArgs args)
    {
        for (int i = 0; i < _unitViews.Count; i++)
        {
            _unitViews[i].DestroyUnitTypeView();
        }
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        _closeButton.MouseClickDetected -= OnCloseWindowButtonClicked;
        _leftArrow.MouseClickDetected -= OnArrowButtonClicked;
        _rightArrow.MouseClickDetected -= OnArrowButtonClicked;
        for (int i = 0; i < _unitViews.Count; i++)
        {
            _unitViews[i].ImageClicked -= OnUnitImageClicked;
            _unitViews[i].QuantityChanged -= OnUnitQuantityChanged;
            _unitViews[i].DestroyUnitTypeView();
        }

        if (WindowClosed != null)
        {
            WindowClosed(this, EventArgs.Empty);
        }
    }

    private void OnUnitImageClicked(object sender, EventArgs args)
    {
        UnitView view = (UnitView)sender;
        for (int i = 0; i < _unitViews.Count; i++)
        {
            if (_unitViews[i] == view)
            {
                _unitViews[i].ToggleUnitTypeView();
            }
            else
            {
                _unitViews[i].DestroyUnitTypeView();
            }
        }
    }

    public List<Unit> GetUnits()
    {
        return _units;
    }

    public Province GetProvince()
    {
        return _province;
    }

    public bool AreButtonsActive()
    {
        return _areButtonsActive;
    }

    private void OnArrowButtonClicked(object sender, EventArgs args)
    {
        MouseClickListener listener = (MouseClickListener)sender;
        bool needsAnUpdate = false;
        if (listener == _leftArrow && _offset > 0)
        {
            _offset--;
            needsAnUpdate = true;
        }
        if (listener == _rightArrow && _offset < _units.Count - _spawnPoints.Length)
        {
            _offset++;
            needsAnUpdate = true;
        }
        if (needsAnUpdate)
        {
            for (int i = 0; i < _unitViews.Count; i++)
            {
                _unitViews[i].SetUnit(_province, _units[i + _offset]);
            }
        }
    }

    private void OnUnitQuantityChanged(object sender, EventArgs args)
    {
        UnitView view = (UnitView)sender;
        UnitType unitType = view.GetUnitType();
        int quantity = view.GetQuantity();
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].GetUnitType() == unitType)
            {
                _units[i].SetQuantity(quantity);
                break;
            }
        }
    }

}
