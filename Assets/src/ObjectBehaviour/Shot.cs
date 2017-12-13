 using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shot : MonoBehaviour {

	private Attackable target;
	public Player owner;

	// Use this for initialization
	public void Start () {
		GetComponent<Steering>().setSpeed(10f, 60f);
	}
	
	// Update is called once per frame
	void Update () {
		//Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		//Vector2 offset = ray.origin - transform.position;
		//float angle = Mathf.Rad2Deg * Mathf.Atan2(offset.y, offset.x);
		//float angle = Vector2.Angle(Vector2.right, );
		//transform.localEulerAngles = new Vector3(0, 0, angle);
	}

	public void FixedUpdate () {
		if (target == null || target.dead) {
			Destroy(gameObject);
		} else {
			if (Vector2.Distance(target.transform.position,
			                     transform.position) < target.radius) {
				hit(target);
				Destroy(gameObject);
			}
			// it looks a bit weird to have an arrow 'lead' a target.
			GetComponent<Steering>().seek(target.transform.position);
		}
	}

	public void setTarget(Attackable u) {
		target = u;
	}

	private void hit(Attackable u) {
		u.damage(owner, 1);
	}
}
