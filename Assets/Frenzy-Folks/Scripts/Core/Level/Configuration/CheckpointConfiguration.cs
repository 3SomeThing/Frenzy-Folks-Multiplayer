using UnityEngine;

public class CheckpointConfiguration : LevelElementConfiguration<Checkpoint, CheckpointConfiguration>
{
    [SerializeField] private int checkpointIndex;
    
    public override void Apply(Checkpoint target)
    {
        throw new System.NotImplementedException();
    }

    public override CheckpointConfiguration From(Checkpoint source)
    {
        throw new System.NotImplementedException();
    }
}