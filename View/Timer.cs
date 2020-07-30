using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A script adding to a mono behavior an ability to be activated
/// and then deactivated on a timer
/// Useful for splash animations
/// </summary>
public class Timer : MonoBehaviour
{
    public event EventHandler<EventArgs> TimeIsUp;

    [SerializeField]
    private float _interval = 1.0f;

    /// <summary>
    /// Start
    /// </summary>
    public void StartTimer()
    {
        StartCoroutine(PassTime());
    }

    private IEnumerator PassTime()
    {
        float currentTime = 0.0f;
        while (currentTime < _interval)
        {
            currentTime = Mathf.Min(_interval, currentTime + Time.deltaTime);
            yield return null;
        }
        if (TimeIsUp != null)
        {
            TimeIsUp(this, new EventArgs());
        }
        gameObject.SetActive(false);
    }

}
