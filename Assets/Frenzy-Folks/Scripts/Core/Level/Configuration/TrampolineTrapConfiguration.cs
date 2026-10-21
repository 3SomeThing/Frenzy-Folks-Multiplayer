using UnityEngine;

public class TrampolineTrapConfiguration : LevelElementConfiguration<TrampolineTrap, TrampolineTrapConfiguration>
{
    [SerializeField, Min(0f)] private float pushSpeedVertical = 30f;
    
    public override void Apply(TrampolineTrap trampoline)
    {
        Debug.Log(transform + " Applying TrampolineTrapConfiguration to " + trampoline.name);
        trampoline.pushSpeedVertical = pushSpeedVertical;
    }

    public override TrampolineTrapConfiguration From(TrampolineTrap source)
    {
        pushSpeedVertical = source.pushSpeedVertical;
        return this;
    }
}
