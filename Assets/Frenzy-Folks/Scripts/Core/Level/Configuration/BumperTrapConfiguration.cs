using UnityEngine;

public class BumperTrapConfiguration : LevelElementConfiguration<BumperTrap, BumperTrapConfiguration>
{
    [SerializeField, Min(0f)] public float pushSpeedHorizontal = 30f;
    [SerializeField, Min(0f)] public float pushSpeedVertical = 100f;
    
    public override void Apply(BumperTrap target)
    {
        throw new System.NotImplementedException();
    }

    public override BumperTrapConfiguration From(BumperTrap source)
    {
        throw new System.NotImplementedException();
    }
}
