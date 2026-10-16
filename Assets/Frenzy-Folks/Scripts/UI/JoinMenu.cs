using System.Text.RegularExpressions;
using Frenzy_Folks.Scripts.Startup;
using TMPro;
using UnityEngine;

public class JoinMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private TMP_InputField ipField;
    [SerializeField] private TMP_InputField portField;

    private void Awake()
    {
        portField.characterValidation = TMP_InputField.CharacterValidation.Integer;
    }

    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(UserSettings.Singleton.LastPlayerName))
            playerNameField.text = UserSettings.Singleton.LastPlayerName;

        if (!string.IsNullOrWhiteSpace(UserSettings.Singleton.LastIP))
            ipField.text = UserSettings.Singleton.LastIP;

        if (UserSettings.Singleton.LastPort != 0)
            portField.text = UserSettings.Singleton.LastPort.ToString();
    }

    public void JoinGame()
    {
        if (!ValidateInputs(true)) return;

        FrenzyNetworkManager.Get().ConnectToServer(UserSettings.Singleton.LastIP, UserSettings.Singleton.LastPort);
    }

    private bool ValidateInputs(bool saveInputs)
    {
        string playerName = playerNameField.text;
        string ip = ipField.text;
        string portText = portField.text;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogError("Name can not be empty");
            return false;
        }

        bool isValidIPv4 = Regex.IsMatch(ip, NetworkUtils.IPv4Pattern);
        bool isValidIPv6 = Regex.IsMatch(ip, NetworkUtils.IPv6Pattern);
        bool isValidHostname = Regex.IsMatch(ip, NetworkUtils.HostnamePattern);
        if (!isValidIPv4 && !isValidIPv6 && !isValidHostname)
        {
            Debug.LogError("Invalid IP/Hostname");
            return false;
        }

        if (!ushort.TryParse(portText, out ushort port))
        {
            Debug.LogError("Invalid Port");
            return false;
        }

        if (saveInputs)
        {
            UserSettings.Singleton.LastPlayerName = playerName;
            UserSettings.Singleton.LastIP = ip;
            UserSettings.Singleton.LastPort = port;
            UserSettings.Singleton.Save();
        }

        return true;
    }
}