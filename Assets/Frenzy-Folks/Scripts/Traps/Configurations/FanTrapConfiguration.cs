using UnityEngine;

public class FanTrapConfiguration : MonoBehaviour
{
    [SerializeField, Min(0f)] public float pushSpeed = 100f;
    [SerializeField, Min(0.1f)] public float blowingDuration = 7f;
    [SerializeField, Min(0.1f)] public float pauseDuration = 2f;
    [SerializeField, Range(0f, 90f)] public float sweepAngle = 45f;
}
 