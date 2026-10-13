using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Network
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private List<GameObject> localOnlyObjects;
        
        private void Awake()
        {
            Debug.Log("Player is awake");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            foreach (GameObject localOnlyObject in localOnlyObjects) localOnlyObject.SetActive(true);
        }
    }
}