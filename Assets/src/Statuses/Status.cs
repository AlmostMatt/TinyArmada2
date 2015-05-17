using System;

// TODO: refactor this to allow superclassing instead of a case statement
public enum State {ANIMATION, STUNNED};

public class Status
{
	// enumerate types

	public State type;
	public float duration;

	public Status (State statusType)
	{
		type = statusType;
		duration = 0;
	}

	public virtual void begin(Unit owner) {
		switch (type) {
		case State.ANIMATION:
			//owner.canTurn = false;
			break;
		}
	}

	public virtual void expire(Unit owner) {
		switch (type) {
		case State.ANIMATION:
			//owner.canTurn = true;
			break;
		}
	}
}