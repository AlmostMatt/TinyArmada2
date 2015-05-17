using UnityEngine;

public interface Attackable {
	Transform transform
	{
		get;
	}
	float radius
	{
		get;
	}
	bool dead
	{
		get;
	}
	void damage(Player attacker, int amount);
}
