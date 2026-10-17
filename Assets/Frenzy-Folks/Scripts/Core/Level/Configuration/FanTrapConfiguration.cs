using UnityEngine;

public class FanTrapConfiguration : LevelElementConfiguration<FanTrap, FanTrapConfiguration>
{
    [SerializeField, Min(0f)] public float pushSpeed = 100f;
    [SerializeField, Min(0.1f)] public float blowingDuration = 7f;
    [SerializeField, Min(0.1f)] public float pauseDuration = 2f;
    [SerializeField, Range(0f, 90f)] public float sweepAngle = 45f;
    
    public override void Apply(FanTrap target)
    {
        throw new System.NotImplementedException();
    }

    public override FanTrapConfiguration From(FanTrap source)
    {
        throw new System.NotImplementedException();
    }
}
 