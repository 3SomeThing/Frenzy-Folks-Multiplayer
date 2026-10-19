using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class Platform : NetworkBehaviour
{
    [SerializeField] public bool makePlatformFall = false;
    [SerializeField] public float delayBeforeFalling = 2f;
    [SerializeField] public float delayBeforeRespawning = 3f;

    [SerializeField] private AudioSource _crumbleSound;
    [SerializeField] private ParticleSystem _crumbleParticles;
    
    private NetworkVariable<bool> _isPlatformFallingTriggered = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool isPlatformFallingTriggered
    {
        get { return _isPlatformFallingTriggered.Value; }
        set { _isPlatformFallingTriggered.Value = value; }
    }

    private Rigidbody _rigidbody;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;


    // Server (or offline) drives the movement, clients only observe it
    private bool HasAuthority => !IsSpawned || IsServer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _initialPosition = _rigidbody.position;
        _initialRotation = _rigidbody.rotation;
    }

    public void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player") && !HasAuthority) ||
            (isPlatformFallingTriggered) ||
            (!makePlatformFall))
        {
            return;
        }

        isPlatformFallingTriggered = true;
        StartCoroutine(FallAfterDelay());
    }

    private IEnumerator FallAfterDelay()
    {
        ToggleSoundAndParticles(true);

        yield return new WaitForSeconds(delayBeforeFalling);

        //NetworkAssetsManager.Instance.SetEnabledForEveryone(_floatingController, false);
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = false;

        StartCoroutine(ReappearAfterDelay());
    }

    private IEnumerator ReappearAfterDelay()
    {
        ToggleSoundAndParticles(false);
        
        yield return new WaitForSeconds(delayBeforeRespawning);

        _rigidbody.isKinematic = true;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.position = _initialPosition;
        _rigidbody.rotation = _initialRotation;
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);

        isPlatformFallingTriggered = false;
    }

    private void ToggleSoundAndParticles(bool status)
    {
        if (status)
        {
            NetworkAssetsManager.Instance.PlayForEveryone(_crumbleSound);
            NetworkAssetsManager.Instance.PlayForEveryone(_crumbleParticles);
        }
        else
        {
            NetworkAssetsManager.Instance.StopForEveryone(_crumbleSound);
            NetworkAssetsManager.Instance.StopForEveryone(_crumbleParticles);
        }
    }
}