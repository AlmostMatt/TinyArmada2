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
		Vector3 worldMousePosition = getWorldMousePos();
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
					mainCamera.Translate(clickWorldPosition - worldMousePosition);
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

	private Vector3 getWorldMousePos() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		// Take the intersection of the ray and the z=desiredZ plane
		float desiredZ = 0f;
		return ray.origin + (desiredZ - ray.origin.z / ray.direction.z) * ray.direction;
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

	/*
	 * the following are old functions that converted various coordinates and assumed orthographic view
	 * I want a single class for input and camera control and another class for overlay stuff

	Vector2 getGUIMousePos() {
		return screenToGUI(Input.mousePosition);
	}

	Vector2 worldToGUI(Vector2 p) {
		return screenToGUI(Camera.main.WorldToScreenPoint(p));
	}

	float worldToGUI(float f) {
		// TODO: compute this once and later just multiply f
		return worldToGUI(new Vector2(f, 0)).x - worldToGUI(new Vector2(0, 0)).x;
	}

	Vector2 screenToGUI(Vector2 p) {
		return new Vector2(p.x, Screen.height - p.y);
	}
	*/
}
