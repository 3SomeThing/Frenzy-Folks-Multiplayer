using Frenzy_Folks.Scripts.Core;
using Unity.Netcode;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Network
{
    public class FrenzyNetworkManager : NetworkManager
    {
        [SerializeField] private GameState gameState;
        [SerializeField] private ServerEventHandler serverEventHandler;
        
        public FrenzyNetworkManager()
        {
            OnInstantiated += OnNetworkManageAvailable;
        }

        private void OnNetworkManageAvailable(NetworkManager manager)
        {
            if (manager.IsServer)
            {
                manager.OnServerStarted += serverEventHandler.OnServerStarted;
                manager.OnServerStopped += serverEventHandler.OnServerStopped;
            }
        }

        public new bool StartHost()
        {
            return base.StartHost();
        }
    }
}