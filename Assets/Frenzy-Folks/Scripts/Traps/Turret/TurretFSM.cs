using System;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core.FSM;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretFSM : FSM
    {
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
        private float detectionRadius = 10f;
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
        
        [SerializeField, Min(0f)]
        private int targetingSpeed = 5;
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
        private NetworkVariable<float> projectileForce = new NetworkVariable<float>(50f);
        public NetworkVariable<float> ProjectileForce => projectileForce;

        [SerializeField, Tooltip("Number of projectile fired every second")] 
        private float fireRate = 1f;
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
        
        void Awake() => ApplyRadius();

        void ApplyRadius()
        {
            if (detectionCollider != null)
                detectionCollider.radius = detectionRadius;
        }
        
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") &&
                States.TryGetValue(CurrentState, out var state) &&
                state is TurretState turretState)
            {
                turretState.OnTargetEntered(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
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
}