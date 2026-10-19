

using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerEventHandler : MonoBehaviour
{
    private NetworkManager _networkManager;

    private int _currentPlayerCount;
    private List<ulong> _approvedClients = new();
    private readonly HashSet<ulong> _spawnedPlayers = new();
    private bool _lobbyLoaded;

    /**
     * This class will be only instantiate by the server (the host).
     * So we are necessarily on the server
     */
    private void Awake()
    {
        DontDestroyOnLoad(this);
        _networkManager = NetworkManager.Singleton;
        _networkManager.ConnectionApprovalCallback += CheckApprovalCallback;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientDisconnect(ulong obj)
    {
        _spawnedPlayers.Remove(obj);
        _approvedClients.Remove(obj);
        _currentPlayerCount = Mathf.Max(0, _currentPlayerCount - 1);
    }

    public void OnServerStarted()
    {
        _networkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadedComplete;
        _networkManager.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    private void CheckApprovalCallback(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        _currentPlayerCount = _networkManager.ConnectedClientsIds.Count;

        bool isThereTooManyPlayer = _currentPlayerCount >= 3;

        if (isThereTooManyPlayer || GameState.Instance.LockJoining)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "The lobby is full.";

            if (GameState.Instance.LockJoining)
            {
                response.Reason = "The game has already started.";
            }
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;

        _currentPlayerCount++;
        _approvedClients.Add(request.ClientNetworkId);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (_lobbyLoaded)
        {
            SpawnPlayer(clientId);
        }
    }

    private void OnSceneLoadedComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        Debug.Log($"Server: {sceneName} loaded");

        if (sceneName != "Lobby")
        {
            return;
        }

        _lobbyLoaded = true;
        foreach (ulong clientId in _networkManager.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!_networkManager.IsServer ||
            _spawnedPlayers.Contains(clientId) ||
            !_networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
            client.PlayerObject != null)
        {
            return;
        }

        BoxCollider spawnZone = GameObject.Find("SpawnZone")?.GetComponent<BoxCollider>();
        GameObject playerPrefab = _networkManager.NetworkConfig.PlayerPrefab;
        if (spawnZone == null || playerPrefab == null)
        {
            Debug.LogError("Cannot spawn player: SpawnZone or PlayerPrefab is missing.");
            return;
        }

        Vector3 localPosition = spawnZone.center + new Vector3(
            Random.Range(-spawnZone.size.x * 0.5f, spawnZone.size.x * 0.5f),
            spawnZone.size.y * 0.5f,
            Random.Range(-spawnZone.size.z * 0.5f, spawnZone.size.z * 0.5f));
        Vector3 spawnPosition = spawnZone.transform.TransformPoint(localPosition);
        Quaternion spawnRotation = spawnZone.transform.rotation;
        NetworkObject player = Instantiate(playerPrefab, spawnPosition, spawnRotation)
            .GetComponent<NetworkObject>();
        if (player == null)
        {
            Debug.LogError("Cannot spawn player: PlayerPrefab has no NetworkObject component.");
            return;
        }

        player.SpawnAsPlayerObject(clientId);
        _spawnedPlayers.Add(clientId);
    }

    public void OnServerStopped(bool isHost)
    {
        _networkManager.ConnectionApprovalCallback -= CheckApprovalCallback;
        _networkManager.OnClientConnectedCallback -= OnClientConnected;
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        _networkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadedComplete;
        Destroy(this);
    }
}