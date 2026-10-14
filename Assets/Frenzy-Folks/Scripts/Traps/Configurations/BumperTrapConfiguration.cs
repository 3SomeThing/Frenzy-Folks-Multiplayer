using UnityEngine;

public class BumperTrapConfiguration : MonoBehaviour
{
    [SerializeField, Min(0f)] public float pushSpeedHorizontal = 30f;
    [SerializeField, Min(0f)] public float pushSpeedVertical = 3f;
}
