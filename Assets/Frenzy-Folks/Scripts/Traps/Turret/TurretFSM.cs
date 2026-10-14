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
    }
}