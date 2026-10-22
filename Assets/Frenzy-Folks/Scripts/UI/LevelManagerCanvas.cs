using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Frenzy_Folks.Scripts.Core;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManagerCanvas : NetworkBehaviour
{
    NetworkVariable<FixedString128Bytes> _levelName = new NetworkVariable<FixedString128Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // _levelName
    private TextMeshProUGUI _levelNameTextComponent;

    private List<string> _allLevelNames = new List<string>();
    
    private int _numberOfLevels = 0;
    private int _levelIndex = 0;
    
    private void Awake()
    {
        Debug.Log("Level canvas awake");
        
        _levelNameTextComponent = gameObject.transform.Find("LevelNamePanel/LevelNameText").GetComponent<TextMeshProUGUI>();
        Debug.unityLogger.Log("Level name text component: " + _levelNameTextComponent.text);
    }

    public override void OnNetworkSpawn()
    {
        _levelName.OnValueChanged += OnLevelNameChanged;
        
        if (!IsServer)
        {
            _levelNameTextComponent.text = FormatName(_levelName.Value.ToString());
            return;
        }

        LoadJsonLevelNames();
    }
    
    public override void OnNetworkDespawn()
    {
        _levelName.OnValueChanged += OnLevelNameChanged;
    }

    public void OnPreviousClick()
    {
        if (!IsServer) return;

        if (_levelIndex - 1 < 0)
        {
            _levelIndex = _numberOfLevels - 1;
        }
        else
        {
            _levelIndex--;
        }
        
        _levelName.Value =  _allLevelNames[_levelIndex];
    }

    public void OnNextClick()
    {
        if (!IsServer) return;
        
        if (_levelIndex + 1 >= _numberOfLevels)
        {
            _levelIndex = 0;
        }
        else
        {
            _levelIndex++;
        }
        
        _levelName.Value =  _allLevelNames[_levelIndex];
    }
    
    public void OnPlayClick()
    {
        if (!IsServer) return;
        
        // Debug.Log("TestButton.OnButtonClick");

        NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
        CursorToggler cursorToggler = playerObject != null
            ? playerObject.GetComponentInChildren<CursorToggler>(true)
            : null;
        cursorToggler?.SetCursor(false);

        GameState.Instance.LevelFileName.Value = _levelName.Value;
        
        NetworkManager.Singleton.SceneManager.LoadScene("Level", LoadSceneMode.Single);
    }

    private void OnLevelNameChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        if (_numberOfLevels == 0)
        {
            _levelNameTextComponent.text = "No level file in json format in the specific folder.";
        }
        
        _levelNameTextComponent.text = FormatName(newValue.Value);
    }
    
    public void LoadJsonLevelNames()
    {
        string LevelsPath = FrenzyLevelConfiguration.LevelsPath;

        if(!Directory.Exists(LevelsPath))
        {
            Directory.CreateDirectory(LevelsPath);
        }
        
        string[] tempJsonFiles = Directory.GetFiles(LevelsPath, "*.json", SearchOption.TopDirectoryOnly);

        Debug.Log("Files: " + tempJsonFiles);
        
        foreach (string jsonFile in tempJsonFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(jsonFile);
            _allLevelNames.Add(fileName);
        }

        _numberOfLevels = _allLevelNames.Count;
        _levelName.Value = _allLevelNames[0];
    }

    private string FormatName(string name)
    {
        TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
        return textInfo.ToTitleCase(name.Replace('-', ' ').ToLowerInvariant());
    }
}
