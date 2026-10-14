using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public abstract class TurretState : State
    {
        public virtual void OnTargetEntered(Collider other) { }
        public virtual void OnTargetLeft(Collider other) { }
    }
}