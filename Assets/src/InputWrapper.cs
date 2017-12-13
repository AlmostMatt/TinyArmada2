using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputWrapper : MonoBehaviour {
	private const float CLICK_DUR = 0.3f;
	private const int LEFT = 0;

	// Record the world and screen coordinates when clicking. Relevant for draging.
	private Vector3 clickWorldPosition;
	private Vector2 clickScreenPosition;
	private float timeSinceClick;

	// Update is called once per frame
	void Update () {
		Transform mainCamera = Camera.main.transform;
		//Touch myTouch = Input.GetTouch(0);
		//Touch[] myTouches = Input.touches;
		//for(int i = 0; i < Input.touchCount; i++)
		if (Input.GetMouseButtonDown(LEFT)) {
			clickScreenPosition = Input.mousePosition;
			clickWorldPosition = getWorldMousePos();
			timeSinceClick = 0f;
		}
		bool clickEligible = timeSinceClick < CLICK_DUR && ((Vector2) Input.mousePosition - clickScreenPosition).magnitude < 10;
		timeSinceClick += Time.deltaTime;
		if (Input.GetMouseButton(LEFT)) {
			if (!clickEligible) {
				// Dragging
				var currentWorldPos = getWorldMousePos();
				// move the camera proportionate to currentWorldPos - clickWorldPosition
				mainCamera.position = mainCamera.position + (clickWorldPosition - currentWorldPos);
			}
		}
		if (Input.GetMouseButtonUp(LEFT)) {
			if (clickEligible) {
				// Click!
				Debug.Log("Click!");
			} else {
				// End of drag
			}
		}
	}

	private Vector3 getWorldMousePos() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		// Take the intersection of the ray and the y=-0.5 plane
		float desiredY = -0.5f;
		return ray.origin + (desiredY - ray.origin.y / ray.direction.y) * ray.direction;
	}
}
