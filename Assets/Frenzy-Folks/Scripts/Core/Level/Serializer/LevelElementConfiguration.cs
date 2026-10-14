using System;
using UnityEngine;

public abstract class LevelElementConfiguration<TLevelElement, TConfiguration>
    : BaseConfigurationSerializable, IApplicable<TLevelElement, TConfiguration>
    where TLevelElement : Component
    where TConfiguration : BaseConfigurationSerializable
{
    public abstract void Apply(TLevelElement target);
    public abstract TConfiguration From(TLevelElement source);

    public sealed override void Apply(GameObject target)
    {
        Apply(GetLevelElement(target));
    }

    public sealed override BaseConfigurationSerializable From(GameObject source)
    {
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