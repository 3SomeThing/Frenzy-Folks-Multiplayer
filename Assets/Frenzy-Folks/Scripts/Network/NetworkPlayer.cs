using System.Collections.Generic;
using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private List<GameObject> localOnlyObjects = new();

    private SUPERCharacterAIO characterController;
    private Vector3 lastValidatedPosition;
    private double lastValidationTime;
    private MovablePlatform carriedPlatform;
    private Matrix4x4 previousPlatformWorldToLocal;

    private Rigidbody _rigidbody;
    private CapsuleCollider _capsuleCollider;
    private readonly NetworkVariable<bool> isRespawning = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
    }

    public override void OnNetworkSpawn()
    {
        characterController = GetComponent<SUPERCharacterAIO>();
        isRespawning.OnValueChanged += OnRespawningChanged;
        ApplyRespawningState(isRespawning.Value);

        if (IsServer)
        {
            lastValidatedPosition = transform.position;
            lastValidationTime = NetworkManager.ServerTime.Time;
        }

        if (IsOwner)
        {
            foreach (GameObject localOnlyObject in localOnlyObjects)
            {
                localOnlyObject.SetActive(true);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        isRespawning.OnValueChanged -= OnRespawningChanged;
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

    [Rpc(SendTo.Owner)]
    private void RespawnAtOwnerRpc(Vector3 position, Quaternion rotation)
    {
        ApplyRespawnPosition(position, rotation);
    }

    private void ApplyRespawnPosition(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        if (_rigidbody != null)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
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
            characterController.enabled = IsOwner && !respawning;
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = !IsOwner || respawning;
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