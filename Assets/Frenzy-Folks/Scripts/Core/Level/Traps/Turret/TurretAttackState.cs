using Frenzy_Folks.Scripts.Core.FSM;
using Unity.Netcode;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretAttackState : TurretState
    {
        private float _fireTime;
        public override void Enter()
        {
            base.Enter();

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsClient &&
                !NetworkManager.Singleton.IsServer)
            {
                Fsm.TransitionTo("Targeting");
                return;
            }
            
            if (!Fsm.ProjectilePrefab || !Fsm.FirePoint || !Fsm.turretHead || !Fsm.targetTransform)
            {
                Debug.LogError("[TurretAttackState] Cannot fire: projectile prefab, fire point, turret head, or target is missing.", Fsm);
                return;
            }

            _fireTime += Time.deltaTime;

            Vector3 direction = Fsm.turretHead.forward;
            if (direction.sqrMagnitude < 0.001f)
            {
                Fsm.TransitionTo("Targeting");
                return;
            }

if (Fsm.FireRate <= 0f || _fireTime >= 1f / Fsm.FireRate)
            {
                _fireTime = 0f;
                Quaternion projectileRotation = Quaternion.LookRotation(direction);
                GameObject projectile = Object.Instantiate(
                    Fsm.ProjectilePrefab,
                    Fsm.FirePoint.position,
                    projectileRotation);
                

                TurretProjectile turretProjectile = projectile.GetComponent<TurretProjectile>();
                if (!turretProjectile)
                {
                    Debug.LogError($"[TurretAttackState] Projectile '{projectile.name}' has no TurretProjectile component.", projectile);
                    Object.Destroy(projectile);
                    return;
                }

                turretProjectile.Initialize(Fsm.ProjectileForce);

                Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
                if (!projectileRigidbody)
                {
                    Debug.LogError($"[TurretAttackState] Projectile '{projectile.name}' has no Rigidbody.", projectile);
                    Object.Destroy(projectile);
                    return;
                }

                NetworkObject projectileNetworkObject = projectile.GetComponent<NetworkObject>();
                if (Fsm.IsSpawned)
                {
                    if (!projectileNetworkObject)
                    {
                        Debug.LogError($"[TurretAttackState] Network projectile '{projectile.name}' has no NetworkObject component.", projectile);
                        Object.Destroy(projectile);
                        return;
                    }

                    projectileNetworkObject.Spawn();
                }

                projectileRigidbody.AddForce(direction.normalized * Fsm.ProjectileSpeed, ForceMode.Impulse);
            }
            
            Fsm.TransitionTo("Targeting");
        }
    }
}