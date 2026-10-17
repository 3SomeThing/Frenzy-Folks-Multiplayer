

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerEventHandler : MonoBehaviour
{
    private NetworkManager _networkManager;

    private int _currentPlayerCount;
    private List<ulong> _approvedClients = new();

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
    }

    private void OnClientDisconnect(ulong obj)
    {
        
    }

    public void OnServerStarted()
    {
        _networkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadedComplete;
        _networkManager.SceneManager.LoadScene("TestingPlayground", LoadSceneMode.Single);
    }

    private void CheckApprovalCallback(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        _currentPlayerCount = _networkManager.ConnectedClientsIds.Count;

        bool isThereTooManyPlayer = _currentPlayerCount >= 3;

        if (isThereTooManyPlayer)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "The lobby is full.";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = GetRandomPosInLobby();

        _currentPlayerCount++;
        _approvedClients.Add(request.ClientNetworkId);
    }

    private Vector3 GetRandomPosInLobby()
    {
        float x = Random.Range(-4F, 4F);
        float z = Random.Range(-7F, 7F);
        return new Vector3(-27 + x, 2, 4 + z);
    }

    private void OnSceneLoadedComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        Debug.Log($"Server: {sceneName} loaded");
    }

    public void OnServerStopped(bool isHost)
    {
        Destroy(this);
    }
}