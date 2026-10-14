using System;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretFSM : FSM
    {
        
        protected override void Start()
        {
            States.Add("Idle", new TurretIdleState());
			States.Add("Targeting", new TurretTargetingState());
            States.Add("Attack", new TurretAttackState());
            CurrentState = "Idle";
            base.Start();
        }
        
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") &&
                States.TryGetValue(CurrentState, out var state) &&
                state is TurretState turretState)
            {
                Debug.Log("Attacking player!");
                turretState.OnTargetEntered(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") &&
                States.TryGetValue(CurrentState, out var state) &&
                state is TurretState turretState)
            {
                Debug.Log("Player left!");
                turretState.OnTargetLeft(other);
            }
        }
    }
}