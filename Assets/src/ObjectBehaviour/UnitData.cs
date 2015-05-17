using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum UnitType {MERCHANT=0, GALLEY=1, LONGBOAT=2};

public class UnitData : MonoBehaviour {
	
	public Sprite[] icons;
	public Sprite[] images;
	public Sprite[] teamImages;

	private static UnitData singleton;
	// Use this for initialization
	void Start () {
		singleton = this;
	}
	
	// Update is called once per frame
	void Update () {
	
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
	
	public static Sprite getImage(UnitType uType) {
		return singleton.images[(int) uType];
	}
	
	public static Sprite getTeamImage(UnitType uType) {
		return singleton.teamImages[(int) uType];
	}
}
