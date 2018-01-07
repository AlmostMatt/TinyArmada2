using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitGroup : IEnumerable<Unit>, Clickable
{
	public Vector2 center;
	public float radius;
	public Color color;
	public int numUnits;
	private HashSet<Unit> units;

	private Vector2 dest;

	private GroupFlag flag;
	private Player owner;

	private static Color[] colors = {
		new Color(1,0,0, 0.8f),
		new Color(0,1,0, 0.8f),
		new Color(0,0,1, 0.8f),
		new Color(0,1,1, 0.8f),
		new Color(1,0,1, 0.8f),
		new Color(1,1,0, 0.8f)
	};

	public UnitGroup (List<Unit> unitsToAdd, GameObject flagObject)
	{
		numUnits = 0;
		radius = 0;
		units = new HashSet<Unit>();
		color = colors[Random.Range(0, colors.Length)];

		foreach (Unit unit in unitsToAdd) {
			unit.setGroup(this);
		}
		flag = flagObject.GetComponent<GroupFlag>();
		flag.setGroup(this);
		flag.transform.position = dest;
	}

	public bool isEmpty() {
		return units.Count == 0;
	}

	public void Add(Unit u) {
		units.Add(u);
		setOwner(u.owner);
	}
	
	public void Remove(Unit u) {
		units.Remove(u);
	}

	//http://stackoverflow.com/questions/8760322/troubles-implementing-ienumerablet

	// this is generic
	public IEnumerator<Unit> GetEnumerator() {
		return units.GetEnumerator();
	}

	// this is not generic
	IEnumerator IEnumerable.GetEnumerator() {
		return this.GetEnumerator();
	}

	public void update() {
		// each has radius 0.25 and area pi r 2
		// so n of them has total area n pi r 2 => scale radius by sqrt(n)
		radius = 0.4f * Mathf.Sqrt(units.Count);
		
		center = avgPos(units);
		numUnits = units.Count;
	}

	private Vector2 avgPos(IEnumerable<Unit> unitList) {
		Vector2 p = new Vector2(0,0);
		int count = 0;
		foreach (Unit unit in unitList) {
			p += (Vector2) unit.transform.position;
			++count;
		}
		return p/count;
	}

	public void cleanup() {
		if (owner != null) {
			owner.unitgroups.Remove(this);
		}
		flag.cleanup();
	}
	
	public void setDest(Vector2 d, bool allowMerge = true) {
		if (flag == null) {
			// If the last unit in the group was destroyed, do nothing.
			return;
		}
		dest = d;
		flag.transform.position = d;
		if (owner.isHuman && allowMerge) {
			// merge groups that have the same destination
			foreach (UnitGroup grp in owner.unitgroups) {
				if (grp == this) { continue; }
				if ((grp.dest - dest).sqrMagnitude < 0.5f * 0.5f) {
					foreach (Unit u in grp.units) {
						u.group = this;
					}
					units.UnionWith(grp.units);
					grp.units.Clear();
					update();
				}
			}
		}
	}
	
	public Vector2 getDest() {
		return dest;
	}

	public GroupFlag getFlag() {
		return flag;
	}

	/*
	 CLICKABLE
	 */
	public bool clickTest(int mouseButton, Vector2 mousePos) {
		return owner.number == Player.HUMAN_PLAYER && getFlag().clickTest(mouseButton, mousePos);
	}
	
	public void handleClick(int mouseButton) {
		split();
	}
	
	public void handleDrag(int mouseButton, Vector2 offset, Vector2 relativeToClick) {
		setDest(dest + offset);
		//flag.handleDrag(mouseButton, offset, relativeToClick);
	}
	public void setHover(bool isHovering)
	{
		flag.setHover(isHovering);
	}
	
	public void handleMouseDown(int mouseButton, Vector2 mousePos) {
		flag.handleMouseDown(mouseButton, mousePos);
	}
	
	public void handleMouseUp(int mouseButton, Vector2 mousePos) {
		flag.handleMouseUp(mouseButton, mousePos);
	}

	private void setOwner(Player p) {
		if (owner != p) {
			owner = p;
			owner.unitgroups.Add(this);
		}
	}

	public UnitGroup split() {
		if (units.Count == 1) {
			return null;
		}
		// k-means clustering
		RandomSet<Unit> unitset = new RandomSet<Unit>(units);
		List<Unit>[] kgroups = new List<Unit>[2];
		Vector2[] kmeans = new Vector2[2];
		for (int k=0; k<2; ++k) {
			kgroups[k] = new List<Unit>();
		}
		for (int repetition = 0; repetition < 10; ++repetition) {
			for (int k=0; k<2; ++k) {
				if (kgroups[k].Count == 0) {
					kmeans[k] = unitset.getRandom().transform.position;
				} else {
					kmeans[k] = avgPos(kgroups[k]);
				}
				kgroups[k].Clear();
			}
			foreach (Unit u in units) {
				int closest = 0;
				float mindd = float.MaxValue;
				for (int k=0; k<2; ++k) {
					float dd = (kmeans[k] - (Vector2) u.transform.position).sqrMagnitude;
					if (dd < mindd) {
						mindd = dd;
						closest = k;
					}
				}
				kgroups[closest].Add(u);
			}
		}
		// TODO: prefer equally sized groups
		UnitGroup newGroup = Scene.get().createUnitGroup(kgroups[1]);
		Vector2 groupOffset = (kmeans[1] - kmeans[0]).normalized;
		newGroup.setDest(dest + groupOffset/2, false);
		setDest(dest - groupOffset/2, false);
		return newGroup;
	}
}