using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class MovablePlatformConfiguration : LevelElementConfiguration<MovablePlatform, MovablePlatformConfiguration>
{
    [SerializeField] public float speed = 5F;
    [SerializeField] public List<Vector3> pathPoints = new List<Vector3>();

    public override void Apply(MovablePlatform target)
    {
        target.SetPathPoints(pathPoints);
    }

    public override MovablePlatformConfiguration From(MovablePlatform source)
    {
        speed = source.Speed;
        pathPoints = source.GetPathPoints();
        return this;
    }
}