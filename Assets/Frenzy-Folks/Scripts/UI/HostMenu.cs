using Frenzy_Folks.Scripts.Network;
using Frenzy_Folks.Scripts.Startup;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HostMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameField;
    
    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(UserSettings.Singleton.LastPlayerName))
        {
            playerNameField.text = UserSettings.Singleton.LastPlayerName;
        }
    }

    public void HostGame()
    {
        FrenzyNetworkManager.Get().StartHost();
    }
}
