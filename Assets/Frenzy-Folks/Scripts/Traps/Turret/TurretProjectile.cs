using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretProjectile : MonoBehaviour
    {
        public float lifetime = 2f;

        public float pushForce = 50f;

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
                other.gameObject.GetComponent<Rigidbody>().AddForce(gameObject.transform.forward * pushForce, ForceMode.Impulse);    
                Destroy(gameObject);
            }
        }
    }
}