using UnityEngine;
using System.Collections;

public interface Clickable {
	bool clickTest(int mouseButton, Vector2 mousePos);
	void handleClick(int mouseButton);
	void handleDrag(int mouseButton, Vector2 offset, Vector2 relativeToClick);
	void setHover(bool isHovering);
	void handleMouseDown(int mouseButton, Vector2 mousePos);
	void handleMouseUp(int mouseButton, Vector2 mousePos);
}

// a clickable object will usually just defer clickTest to it's click region
public abstract class ClickRegion {
	public abstract bool Contains(Vector2 mousePos);
}

public class CircleRegion : ClickRegion {
	private Vector2 center;
	private float radius;

	public CircleRegion(Vector2 p, float r) {
		center = p;
		radius = r;
	}

	public CircleRegion(float r)
		: this(new Vector2(), r)
	{
	}

	public override bool Contains(Vector2 mousePos) {
		return (mousePos - center).sqrMagnitude < radius * radius;
	}
}

public class RectRegion : ClickRegion {
	private Rect rect;

	public RectRegion(Rect r) {
		rect = r;
	}
	
	public override bool Contains(Vector2 mousePos) {
		return rect.Contains(mousePos);
	}
}