using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Frenzy_Folks.Scripts.Network;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : NetworkBehaviour
{
    // Every live checkpoint. Lookups scan this list using the *current* checkpointIndex, so they stay
    // correct even when the index is changed after Awake (which is always the case for spawned
    // checkpoints: Awake runs with the prefab's default index, the real one comes from the level config).
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
        registeredCheckpoints.Clear();
        localReachedCheckpointIndex = -1;
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (checkpointIndex < 0)
        {
            checkpointIndex = GetNextFreeIndex();
        }

        RefreshRegistration();
    }

    private void OnDestroy()
    {
        registeredCheckpoints.Remove(this);
    }

    /// <summary>
    /// Registers this checkpoint (once) and refreshes its color. Call it after changing checkpointIndex.
    /// </summary>
    public void RefreshRegistration()
    {
        registeredCheckpoints.RemoveAll(checkpoint => checkpoint == null);

        if (!registeredCheckpoints.Contains(this))
        {
            registeredCheckpoints.Add(this);
        }

        if (checkpointIndex <= localReachedCheckpointIndex)
        {
            SetColor();
        }
    }

    private static int GetNextFreeIndex()
    {
        int index = 0;
        while (TryGetByIndex(index, out _))
        {
            index++;
        }

        return index;
    }

    public static bool TryGetByIndex(int index, out Checkpoint checkpoint)
    {
        foreach (Checkpoint candidate in registeredCheckpoints)
        {
            if (candidate != null && candidate.checkpointIndex == index)
            {
                checkpoint = candidate;
                return true;
            }
        }

        checkpoint = null;
        return false;
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

            // Only the player who touched the checkpoint gets the green color and the animation.
            // Everybody else keeps seeing it as "not reached yet".
            ShowCheckpointReachedRpc(
                checkpointIndex,
                RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp));
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
            if (checkpoint == null) continue;

            if (!checkpoint.changeColor || !checkpoint.cylinder) continue;

            checkpoint.cylinder.SetPropertyBlock(null);
        }
    }

    // Runs only on the client (or host) that owns the player who reached the checkpoint.
    [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
    private void ShowCheckpointReachedRpc(int reachedIndex, RpcParams rpcParams = default)
    {
        ApplyColorsUpTo(reachedIndex);
        PlaySoundAndAnimation();
    }

    private void PlaySoundAndAnimation()
    {
        if (!playSoundAndAnimation) return;

        if (checkpointSound != null)
        {
            checkpointSound.Play();
        }

        if (checkpointParticules != null)
        {
            checkpointParticules.Play();
        }
    }

    public void SetColor()
    {
        if (!changeColor || !cylinder) return;

        block ??= new MaterialPropertyBlock();
        cylinder.GetPropertyBlock(block);
        block.SetColor(BaseColorId, reachedColor);
        cylinder.SetPropertyBlock(block);
    }
}