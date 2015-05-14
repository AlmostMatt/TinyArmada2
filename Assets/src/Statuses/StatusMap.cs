//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

public class StatusMap
{
	private Dictionary<State, Status> statusMap;
	private Unit owner;

	public StatusMap(Unit unit)
	{
		statusMap = new Dictionary<State, Status>();
		owner = unit;
	}
	
	public void update(float dt) {
		HashSet<State> expired = new HashSet<State>();
		foreach (Status s in statusMap.Values) {
			s.duration -= dt;
			if (s.duration <= 0) {
				s.expire(owner);
				expired.Add(s.type);
			}
		}
		foreach(State state in expired) {
			statusMap.Remove(state);
		}
	}

	public void add(Status s, float duration) {
		if (has (s.type)) {
			statusMap[s.type].duration += duration;
		} else {
			s.duration = duration;
			statusMap[s.type] = s;
			s.begin(owner);
		}
	}

	public bool has(State st) {
		return (statusMap.ContainsKey(st));
	}

	// this assumes the status exists
	public float duration(State st) {
		return statusMap[st].duration;
	}
}