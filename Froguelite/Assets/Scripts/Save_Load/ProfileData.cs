using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

[Serializable]
public class ProfileData
{
    // Main dictionary for storing all saved variables
    [JsonProperty]
    public Dictionary<SaveVariable, ISaveValue> data { get; private set; }

    // Needed for JSON deserialization
    public ProfileData()
    {
        data = new Dictionary<SaveVariable, ISaveValue>();
    }
}