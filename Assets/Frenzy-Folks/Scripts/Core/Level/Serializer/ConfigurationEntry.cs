using System;
using UnityEngine;

[Serializable]
public class ConfigurationEntry
{
    public string key;
    [SerializeReference] public BaseConfigurationSerializable value;
}