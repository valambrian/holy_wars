using System.Collections;
using UnityEngine;

public class Selectable : MonoBehaviour
{
	public Color highlightedBackground = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	public Color standardBackground = new Color(0.75f, 0.75f, 0.75f, 1.0f);
	protected Color currentBackgroundColor;
	public float colorChangeSpeed = 15.0f; // how fast does the active unit change its background color
	protected bool isMovingToHighlighted = false; // the phase of the active unit background color change

	public bool IsSelected {get; set;}
	public float moveAnimationTime = 1.0f;

	[SerializeField]
	protected MeshRenderer meshRenderer;

	void Update()
	{
		if (IsSelected)
		{
			PlayUnitSelectedAnimation();
		}
		else
		{
			meshRenderer.material.color = standardBackground;
			isMovingToHighlighted = false;
		}
	}
	
	public void MoveTo(Vector3 newPosition)
	{
		StartCoroutine(AnimateMovement(transform.position, newPosition));
	}

	private IEnumerator AnimateMovement(Vector3 start, Vector3 goal)
	{
		float currentTime = 0.0f;
		while(currentTime < moveAnimationTime)
		{
			
			transform.position = Vector3.Lerp(start, goal, currentTime / moveAnimationTime);
			
			currentTime = Mathf.Min(moveAnimationTime, currentTime + Time.deltaTime);
			yield return null;
		}
	} 

	private void PlayUnitSelectedAnimation()
	{
		if (meshRenderer.material.color == standardBackground)
		{
			isMovingToHighlighted = true;
		}
		if (meshRenderer.material.color == highlightedBackground)
		{
			isMovingToHighlighted = false;
		}

		if (isMovingToHighlighted)
		{
			meshRenderer.material.color =
				Color.Lerp(meshRenderer.material.color, highlightedBackground, Time.deltaTime * colorChangeSpeed);
		}
		else
		{
			meshRenderer.material.color =
				Color.Lerp(meshRenderer.material.color, standardBackground, Time.deltaTime * colorChangeSpeed);
		}
	}
}
