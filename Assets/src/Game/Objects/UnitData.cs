using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum UnitType {MERCHANT=0, GALLEY=1, LONGBOAT=2, TRADER=3};

public class UnitData : MonoBehaviour {
	
	public Sprite[] icons;
	public GameObject[] unitModels;

	private static UnitData singleton;
	// Use this for initialization
	void Start () {
		singleton = this;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public static Dictionary<string, float> getStats(UnitType type) {
		// TODO: store data in JSON and parse it in unit init
		Dictionary<string, float> stats = new Dictionary<string, float>();
		switch(type) {
		case UnitType.MERCHANT:
		case UnitType.TRADER:
			stats["capacity"] = 25f;
			stats["maxV"] = 2f;
			stats["accel"] = 5;
			stats["canTrade"] = 1f;
			stats["canAttack"] = 0f;
			break;
		default:
			stats["maxV"] = 1.5f;
			stats["accel"] = 5;
			stats["range"] = 3f;
			stats["canTrade"] = 0f;
			stats["canAttack"] = 1f;
			break;
		}
		return stats;
	}
	
	public static Dictionary<Resource, int> getCost(UnitType type) {
		Dictionary<Resource, int> cost = new Dictionary<Resource, int>();
		switch(type) {
		case UnitType.MERCHANT:
			cost[Resource.FOOD] = 50;
			break;
		case UnitType.GALLEY:
			cost[Resource.FOOD] = 100;
			cost[Resource.GOLD] = 10;
			break;
		case UnitType.TRADER:
			cost[Resource.FOOD] = 50;
			break;
		case UnitType.LONGBOAT:
			cost[Resource.FOOD] = 100;
			cost[Resource.GOLD] = 10;
			break;
		default:
			cost[Resource.FOOD] = 100;
			cost[Resource.GOLD] = 10;
			break;
		}
		return cost;
	}
	
	public static Sprite getIcon(UnitType uType) {
		return singleton.icons[(int) uType];
	}
	
	public static GameObject getModel(UnitType uType) {
		return singleton.unitModels[(int) uType];
	}
}
