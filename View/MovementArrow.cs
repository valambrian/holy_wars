using UnityEngine;

public class MovementArrow : MonoBehaviour
{
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private float startWidth = 1.0f;
    [SerializeField]
    private float endWidth = 1.0f;

    public void SetPoints(Vector3 start, Vector3 end)
    {
		//lineRenderer.SetWidth(startWidth, endWidth);
		lineRenderer.startWidth = startWidth;
		lineRenderer.endWidth = endWidth;
		//lineRenderer.SetVertexCount(2);
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}
