 using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shot : MonoBehaviour {

	private Attackable target;
	public Actor attacker;
	private Seek seekBehaviour = new Seek(Vector2.zero);

	// Use this for initialization
	public void Start () {
		Steering steering = GetComponent<Steering>();
		steering.setSpeed(10f, 60f);
		steering.addBehaviour(1f, seekBehaviour);
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
			// It looks weird to have an arrow 'lead' a target, so I seek
			seekBehaviour.setTarget(target.transform.position);
		}
	}

	public void setTarget(Attackable u) {
		target = u;
	}

	private void hit(Attackable u) {
		u.damage(attacker, 1);
	}
}
