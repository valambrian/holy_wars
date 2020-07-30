using System;
using UnityEngine;

public class SiegeView : MonoBehaviour
{
    public event EventHandler<EventArgs> SiegeActivated;

    [SerializeField]
    private MouseClickListener _siegeIcon;

    private Province _province;

    public void SetModel(Province province)
    {
        _province = province;
        _siegeIcon.MouseClickDetected += OnSiegeIconClicked;
    }

    private void OnSiegeIconClicked(object sender, EventArgs args)
    {
        //Debug.Log("Siege Icon Clicked!");
        if (SiegeActivated != null)
        {
            SiegeActivated(this, new EventArgs());
        }
    }

    public Province GetProvince()
    {
        return _province;
    }


}
