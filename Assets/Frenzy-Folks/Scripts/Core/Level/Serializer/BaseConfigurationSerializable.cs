using System;
using UnityEngine;

[Serializable]
public abstract class BaseConfigurationSerializable
{
    [SerializeField] public string name;
    [SerializeField] public SerializableTransform transform;
    
    public abstract GameObject Apply(GameObject target);
    public abstract BaseConfigurationSerializable From(GameObject source);
}
