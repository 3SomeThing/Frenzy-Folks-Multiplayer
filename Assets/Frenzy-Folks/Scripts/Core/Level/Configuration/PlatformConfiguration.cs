using UnityEngine;

public class PlatformConfiguration : LevelElementConfiguration<Platform, PlatformConfiguration>
{
    [SerializeField] private bool makePlatformFall = false;
    [SerializeField] private float delayBeforeFalling = 2f;
    [SerializeField] private float delayBeforeRespawning = 3f;

    public override void Apply(Platform target)
    {
        target.makePlatformFall = makePlatformFall;
        target.delayBeforeFalling = delayBeforeFalling;
        target.delayBeforeRespawning = delayBeforeRespawning;
    }

    public override PlatformConfiguration From(Platform source)
    {
        makePlatformFall = source.makePlatformFall;
        delayBeforeFalling = source.delayBeforeFalling;
        delayBeforeRespawning = source.delayBeforeRespawning;
        return this;
    }
}