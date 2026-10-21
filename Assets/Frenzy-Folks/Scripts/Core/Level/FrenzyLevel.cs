using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Frenzy_Folks.Scripts.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class FrenzyLevel : NetworkBehaviour
{
    private string _levelName;

    private int _maxTime;

    [SerializeField] public Dictionary<string, GameObject> levelPrefabs;
    
    private InputAction _inputActionSave = new InputAction(
        "Save Level Configuration",
        InputActionType.Button,
        binding: "<Keyboard>/h");
    
    private InputAction _inputActionLoad = new InputAction(
        "Load Level Configuration",
        InputActionType.Button,
        binding: "<Keyboard>/u");

    private void Awake()
    {
        // string name = "movable_platform";
        // GameObject go = levelPrefabs[name];
        // // MovablePlatformConfiguration configuration = (MovablePlatformConfiguration)configurations[name];
        // configuration.Apply(go);    
        //
        // Debug.Log(JsonUtility.ToJson(configuration));
        _inputActionSave.Enable();
        _inputActionLoad.Enable();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        if (!IsJsonLevelLoaderActive())
        {
            StartCoroutine(SynchronizeSceneElements());
        }
        //
        // string path = Path.Combine(Application.streamingAssetsPath, "test-level.json");
        // // File.Create(path).Close();
        // // File.WriteAllText(path, JsonUtility.ToJson(new FrenzyLevelConfiguration(this).Save(), true));
        // // const string json = "{\"configurationEntries\":[{\"key\":\"movable_platform\",\"value\":{\"rid\":1000}}],\"references\":{\"version\":2,\"RefIds\":[{\"rid\":1000,\"type\":{\"class\":\"MovablePlatformConfiguration\",\"ns\":\"\",\"asm\":\"Assembly-CSharp\"},\"data\":{\"name\":\"\",\"transform\":{\"position\":{\"x\":10,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":0},\"scale\":{\"x\":0,\"y\":0,\"z\":0}},\"pathPoints\":[{\"x\":10,\"y\":10,\"z\":10},{\"x\":2,\"y\":2,\"z\":2},{\"x\":3,\"y\":3,\"z\":3}]}}]}}";
        // string json = File.ReadAllText(path);
        // FrenzyLevelConfiguration.Load(this, json, levelPrefabs);
    }

    private IEnumerator SynchronizeSceneElements()
    {
        yield return null;

        foreach (SerializedLevelElement element in
                 FindObjectsOfType<SerializedLevelElement>(true))
        {
            NetworkObject networkObject = element.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.IsSpawned)
            {
                continue;
            }

            if (FrenzyLevelConfiguration.TryGetConfigurationJson(
                    element.gameObject,
                    element.elementName,
                    out string configurationJson))
            {
                ApplyElementConfigurationRpc(
                    networkObject,
                    element.elementName,
                    configurationJson);
            }
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (_inputActionSave.WasPressedThisFrame())
        {
            SaveLevelConfiguration();
        }
        
        if( _inputActionLoad.WasPressedThisFrame())
        {
            LoadLevelConfiguration();
        }
    }

    private void LoadLevelConfiguration()
    {
        if (!IsJsonLevelLoaderActive())
        {
            Debug.Log("Skipping JSON level configuration because the active level is not a LevelLoader.");
            return;
        }

        FrenzyLevelConfiguration.Load(this, $"{GameState.Instance.LevelFileName.Value}.json", levelPrefabs);
        Debug.Log($"Level configuration loaded from {GameState.Instance.LevelFileName.Value}.json");
    }

    private bool IsJsonLevelLoaderActive()
    {
        GameObject levelLoader = GameObject.Find("LevelLoader");
        return levelLoader != null
               && levelLoader.activeInHierarchy
               && levelLoader.TryGetComponent<FrenzyLevel>(out FrenzyLevel loader)
               && loader.enabled;
    }

    public void SaveLevelConfiguration()
    {
        FrenzyLevelConfiguration configuration = new FrenzyLevelConfiguration(this);
        configuration.SaveLevelToFile("testing-playground.json");
        Debug.Log("Level configuration saved to testing-playground.json");
    }

    //TODO: Verifier tout ça
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void ApplyElementConfigurationRpc(
        NetworkObjectReference elementReference,
        string elementKey,
        string configurationJson)
    {
        if (!elementReference.TryGet(out NetworkObject networkObject))
        {
            Debug.LogWarning($"Could not find spawned element for configuration '{elementKey}'.", this);
            return;
        }

        FrenzyLevelConfiguration.ApplyToElement(
            networkObject.gameObject,
            elementKey,
            configurationJson);
    }
}