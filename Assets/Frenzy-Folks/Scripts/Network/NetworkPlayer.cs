using System.Collections.Generic;
using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private List<GameObject> localOnlyObjects = new();
    [SerializeField, Min(0f)] private float serverPositionTolerance = 0.2f;

    private SUPERCharacterAIO characterController;
    private Vector3 lastValidatedPosition;
    private double lastValidationTime;
    private MovablePlatform carriedPlatform;
    private Matrix4x4 previousPlatformWorldToLocal;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        characterController = GetComponent<SUPERCharacterAIO>();
        characterController.enabled = IsOwner;

        Rigidbody playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = !IsOwner;
        }

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

    // private void FixedUpdate()
    // {
    //     if (!IsSpawned)
    //     {
    //         return;
    //     }
    //
    //     if (IsOwner && !IsServer)
    //     {
    //         CarryWithPlatform();
    //         return;
    //     }
    //
    //     if (!IsServer || IsOwner)
    //     {
    //         return;
    //     }
    //
    //     double now = NetworkManager.ServerTime.Time;
    //     float elapsed = Mathf.Max((float)(now - lastValidationTime), Time.fixedDeltaTime);
    //     Vector3 position = transform.position;
    //     Vector3 displacement = position - lastValidatedPosition;
    //     float platformSpeed = GetGroundPlatformSpeed();
    //     float horizontalLimit = (characterController.MaxNetworkMovementSpeed + platformSpeed) * elapsed
    //         + serverPositionTolerance;
    //     float verticalLimit = Mathf.Max(characterController.MaxNetworkJumpSpeed * 2f, 15f) * elapsed
    //         + serverPositionTolerance;
    //
    //     if (new Vector2(displacement.x, displacement.z).magnitude > horizontalLimit
    //         || Mathf.Abs(displacement.y) > verticalLimit)
    //     {
    //         transform.SetPositionAndRotation(lastValidatedPosition, transform.rotation);
    //         lastValidationTime = now;
    //         CorrectOwnerPositionRpc(lastValidatedPosition, transform.rotation);
    //         return;
    //     }
    //
    //     lastValidatedPosition = position;
    //     lastValidationTime = now;
    // }
    //
    // private void CarryWithPlatform()
    // {
    //     float rayLength = characterController.NetworkCapsuleHeight * 0.5f + 0.25f;
    //     if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayLength))
    //     {
    //         carriedPlatform = null;
    //         return;
    //     }
    //
    //     MovablePlatform platform = hit.collider.GetComponentInParent<MovablePlatform>();
    //     if (platform == null)
    //     {
    //         carriedPlatform = null;
    //         return;
    //     }
    //
    //     if (carriedPlatform != platform)
    //     {
    //         carriedPlatform = platform;
    //         previousPlatformWorldToLocal = platform.transform.worldToLocalMatrix;
    //         return;
    //     }
    //
    //     Vector3 platformLocalPlayerPosition = previousPlatformWorldToLocal.MultiplyPoint3x4(transform.position);
    //     Vector3 carriedPosition = platform.transform.TransformPoint(platformLocalPlayerPosition);
    //     Vector3 platformDelta = carriedPosition - transform.position;
    //     previousPlatformWorldToLocal = platform.transform.worldToLocalMatrix;
    //     if (platformDelta.sqrMagnitude > 0f)
    //     {
    //         Rigidbody playerRigidbody = GetComponent<Rigidbody>();
    //         if (playerRigidbody != null)
    //         {
    //             playerRigidbody.MovePosition(playerRigidbody.position + platformDelta);
    //         }
    //         else
    //         {
    //             transform.position = carriedPosition;
    //         }
    //     }
    // }
    //
    // private float GetGroundPlatformSpeed()
    // {
    //     float rayLength = characterController.NetworkCapsuleHeight + 0.25f;
    //     if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayLength))
    //     {
    //         return 0f;
    //     }
    //
    //     MovablePlatform platform = hit.collider.GetComponentInParent<MovablePlatform>();
    //     return platform ? platform.Speed : 0f;
    // }

    // [Rpc(SendTo.Owner)]
    // private void CorrectOwnerPositionRpc(Vector3 position, Quaternion rotation)
    // {
    //     if (!IsOwner)
    //     {
    //         return;
    //     }
    //
    //     transform.SetPositionAndRotation(position, rotation);
    //     Rigidbody playerRigidbody = GetComponent<Rigidbody>();
    //     if (playerRigidbody)
    //     {
    //         playerRigidbody.position = position;
    //         playerRigidbody.rotation = rotation;
    //         playerRigidbody.linearVelocity = Vector3.zero;
    //     }
    // }
    //
    // [Rpc(SendTo.Owner)]
    // public void MovePositionClientRpc(Vector3 move)
    // {
    //     _rigidbody.MovePosition(move);
    // }
}