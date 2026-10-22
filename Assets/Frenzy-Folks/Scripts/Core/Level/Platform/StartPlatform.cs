using System;
using System.Collections;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkObject))]
public class StartPlatform : Checkpoint
{
    // Safety net: if the level never reports ready, don't leave the players locked forever.
    private const float LevelReadyTimeoutSeconds = 30f;

    [SerializeField] private BoxCollider startZone;
    [SerializeField, Min(1f)] private float countdownDuration = 5f;
    [SerializeField] private string countdownCanvasName = "PlayerCanvas";
    [SerializeField] private string countdownTextName = "StartTimerText";

    private readonly List<NetworkPlayer> players = new();
    private readonly HashSet<NetworkPlayer> placedPlayers = new();
    private FrenzyLevel level;
    private bool countdownStarted;
    private bool gameStarted;

    private IEnumerator Start()
    {
        while (!IsSpawned || !IsServer)
        {
            yield return null;
        }

        if (startZone == null)
        {
            startZone = GetComponent<BoxCollider>();
        }

        if (startZone == null)
        {
            startZone = GameObject.Find("StartZone")?.GetComponent<BoxCollider>();
        }

        level = GetComponentInParent<FrenzyLevel>();
        if (level == null)
        {
            level = FindObjectOfType<FrenzyLevel>();
        }

        // The platform spawns while the level is still being built and is scaled afterwards,
        // so wait until the level is final before moving anybody onto it.
        yield return WaitForLevelReady();

        TeleportPlayersToStart();

        while (!gameStarted)
        {
            RefreshPlayers();
            TeleportNewPlayersToStart(); // players who joined after the level was loaded
            LockPlayers(true);

            if (!countdownStarted && players.Count > 0 && AllPlayersAreInStartZone())
            {
                StartCoroutine(StartCountdown());
            }

            yield return null;
        }
    }

    private IEnumerator WaitForLevelReady()
    {
        if (level == null)
        {
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + LevelReadyTimeoutSeconds;
        while (!level.IsLevelReady)
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                Debug.LogWarning(
                    "[StartScript] Level was not ready in time, teleporting players anyway.",
                    this);
                yield break;
            }

            // Keep players still while the level is being built.
            RefreshPlayers();
            LockPlayers(true);
            yield return null;
        }
    }

    /// <summary>
    /// Teleports every connected player onto the start zone. Server only.
    /// Called automatically once the level is ready; can also be called manually.
    /// </summary>
    public void TeleportPlayersToStart()
    {
        if (!IsServer)
        {
            return;
        }

        if (!startZone)
        {
            Debug.LogError("[StartScript] A StartZone BoxCollider is required.", this);
            return;
        }

        RefreshPlayers();
        placedPlayers.Clear();

        // Make sure the physics world knows about the final (scaled) transforms.
        Physics.SyncTransforms();

        foreach (NetworkPlayer player in players)
        {
            TeleportPlayer(player);
        }

        if (players.Count > 0)
        {
            players[0].SetPlayerLayerCollisionIgnored(true);
        }
    }

    private void TeleportNewPlayersToStart()
    {
        if (!startZone)
        {
            return;
        }

        placedPlayers.RemoveWhere(player => !player);

        foreach (NetworkPlayer player in players)
        {
            if (placedPlayers.Contains(player))
            {
                continue;
            }

            if (!player.IsInside(startZone))
            {
                TeleportPlayer(player);
            }
            else
            {
                placedPlayers.Add(player);
            }
        }
    }

    private void TeleportPlayer(NetworkPlayer player)
    {
        Vector3 spawnPosition = startZone.transform.TransformPoint(startZone.center);
        spawnPosition.y += 0.35f;

        player.RespawnAt(spawnPosition, startZone.transform.rotation);
        placedPlayers.Add(player);
    }

    private void RefreshPlayers()
    {
        players.Clear();
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject &&
                client.PlayerObject.TryGetComponent(out NetworkPlayer player))
            {
                players.Add(player);
            }
        }
    }

    private bool AllPlayersAreInStartZone()
    {
        if (!startZone)
        {
            Debug.LogError("[StartScript] A StartZone BoxCollider is required.", this);
            return false;
        }

        foreach (NetworkPlayer player in players)
        {
            if (!player.IsInside(startZone))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator StartCountdown()
    {
        countdownStarted = true;
        GameState.Instance.LockJoining = true;

        float remaining = countdownDuration;
        NetworkAssetsManager.Instance.SetActiveForEachPlayer(countdownTextName, true);

        while (remaining > 0f)
        {
            RefreshPlayers();
            if (players.Count == 0 || !AllPlayersAreInStartZone())
            {
                countdownStarted = false;
                SendCountdownToClientsRpc(string.Empty);
                yield break;
            }

            int seconds = Mathf.CeilToInt(remaining);
            SendCountdownToClientsRpc(seconds.ToString());
            Debug.Log($"[StartScript] Game starts in {seconds}.", this);
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        gameStarted = true;
        LockPlayers(false);
        SendCountdownToClientsRpc(string.Empty);
        NetworkAssetsManager.Instance.SetActiveForEachPlayer(countdownTextName, false);
        Debug.Log("[StartScript] Game started.", this);
    }

    private void LockPlayers(bool locked)
    {
        foreach (NetworkPlayer player in players)
        {
            player.SetMovementLocked(locked);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendCountdownToClientsRpc(string text)
    {
        UpdateCountdownRpc(text);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void UpdateCountdownRpc(string text)
    {
        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null)
        {
            Debug.LogWarning("[StartScript] Local player not found while updating countdown.", this);
            return;
        }

        Transform canvasTransform = FindChildByName(localPlayer.transform, countdownCanvasName);
        GameObject canvasObject = canvasTransform
            ? canvasTransform.gameObject
            : null;
        if (!canvasObject)
        {
            Debug.LogWarning(
                $"[StartScript] Canvas '{countdownCanvasName}' not found under the local player.",
                this);
            return;
        }

        TMP_Text countdownText = canvasObject.GetComponentInChildren<TMP_Text>();
        if (!countdownText || countdownText.gameObject.name != countdownTextName)
        {
            Debug.LogWarning(
                $"[StartScript] TMP text '{countdownTextName}' not found under '{countdownCanvasName}'.",
                this);
            return;
        }

        countdownText.text = text;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private void OnTriggerExit(Collider other)
    {
        // Debug.Log("OnTriggerExit");
        if (!other.TryGetComponent(out NetworkPlayer player))
        {
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player == null)
        {
            return;
        }

        player.SetPlayerLayerCollisionIgnored(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (!other.TryGetComponent(out NetworkPlayer player))
        {
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player == null)
        {
            return;
        }

        player.SetPlayerLayerCollisionIgnored(true);
    }
}