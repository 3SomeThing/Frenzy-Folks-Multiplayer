using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public abstract class TurretState : State
    {
        protected new TurretFSM Fsm;
        
        public override void Init(FSM fsm)
        {
            base.Init(fsm);
            Fsm = fsm as TurretFSM;
        }

        public virtual void OnTargetEntered(Collider other) { }
        public virtual void OnTargetLeft(Collider other) { }
        public virtual void OnTargetStay(Collider other) { }
    }
}