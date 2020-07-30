using System;
using UnityEngine;

public class MouseClickListener: MonoBehaviour
{
	public event EventHandler<EventArgs> MouseClickDetected;
    public event EventHandler<EventArgs> MouseUpDetected;
    public event EventHandler<EventArgs> MouseDragDetected;

    void OnMouseDown()
	{
		//Debug.Log ("Click!");
		if (MouseClickDetected != null)
		{
			//Debug.Log ("Clicky click!");
			MouseClickDetected(this, new EventArgs());
		}
	}

    void OnMouseUp()
    {
        //Debug.Log ("Unclick!");
        if (MouseUpDetected != null)
        {
            //Debug.Log ("Clicky click!");
            MouseUpDetected(this, new EventArgs());
        }
    }

    void OnMouseDrag()
    {
        //Debug.Log ("Draag!");
        if (MouseDragDetected != null)
        {
            //Debug.Log ("Clicky click!");
            MouseDragDetected(this, new EventArgs());
        }
    }
}
