using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputWrapper : MonoBehaviour {
	private const float CLICK_DUR = 0.3f;
	private const int LEFT = 0;

	// Record the world and screen coordinates when clicking. Relevant for dragging.
	private Vector3 prevWorldMousePosition;
	private Vector3 clickWorldPosition;
	private Vector2 clickScreenPosition;
	private float timeSinceClick;
	private Clickable clickObject;

	void Update () {
		Transform mainCamera = Camera.main.transform;
		Vector3 worldMousePosition = getWorldMousePosition(Input.mousePosition);
		//Touch myTouch = Input.GetTouch(0);
		//Touch[] myTouches = Input.touches;
		//for(int i = 0; i < Input.touchCount; i++)
		if (Input.GetMouseButtonDown(LEFT)) {
			clickScreenPosition = Input.mousePosition;
			clickWorldPosition = worldMousePosition;
			timeSinceClick = 0f;
			clickObject = getWorldObject(clickWorldPosition);
			if (clickObject != null) {
				clickObject.handleMouseDown(0, clickWorldPosition);
				// TODO: use 'hover' to highlight the object while it is being clicked but has not yet been released
			}
		}
		bool clickEligible = timeSinceClick < CLICK_DUR && ((Vector2) Input.mousePosition - clickScreenPosition).magnitude < 10;
		timeSinceClick += Time.deltaTime;
		if (Input.GetMouseButton(LEFT)) {
			// Drag
			if (!clickEligible) {
				if (clickObject != null) {
					Vector2 delta = (worldMousePosition - prevWorldMousePosition);
					clickObject.handleDrag(0, delta, worldMousePosition - clickWorldPosition);
				} else {
					mainCamera.Translate(clickWorldPosition - worldMousePosition, Space.World);
				}
			}
		}
		if (Input.GetMouseButtonUp(LEFT)) {
			if (clickObject != null) {
				if (clickEligible) {
					clickObject.handleClick(0);
				}
				clickObject.handleMouseUp(0, worldMousePosition);
				clickObject = null;
			}
		}
		prevWorldMousePosition = worldMousePosition;
	}

	public static void FocusCameraOn(Vector3 worldPosition) {
		Vector3 offset = worldPosition - getWorldMousePosition(new Vector2(Screen.width/2f, Screen.height/2f), worldPosition.z);
		Camera.main.transform.Translate(offset, Space.World);
	}

	private static Vector3 getWorldMousePosition(Vector2 mousePosition, float desiredZ = -0.2f) {
		Ray ray = Camera.main.ScreenPointToRay(mousePosition);
		// Take the intersection of the ray and the z=desiredZ plane
		return ray.origin + ((desiredZ - ray.origin.z) / ray.direction.z) * ray.direction;
	}

	private Clickable getWorldObject(Vector2 worldMousePosition) {
		Clickable ret = null;
		foreach(Clickable clickable in Scene.get().getClickableObjects()) {
			if (clickable.clickTest(0, worldMousePosition)) {
				// TODO: pick closest object or multiple objects if they overlap
				ret = clickable;
			}
		}
		return ret;
	}
}
