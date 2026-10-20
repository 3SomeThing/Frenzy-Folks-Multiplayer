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
        string playerName = playerNameField.text.Trim();
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogError("Name can not be empty");
            return;
        }

        UserSettings.Singleton.LastPlayerName = playerName;
        UserSettings.Singleton.Save();
        FrenzyNetworkManager.Get().StartHost(playerName);
    }
}
