using UnityEngine;

public class TurretTrapConfiguration : LevelElementConfiguration<TurretFSM, TurretTrapConfiguration>
{
    [SerializeField] 
    private float fireRate = 1f;
    [SerializeField, Min(0f)]
    private int targetingSpeed = 5;
    [SerializeField, Range(0f, 360f)] 
    private float detectionRadius = 10f;
    
    public override void Apply(TurretFSM target)
    {
        target.fireRate = fireRate;
        target.targetingSpeed = targetingSpeed;
        target.detectionRadius = detectionRadius;
    }

    public override TurretTrapConfiguration From(TurretFSM source)
    {
        fireRate = source.fireRate;
        targetingSpeed = source.targetingSpeed;
        detectionRadius = source.detectionRadius;
        return this;
    }
}