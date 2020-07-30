using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitStackView : MonoBehaviour
{
    public event EventHandler<EventArgs> StackSelected;
    public event EventHandler<EventArgs> StackInspected;
    public event EventHandler<EventArgs> StackInspectionEnded;
    public event EventHandler<EventArgs> ExplosionAnimationCompleted;
    public event EventHandler<EventArgs> HealAnimationCompleted;

    [SerializeField]
    private MeshRenderer _unitImage;

    [SerializeField]
    private TextMesh _quantityField;

    [SerializeField]
    private TextMesh _healthField;

    [SerializeField]
    private TextMesh _mirrorImageField;

    [SerializeField]
    private TextMesh _confusionField;

    [SerializeField]
    private TextMesh _magicShieldField;

    [SerializeField]
    private TextMesh _stoneSkinField;

    [SerializeField]
    private MouseClickListener _mouseListener;

    [SerializeField]
    private MouseOverListener _mouseOverListener;

    [SerializeField]
    private Timer _explosion;

    [SerializeField]
    private Timer _healing;

    private bool _explosionActivated;
    private bool _healingActivated;

    private UnitStack _model;

    private List<WoundCheckEvent> _woundChecks = new List<WoundCheckEvent>();

    public void SetModel(UnitStack unitStack)
    {
        _model = unitStack;
        _model.WoundCheckMade += OnWoundCheckMade;
        _unitImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(_model.GetUnitType().GetName());
        _mouseListener.MouseClickDetected += OnStackSelected;
        _mouseOverListener.MouseOverDetected += OnStackInspected;
        _mouseOverListener.MouseExitDetected += OnStackInspectionEnd;
        _explosion.gameObject.SetActive(false);
        _healing.gameObject.SetActive(false);
        UpdateView();
    }

    public void UpdateView()
    {
        _quantityField.text = _model.GetTotalQty().ToString();
        _healthField.text = _model.GetTotalHealth().ToString();
        _mirrorImageField.gameObject.SetActive(_model.IsAffectedBy("Mirror Image"));
        _confusionField.gameObject.SetActive(_model.IsAffectedBy("Confusion"));
        _magicShieldField.gameObject.SetActive(_model.IsAffectedBy("Magic Shield"));
        _stoneSkinField.gameObject.SetActive(_model.IsAffectedBy("Stone Skin"));
    }

    public void PlayExplosionAnimation()
    {
        if (!_explosionActivated)
        {
            FileLogger.Trace("COMBAT VIEW", "Playing explosion animation on " + _model.GetUnitType().GetName());
            _explosion.gameObject.SetActive(true);
            _explosion.TimeIsUp += OnExplosionCompleted;
            _explosionActivated = true;
            _explosion.StartTimer();
        }
    }

    private void OnExplosionCompleted(object sender, EventArgs args)
    {
        FileLogger.Trace("COMBAT VIEW", "Completed explosion animation for " + _model.GetUnitType().GetName());
        _explosion.TimeIsUp -= OnExplosionCompleted;
        _explosionActivated = false;
        if (ExplosionAnimationCompleted != null)
        {
            ExplosionAnimationCompleted(this, EventArgs.Empty);
        }
    }

    public void PlayHealAnimation()
    {
        if (!_healingActivated)
        {
            FileLogger.Trace("COMBAT VIEW", "Playing healing animation on " + _model.GetUnitType().GetName());
            _healing.gameObject.SetActive(true);
            _healing.TimeIsUp += OnHealingCompleted;
            _healingActivated = true;
            _healing.StartTimer();
        }
    }

    private void OnHealingCompleted(object sender, EventArgs args)
    {
        FileLogger.Trace("COMBAT VIEW", "Completed healing animation for " + _model.GetUnitType().GetName());
        _healing.TimeIsUp -= OnHealingCompleted;
        _healingActivated = false;
        if (HealAnimationCompleted != null)
        {
            HealAnimationCompleted(this, EventArgs.Empty);
        }
    }

    private void OnStackSelected(object sender, EventArgs args)
    {
        if (StackSelected != null)
        {
            StackSelected(this, EventArgs.Empty);
        }
    }

    private void OnStackInspected(object sender, EventArgs args)
    {
        if (StackInspected != null)
        {
            StackInspected(this, EventArgs.Empty);
        }
    }

    private void OnStackInspectionEnd(object sender, EventArgs args)
    {
        if (StackInspectionEnded != null)
        {
            StackInspectionEnded(this, EventArgs.Empty);
        }
    }

    private void OnWoundCheckMade(object sender, WoundCheckEvent args)
    {
        _woundChecks.Add(args);
    }

    public UnitStack GetModel()
    {
        return _model;
    }

    public List<WoundCheckEvent> GetWoundCheckEvents()
    {
        return _woundChecks;
    }

    void OnDestroy()
    {
        _model.WoundCheckMade -= OnWoundCheckMade;
        _mouseListener.MouseClickDetected -= OnStackSelected;
        _mouseOverListener.MouseOverDetected -= OnStackInspected;
        _mouseOverListener.MouseExitDetected -= OnStackInspectionEnd;
    }

}
