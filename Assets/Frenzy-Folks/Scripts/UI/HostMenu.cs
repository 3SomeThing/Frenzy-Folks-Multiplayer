using Frenzy_Folks.Scripts.Startup;
using TMPro;
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
}
