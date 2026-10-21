using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace Frenzy_Folks.Scripts.Core
{
    public class GameState : NetworkBehaviour
    {
        public static GameState Instance;
        
        public NetworkVariable<FixedString128Bytes> LevelFileName = new NetworkVariable<FixedString128Bytes>("",
         NetworkVariableReadPermission.Everyone,
         NetworkVariableWritePermission.Server);
        
        public readonly Dictionary<ulong, int> usersLastCheckpoint =
            new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, Checkpoint> playerCheckpoints =
            new Dictionary<ulong, Checkpoint>();
        private NetworkList<FixedString128Bytes> _playerArrivalOrder;
        private NetworkList<FixedString128Bytes> _unfinishedPlayerNames;

        public NetworkList<FixedString128Bytes> PlayerArrivalOrder => _playerArrivalOrder;
        public NetworkList<FixedString128Bytes> UnfinishedPlayerNames => _unfinishedPlayerNames;
        
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
            _playerArrivalOrder = new NetworkList<FixedString128Bytes>();
            _unfinishedPlayerNames = new NetworkList<FixedString128Bytes>();
        }

        private void OnDestroy()
        {
            _playerArrivalOrder?.Dispose();
            _unfinishedPlayerNames?.Dispose();

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

        public void ResetCheckpoints(IEnumerable<ulong> playerIds)
        {
            if (!IsServer)
            {
                return;
            }

            usersLastCheckpoint.Clear();
            playerCheckpoints.Clear();

            foreach (ulong playerId in playerIds)
            {
                usersLastCheckpoint[playerId] = -1;
            }
        }

        public void ClearPlayerArrivalOrder()
        {
            if (!IsServer)
            {
                return;
            }

            _playerArrivalOrder.Clear();
        }

        public void SetPlayerArrivalOrder(IReadOnlyList<string> playerNames)
        {
            if (!IsServer)
            {
                return;
            }

            ClearPlayerArrivalOrder();
            foreach (string playerName in playerNames)
            {
                _playerArrivalOrder.Add(new FixedString128Bytes(playerName));
            }
        }

        public void SetUnfinishedPlayerNames(IReadOnlyList<string> playerNames)
        {
            if (!IsServer)
            {
                return;
            }

            _unfinishedPlayerNames.Clear();
            foreach (string playerName in playerNames)
            {
                _unfinishedPlayerNames.Add(new FixedString128Bytes(playerName));
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