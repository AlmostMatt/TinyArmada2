using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public delegate void RadialMenuCallback(int optionSelected);

public class RadialMenu : MonoBehaviour {

	public GameObject radialButtonObject;
	private List<Sprite> icons = new List<Sprite>();
	private float anglePerButton;
	private int numButtons;
	
	private List<CanvasGroup> normalButtons = new List<CanvasGroup>();
	private List<CanvasGroup> selectedButtons = new List<CanvasGroup>(); 
	private CanvasGroup group;

	public bool visible = false;
	private RadialMenuCallback mouseUpCallback;

	void Awake() {
	}
	// Use this for initialization
	void Start () {
		group = GetComponent<CanvasGroup>();
		setup(new Vector2(0,0), new List<Sprite>(), null);
		setVisible(false);
	}

	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI() {
		Event e = Event.current;
		if (!visible || !e.isMouse) {
			return;
		}
		Vector2 mousePosition = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
		int optionSelected = getOptionSelected(mousePosition);

		if (e.type == EventType.MouseUp) {
			//callback
			if (mouseUpCallback != null) {
				mouseUpCallback(optionSelected);
				mouseUpCallback = null;
			}
			setVisible(false);
		}
		for (int i=0; i<numButtons; ++i) {
			float a1 = (optionSelected == i) ? 1f : 0f;
			float a2 = 1f - a1;
			selectedButtons[i].alpha = a1;
			normalButtons[i].alpha = a2;
		}
	}

	private int getOptionSelected(Vector2 mousePosition) {
		Vector2 offset = mousePosition - (Vector2) transform.position;
		float minDD = Mathf.Pow(20, 2);
		if (offset.sqrMagnitude > minDD) {
			// radial starts vertically, and also inverts y coordinates. So 90 degree mouse angle is in the middle of button 0.
			float mouseAngle = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
			mouseAngle += anglePerButton / 2f - 90f; 
			while (mouseAngle < 0) mouseAngle += 360f;
			return (int) (mouseAngle / anglePerButton);
		} 
		return -1;
	}

	private void setVisible(bool isVisible) {
		visible = isVisible;
		group.alpha = isVisible ? 1f : 0f;
	}

	public void cancel() {
		setVisible(false);
		mouseUpCallback = null;
	}

	public void setup(Vector2 screenPosition, List<Sprite> newIcons, RadialMenuCallback mouseUpCallback) {
		transform.position = screenPosition;
		setupButtons(newIcons);
		setVisible(true);
		this.mouseUpCallback = mouseUpCallback;
	}

	private void setupButtons(List<Sprite> newIcons) {
		// Remove any old button objects
		foreach (var button in normalButtons) {
			Destroy(button.gameObject);
		}
		normalButtons.Clear();
		foreach (var button in selectedButtons) {
			Destroy(button.gameObject);
		}
		selectedButtons.Clear();

		icons = newIcons;
		numButtons = icons.Count;
		anglePerButton = 360f / numButtons;

		// TODO: take a list of costs as well and tint icons accordingly
		// Green - hover, gray - default. red - can't afford
		// or show the costs in the button as coloured text with icons
		for (int i=0; i<numButtons; i++) {
			GameObject button = Instantiate(radialButtonObject);
			button.transform.SetParent(transform);
			button.transform.localPosition = new Vector3(0,0,0);
			button.transform.localEulerAngles = new Vector3(0f,0f,i * anglePerButton );
			// selected and normal versions of buttons
			Transform t1 = button.transform.FindChild("normal");
			Transform t2 = button.transform.FindChild("selected");
			normalButtons.Add(t1.GetComponent<CanvasGroup>());
			selectedButtons.Add(t2.GetComponent<CanvasGroup>());
			t1.FindChild("icon").GetComponent<Image>().sprite = icons[i];
			t2.FindChild("icon").GetComponent<Image>().sprite = icons[i];
			normalButtons[i].alpha = 0f;
			selectedButtons[i].alpha = 0f;
		}
	}
}
