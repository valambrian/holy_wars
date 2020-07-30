using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class UnitListUIView : MonoBehaviour
{
    public event EventHandler<EventArgs> WindowClosed;
    public event EventHandler<EventArgs> UnitInspected;
    public event EventHandler<EventArgs> UnitInspectionEnded;

    [SerializeField]
    private UnitUIView[] _unitViews;

    [SerializeField]
    private Button _leftArrow;

    [SerializeField]
    private Button _rightArrow;

    [SerializeField]
    private Button _done;

    [SerializeField]
    private Button _bribe;

    private List<Unit> _units;
    private Province _province;
    private bool _areButtonsActive;
    private int _offset;
    private bool _hasMovementOrderBeenEdited;

    public void SetModel(Province province, List<Unit> unitList, bool areButtonsActive, bool canBribe)
    {
        _units = new List<Unit>();
        // clone units so that they can be compared with the province's garrison
        for (int i = 0; i < unitList.Count; i++)
        {
            _units.Add(new Unit(unitList[i]));
        }
        _province = province;
        _areButtonsActive = areButtonsActive;
        _done.gameObject.SetActive(_areButtonsActive);

        if (!_areButtonsActive && canBribe)
        {
            _bribe.gameObject.SetActive(true);
        }
        else
        {
            _bribe.gameObject.SetActive(false);
        }

        _hasMovementOrderBeenEdited = false;
        _offset = 0;

        UpdateUnitViews();

        if (_units.Count > _unitViews.Length)
        {
            _leftArrow.gameObject.SetActive(true);
            _rightArrow.gameObject.SetActive(true);
        }
        else
        {
            _leftArrow.gameObject.SetActive(false);
            _rightArrow.gameObject.SetActive(false);
        }
    }

    private void UpdateUnitViews()
    {
        int slotsUsed = Mathf.Min(_unitViews.Length, _units.Count);
        for (int i = 0; i < slotsUsed; i++)
        {
            _unitViews[i].gameObject.SetActive(true);
            _unitViews[i].SetModel(_province, _units[i + _offset], _areButtonsActive);
            _unitViews[i].MouseOver += OnMouseOver;
            _unitViews[i].MouseOut += OnMouseOut;
        }
        for (int i = slotsUsed; i < _unitViews.Length; i++)
        {
            _unitViews[i].gameObject.SetActive(false);
            _unitViews[i].MouseOver -= OnMouseOver;
            _unitViews[i].MouseOut -= OnMouseOut;
        }
    }

    public void MoveUnitViewsRight()
    {
        if (_offset < _units.Count - _unitViews.Length)
        {
            _offset++;
            UpdateUnitViews();
        }
    }

    public void MoveUnitViewsLeft()
    {
        if (_offset > 0)
        {
            _offset--;
            UpdateUnitViews();
        }
    }

    public void CloseWindow()
    {
        if (WindowClosed != null)
        {
            WindowClosed(this, EventArgs.Empty);
        }
    }

    public void UpdateMovementOrder()
    {
        _hasMovementOrderBeenEdited = true;
        CloseWindow();
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

    public bool HasMovementOrderBeenEdited()
    {
        // we don't know for sure, but the player clicked "Done" button...
        return _hasMovementOrderBeenEdited;
    }

    private void OnMouseOver(object sender, EventArgs args)
    {
        if (UnitInspected != null)
        {
            UnitUIView view = (UnitUIView)sender;
            UnitInspected(view.GetUnitType(), EventArgs.Empty);
        }
    }

    private void OnMouseOut(object sender, EventArgs args)
    {
        if (UnitInspectionEnded != null)
        {
            UnitUIView view = (UnitUIView)sender;
            UnitInspectionEnded(view.GetUnitType(), EventArgs.Empty);
        }
    }

}
