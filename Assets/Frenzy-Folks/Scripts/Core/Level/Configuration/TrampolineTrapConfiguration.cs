using UnityEngine;

public class TrampolineTrapConfiguration : LevelElementConfiguration<TrampolineTrap, TrampolineTrapConfiguration>
{
    [SerializeField, Min(0f)] public float pushSpeedVertical = 30f;
    
    public override void Apply(TrampolineTrap trampoline)
    {
        throw new System.NotImplementedException();
    }

    public override TrampolineTrapConfiguration From(TrampolineTrap source)
    {
        throw new System.NotImplementedException();
    }
}
