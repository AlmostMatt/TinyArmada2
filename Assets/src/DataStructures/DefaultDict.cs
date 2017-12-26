using System.Collections;
using System.Collections.Generic;

/**
 * Similar to python's default dict, the dictionary has a default value to return if the key is not present.
 * Based on https://stackoverflow.com/a/9264669
 */
public class DefaultDict<TKey, TValue> : Dictionary<TKey, TValue>
{
	TValue defaultValue;
	public DefaultDict(TValue defaultValue) : base() {
		this.defaultValue = defaultValue;
	}
	public new TValue this[TKey key]
	{
		get { 
			TValue t;
			return base.TryGetValue(key, out t) ? t : defaultValue;
		}
		set { base[key] = value; }
	}
}