public class MouseClickCounter
{
    public const float MAX_DELAY = 0.25f;

    private float _lastClickTime = 0;

    public MouseClickCounter()
    {
    }

    public int GetClickCount()
    {
        float now = UnityEngine.Time.time;

        int count;
        if (now - _lastClickTime > MAX_DELAY)
        {
            count = 1;
            _lastClickTime = now;
        }
        else
        {
            // reset the timer after the second click
            count = 2;
            _lastClickTime = 0f;
        }
        return count;
    }
}
