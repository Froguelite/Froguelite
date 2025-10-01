using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ProfileData
{
    //Essentially a hashmap of key-value pairs
    //Key is variable name's enum value, value is variable value
    public SerializableDictionary<SaveVariable, SaveValue> data = new SerializableDictionary<SaveVariable, SaveValue>();

}
