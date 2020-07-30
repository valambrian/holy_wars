using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// View class for an army movement order
/// Displays an army image, number of units, and a movement arrow
/// </summary>

public class ArmyMovementOrderView : MonoBehaviour
{
    public event EventHandler<EventArgs> ImageClicked;

    [SerializeField]
    private MeshRenderer _armyImage;
    [SerializeField]
    private TextMesh _quantityField;
    [SerializeField]
    private MeshRenderer _arrowheadImage;
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private float _startWidth = 0.5f;
    [SerializeField]
    private float _endWidth = 0.2f;

    [SerializeField]
    private float _arrowHeadShift = 0.1f;

    private ArmyMovementOrder _model;

    /// <summary>
    /// Sets a model for the class
    /// In addition to the army movement order itself it needs to know where does the movement arrow start and end
    /// </summary>
    /// <param name="order">Army movement order</param>
    /// <param name="start">Start point of the movement arrow</param>
    /// <param name="end">End point of the movement arrow</param>
    public void SetModel(ArmyMovementOrder order, Vector3 start, Vector3 end)
    {
        // set the model proper
        _model = order;
        _model.Updated += OnModelUpdated;

        // place the head of the movement arrow
        _arrowheadImage.transform.position = new Vector3((1 + _arrowHeadShift) * end.x - _arrowHeadShift * start.x,
                                                            (1 + _arrowHeadShift) * end.y - _arrowHeadShift * start.y,
                                                            end.z);
        _arrowheadImage.transform.rotation = Quaternion.FromToRotation(Vector3.down, end - start);

        // select army image and update the number of units field
        Update();

        // place the army image on the screen
        _armyImage.transform.position = new Vector3(0.5f * (start.x + end.x) - 1f, 0.5f * (start.y + end.y) + 1f, 0.5f * (start.z + end.z));

        // the player can edit an army movement order by clicking on the army image
        _armyImage.GetComponentInChildren<MouseClickListener>().MouseClickDetected += OnArmyImageClicked;

        // place the number of units info on the screen
        _quantityField.transform.position = new Vector3(0.5f * (start.x + end.x) - 1f, 0.5f * (start.y + end.y) + 3.5f, 0.5f * (start.z + end.z));

        // set line renderer parameters (for the movement arrow)
        //_lineRenderer.SetWidth(_startWidth, _endWidth);
		_lineRenderer.startWidth = _startWidth;
		_lineRenderer.endWidth = _endWidth;
		//_lineRenderer.SetVertexCount(2);
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
    }

    /// <summary>
    /// Updates the image of the army and the number of units in it
    /// Usually called after the underlying model is updated
    /// </summary>
    private void Update()
    {
        UnitType armyRepresentative = GameUtils.GetRepresentative(_model.GetUnits());
        _armyImage.material.mainTexture = SpriteCollectionManager.GetTextureByName(armyRepresentative.GetName());
        _quantityField.text = _model.GetUnitsCount().ToString();
    }

    /// <summary>
    /// Returns the army movement order, which serves as a model
    /// </summary>
    /// <returns>The model, an army movement order</returns>
    public ArmyMovementOrder GetModel()
    {
        return _model;
    }

    /// <summary>
    /// Returns the province from which the movement order originated
    /// </summary>
    /// <returns>The province from which the movement order originated</returns>
    public Province GetOriginProvince()
    {
        return _model.GetOrigin();
    }

    /// <summary>
    /// Returns the list of moving units
    /// </summary>
    /// <returns>The list of moving units</returns>
    public List<Unit> GetUnits()
    {
        return _model.GetUnits();
    }

    /// <summary>
    /// Callback for the player clicking on the army image
    /// Is used to pass the event up to the observer
    /// </summary>
    /// <param name="sender">Army image that sent the event (ignored)</param>
    /// <param name="args">Corresponding event args (ignored)</param>
    private void OnArmyImageClicked(object sender, EventArgs args)
    {
        if (ImageClicked != null)
        {
            ImageClicked(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Reacts to the noification that the model has been updated
    /// </summary>
    /// <param name="sender">The model (ignored)</param>
    /// <param name="args">Corresponding event args (ignored)</param>
    private void OnModelUpdated(object sender, EventArgs args)
    {
        Update();
    }

    void OnDestroy()
    {
        _model.Updated -= OnModelUpdated;
    }

}
