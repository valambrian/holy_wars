using System;
using UnityEngine;

public class AttackRollResultsView : MonoBehaviour
{
    public event EventHandler<EventArgs> MouseOver;
    public event EventHandler<EventArgs> MouseLeft;

    [SerializeField]
    private MeshRenderer _unitImage;

    [SerializeField]
    private TextMesh _quantityField;

    [SerializeField]
    private MouseOverListener _mouseOverListener;

    private AttackRollResultsCollection _model;

    public void SetModel(AttackRollResultsCollection model)
    {
        _model = model;
        _unitImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(_model.GetUnitType().GetName());
        _mouseOverListener.MouseOverDetected += OnMouseOver;
        _mouseOverListener.MouseExitDetected += OnMouseLeft;
        UpdateView();
    }

    public void UpdateView()
    {
        _quantityField.text = _model.Count.ToString();
    }

    public AttackRollResultsCollection GetModel()
    {
        return _model;
    }

    private void OnMouseOver(object sender, EventArgs args)
    {
        if (MouseOver != null)
        {
            MouseOver(this, EventArgs.Empty);
        }
    }

    private void OnMouseLeft(object sender, EventArgs args)
    {
        if (MouseLeft != null)
        {
            MouseLeft(this, EventArgs.Empty);
        }
    }

    void OnDestroy()
    {
        _mouseOverListener.MouseOverDetected -= OnMouseOver;
        _mouseOverListener.MouseExitDetected -= OnMouseLeft;
    }

}
