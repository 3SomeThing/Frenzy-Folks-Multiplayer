using System;
using UnityEngine;

[Serializable]
public abstract class BaseConfigurationSerializable
{
    private string _name;
    private SerializableTransform _transform;
    
    public abstract void Apply(GameObject target);
    public abstract BaseConfigurationSerializable From(GameObject source);
}
