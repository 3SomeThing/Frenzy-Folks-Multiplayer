using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MovablePlatformConfiguration : LevelElementConfiguration<MovablePlatform, MovablePlatformConfiguration>
{
    [SerializeField] public float speed = 5F;
    [SerializeField] public Vector3 secondPoint;

    public override void Apply(MovablePlatform target)
    {
        target.Speed = speed;
        target.SetPathPoints(new List<Vector3>() { target.transform.position, secondPoint });
    }

    public override MovablePlatformConfiguration From(MovablePlatform source)
    {
        speed = source.Speed;
        secondPoint = source.GetPathPoints().Count > 1 ? source.GetPathPoints()[1] : source.transform.position;
        return this;
    }
}