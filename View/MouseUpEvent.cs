using System;
using UnityEngine;

public class MouseUpEvent : EventArgs
{
    private Vector3 _position;

    public MouseUpEvent(Vector3 position)
    {
        _position = position;
    }

    public Vector3 GetPosition()
    {
        return _position;
    }
}
