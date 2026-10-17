using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;
using SimpleUnitySprings;

public class TrampolineTrap : MonoBehaviour
{
    [SerializeField, Min(0f)] public float pushSpeedVertical = 30f;
    [SerializeField] private AudioSource trampolineSound;
    [SerializeField] private bool debugLogs = false;
    [SerializeField, Min(1f)] private float bumpScale = 1.25f;
    [SerializeField, Min(1f)] private float animationSpeed = 5f;

    private int playerLayer; //TODO: Generique partout

    private Spring spring;
    private Vector3 initialScale;
    private bool isBumping;
    private bool isReturningToRest;

    private void Awake()
    {
        initialScale = transform.localScale;
        spring = new Spring(
            config: SpringConfig.Create(tension: 600, friction: 12),
            from: 1f,
            to: 1f);

        playerLayer = LayerMask.NameToLayer("Player"); //TODO: Generique partout
        if (playerLayer < 0)
        {
            Debug.LogError(
                "[TrampolineTrap] Unity layer 'Player' does not exist. Add it in Project Settings > Tags and Layers.", this);
            enabled = false;
            return;
        }

        if (trampolineSound == null)
        {
            trampolineSound = GetComponent<AudioSource>();
            if (trampolineSound == null && debugLogs)
            {
                Debug.LogWarning($"[TrampolineTrap] No audio sound assigned or found under '{name}'.", this);
            }
        }
    }

    private void Update()
    {
        bool isAnimating = spring.Tick(Time.deltaTime * animationSpeed);
        transform.localScale = initialScale * spring.Get();

        if (isBumping && !isAnimating)
        {
            if (!isReturningToRest)
            {
                spring.To(1f);
                isReturningToRest = true;
            }
            else
            {
                isBumping = false;
                isReturningToRest = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPushPlayer(other);
    }

    private void TryPushPlayer(Collider other)
    {
        SUPERCharacterAIO player = other.GetComponentInParent<SUPERCharacterAIO>();
        if (player == null)
        {
            if (debugLogs)
            {
                Debug.Log($"[TrampolineTrap] Capsule touched '{other.name}', but no SUPERCharacterAIO was found in its parents.", this);
            }
            return;
        }

        if (player.gameObject.layer != playerLayer)
        {
            return;
        }

        playSound();
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[TrampolineTrap] Player '{player.name}' has no Rigidbody for wind force.", player);
            return;
        }

        if (rb.isKinematic)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[TrampolineTrap] Player '{player.name}' has a kinematic Rigidbody; AddForce will not affect it.", player);
            }

            return;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 impulseForce = Vector3.up * pushSpeedVertical;
        rb.AddForce(impulseForce, ForceMode.Impulse);

        spring.To(bumpScale);
        isBumping = true;
        isReturningToRest = false;

        if (debugLogs)
        {
            Debug.Log($"[TrampolineTrap] Collision hit player '{player.name}'; push={impulseForce}.", player);
        }
    }

    private void playSound()
    {
        if (trampolineSound == null)
        {
            return;
        }

        if (NetworkAssetsManager.Instance == null)
        {
            trampolineSound.Play();
            return;
        }

        NetworkAssetsManager.Instance.PlayForEveryone(trampolineSound);
    }
}
