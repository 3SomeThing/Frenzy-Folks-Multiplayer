using System.Collections;
using System.Collections.Generic;
using Frenzy_Folks.Scripts.Core;
using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    private const ulong NoSpectatorTarget = ulong.MaxValue;

    [SerializeField] private List<GameObject> localOnlyObjects = new();

    private SUPERCharacterAIO characterController;
    private Transform _originalCameraParent;
    private Vector3 _originalCameraLocalPosition;
    private Quaternion _originalCameraLocalRotation;
    private Vector3 lastValidatedPosition;
    private double lastValidationTime;
    private MovablePlatform carriedPlatform;
    private Matrix4x4 previousPlatformWorldToLocal;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;
    private Collider[] _colliders;
    private Renderer[] _renderers;
    private bool collisionsEnabled = true;
    private readonly NetworkVariable<bool> isRespawning = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> isSpectating = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<ulong> spectatorTargetClientId = new(
        NoSpectatorTarget,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        characterController = GetComponent<SUPERCharacterAIO>();
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);

        if (characterController != null && characterController.playerCamera != null)
        {
            Transform cameraTransform = characterController.playerCamera.transform;
            _originalCameraParent = cameraTransform.parent;
            _originalCameraLocalPosition = cameraTransform.localPosition;
            _originalCameraLocalRotation = cameraTransform.localRotation;
        }
    }

    public override void OnNetworkSpawn()
    {
        characterController = GetComponent<SUPERCharacterAIO>();
        AlignOwnerCameraToPlayerRotation();
        isRespawning.OnValueChanged += OnRespawningChanged;
        isMovementLocked.OnValueChanged += OnMovementLockedChanged;
        isSpectating.OnValueChanged += OnSpectatingChanged;
        spectatorTargetClientId.OnValueChanged += OnSpectatorTargetChanged;
        ApplyRespawningState(isRespawning.Value);
        ApplyMovementLockedState(isMovementLocked.Value);
        ApplySpectatingState(isSpectating.Value);
        ApplySpectatorTarget();

        if (IsServer)
        {
            lastValidatedPosition = transform.position;
            lastValidationTime = NetworkManager.ServerTime.Time;
            GameState.Instance.InitializePlayerCheckpoint(OwnerClientId);
        }

        if (IsOwner)
        {
            foreach (GameObject localOnlyObject in localOnlyObjects)
            {
                localOnlyObject.SetActive(true);
            }

            AssignWorldCanvasCamera();
        }
    }

    public void AssignWorldCanvasCameraRpc()
    {
        if (!IsServer)
        {
            return;
        }

        AssignWorldCanvasCameraClientRpc();
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void AssignWorldCanvasCameraClientRpc()
    {
        AssignWorldCanvasCamera();
    }

    private void AssignWorldCanvasCamera()
    {
        if (characterController == null || characterController.playerCamera == null)
        {
            return;
        }

        GameObject levelCanvas = GameObject.Find("LevelCanvas");
        if (levelCanvas == null)
        {
            Debug.LogWarning("LevelCanvas not found while assigning the player camera.", this);
            return;
        }

        Canvas[] canvases = levelCanvas.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            canvas.worldCamera = characterController.playerCamera;
        }
    }

    public override void OnNetworkDespawn()
    {
        isRespawning.OnValueChanged -= OnRespawningChanged;
        isMovementLocked.OnValueChanged -= OnMovementLockedChanged;
        isSpectating.OnValueChanged -= OnSpectatingChanged;
        spectatorTargetClientId.OnValueChanged -= OnSpectatorTargetChanged;
    }

    private readonly NetworkVariable<bool> isMovementLocked = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public void SetMovementLocked(bool locked)
    {
        if (!IsServer)
        {
            return;
        }

        isMovementLocked.Value = locked;
        ApplyMovementLockedState(locked);
    }

    public void SetSpectating(ulong targetClientId)
    {
        if (!IsServer)
        {
            return;
        }

        spectatorTargetClientId.Value = targetClientId;
        isSpectating.Value = true;
        ApplySpectatingState(true);
        ApplySpectatorTarget();
    }

    public void SetSpectatorTarget(ulong targetClientId)
    {
        if (!IsServer || !isSpectating.Value)
        {
            return;
        }

        spectatorTargetClientId.Value = targetClientId;
        ApplySpectatorTarget();
    }

    public void ResetSpectating()
    {
        if (!IsServer)
        {
            return;
        }

        spectatorTargetClientId.Value = NoSpectatorTarget;
        isSpectating.Value = false;
        isMovementLocked.Value = false;
        collisionsEnabled = true;
        ApplySpectatingState(false);
        ApplyMovementLockedState(false);
    }

    public void ResetForLobby()
    {
        if (!IsServer)
        {
            return;
        }

        isRespawning.Value = false;
        spectatorTargetClientId.Value = NoSpectatorTarget;
        isSpectating.Value = false;
        isMovementLocked.Value = false;
        collisionsEnabled = true;

        ResetForLobbyRpc();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ResetForLobbyRpc()
    {
        collisionsEnabled = true;
        ApplyRespawningState(false);
        ApplySpectatingState(false);
        ApplyMovementLockedState(false);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void SetCollisionsEnabledRpc(bool enabled)
    {
        collisionsEnabled = enabled;
        ApplyCollisionsEnabled(enabled && !isMovementLocked.Value);
    }

    public void SetPlayerLayerCollisionIgnored(bool ignored)
    {
        if (!IsServer)
        {
            return;
        }

        ApplyPlayerLayerCollisionIgnored(ignored);
        SetPlayerLayerCollisionIgnoredRpc(ignored);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void SetPlayerLayerCollisionIgnoredRpc(bool ignored)
    {
        ApplyPlayerLayerCollisionIgnored(ignored);
    }

    private void ApplyPlayerLayerCollisionIgnored(bool ignored)
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            Physics.IgnoreLayerCollision(playerLayer, playerLayer, ignored);
        }
    }

    private void ApplyCollisionsEnabled(bool enabled)
    {
        if (_colliders == null)
        {
            return;
        }

        foreach (Collider collider in _colliders)
        {
            collider.enabled = enabled;
        }
    }

    public bool IsInside(BoxCollider zone)
    {
        Vector3 localPosition = zone.transform.InverseTransformPoint(transform.position);
        Vector3 halfSize = zone.size * 0.5f;
        Vector3 offset = localPosition - zone.center;
        return Mathf.Abs(offset.x) <= halfSize.x &&
               Mathf.Abs(offset.y) <= halfSize.y &&
               Mathf.Abs(offset.z) <= halfSize.z;
    }

    public void SetRespawning(bool respawning)
    {
        if (!IsServer)
        {
            return;
        }

        isRespawning.Value = respawning;
        ApplyRespawningState(respawning);
    }

    public void RespawnAt(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
        {
            return;
        }

        ApplyRespawnPosition(position, rotation);
        RespawnAtOwnerRpc(position, rotation);
    }

    private void ApplyRespawnPosition(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        if (_rigidbody)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            _rigidbody.linearVelocity = Vector3.zero;
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        AlignOwnerCameraToPlayerRotation();
    }

    [Rpc(SendTo.Owner)]
    private void RespawnAtOwnerRpc(Vector3 position, Quaternion rotation)
    {
        ApplyRespawnPosition(position, rotation);
    }

    [Rpc(SendTo.Owner)]
    public void ApplyCheckpointColorRpc(int reachedCheckpointIndex)
    {
        Checkpoint.ApplyColorsUpTo(reachedCheckpointIndex);
    }

    public void AlignOwnerCameraToPlayerRotation()
    {
        if (IsOwner && characterController != null && characterController.playerCamera != null)
        {
            characterController.RotateView(transform.rotation.eulerAngles, false);
            characterController.RefreshCameraPosition();
        }
    }

    private void OnRespawningChanged(bool previousValue, bool currentValue)
    {
        ApplyRespawningState(currentValue);
    }

    private void ApplyRespawningState(bool respawning)
    {
        if (characterController != null)
        {
            characterController.enabled = IsOwner && !respawning && !isMovementLocked.Value;
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = !IsOwner || respawning || isMovementLocked.Value;
        }
    }

    private void OnMovementLockedChanged(bool previousValue, bool currentValue)
    {
        ApplyMovementLockedState(currentValue);
    }

    private void OnSpectatingChanged(bool previousValue, bool currentValue)
    {
        ApplySpectatingState(currentValue);
    }

    private void OnSpectatorTargetChanged(ulong previousValue, ulong currentValue)
    {
        ApplySpectatorTarget();
    }

    private void ApplySpectatingState(bool spectating)
    {
        if (!spectating)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.enabled = true;
            }

            ApplyCollisionsEnabled(true);

            if (IsOwner)
            {
                foreach (GameObject localOnlyObject in localOnlyObjects)
                {
                    localOnlyObject.SetActive(true);
                }

                Camera playerCamera = characterController?.playerCamera;
                if (playerCamera != null && _originalCameraParent != null)
                {
                    playerCamera.transform.SetParent(_originalCameraParent, false);
                    playerCamera.transform.localPosition = _originalCameraLocalPosition;
                    playerCamera.transform.localRotation = _originalCameraLocalRotation;
                    playerCamera.enabled = true;
                    AlignOwnerCameraToPlayerRotation();
                }
            }

            return;
        }

        ApplyMovementLockedState(true);
        ApplyCollisionsEnabled(false);

        foreach (Renderer renderer in _renderers)
        {
            renderer.enabled = false;
        }

        if (!IsOwner || characterController == null || characterController.playerCamera == null)
        {
            return;
        }

        foreach (GameObject localOnlyObject in localOnlyObjects)
        {
            localOnlyObject.SetActive(false);
        }

        Camera playerCameraVar = characterController.playerCamera;
        playerCameraVar.gameObject.SetActive(true);
        playerCameraVar.enabled = true;
    }

    private void ApplySpectatorTarget()
    {
        if (!IsOwner ||
            !isSpectating.Value ||
            spectatorTargetClientId.Value == NoSpectatorTarget ||
            !NetworkManager.Singleton.ConnectedClients.TryGetValue(
                spectatorTargetClientId.Value,
                out NetworkClient targetClient) ||
            targetClient.PlayerObject == null)
        {
            return;
        }

        Camera playerCamera = characterController?.playerCamera;
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.transform.SetParent(targetClient.PlayerObject.transform, false);
        playerCamera.transform.localPosition = new Vector3(0f, 2f, -8f);
        playerCamera.transform.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        ApplySpectatorTarget();
    }

    private void ApplyMovementLockedState(bool locked)
    {
        if (characterController != null && !isRespawning.Value)
        {
            characterController.enabled = IsOwner && !locked;
        }

        if (_rigidbody != null && !isRespawning.Value)
        {
            _rigidbody.isKinematic = !IsOwner || locked;
            if (locked)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }

        if (_colliders != null)
        {
            ApplyCollisionsEnabled(collisionsEnabled && !locked);
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner || IsServer)
        {
            return;
        }

        CarryWithPlatform();
    }

    private void CarryWithPlatform()
    {
        float rayLength = (_capsuleCollider ? _capsuleCollider.height : 1f) * 0.5f + 0.25f;
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayLength))
        {
            carriedPlatform = null;
            return;
        }

        MovablePlatform platform = hit.collider.GetComponentInParent<MovablePlatform>();
        if (platform is null)
        {
            carriedPlatform = null;
            return;
        }

        if (carriedPlatform != platform)
        {
            carriedPlatform = platform;
            previousPlatformWorldToLocal = platform.transform.worldToLocalMatrix;
            return;
        }

        Vector3 localPlayerPosition =
            previousPlatformWorldToLocal.MultiplyPoint3x4(transform.position);
        Vector3 carriedPosition = platform.transform.TransformPoint(localPlayerPosition);
        Vector3 platformDelta = carriedPosition - transform.position;
        previousPlatformWorldToLocal = platform.transform.worldToLocalMatrix;

        if (platformDelta.sqrMagnitude > 0f)
        {
            _rigidbody.MovePosition(_rigidbody.position + platformDelta);
        }
    }
    
    [Rpc(SendTo.Owner)]
    public void ApplyKnockbackRpc(Vector3 force)
    {
        if (!IsOwner)
            return;

        Rigidbody playerRigidbody = GetComponent<Rigidbody>();
        SUPERCharacterAIO playerController = GetComponent<SUPERCharacterAIO>();
        if (playerRigidbody == null || playerRigidbody.isKinematic || playerController == null)
        {
            Debug.LogWarning($"[NetworkPlayer] Cannot apply knockback to '{name}': its Rigidbody is missing or kinematic.", this);
            return;
        }
        
        playerController.Jump(playerController.jumpPower);
        playerRigidbody.AddForce(force, ForceMode.Impulse);
    }
}