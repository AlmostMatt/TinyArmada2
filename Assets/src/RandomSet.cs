/*
 * Like hashset but supports random element selection via getRandom() and popRandom()
 * Note that iteration is not in a random order, but getRandom is
 */

using System;
using System.Collections;
using System.Collections.Generic;

public class RandomSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable 
    //ISerializable, IDeserializationCallback
{
    private List<T> data = new List<T>();
    private Dictionary<T, int> indexOf = new Dictionary<T, int>();
    private Random rnd;

    public int Count
    {
        get { return data.Count; }
    }
    
    public RandomSet()
    {
        rnd = new Random();
    }
    
    public RandomSet(Random rand)
    {
        rnd = rand;
    }

    public bool IsReadOnly { get { return false; } }

    public void CopyTo(T[] array) {
        data.CopyTo(array);
    }

    public void CopyTo(T[] array, int arrayIndex) {
        data.CopyTo(array, arrayIndex);
    }

    public void Clear() {
        data.Clear();
        indexOf.Clear();
    }
    
    public void Add(T obj) {
        if (indexOf.ContainsKey(obj)) {
            return;
        } else {
            indexOf[obj] = data.Count;
            data.Add(obj);
        }
    }
    
    public bool Remove(T obj) {
        if (indexOf.ContainsKey(obj)) {
            data.RemoveAt(indexOf[obj]);
            return true;
        } else {
            return false;
        }
    }
    
    public T RemoveAt(int index) {
        // swap with last item, then pop
        T result = data[index];
        if (index < data.Count - 1) {
            data[index] = data[data.Count - 1];
            indexOf[data[index]] = index;
        }
        indexOf.Remove(result);
        return result;
    }
    
    public bool Contains(T obj) {
        return indexOf.ContainsKey(obj);
    }

    public T get(int i) {
        return data[i];
    }

    public T getRandom() {
        return data[rnd.Next(0,data.Count)];
    }
    
    public T popRandom() {
        return RemoveAt(rnd.Next(0,data.Count));
    }
    
    // this is generic
    public IEnumerator<T> GetEnumerator() {
        return data.GetEnumerator();
    }
    
    // this is not generic
    IEnumerator IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
    }
}

