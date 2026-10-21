using System.Collections;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Frenzy_Folks.Scripts.Network;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class EndPlatform : NetworkBehaviour
{
    [SerializeField] private BoxCollider endZone;
    [SerializeField, Min(1f)] private float endTimerDuration = 20f;
    [SerializeField] private string countdownCanvasName = "PlayerCanvas";
    [SerializeField] private string countdownTextName = "EndTimerText";

    private List<string> playerNameArrivalsOrder = new List<string>();
    private readonly HashSet<ulong> _arrivedPlayers = new();
    
    private bool endCountdownStarted;

    public override void OnNetworkSpawn()
    {
        if (endZone == null)
        {
            endZone = GetComponent<BoxCollider>();
        }

        if (endZone == null)
        {
            endZone = GameObject.Find("StartZone")?.GetComponent<BoxCollider>();
        }
    }
    
    private IEnumerator StartEndCountdown()
    {
        endCountdownStarted = true;
        GameState.Instance.LockJoining = true;
        
        float remaining = endTimerDuration;
        NetworkAssetsManager.Instance.SetActiveForEachPlayer(countdownTextName, true);
        
        while (remaining > 0f && !AreAllPlayersInEndZone())
        {
            int seconds = Mathf.CeilToInt(remaining);
            SendCountdownToClientsRpc(seconds.ToString());
            Debug.Log($"[EndScript] Game ends in {seconds}.", this);
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
        
        SendCountdownToClientsRpc(string.Empty);
        NetworkAssetsManager.Instance.SetActiveForEachPlayer(countdownTextName, false);
        Debug.Log("[EndScript] Game ended.", this);

        GameState.Instance.SetPlayerArrivalOrder(playerNameArrivalsOrder);
        GameState.Instance.SetUnfinishedPlayerNames(GetUnfinishedPlayerNames());
        
        FrenzyNetworkManager.Get().DespawnAllPlayers();
        
        SceneEventProgressStatus status =
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Could not load Lobby scene: {status}.", this);
        }
    }

    private List<string> GetUnfinishedPlayerNames()
    {
        List<string> unfinishedPlayerNames = new List<string>();

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (_arrivedPlayers.Contains(client.ClientId) ||
                client.PlayerObject == null ||
                !client.PlayerObject.TryGetComponent(out PlayerData playerData))
            {
                continue;
            }

            unfinishedPlayerNames.Add(playerData.PlayerName);
        }

        return unfinishedPlayerNames;
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
    
    private void OnTriggerEnter(Collider other)
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

        if (!_arrivedPlayers.Add(player.OwnerClientId))
        {
            return;
        }
        
        playerNameArrivalsOrder.Add(player.GetComponent<PlayerData>().PlayerName);

        player.SetSpectating(GetNextSpectatorTarget(player.OwnerClientId));
        UpdateSpectatorTargets();

        if (!endCountdownStarted)
        {
            StartCoroutine(StartEndCountdown());
        }
    }

    private ulong GetNextSpectatorTarget(ulong excludedClientId)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != excludedClientId && !_arrivedPlayers.Contains(clientId))
            {
                return clientId;
            }
        }

        return ulong.MaxValue;
    }

    private void UpdateSpectatorTargets()
    {
        foreach (ulong arrivedClientId in _arrivedPlayers)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    arrivedClientId,
                    out NetworkClient client) ||
                client.PlayerObject == null ||
                !client.PlayerObject.TryGetComponent(out NetworkPlayer player))
            {
                continue;
            }

            ulong targetClientId = GetNextSpectatorTarget(arrivedClientId);
            if (targetClientId != ulong.MaxValue)
            {
                player.SetSpectatorTarget(targetClientId);
            }
        }
    }

    private bool AreAllPlayersInEndZone()
    {
        return NetworkManager.Singleton.ConnectedClientsIds.Count > 0 &&
               _arrivedPlayers.Count >= NetworkManager.Singleton.ConnectedClientsIds.Count;
    }
}
