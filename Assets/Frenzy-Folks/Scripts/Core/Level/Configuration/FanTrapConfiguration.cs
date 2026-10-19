using UnityEngine;

public class FanTrapConfiguration : LevelElementConfiguration<FanTrap, FanTrapConfiguration>
{
    [SerializeField, Min(0f)] public float pushSpeed = 100f;
    [SerializeField, Min(0.1f)] public float blowingDuration = 7f;
    [SerializeField, Min(0.1f)] public float pauseDuration = 2f;
    [SerializeField, Range(0f, 90f)] public float sweepAngle = 45f;
    
    public override void Apply(FanTrap target)
    {
        target.pushSpeed = pushSpeed;
        target.blowingDuration = blowingDuration;
        target.pauseDuration = pauseDuration;
        target.sweepAngle = sweepAngle;
    }

    public override FanTrapConfiguration From(FanTrap source)
    {
        pushSpeed = source.pushSpeed;
        blowingDuration = source.blowingDuration;
        pauseDuration = source.pauseDuration;
        sweepAngle = source.sweepAngle;
        return this;
    }
}
 