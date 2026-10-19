using System.Collections.Generic;
using Unity.Netcode;

namespace Frenzy_Folks.Scripts.Core
{
    public class GameState : NetworkBehaviour
    {
        public static GameState Instance;
        
        public readonly Dictionary<ulong, int> usersLastCheckpoint =
            new Dictionary<ulong, int>();
        
        private NetworkVariable<bool> _lockJoining = new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        public bool LockJoining
        {
            get
            {
                return _lockJoining.Value;
            }
            set
            {
                _lockJoining.Value = value;
            }
        }

        public void Awake()
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
        
        public override void OnNetworkSpawn()
        {
            // Do something
        }

        public void SetCheckpoint(ulong playerId, int checkpointIndex)
        {
            usersLastCheckpoint[playerId] = checkpointIndex;
        }

        public bool TryGetPlayerCheckpoint(ulong playerId, out Checkpoint checkpoint)
        {
            checkpoint = null;

            if (!usersLastCheckpoint.TryGetValue(playerId, out int checkpointIndex))
            {
                return false;
            }

            return Checkpoint.TryGetByIndex(checkpointIndex, out checkpoint);
        }
    }
}