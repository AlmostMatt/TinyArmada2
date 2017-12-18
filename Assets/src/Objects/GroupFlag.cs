using UnityEngine;
using System.Collections;

public class GroupFlag : MonoBehaviour, Clickable {

	private UnitGroup group;
	public ClickRegion clickArea = new CircleRegion(0.5f);

	private SpriteRenderer ring;
	private SpriteRenderer icon;

	// Use this for initialization
	void Start () {
		ring = transform.FindChild("ring").GetComponent<SpriteRenderer>();
		icon = transform.FindChild("icon").GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void setGroup(UnitGroup grp) {
		group = grp;
	}

	public void cleanup() {
		Destroy(gameObject);
	}

	/*
	 * Clickable
	 */
	
	public bool clickTest(int mouseButton, Vector2 mousePos) {
		return clickArea.Contains(mousePos - (Vector2) transform.position);
	}
	
	public void handleClick(int mouseButton) {
		
	}
	
	public void handleDrag(int mouseButton, Vector2 offset, Vector2 relativeToClick) {
		
	}

	public void setHover(bool isHovering)
	{
		if (isHovering) {
			ring.color = new Color(1f, 0.5f, 0.5f);
			icon.color = new Color(1f, 0.5f, 0.5f);
		} else {
			ring.color = Color.white;
			icon.color = Color.white;
		}
	}

	public void handleMouseDown(int mouseButton, Vector2 mousePos) {
		ring.color = new Color(1f, 0.5f, 0.5f);
		icon.color = new Color(1f, 0.5f, 0.5f);
	}
	
	public void handleMouseUp(int mouseButton, Vector2 mousePos) {
		ring.color = Color.white;
		icon.color = Color.white;
	}
}
