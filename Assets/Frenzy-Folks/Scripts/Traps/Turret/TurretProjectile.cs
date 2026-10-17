using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretProjectile : NetworkBehaviour
    {
        [SerializeField, Min(0f)] private float lifetime = 2f;
        [SerializeField, Min(0f)] private float pushForce = 50f;

        private float _remainingLifetime;

        public void Initialize(float force)
        {
            pushForce = force;
            _remainingLifetime = lifetime;
        }

        private void Update()
        {
            if (IsSpawned && !IsServer)
                return;

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime > 0f)
                return;

            Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsSpawned && !IsServer)
                return;

            if (!other.CompareTag("Player"))
                return;

            NetworkPlayer networkPlayer = other.GetComponentInParent<NetworkPlayer>();
            if (IsSpawned)
            {
                if (networkPlayer == null)
                {
                    Debug.LogError($"[TurretProjectile] Player '{other.name}' has no NetworkPlayer component.", other);
                }
                else
                {
                    networkPlayer.ApplyKnockbackRpc(transform.forward * pushForce);
                }
            }
            else
            {
                Rigidbody playerRigidbody = other.attachedRigidbody;
                SUPERCharacterAIO playerController = other.GetComponent<SUPERCharacterAIO>();
                Debug.Log("Not spawned");
                if (playerRigidbody != null && !playerRigidbody.isKinematic && !playerController)
                    playerRigidbody.AddForce(transform.forward * pushForce, ForceMode.Impulse);
            }

            Despawn();
        }

        private void Despawn()
        {
            if (IsSpawned)
                NetworkObject.Despawn(true);
            else
                Destroy(gameObject);
        }
    }
}