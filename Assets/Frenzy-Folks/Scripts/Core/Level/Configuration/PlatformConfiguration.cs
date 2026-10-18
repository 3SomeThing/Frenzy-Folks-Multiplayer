using UnityEngine;

public class PlatformConfiguration : LevelElementConfiguration<Platform, PlatformConfiguration>
{
    [SerializeField] private bool makePlatformFall = false;
    [SerializeField] private float delayBeforeFalling = 2f;
    [SerializeField] private float delayBeforeRespawning = 3f;

    public override void Apply(Platform target)
    {
        throw new System.NotImplementedException();
    }

    public override PlatformConfiguration From(Platform source)
    {
        throw new System.NotImplementedException();
    }
}