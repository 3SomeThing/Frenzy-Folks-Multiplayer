using System;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core.FSM;
using Frenzy_Folks.Scripts.Traps.Turret;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

public class TurretFSM : FSM
{
    private readonly NetworkVariable<Quaternion> _headRotation =
        new NetworkVariable<Quaternion>(Quaternion.identity);

    [SerializeField] public Transform turretHead;
    
    [Header("Idle")]
    [SerializeField, Range(0f, 360f)] private float sweepAngle = 45f;
    public float SweepAngle => sweepAngle;
    
    [SerializeField, Min(0f)] private float sweepSpeed = 1f;
    public float SweepSpeed => sweepSpeed;

    [SerializeField, Min(0f)] private float returnToIdleDuration = 0.5f;
    public float ReturnToIdleDuration => returnToIdleDuration;

    [Header("Targeting")]
    [SerializeField, Range(0f, 360f)]
    public float detectionRadius = 10f;
    public float DetectionRadius
    {
        get => detectionRadius;
        set
        {
            detectionRadius = value;
            ApplyRadius();
        }
    }
    
    [SerializeField] private SphereCollider detectionCollider;
    
    [SerializeField, Min(0f)] public int targetingSpeed = 5;
    public int TargetingSpeed => targetingSpeed;
    
    [SerializeField, Range(0f, 360f)]
    private float lockTolerance = 5f;
    public float LockTolerance => lockTolerance;

    [Header("Attack")] 
    [SerializeField]
    private GameObject projectilePrefab;
    public GameObject ProjectilePrefab => projectilePrefab;

    [SerializeField] 
    private Transform firePoint;
    public Transform FirePoint => firePoint;

    [SerializeField] 
    private float projectileSpeed = 10f;
    public float ProjectileSpeed => projectileSpeed;

    [SerializeField]
    private float projectileForce = 50f;
    public float ProjectileForce => projectileForce;

    [SerializeField, Tooltip("Number of projectile fired every second")]
    public float fireRate = 1f;
    public float FireRate => fireRate;
    
    [HideInInspector] public Transform targetTransform;
    
    protected override void Start()
    {
        States.Add("Idle", new TurretIdleState());
		States.Add("Targeting", new TurretTargetingState());
        States.Add("Attack", new TurretAttackState());
        CurrentState = "Idle";
        base.Start();
    }
    
    void Awake()
    {
        ApplyRadius();
    }

    public override void OnNetworkSpawn()
    {
        _headRotation.OnValueChanged += OnHeadRotationChanged;

        if (IsServer)
        {
            _headRotation.Value = turretHead.localRotation;
        }
        else
        {
            turretHead.localRotation = _headRotation.Value;
        }
    }

    public override void OnNetworkDespawn()
    {
        _headRotation.OnValueChanged -= OnHeadRotationChanged;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsServer || !turretHead)
            return;

        Quaternion currentRotation = turretHead.localRotation;
        if (Quaternion.Angle(_headRotation.Value, currentRotation) > 0.05f)
            _headRotation.Value = currentRotation;
    }

    private void OnHeadRotationChanged(Quaternion previousRotation, Quaternion newRotation)
    {
        if (!IsServer && turretHead)
            turretHead.localRotation = newRotation;
    }

    void ApplyRadius()
    {
        if (detectionCollider != null)
            detectionCollider.radius = detectionRadius;
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (IsSpawned && !IsServer)
            return;

        if (other.CompareTag("Player") &&
            States.TryGetValue(CurrentState, out var state) &&
            state is TurretState turretState)
        {
            turretState.OnTargetEntered(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsSpawned && !IsServer)
            return;

        if (other.CompareTag("Player") &&
            States.TryGetValue(CurrentState, out var state) &&
            state is TurretState turretState)
        {
            turretState.OnTargetStay(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsSpawned && !IsServer)
            return;

        if (other.CompareTag("Player") &&
            States.TryGetValue(CurrentState, out var state) &&
            state is TurretState turretState)
        {
            turretState.OnTargetLeft(other);
        }
    }
    
    void OnValidate()
    {
        ApplyRadius();
    }
    
    void OnDrawGizmosSelected()
    {
        
#if UNITY_EDITOR
        // Arc starts at the left edge and sweeps clockwise
        Vector3 from = Quaternion.Euler(0, -sweepAngle / 2f, 0) * transform.forward;

        Handles.color = new Color(1f, 0.3f, 0.2f, 0.2f);
        Handles.DrawSolidArc(transform.position, Vector3.up, from, sweepAngle, detectionRadius);

        Handles.color = Color.red;
        Handles.DrawWireArc(transform.position, Vector3.up, from, sweepAngle, detectionRadius);
#endif
    }
}