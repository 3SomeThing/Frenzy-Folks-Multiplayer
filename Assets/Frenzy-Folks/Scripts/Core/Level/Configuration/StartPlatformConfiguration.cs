using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class StartPlatformConfiguration : LevelElementConfiguration<StartPlatform, StartPlatformConfiguration>
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private bool playSoundAndAnimation;
    [SerializeField] private bool changeColor;
    
    
    public override void Apply(StartPlatform target)
    {
        target.checkpointIndex = checkpointIndex;
        target.playSoundAndAnimation = playSoundAndAnimation;
        target.changeColor = changeColor;
    }

    public override StartPlatformConfiguration From(StartPlatform source)
    {
        checkpointIndex = source.checkpointIndex;
        playSoundAndAnimation = source.playSoundAndAnimation;
        changeColor = source.changeColor;
        
        return this;
    }
}