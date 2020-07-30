using System;
using UnityEngine;

public class MouseOverListener: MonoBehaviour
{
	public event EventHandler<EventArgs> MouseOverDetected;
    public event EventHandler<EventArgs> MouseExitDetected;

    void OnMouseOver()
	{
		if (MouseOverDetected != null)
		{
			MouseOverDetected(this, new EventArgs());
		}
	}

    void OnMouseExit()
    {
        if (MouseExitDetected != null)
        {
            MouseExitDetected(this, new EventArgs());
        }
    }
}
