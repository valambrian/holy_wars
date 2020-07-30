using UnityEngine;

public class ResourceBar : MonoBehaviour
{
    [SerializeField]
    private TextMesh _money;

    [SerializeField]
    private TextMesh _favor;

    [SerializeField]
    private TextMesh _phaseDescription;

    public void SetModel(Faction faction)
    {
        if (faction.IsPC())
        {
            _money.text = faction.GetMoneyBalance().ToString();
            _favor.text = faction.GetFavors().ToString();
        }
        else
        {
            _money.text = "";
            _favor.text = "";
        }
    }

    public void SetPhaseDesription(string description)
    {
        _phaseDescription.text = description;
    }

}
