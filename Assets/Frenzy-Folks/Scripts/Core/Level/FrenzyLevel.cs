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
        _inputActionSave.Enable();
        _inputActionLoad.Enable();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        StartCoroutine(LoadSceneAfterFrame());

        if (!IsJsonLevelLoaderActive())
        {
            StartCoroutine(SynchronizeSceneElements());
        }
    }
    
    private IEnumerator<WaitForEndOfFrame> LoadSceneAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        LoadLevelConfiguration();
    }

    private IEnumerator SynchronizeSceneElements()
    {
        yield return null;

        foreach (SerializedLevelElement element in FindObjectsOfType<SerializedLevelElement>(true))
        {
            NetworkObject networkObject = element.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.IsSpawned ||
                !FrenzyLevelConfiguration.TryGetConfigurationJson(
                    element.gameObject,
                    element.elementName,
                    out string configurationJson))
            {
                continue;
            }

            SynchronizeElementConfiguration(networkObject, element.elementName, configurationJson);
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

        FrenzyLevelConfiguration.Load(this, $"{GameState.Instance.LevelFileName.Value}.json");
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

    public void SetLoadedLevelScale(float scale)
    {
        if (!IsServer)
        {
            return;
        }

        SetLevelScaleClientRpc(transform.localScale * scale);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void SetLevelScaleClientRpc(Vector3 scale)
    {
        transform.localScale = scale;
    }

    public void SynchronizeElementConfiguration(
        NetworkObject networkObject,
        string elementKey,
        string configurationJson)
    {
        if (!IsServer || networkObject == null || !networkObject.IsSpawned)
        {
            return;
        }

        ApplyElementConfigurationClientRpc(networkObject, elementKey, configurationJson);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ApplyElementConfigurationClientRpc(
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