using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovablePlatform : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float pauseDuration = 1f;
    [SerializeField] private bool smoothMovement = true;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

    public float Speed => speed;

    /// <summary>World-space velocity of the platform, valid on host AND clients.</summary>
    public Vector3 Velocity { get; private set; }

    private readonly List<Vector3> pathPoints = new();
    private int _currentPathIndex;
    private float _pauseTimer;

    private Rigidbody _body;
    private Vector3 _lastObservedPos;
    private bool _hasObserved;
    private Vector3 _smoothVelocity;

    // Server (or offline) drives the movement, clients only observe it
    private bool HasAuthority => !IsSpawned || IsServer;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true; // on every instance; NetworkTransform moves it on clients
    }

    public void SetPathPoints(List<Vector3> points)
    {
        pathPoints.Clear();
        pathPoints.AddRange(points);
        _currentPathIndex = 0;
        _pauseTimer = 0f;
        _smoothVelocity = Vector3.zero;
    }

    public List<Vector3> GetPathPoints()
    {
        return pathPoints;
    }

    private void FixedUpdate()
    {
        if (HasAuthority) ServerStep();
        else ObserveStep();
    }

    private void ServerStep()
    {
        if (pathPoints.Count == 0)
        {
            Velocity = Vector3.zero;
            return;
        }

        if (_pauseTimer > 0f)
        {
            _pauseTimer -= Time.fixedDeltaTime;
            Velocity = Vector3.zero;
            _smoothVelocity = Vector3.zero;
            return;
        }

        Vector3 current = _body.position;
        Vector3 target = pathPoints[_currentPathIndex];
        Vector3 next = smoothMovement
            ? Vector3.SmoothDamp(
                current,
                target,
                ref _smoothVelocity,
                smoothTime,
                speed,
                Time.fixedDeltaTime)
            : Vector3.MoveTowards(current, target, speed * Time.fixedDeltaTime);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            next = target;
            _smoothVelocity = Vector3.zero;
        }

        Velocity = (next - current) / Time.fixedDeltaTime;
        _body.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            _currentPathIndex = (_currentPathIndex + 1) % pathPoints.Count;
            _pauseTimer = pauseDuration;
        }
    }

    private void ObserveStep()
    {
        // Measure what the player actually stands on (the NetworkTransform-driven copy)
        Vector3 pos = transform.position;

        if (_hasObserved)
        {
            Vector3 raw = (pos - _lastObservedPos) / Time.fixedDeltaTime;
            float alpha = 1f - Mathf.Exp(-25f * Time.fixedDeltaTime); // light smoothing
            Velocity = Vector3.Lerp(Velocity, raw, alpha);
        }

        _lastObservedPos = pos;
        _hasObserved = true;
    }
}