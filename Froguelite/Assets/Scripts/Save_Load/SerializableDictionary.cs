using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    //Essentially a hashmap of key-value pairs
    //Key is variable name's enum value, value is variable value

    [SerializeField] private List<TKey> keys = new List<TKey>();
    // Important: store values as SerializeReference so Unity keeps polymorphic types
    [SerializeReference] private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();
        if (keys.Count != values.Count)
            throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));
        for (int i = 0; i < keys.Count; i++)
            this.Add(keys[i], values[i]);
    }
}


[Serializable]
public abstract class SaveValue
{
    public abstract object BoxedValue { get; }
}

[Serializable]
public class SaveValue<T> : SaveValue
{
    public T value;
    public override object BoxedValue => value;
}