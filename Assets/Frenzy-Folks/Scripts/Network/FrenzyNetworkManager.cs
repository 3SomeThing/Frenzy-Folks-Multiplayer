using System;
using Frenzy_Folks.Scripts.Core;
using Frenzy_Folks.Scripts.Startup;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class FrenzyNetworkManager : MonoBehaviour
{
    [SerializeField] private GameState gameState;
    [SerializeField] private NetworkAssetsManager networkAssetsManager;
    [SerializeField] private ServerEventHandler serverEventHandlerPrefab;

    private ServerEventHandler _serverEventHandler;

    public void StartHost()
    {
        // We assume that we are the server, I don't see a way as it is not possible
        _serverEventHandler = Instantiate(serverEventHandlerPrefab);
        NetworkManager.Singleton.OnServerStarted += _serverEventHandler.OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += _serverEventHandler.OnServerStopped;
        
        SetConnectionData("0.0.0.0", 6767);

        bool success = NetworkManager.Singleton.StartHost();

        if (success) return;

        MainMenuLoader.LoadMainMenu();
    }

    public void ConnectToServer(string ip, ushort port)
    {
        SetConnectionData(ip, port);

        NetworkManager.Singleton.StartClient();
    }

    private void SetConnectionData(string ip, ushort port)
    {
        UnityTransport utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        utp.SetConnectionData(ip, port);
    }

    public static FrenzyNetworkManager Get()
    {
        return NetworkManager.Singleton.GetComponent<FrenzyNetworkManager>();
    }
}