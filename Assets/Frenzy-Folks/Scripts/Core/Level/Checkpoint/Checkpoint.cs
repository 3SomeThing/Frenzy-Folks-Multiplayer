using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Frenzy_Folks.Scripts.Network;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : NetworkBehaviour
{
    private static int nextCheckpointIndex;
    private static readonly Dictionary<int, Checkpoint> checkpoints =
        new Dictionary<int, Checkpoint>();
    private static readonly List<Checkpoint> registeredCheckpoints =
        new List<Checkpoint>();
    private static int localReachedCheckpointIndex = -1;

    [SerializeField] public int checkpointIndex;

    [SerializeField] private AudioSource checkpointSound;
    [SerializeField] private ParticleSystem checkpointParticules;
    [SerializeField] private SkinnedMeshRenderer cylinder;
    
    [SerializeField] public bool playSoundAndAnimation = true;
    [SerializeField] public bool changeColor = true;
    
    private MaterialPropertyBlock block;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    
    private static readonly Color reachedColor = new(0.0f, 0.8f, 0.0f, 0.5f);
        
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIndex()
    {
        nextCheckpointIndex = 0;
        checkpoints.Clear();
        registeredCheckpoints.Clear();
        localReachedCheckpointIndex = -1;
    }

    private void Awake()
    {        
        block = new MaterialPropertyBlock();
        
        if (checkpointIndex < 0)
        {
            while (checkpoints.ContainsKey(nextCheckpointIndex))
            {
                nextCheckpointIndex++;
            }

            checkpointIndex = nextCheckpointIndex;
        }

        RefreshRegistration();
    }

    private void OnDestroy()
    {
        RemoveRegistrations();
    }

    public void RefreshRegistration()
    {
        RemoveRegistrations();

        nextCheckpointIndex = Mathf.Max(nextCheckpointIndex, checkpointIndex + 1);
        checkpoints[checkpointIndex] = this;
        registeredCheckpoints.Add(this);

        if (checkpointIndex <= localReachedCheckpointIndex)
        {
            SetColor();
        }
    }

    private void RemoveRegistrations()
    {
        List<int> registrationsToRemove = new List<int>();
        foreach (KeyValuePair<int, Checkpoint> registration in checkpoints)
        {
            if (registration.Value == this)
            {
                registrationsToRemove.Add(registration.Key);
            }
        }

        foreach (int registration in registrationsToRemove)
        {
            checkpoints.Remove(registration);
        }

        registeredCheckpoints.Remove(this);
    }

    public static bool TryGetByIndex(int index, out Checkpoint checkpoint)
    {
        return checkpoints.TryGetValue(index, out checkpoint);
    }

    public void OnTriggerEnter(Collider other)
    {
        RegisterPlayer(other);
    }

    private void RegisterPlayer(Collider other)
    {
        if (!other.TryGetComponent(out NetworkPlayer player))
        {
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player == null ||
            GameState.Instance == null ||
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        GameState.Instance.InitializePlayerCheckpoint(player.OwnerClientId);
        int userLastCheckpointIndex =
            GameState.Instance.usersLastCheckpoint[player.OwnerClientId];

        if (checkpointIndex > userLastCheckpointIndex)
        {
            GameState.Instance.SetCheckpoint(
                player.OwnerClientId,
                checkpointIndex,
                this);
            PlaySoundAndAnimation();
            player.ApplyCheckpointColorRpc(checkpointIndex);
        }
    }

    public static void ApplyColorsUpTo(int reachedCheckpointIndex)
    {
        localReachedCheckpointIndex = Mathf.Max(
            localReachedCheckpointIndex,
            reachedCheckpointIndex);

        foreach (Checkpoint checkpoint in registeredCheckpoints)
        {
            if (checkpoint != null &&
                checkpoint.checkpointIndex <= localReachedCheckpointIndex)
            {
                checkpoint.SetColor();
            }
        }
    }

    public static void ResetColors()
    {
        localReachedCheckpointIndex = -1;

        foreach (Checkpoint checkpoint in registeredCheckpoints)
        {
            if (checkpoint != null && checkpoint.cylinder != null && checkpoint.changeColor)
            {
                checkpoint.cylinder.SetPropertyBlock(null);
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
