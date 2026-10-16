using System;
using UnityEngine;

[Serializable]
public class SerializableTransform : IApplicable<Transform, SerializableTransform>
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    
    public void Apply(Transform obj)
    {
        obj.position = position;
        obj.rotation = rotation;
        obj.localScale = scale;
    }

    public SerializableTransform From(Transform obj)
    {
        return new SerializableTransform
        {
            position = obj.position,
            rotation = obj.rotation,
            scale = obj.localScale
        };
    }

    public override string ToString()
    {
        return $"pos: {position.ToString()} - rotation: {rotation.ToString()} - scale: {scale.ToString()}";
    }
}