using UnityEngine;
using System;
using System.Collections.Generic;

public class ProvinceTrainingView : MonoBehaviour
{
    public event EventHandler<EventArgs> SelectionDone;

    [SerializeField]
    private UnitTrainingView _unitTrainingPrefab;

    [SerializeField]
    private TextMesh _moneyBalanceField;

    [SerializeField]
    private TextMesh _manpowerField;

    [SerializeField]
    private MouseClickListener _doneButton;

    [SerializeField]
    private GameObject[] _spawnPoints;

    private Province _province;
    private List<UnitTrainingView> _unitTrainingViews = new List<UnitTrainingView>();
    private int _moneyBalance;
    private int _manpower;

    public void SetModel(Province province)
    {
        _province = province;
        _moneyBalance = _province.GetOwnersFaction().GetMoneyBalance();
        _manpower = _province.GetRemainingManpower();
        _doneButton.MouseClickDetected += OnDoneButtonClicked;

        List<UnitType> trainableUnits = _province.GetTrainableUnits();
        Dictionary<UnitType, UnitTrainingOrder> trainingQueue = _province.GetTrainingQueue();
        int slots = Mathf.Min(_spawnPoints.Length, trainableUnits.Count);
        for (int i = 0; i < slots; i++)
        {
            UnitTrainingView view = (UnitTrainingView)Instantiate(_unitTrainingPrefab, _spawnPoints[i].transform.position, Quaternion.identity);
            view.transform.parent = gameObject.transform;
            int quantity = trainingQueue.ContainsKey(trainableUnits[i]) ? trainingQueue[trainableUnits[i]].GetQuantity() : 0;
            bool isOrderStanding = trainingQueue.ContainsKey(trainableUnits[i]) ? trainingQueue[trainableUnits[i]].IsStanding() : false;
            view.SetModel(trainableUnits[i], quantity, isOrderStanding);
            view.QuantityIncreased += OnUnitQuanityIncreased;
            view.QuantityDecreased += OnUnitQuanityDecreased;
            view.ImageClicked += OnUnitImageClicked;
            _unitTrainingViews.Add(view);
        }

        UpdateViewFields();
    }

    private void UpdateViewFields()
    {
        _moneyBalanceField.text = _moneyBalance.ToString();
        _manpowerField.text = _manpower.ToString();
    }

    private void OnUnitQuanityIncreased(object sender, EventArgs args)
    {
        UnitTrainingView view = (UnitTrainingView)sender;
        _moneyBalance -= view.GetUnitType().GetTrainingCost();
        _manpower -= 1;
        UpdateViewFields();
    }

    private void OnUnitQuanityDecreased(object sender, EventArgs args)
    {
        UnitTrainingView view = (UnitTrainingView)sender;
        _moneyBalance += view.GetUnitType().GetTrainingCost();
        _manpower += 1;
        UpdateViewFields();
    }

    private void OnDoneButtonClicked(object sender, EventArgs args)
    {
        if (_manpower >= 0)
        {
            // this includes refunding the current training queue
            _province.RefundTrainingQueue();

            for (int i = 0; i < _unitTrainingViews.Count; i++)
            {
                int qty = _unitTrainingViews[i].GetQuanitity();
                if (qty > 0)
                {
                    _province.QueueTraining(_unitTrainingViews[i].GetUnitType(), qty, _unitTrainingViews[i].IsOrderStanding());
                }
            }

            if (SelectionDone != null)
            {
                SelectionDone(this, EventArgs.Empty);
            }

            for (int i = 0; i < _unitTrainingViews.Count; i++)
            {
                _unitTrainingViews[i].DestroyUnitTypeView();
            }
            Destroy(gameObject);
        }
    }

    private void OnUnitImageClicked(object sender, EventArgs args)
    {
        UnitTrainingView view = (UnitTrainingView)sender;
        for (int i = 0; i < _unitTrainingViews.Count; i++)
        {
            if (_unitTrainingViews[i] == view)
            {
                _unitTrainingViews[i].ToggleUnitTypeView();
            }
            else
            {
                _unitTrainingViews[i].DestroyUnitTypeView();
            }
        }
    }

}
