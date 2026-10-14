using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class MovablePlatformConfiguration : LevelElementConfiguration<MovablePlatform, MovablePlatformConfiguration>
{
    [SerializeField]
    private List<Vector3> pathPoints = new List<Vector3>()
    {
        new Vector3(1, 1, 1),
        new Vector3(2, 2, 2),
        new Vector3(3, 3, 3)
    };

    public override void Apply(MovablePlatform target)
    {
        Debug.Log("Works");
    }

    public override MovablePlatformConfiguration From(MovablePlatform source)
    {
        throw new System.NotImplementedException();
    }
}