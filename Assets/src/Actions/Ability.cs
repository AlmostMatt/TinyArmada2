using UnityEngine;
using System;

public class Ability
{
	// maxcd is constant, cd is remaining
	private float maxcd;
	private float cd;

	// castTime is constant, animTime is the remaining amount
	public float castTime;
	private float animTime;

	public Unit owner;

	// either a unit, a point, a direction, or none
	private object target;

	public Ability (float cooldown)
	{
		cd = 0f;
		castTime = 0.15f;
		maxcd = cooldown;
	}

	public bool ready() {
		return cd <= 0f;
	}

	public void update(float dt) {
		cd = Math.Max(0f, cd - dt);
		if (animTime > 0) {
			if (animTime < dt) {
				animTime = 0f;
				onCast();
			} else {
				animTime -= dt;
			}
		}
	}

	// when the unit decides to use the ability
	public void use(object targ) {
		cd = maxcd;
		animTime = castTime;
		target = targ;
	}

	// when the ability happens (after the animation)
	public virtual void onCast() {
		// override this

		// attack
		owner.fire((Unit) target);
	}
}
