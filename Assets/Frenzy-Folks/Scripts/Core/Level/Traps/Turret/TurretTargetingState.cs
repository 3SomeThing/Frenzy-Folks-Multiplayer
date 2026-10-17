using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretTargetingState : TurretState
    {
        public override void Update()
        {
            base.Update();
            if (!Fsm || !Fsm.targetTransform || !Fsm.turretHead) return;

            Vector3 direction = Fsm.targetTransform.position - Fsm.turretHead.position;
            
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Fsm.turretHead.rotation = Quaternion.Slerp(
                Fsm.turretHead.rotation,
                lookRotation,
                Time.deltaTime * Fsm.TargetingSpeed);

            bool isLockedOn = Vector3.Angle(Fsm.turretHead.forward, direction) <= Fsm.LockTolerance;
            if (isLockedOn)
            {
                Fsm.TransitionTo("Attack");
            }
        }

        public override void OnTargetLeft(Collider other)
        {
            base.OnTargetLeft(other);
            Fsm.TransitionTo("Idle");
        }
    }
}