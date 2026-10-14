using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretTargetingState : State
    {
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Attacking player!");
            if (other.CompareTag("Player"))
            {
                // Attack the player
            }
        }
    }
}