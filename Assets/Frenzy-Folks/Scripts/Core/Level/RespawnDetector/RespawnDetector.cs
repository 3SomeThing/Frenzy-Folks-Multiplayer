using System.Collections;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Frenzy_Folks.Scripts.Network;
using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;

public class RespawnDetector : MonoBehaviour
{
    public static RespawnDetector Instance { get; private set; }
    
    private readonly HashSet<NetworkPlayer> respawningPlayers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") ||
            GameState.Instance == null ||
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (!other.TryGetComponent(out NetworkPlayer player))
        {
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player == null ||
            !GameState.Instance.TryGetPlayerCheckpoint(player.OwnerClientId, out Checkpoint checkpoint) ||
            respawningPlayers.Contains(player))
        {
            return;
        }

        StartCoroutine(RespawnAfterDelay(player, checkpoint));
    }

    private IEnumerator RespawnAfterDelay(NetworkPlayer player, Checkpoint checkpoint)
    {
        respawningPlayers.Add(player);

        SUPERCharacterAIO characterController = player.GetComponent<SUPERCharacterAIO>();
        Rigidbody characterRigidbody = player.GetComponent<Rigidbody>();
        if (characterController && characterRigidbody)
        {
            GameObject character = player.gameObject.transform.Find("Character").gameObject;
            NetworkAssetsManager.Instance.SetActiveForEveryone(character, false);
            NetworkAssetsManager.Instance.PlayForEveryone(player.gameObject.transform.Find("Explosion").gameObject.GetComponent<ParticleSystem>());
            NetworkAssetsManager.Instance.PlayForEveryone(player.gameObject.transform.Find("Death").gameObject.GetComponent<AudioSource>());
            player.SetRespawning(true);
        }

        yield return new WaitForSeconds(2f);

        if (player && checkpoint)
        {
            player.RespawnAt(checkpoint.transform.position, checkpoint.transform.rotation);
        }
        
        if (characterController && characterRigidbody)
        {
            GameObject character = player.gameObject.transform.Find("Character").gameObject;
            NetworkAssetsManager.Instance.SetActiveForEveryone(character, true);
            player.SetRespawning(false);
        }

        respawningPlayers.Remove(player);
    }
}
