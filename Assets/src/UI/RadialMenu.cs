using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public delegate void Callback(int selected);

public class RadialMenu : MonoBehaviour {

	public Vector2 center = new Vector2(500,500);
	public GameObject radialButton;
	private List<Sprite> icons = new List<Sprite>();
	private float angle;
	private int numButtons;
	
	private List<CanvasGroup> normalButtons = new List<CanvasGroup>();
	private List<CanvasGroup> selectedButtons = new List<CanvasGroup>(); 
	private CanvasGroup group;

	private int selected = -1;
	public bool visible = false;
	private Callback callback;

	void Awake() {
	}
	// Use this for initialization
	void Start () {
		group = GetComponent<CanvasGroup>();
		init();
	}

	private void init() {
		center = transform.position;
		numButtons = icons.Count;
		angle = 360f / numButtons;

		for (int i=0; i<numButtons; ++i) {
			GameObject button = Instantiate(radialButton);
			button.transform.SetParent(transform);
			button.transform.localPosition = new Vector3(0,0,0);
			button.transform.localEulerAngles = new Vector3(0f,0f,i * angle);
			// selected and normal versions of buttons
			Transform t1 = button.transform.FindChild("normal");
			Transform t2 = button.transform.FindChild("selected");
			normalButtons.Add(t1.GetComponent<CanvasGroup>());
			selectedButtons.Add(t2.GetComponent<CanvasGroup>());
			// icon images and orientation
			Transform icon1 = t1.FindChild("icon");
			icon1.GetComponent<Image>().sprite = icons[i];
			icon1.rotation = Quaternion.identity;
			Transform icon2 = t2.FindChild("icon");
			icon2.GetComponent<Image>().sprite = icons[i];
			icon2.rotation = Quaternion.identity;

			normalButtons[i].alpha = 0f;
			selectedButtons[i].alpha = 0f;
		}
		group.alpha = 0f;
		visible = false;
	}

	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI() {
		Event e = Event.current;

		if (e.type == EventType.MouseUp) {
			//callback
			group.alpha = 0f;
			visible = false;
			if (callback != null) {
				callback(selected);
			}
		}

		Vector2 mouse = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
		Vector2 offset = mouse - (Vector2) transform.position;
		float minDD = Mathf.Pow(20, 2);
		if (offset.sqrMagnitude > minDD) {
			// radial starts vertically, and also inverts y coordinates
			float mouseAngle = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
			mouseAngle += angle / 2f - 90f; // for rounding
			while (mouseAngle < 0) mouseAngle += 360f;
			selected = (int) (mouseAngle / angle);
		} else {
			selected = -1;
		}

		for (int i=0; i<numButtons; ++i) {
			float a1 = (selected == i) ? 1f : 0f;
			float a2 = 1f - a1;
			selectedButtons[i].alpha = a1;
			normalButtons[i].alpha = a2;
		}
	}

	public void mouseDown(Vector2 pos, List<Sprite> newIcons, Callback f) {
		// TODO: take a list of costs as well and tint icons accordingly
		// Green - hover, gray - default. red - can't afford
		// or show the costs in the button as coloured text with icons
		foreach (var button in normalButtons) {
			Destroy(button.gameObject);
		}
		normalButtons.Clear();
		foreach (var button in selectedButtons) {
			Destroy(button.gameObject);
		}
		selectedButtons.Clear();
		icons = newIcons;
		init ();
		transform.position = pos;
		group.alpha = 1f;
		visible = true;
		callback = f;
	}
}
