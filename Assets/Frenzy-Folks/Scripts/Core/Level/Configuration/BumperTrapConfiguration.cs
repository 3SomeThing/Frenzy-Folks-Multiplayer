using UnityEngine;

public class BumperTrapConfiguration : LevelElementConfiguration<BumperTrap, BumperTrapConfiguration>
{
    [SerializeField, Min(0f)] private float pushSpeedHorizontal = 30f;
    [SerializeField, Min(0f)] private float pushSpeedVertical = 100f;
    
    public override void Apply(BumperTrap target)
    {
        target.pushSpeedHorizontal = pushSpeedHorizontal;
        target.pushSpeedVertical = pushSpeedVertical;
    }

    public override BumperTrapConfiguration From(BumperTrap source)
    {
        pushSpeedHorizontal = source.pushSpeedHorizontal;
        pushSpeedVertical = source.pushSpeedVertical;
        return this;
    }
}
