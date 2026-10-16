using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretAttackState : TurretState
    {
        public override void Enter()
        {
            base.Enter();

            Vector3 direction = Fsm.targetTransform.position - Fsm.FirePoint.position;
            if (direction.sqrMagnitude < 0.001f)
            {
                Debug.LogError("[TurretAttackState] Cannot fire: target is too close to the fire point.", Fsm);
                return;
            }

            GameObject projectile = Object.Instantiate(Fsm.ProjectilePrefab, Fsm.FirePoint.position, Fsm.turretHead.localRotation );
            Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
            if (projectileRigidbody == null)
            {
                Debug.LogError($"[TurretAttackState] Projectile '{projectile.name}' has no Rigidbody.", projectile);
                Object.Destroy(projectile);
                return;
            }

            projectileRigidbody.AddForce(direction.normalized * Fsm.ProjectileForce, ForceMode.Impulse);
            
            Fsm.TransitionTo("Targeting");
        }
    }
}