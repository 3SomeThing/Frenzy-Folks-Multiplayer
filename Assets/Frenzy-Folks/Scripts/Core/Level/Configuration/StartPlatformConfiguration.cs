using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class StartPlatformConfiguration : LevelElementConfiguration<StartPlatform, StartPlatformConfiguration>
{
    [SerializeField] private int checkpointIndex;
    
    public override void Apply(StartPlatform target)
    {
        target.checkpointIndex = checkpointIndex;
    }

    public override StartPlatformConfiguration From(StartPlatform source)
    {
        checkpointIndex = source.checkpointIndex;
        return this;
    }
}