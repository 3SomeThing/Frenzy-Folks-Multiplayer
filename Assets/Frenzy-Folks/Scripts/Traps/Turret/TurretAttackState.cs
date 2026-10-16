using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretAttackState : TurretState
    {
        private float _fireTime;
        public override void Enter()
        {
            base.Enter();
            
            _fireTime += Time.deltaTime;

            Vector3 direction = Fsm.targetTransform.position - Fsm.FirePoint.position;
            if (direction.sqrMagnitude < 2f)
            {
                Fsm.TransitionTo("Targeting");
                return;
            }

            if (_fireTime >= Fsm.FireRate)
            {
                _fireTime = 0f;
                GameObject projectile = Object.Instantiate(Fsm.ProjectilePrefab, Fsm.FirePoint.position, Fsm.turretHead.localRotation );
                projectile.GetComponent<TurretProjectile>().pushForce = Fsm.ProjectileForce;
                Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
                if (!projectileRigidbody)
                {
                    Debug.LogError($"[TurretAttackState] Projectile '{projectile.name}' has no Rigidbody.", projectile);
                    Object.Destroy(projectile);
                    return;
                }

                projectileRigidbody.AddForce(Fsm.turretHead.forward * Fsm.ProjectileSpeed, ForceMode.Impulse);
            }
            
            Fsm.TransitionTo("Targeting");
        }
    }
}