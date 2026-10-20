using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EndPlatformConfiguration : LevelElementConfiguration<EndPlatform, EndPlatformConfiguration>
{
    public override void Apply(EndPlatform target)
    {
        return;
    }

    public override EndPlatformConfiguration From(EndPlatform source)
    {
        return this;
    }
}