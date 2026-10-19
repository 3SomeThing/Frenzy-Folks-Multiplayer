using System.Collections.Generic;
using Unity.Netcode;

namespace Frenzy_Folks.Scripts.Core
{
    public class GameState : NetworkBehaviour
    {
        public static GameState Instance;
        
        public readonly Dictionary<ulong, int> usersLastCheckpoint =
            new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, Checkpoint> playerCheckpoints =
            new Dictionary<ulong, Checkpoint>();
        
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

        public void SetCheckpoint(
            ulong playerId,
            int checkpointIndex,
            Checkpoint checkpoint)
        {
            usersLastCheckpoint[playerId] = checkpointIndex;
            playerCheckpoints[playerId] = checkpoint;
        }

        public void InitializePlayerCheckpoint(ulong playerId)
        {
            if (!usersLastCheckpoint.ContainsKey(playerId))
            {
                usersLastCheckpoint[playerId] = -1;
            }
        }

        public bool TryGetPlayerCheckpoint(ulong playerId, out Checkpoint checkpoint)
        {
            if (playerCheckpoints.TryGetValue(playerId, out checkpoint) &&
                checkpoint != null)
            {
                return true;
            }

            checkpoint = null;
            return false;
        }
    }
}