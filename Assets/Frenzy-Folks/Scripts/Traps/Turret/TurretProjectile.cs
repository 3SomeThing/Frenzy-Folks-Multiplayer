using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretProjectile : NetworkBehaviour
    {
        public float lifetime = 2f;

        public NetworkVariable<float> pushForce = new NetworkVariable<float>(50f);

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime > 0f) return;
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                other.gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * pushForce.Value, ForceMode.Impulse);    
                Destroy(gameObject);
            }
        }
    }
}