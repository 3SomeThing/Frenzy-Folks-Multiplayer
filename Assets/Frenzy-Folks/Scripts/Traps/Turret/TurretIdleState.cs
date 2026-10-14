using Frenzy_Folks.Scripts.Core.FSM;
using Frenzy_Folks.Scripts.Traps.Turret;
using UnityEngine;

public class TurretIdleState : TurretState
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
