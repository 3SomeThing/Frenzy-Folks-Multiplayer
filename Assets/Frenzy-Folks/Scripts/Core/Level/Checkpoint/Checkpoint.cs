using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Frenzy_Folks.Scripts.Network;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private static int nextCheckpointIndex;
    private static readonly Dictionary<int, Checkpoint> checkpoints =
        new Dictionary<int, Checkpoint>();

    [SerializeField] public int checkpointIndex;

    [SerializeField] private AudioSource checkpointSound;
    [SerializeField] private ParticleSystem checkpointParticules;
    [SerializeField] private SkinnedMeshRenderer cylinder;
    
    [SerializeField] private bool playSoundAndAnimation = true;
    [SerializeField] private bool changeColor = true;
    
    private MaterialPropertyBlock block;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    
    private static readonly Color reachedColor = new(0.0f, 0.8f, 0.0f, 0.5f);
        
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIndex()
    {
        nextCheckpointIndex = 1;
        checkpoints.Clear();
    }

    private void Awake()
    {        
        block = new MaterialPropertyBlock();
        
        if (checkpointIndex <= 0)
        {
            while (checkpoints.ContainsKey(nextCheckpointIndex))
            {
                nextCheckpointIndex++;
            }

            checkpointIndex = nextCheckpointIndex;
        }

        nextCheckpointIndex = Mathf.Max(nextCheckpointIndex, checkpointIndex + 1);
        checkpoints[checkpointIndex] = this;
    }

    private void OnDestroy()
    {
        if (checkpoints.TryGetValue(checkpointIndex, out Checkpoint checkpoint) &&
            checkpoint == this)
        {
            checkpoints.Remove(checkpointIndex);
        }
    }

    public static bool TryGetByIndex(int index, out Checkpoint checkpoint)
    {
        return checkpoints.TryGetValue(index, out checkpoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterPlayer(other);
    }

    private void RegisterPlayer(Collider other)
    {
        if (!other.TryGetComponent(out NetworkPlayer player))
        {
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player == null || GameState.Instance == null)
        {
            return;
        }

        bool hasCheckpoint = GameState.Instance.usersLastCheckpoint.TryGetValue(
            player.OwnerClientId,
            out int userLastCheckpointIndex);

        if (!hasCheckpoint || checkpointIndex > userLastCheckpointIndex)
        {
            bool isServer = NetworkManager.Singleton != null &&
                            NetworkManager.Singleton.IsServer;

            if (isServer)
            {
                GameState.Instance.SetCheckpoint(player.OwnerClientId, checkpointIndex);
                PlaySoundAndAnimation();
            }

            if (player.IsOwner)
            {
                foreach (Checkpoint checkpoint in checkpoints.Values)
                {
                    if (checkpoint.checkpointIndex <= checkpointIndex)
                    {
                        checkpoint.SetColor();
                    }
                }
            }
        }
    }

    private void PlaySoundAndAnimation()
    {
        if (!playSoundAndAnimation) return;
        
        NetworkAssetsManager.Instance.PlayForEveryone(checkpointSound);
        NetworkAssetsManager.Instance.PlayForEveryone(checkpointParticules);
    }
    
    public void SetColor()
    {
        if (!changeColor) return;
        
        cylinder.GetPropertyBlock(block);
        block.SetColor(BaseColorId, reachedColor);
        cylinder.SetPropertyBlock(block);
    }
}
