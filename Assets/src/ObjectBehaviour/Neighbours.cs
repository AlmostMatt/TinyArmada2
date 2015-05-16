using System.Collections;
using System.Collections.Generic;

public class Tuple<T1, T2>
{
	public T1 First { get; private set; }
	public T2 Second { get; private set; }
	internal Tuple(T1 first, T2 second)
	{
		First = first;
		Second = second;
	}
}

public class Neighbours<T> : IEnumerable<Tuple<float, T>>
{
	private SortedList<float, List<T>> _neighbours;

	public Neighbours ()
	{
		_neighbours = new SortedList<float, List<T>>();
	}

	public void Clear() {
		_neighbours.Clear();
	}

	public void Add(T other, float dd) {
		if (_neighbours.ContainsKey(dd)) {
			_neighbours[dd].Add(other);
		} else {
			_neighbours.Add(dd, new List<T>() {other});
		}
	}

	// this is generic
	public IEnumerator<Tuple<float, T>> GetEnumerator() {
		foreach (KeyValuePair<float, List<T>> kvp in _neighbours) {
			foreach (T other in kvp.Value) {
				yield return new Tuple<float, T>(kvp.Key, other);
			}
		}
	}

	// this is not generic
	IEnumerator IEnumerable.GetEnumerator() {
		return this.GetEnumerator();
	}
}

