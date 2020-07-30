using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ProvinceView : MonoBehaviour
{
    public event EventHandler<EventArgs> TrainingInitiated;
    public event EventHandler<EventArgs> SiegeImageClicked;

    [SerializeField]
    private TextMesh _provinceName;

    [SerializeField]
    private MeshRenderer _favorImage;

    [SerializeField]
    private TextMesh _favor;

    [SerializeField]
    private TextMesh _income;

    [SerializeField]
    private TextMesh _manpower;

    [SerializeField]
    private MouseClickListener _unitTrainingButton;

    [SerializeField]
    private Selectable _meepleImage;

    [SerializeField]
    private Selectable _siegeIcon;

    [SerializeField]
    private MouseClickListener _siegeButton;

    [SerializeField]
    private Selectable _exclamationMarkImage;

    [SerializeField]
    private MouseClickListener _exclamationMark;

    private Province _province;
    private Game.TurnPhases _currentPhase;
    private bool _isOwnerActive;

    public void SetModel(Province province)
    {
        _province = province;
        _unitTrainingButton.MouseClickDetected += OnUnitTrainingButtonClick;
        _exclamationMark.MouseClickDetected += OnUnitTrainingButtonClick;
        UpdateView();
    }

    public void UpdateView()
    {
            _provinceName.text = _province.GetName();
            _income.text = _province.GetIncome().ToString();
            _siegeIcon.gameObject.SetActive(false);

            if (_province.GetFavor() == 0)
            {
                _favor.gameObject.SetActive(false);
                _favorImage.gameObject.SetActive(false);
            }
            else
            {
                _favor.text = _province.GetFavor().ToString();
            }

            UpdateManpowerView();
    }

    public void UpdateManpowerView()
    {
        bool animate = _isOwnerActive && _currentPhase == Game.TurnPhases.TRAINING && _province.GetRemainingManpower() > 0;
        _meepleImage.IsSelected = animate;
        _exclamationMarkImage.IsSelected = animate;
        _exclamationMark.gameObject.SetActive(animate);

        if (_isOwnerActive && _province.GetOwnersFaction().IsPC())
        {
            _manpower.text = _province.GetRemainingManpower() + " / " + _province.GetManpower();
        }
        else
        {
            _manpower.text = _province.GetManpower().ToString();
        }
    }

    public void SetCurrentPhase(Game.TurnPhases phase)
    {
        _currentPhase = phase;
        UpdateManpowerView();
    }

    public void SetCurrentFaction(Faction faction)
    {
        _isOwnerActive = faction.GetId() == _province.GetOwnersFaction().GetId();
        if (_isOwnerActive && faction.IsPlayable())
        {
            UpdateManpowerView();
        }
    }

    private void OnUnitTrainingButtonClick(object sender, EventArgs args)
    {
        if (_currentPhase == Game.TurnPhases.TRAINING && _isOwnerActive) // && !EventSystem.current.IsPointerOverGameObject())
        {
            if (TrainingInitiated != null)
            {
                TrainingInitiated(this, new EventArgs());
            }
        }
    }

    private void OnSiegeIconClicked(object sender, EventArgs args)
    {
        //Debug.Log("Click click click!");
        if (SiegeImageClicked != null) // && !EventSystem.current.IsPointerOverGameObject())
        {
            //Debug.Log("So many clicks!");
            SiegeImageClicked(this, new EventArgs());
        }
    }

    public Province GetProvince()
    {
        return _province;
    }

    public void ShowSiegeIcon(bool animate)
    {
        _siegeIcon.gameObject.SetActive(true);
        _siegeIcon.IsSelected = animate;
        _siegeButton.MouseClickDetected += OnSiegeIconClicked;
    }

    public void HideSiegeIcon()
    {
        _siegeIcon.gameObject.SetActive(false);
    }

    public void Destruct()
    {
        _unitTrainingButton.MouseClickDetected -= OnUnitTrainingButtonClick;
        _exclamationMark.MouseClickDetected -= OnUnitTrainingButtonClick;
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

}
