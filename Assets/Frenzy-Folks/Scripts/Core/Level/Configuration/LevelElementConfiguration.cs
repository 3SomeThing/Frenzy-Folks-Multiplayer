using System;
using UnityEngine;

public abstract class LevelElementConfiguration<TLevelElement, TConfiguration>
    : BaseConfigurationSerializable, IApplicable<TLevelElement, TConfiguration>
    where TLevelElement : Component
    where TConfiguration : BaseConfigurationSerializable
{
    [SerializeField] public string name;
    [SerializeField] public SerializableTransform transform;
    
    public abstract void Apply(TLevelElement target);
    public abstract TConfiguration From(TLevelElement source);

    public sealed override GameObject Apply(GameObject target)
    {
        transform.Apply(target.transform);
        Apply(GetLevelElement(target));
        return target;
    }

    public sealed override BaseConfigurationSerializable From(GameObject source)
    {
        transform = new SerializableTransform().From(source.transform);
        return From(GetLevelElement(source));
    }

    private static TLevelElement GetLevelElement(GameObject target)
    {
        TLevelElement trap = target.GetComponent<TLevelElement>();
        if (trap == null)
            throw new InvalidOperationException(
                $"{target.name} needs a {typeof(TLevelElement).Name} component.");

        return trap;
    }
}