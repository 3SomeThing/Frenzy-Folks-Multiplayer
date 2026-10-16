using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretTargetingState : TurretState
    {
        public override void Update()
        {
            base.Update();
            if (!Fsm.targetTransform || !Fsm) return;
            Vector3 direction = Fsm.targetTransform.position - Fsm.turretHead.transform.position;
                
            
            if (direction.sqrMagnitude < 0.001f) // If the difference between the direction the turret has to face and the current direction is too small, return early
                return;
                
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Vector3 euler = lookRotation.eulerAngles;
            euler.x = -Mathf.DeltaAngle(0f, euler.x); // Invert the x-axis rotation to make the turret look downwards at the player instead of upwards
            lookRotation = Quaternion.Euler(euler);
            
            Fsm.turretHead.transform.rotation = Quaternion.Slerp(Fsm.turretHead.transform.rotation, lookRotation, Time.deltaTime * Fsm.TargetingSpeed);
            
            bool isLockedOn = Vector3.Angle(Fsm.turretHead.transform.forward, direction) <= Fsm.LockTolerance;
            if (isLockedOn)
            {
                /*Fsm.TransitionTo("Attack");*/
            }
        }

        public override void OnTargetLeft(Collider other)
        {
            base.OnTargetLeft(other);
            Fsm.TransitionTo("Idle");
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}