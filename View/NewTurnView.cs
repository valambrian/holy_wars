using UnityEngine;

public class NewTurnView : MonoBehaviour
{
    [SerializeField]
    private TextMesh _factionName;

    [SerializeField]
    private TextMesh _turnNumber;

    [SerializeField]
    private MouseClickListener _image;

    public void SetParameters(string factionName, int turnNumber)
    {
        _factionName.text = factionName;
        _turnNumber.text = "Turn " + turnNumber;
        _image.MouseClickDetected += OnClick;
    }

    private void OnClick(object sender, System.EventArgs args)
    {
        _image.MouseClickDetected -= OnClick;
        Destroy(gameObject);
    }

}
