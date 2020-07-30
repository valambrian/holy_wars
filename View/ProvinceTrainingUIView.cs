using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class ProvinceTrainingUIView : MonoBehaviour
{
    public event EventHandler<EventArgs> SelectionDone;
    public event EventHandler<EventArgs> UnitTypeInspected;
    public event EventHandler<EventArgs> UnitTypeInspectionEnded;

    [SerializeField]
    private UnitTrainingUIView[] _unitTrainingViews;

    [SerializeField]
    private Text _moneyBalanceField;

    [SerializeField]
    private Text _manpowerField;

    private Province _province;
    private int _moneyBalance;
    private int _manpower;
    private bool _orderPlaced;

    public void SetModel(Province province)
    {
        _province = province;
        _moneyBalance = _province.GetOwnersFaction().GetMoneyBalance();
        _manpower = _province.GetRemainingManpower();
        _orderPlaced = false;

        List<UnitType> trainableUnits = _province.GetTrainableUnits();
        Dictionary<UnitType, UnitTrainingOrder> trainingQueue = _province.GetTrainingQueue();
        int slotsUsed = Mathf.Min(_unitTrainingViews.Length, trainableUnits.Count);
        for (int i = 0; i < slotsUsed; i++)
        {
            UnitTrainingUIView view = _unitTrainingViews[i];
            int quantity = trainingQueue.ContainsKey(trainableUnits[i]) ? trainingQueue[trainableUnits[i]].GetQuantity() : 0;
            bool isOrderStanding = trainingQueue.ContainsKey(trainableUnits[i]) ? trainingQueue[trainableUnits[i]].IsStanding() : false;
            view.gameObject.SetActive(true);
            view.SetModel(trainableUnits[i], quantity, isOrderStanding);
            //view.QuantityIncreased -= OnUnitQuanityIncreased;
            //view.QuantityDecreased -= OnUnitQuanityDecreased;
            view.QuantityIncreased += OnUnitQuantityIncreased;
            view.QuantityDecreased += OnUnitQuantityDecreased;
            view.MouseOver += OnMouseOver;
            view.MouseOut += OnMouseOut;
        }
        for (int i = slotsUsed; i < _unitTrainingViews.Length; i++)
        {
            _unitTrainingViews[i].QuantityIncreased -= OnUnitQuantityIncreased;
            _unitTrainingViews[i].QuantityDecreased -= OnUnitQuantityDecreased;
            _unitTrainingViews[i].MouseOver -= OnMouseOver;
            _unitTrainingViews[i].MouseOut -= OnMouseOut;
            _unitTrainingViews[i].ResetQuantity();
            _unitTrainingViews[i].gameObject.SetActive(false);
        }

        UpdateViewFields();
    }

    private void UpdateViewFields()
    {
        _moneyBalanceField.text = _moneyBalance.ToString();
        _manpowerField.text = _manpower.ToString();
        for (int i = 0; i < _unitTrainingViews.Length; i++)
        {
            _unitTrainingViews[i].SetRemainingManpower(_manpower);
        }
    }

    private void OnUnitQuantityIncreased(object sender, EventArgs args)
    {
        UnitTrainingUIView view = (UnitTrainingUIView)sender;
        _moneyBalance -= view.GetUnitType().GetTrainingCost();
        _manpower -= 1;
        UpdateViewFields();
    }

    private void OnUnitQuantityDecreased(object sender, EventArgs args)
    {
        UnitTrainingUIView view = (UnitTrainingUIView)sender;
        _moneyBalance += view.GetUnitType().GetTrainingCost();
        _manpower += 1;
        UpdateViewFields();
    }

    public void OnDoneButtonClicked()
    {
        if ((_manpower >= 0 && _moneyBalance >= 0) || (_manpower == _province.GetManpower()))
        {
            // this includes refunding the current training queue
            _province.RefundTrainingQueue();

            for (int i = 0; i < _unitTrainingViews.Length; i++)
            {
                int qty = _unitTrainingViews[i].GetQuanitity();
                if (qty > 0)
                {
                    _province.QueueTraining(_unitTrainingViews[i].GetUnitType(), qty, _unitTrainingViews[i].IsOrderStanding());
                    _orderPlaced = true;
                }
                _unitTrainingViews[i].QuantityIncreased -= OnUnitQuantityIncreased;
                _unitTrainingViews[i].QuantityDecreased -= OnUnitQuantityDecreased;
            }

            if (SelectionDone != null)
            {
                SelectionDone(this, EventArgs.Empty);
            }
        }
    }

    public bool WasOrderPlaced()
    {
        return _orderPlaced;
    }

    private void OnMouseOver(object sender, EventArgs args)
    {
        if (UnitTypeInspected != null)
        {
            UnitTrainingUIView view = (UnitTrainingUIView)sender;
            UnitTypeInspected(view.GetUnitType(), EventArgs.Empty);
        }
    }

    private void OnMouseOut(object sender, EventArgs args)
    {
        if (UnitTypeInspectionEnded != null)
        {
            UnitTrainingUIView view = (UnitTrainingUIView)sender;
            UnitTypeInspectionEnded(view.GetUnitType(), EventArgs.Empty);
        }
    }

    public void ForgetModel()
    {
        for (int i = 0; i < _unitTrainingViews.Length; i++)
        {
            _unitTrainingViews[i].QuantityIncreased -= OnUnitQuantityIncreased;
            _unitTrainingViews[i].QuantityDecreased -= OnUnitQuantityDecreased;
        }
    }

}
