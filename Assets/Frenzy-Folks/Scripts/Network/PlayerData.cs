using Unity.Collections;
using Unity.Netcode;

namespace Frenzy_Folks.Scripts.Network
{
    public class PlayerData : NetworkBehaviour
    {
        private NetworkVariable<FixedString128Bytes> _playerName =
            new NetworkVariable<FixedString128Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        public string PlayerName
        {
            get => _playerName.Value.ToString();
            set => _playerName.Value = new FixedString128Bytes(value);
        }
    }
}