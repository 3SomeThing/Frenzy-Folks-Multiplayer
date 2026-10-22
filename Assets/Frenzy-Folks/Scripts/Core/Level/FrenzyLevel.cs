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
    // How long the server waits for clients to confirm they spawned an element
    // before applying its configuration anyway (so one stuck client can't block the level).
    private const float HandshakeTimeoutSeconds = 5f;

    private sealed class PendingElementConfiguration
    {
        public readonly string ElementKey;
        public readonly string ConfigurationJson;
        public readonly HashSet<ulong> AwaitingClientIds;

        public PendingElementConfiguration(
            string elementKey,
            string configurationJson,
            IEnumerable<ulong> clientIds)
        {
            ElementKey = elementKey;
            ConfigurationJson = configurationJson;
            AwaitingClientIds = new HashSet<ulong>(clientIds);
        }
    }

    [SerializeField] public Dictionary<string, GameObject> levelPrefabs;

    private readonly Dictionary<ulong, PendingElementConfiguration> pendingElementConfigurations = new();
    private readonly HashSet<ulong> pendingSpawnAcknowledgements = new();
    private readonly List<NetworkObject> loadedElements = new();

    // Multiplier applied on top of the level's authored scale.
    // Server-written NetworkVariable: it is part of the spawn payload (late joiners get it)
    // and it can never arrive "out of order" relative to the level's own spawn.
    private readonly NetworkVariable<float> loadedScaleFactor = new(1f);

    private Vector3 _baseScale = Vector3.one;
    private bool _isLoading;
    private bool _spawningFinished;
    private float _pendingScaleFactor = 1f;

    public bool IsLoadingLevel => _isLoading;

    private int _loadGeneration;

    [Header("Loading screen")]
    [Tooltip("Canvas covering the screen while the level is being built. Looked up by name if left empty.")]
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private string loadingCanvasName = "LoadingCanvas";
    
    [SerializeField] private AudioSource levelSoundSource;

    // Replicated (server-written). True once the level is fully usable: every element is spawned and
    // configured on all clients and the final scale has been applied. Late joiners receive it too.
    private readonly NetworkVariable<bool> levelReady = new(false);

    public bool IsLevelReady => levelReady.Value;

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
        _baseScale = transform.localScale;
        _inputActionSave.Enable();
        _inputActionLoad.Enable();
    }

    public override void OnNetworkSpawn()
    {
        loadedScaleFactor.OnValueChanged += OnLoadedScaleFactorChanged;
        ApplyLoadedScale(loadedScaleFactor.Value);

        levelReady.OnValueChanged += OnLevelReadyChanged;
        UpdateLoadingCanvas(levelReady.Value);

        if (!IsServer)
        {
            return;
        }

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        StartCoroutine(LoadSceneAfterFrame());

        if (!IsJsonLevelLoaderActive())
        {
            StartCoroutine(SynchronizeSceneElements());
        }
    }

    public override void OnNetworkDespawn()
    {
        loadedScaleFactor.OnValueChanged -= OnLoadedScaleFactorChanged;
        levelReady.OnValueChanged -= OnLevelReadyChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnLoadedScaleFactorChanged(float previous, float current)
    {
        ApplyLoadedScale(current);
    }

    private void ApplyLoadedScale(float factor)
    {
        // Always absolute (base * factor), never "current * factor", so reloading can't compound the scale.
        transform.localScale = _baseScale * factor;
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

        // Scene-built level (no JSON loading, no runtime scaling): nothing else to wait for.
        SetLevelReady(true);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (_inputActionSave.WasPressedThisFrame())
        {
            SaveLevelConfiguration();
        }

        if (_inputActionLoad.WasPressedThisFrame())
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

        if (_isLoading)
        {
            Debug.LogWarning("A level is already being loaded, ignoring the new load request.", this);
            return;
        }

        FrenzyLevelConfiguration.Load(this, $"{GameState.Instance.LevelFileName.Value}.json");
        Debug.Log($"Level configuration loading from {GameState.Instance.LevelFileName.Value}.json");
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

    // ------------------------------------------------------------------
    // Level load lifecycle (server only)
    //   BeginLevelLoad()  -> reset previous level + scale back to the authored scale
    //   RegisterSpawnedElement() x N
    //   EndLevelLoad(f)   -> once every element is spawned AND configured on every client,
    //                        the level is scaled by f (single NetworkVariable write)
    // ------------------------------------------------------------------

    public void BeginLevelLoad()
    {
        if (!IsServer)
        {
            return;
        }

        foreach (NetworkObject element in loadedElements)
        {
            if (element != null && element.IsSpawned)
            {
                element.Despawn(true);
            }
        }

        loadedElements.Clear();
        pendingElementConfigurations.Clear();

        _loadGeneration++;
        SetLevelReady(false);
        _isLoading = true;
        _spawningFinished = false;

        // Elements are spawned at the authored (unscaled) size, identically on server and clients.
        loadedScaleFactor.Value = 1f;
    }

    public void EndLevelLoad(float scaleFactor)
    {
        if (!IsServer)
        {
            return;
        }

        _pendingScaleFactor = scaleFactor;
        _spawningFinished = true;
        TryApplyLoadedScale();
    }

    // Kept for compatibility: sets the absolute multiplier (not cumulative).
    public void SetLoadedLevelScale(float scale)
    {
        if (!IsServer)
        {
            return;
        }

        loadedScaleFactor.Value = scale;
    }

    private void TryApplyLoadedScale()
    {
        if (!_spawningFinished || pendingElementConfigurations.Count > 0)
        {
            return;
        }

        _spawningFinished = false;
        _isLoading = false;
        loadedScaleFactor.Value = _pendingScaleFactor;
        ApplyLoadedScale(_pendingScaleFactor); // explicit, so the server never depends on callback timing
        Debug.Log($"All level elements spawned and configured, level scaled x{_pendingScaleFactor}.");

        StartCoroutine(MarkLevelReady(_loadGeneration));
    }

    private IEnumerator MarkLevelReady(int generation)
    {
        // Wait one frame so the scale update is queued for the clients before anything
        // that depends on it (like teleporting players) is sent after it.
        yield return null;
        yield return new WaitForFixedUpdate();

        // A new load started in the meantime: this one is obsolete.
        if (generation != _loadGeneration)
        {
            yield break;
        }

        // Make the physics world see the new transforms (IsInside checks, raycasts...) right away.
        Physics.SyncTransforms();
        if(levelSoundSource != null) levelSoundSource.Play();
        SetLevelReady(true);
    }

    private void SetLevelReady(bool ready)
    {
        if (!IsServer)
        {
            return;
        }

        levelReady.Value = ready;
        UpdateLoadingCanvas(ready); // explicit, so the host never depends on callback timing
    }

    private void OnLevelReadyChanged(bool previous, bool current)
    {
        UpdateLoadingCanvas(current);
    }

    private void UpdateLoadingCanvas(bool levelIsReady)
    {
        if (loadingCanvas == null)
        {
            loadingCanvas = GameObject.Find(loadingCanvasName);
        }

        if (loadingCanvas == null)
        {
            Debug.LogWarning(
                $"Loading canvas '{loadingCanvasName}' not found. Assign it on FrenzyLevel.",
                this);
            return;
        }

        loadingCanvas.SetActive(!levelIsReady);
    }

    // ------------------------------------------------------------------
    // Element configuration sync
    // ------------------------------------------------------------------

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

    public void RegisterSpawnedElement(
        NetworkObject networkObject,
        string elementKey,
        string configurationJson)
    {
        if (!IsServer || networkObject == null || !networkObject.IsSpawned)
        {
            return;
        }

        loadedElements.Add(networkObject);

        PendingElementConfiguration pendingConfiguration =
            new(elementKey, configurationJson, NetworkManager.ConnectedClientsIds);
        pendingElementConfigurations[networkObject.NetworkObjectId] = pendingConfiguration;

        RequestElementSpawnHandshakeClientRpc(networkObject.NetworkObjectId);
        StartCoroutine(ExpireHandshake(networkObject.NetworkObjectId));
        CompleteElementHandshakeIfReady(networkObject.NetworkObjectId);
    }

    private IEnumerator ExpireHandshake(ulong networkObjectId)
    {
        yield return new WaitForSecondsRealtime(HandshakeTimeoutSeconds);

        if (pendingElementConfigurations.TryGetValue(
                networkObjectId,
                out PendingElementConfiguration pendingConfiguration))
        {
            Debug.LogWarning(
                $"Element {networkObjectId} handshake timed out, still waiting for clients: " +
                $"{string.Join(", ", pendingConfiguration.AwaitingClientIds)}. Applying configuration anyway.",
                this);

            pendingConfiguration.AwaitingClientIds.Clear();
            CompleteElementHandshakeIfReady(networkObjectId);
        }
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void RequestElementSpawnHandshakeClientRpc(ulong networkObjectId)
    {
        if (IsServer)
        {
            AcknowledgeElementSpawned(networkObjectId, NetworkManager.LocalClientId);
            return;
        }

        if (pendingSpawnAcknowledgements.Add(networkObjectId))
        {
            StartCoroutine(AcknowledgeElementWhenSpawned(networkObjectId));
        }
    }

    private IEnumerator AcknowledgeElementWhenSpawned(ulong networkObjectId)
    {
        float deadline = Time.realtimeSinceStartup + HandshakeTimeoutSeconds * 2f;

        while (NetworkManager != null &&
               !NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                pendingSpawnAcknowledgements.Remove(networkObjectId);
                yield break;
            }

            yield return null;
        }

        pendingSpawnAcknowledgements.Remove(networkObjectId);

        if (NetworkManager != null)
        {
            AcknowledgeElementSpawnedServerRpc(networkObjectId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AcknowledgeElementSpawnedServerRpc(
        ulong networkObjectId,
        RpcParams rpcParams = default)
    {
        AcknowledgeElementSpawned(
            networkObjectId,
            rpcParams.Receive.SenderClientId);
    }

    private void AcknowledgeElementSpawned(ulong networkObjectId, ulong clientId)
    {
        if (!IsServer ||
            !pendingElementConfigurations.TryGetValue(
                networkObjectId,
                out PendingElementConfiguration pendingConfiguration))
        {
            return;
        }

        pendingConfiguration.AwaitingClientIds.Remove(clientId);
        CompleteElementHandshakeIfReady(networkObjectId);
    }

    private void CompleteElementHandshakeIfReady(ulong networkObjectId)
    {
        if (!pendingElementConfigurations.TryGetValue(
                networkObjectId,
                out PendingElementConfiguration pendingConfiguration) ||
            pendingConfiguration.AwaitingClientIds.Count > 0)
        {
            return;
        }

        pendingElementConfigurations.Remove(networkObjectId);

        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                networkObjectId,
                out NetworkObject networkObject))
        {
            ApplyElementConfigurationClientRpc(
                networkObject,
                pendingConfiguration.ElementKey,
                pendingConfiguration.ConfigurationJson);
        }
        else
        {
            Debug.LogWarning(
                $"Could not find spawned element {networkObjectId} after the client handshake.",
                this);
        }

        // This element may have been the last one the level was waiting for.
        TryApplyLoadedScale();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        List<ulong> completedHandshakes = new();
        foreach (KeyValuePair<ulong, PendingElementConfiguration> pending in pendingElementConfigurations)
        {
            pending.Value.AwaitingClientIds.Remove(clientId);
            if (pending.Value.AwaitingClientIds.Count == 0)
            {
                completedHandshakes.Add(pending.Key);
            }
        }

        foreach (ulong networkObjectId in completedHandshakes)
        {
            CompleteElementHandshakeIfReady(networkObjectId);
        }
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