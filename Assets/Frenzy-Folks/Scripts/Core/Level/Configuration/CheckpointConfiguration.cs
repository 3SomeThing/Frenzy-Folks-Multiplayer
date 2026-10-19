using UnityEngine;

public class CheckpointConfiguration : LevelElementConfiguration<Checkpoint, CheckpointConfiguration>
{
    [SerializeField] private int checkpointIndex;
    
    public override void Apply(Checkpoint target)
    {
        target.checkpointIndex = checkpointIndex;
        target.RefreshRegistration();
    }

    public override CheckpointConfiguration From(Checkpoint source)
    {
        checkpointIndex = source.checkpointIndex;
        return this;
    }
}