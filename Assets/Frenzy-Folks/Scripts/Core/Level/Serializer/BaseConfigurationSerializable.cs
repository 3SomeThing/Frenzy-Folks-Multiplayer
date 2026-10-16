using System;
using UnityEngine;

[Serializable]
public abstract class BaseConfigurationSerializable
{
    public abstract GameObject Apply(GameObject target);
    public abstract BaseConfigurationSerializable From(GameObject source);
}
