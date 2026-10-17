using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;
using SimpleUnitySprings;

public class BumperTrap : MonoBehaviour
{
    [SerializeField, Min(0f)] public float pushSpeedHorizontal = 30f;
    [SerializeField, Min(0f)] public float pushSpeedVertical = 100f;
    
    [SerializeField] private AudioSource bumperSound;
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
                "[BumperTrap] Unity layer 'Player' does not exist. Add it in Project Settings > Tags and Layers.", this);
            enabled = false;
            return;
        }

        if (bumperSound == null)
        {
            bumperSound = GetComponent<AudioSource>();
            if (bumperSound == null && debugLogs)
            {
                Debug.LogWarning($"[BumperTrap] No audio sound assigned or found under '{name}'.", this);
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

    private void OnCollisionEnter(Collision other)
    {
        TryPushPlayer(other.gameObject.GetComponent<Collider>());
    }

    private void TryPushPlayer(Collider other)
    {
        SUPERCharacterAIO player = other.GetComponentInParent<SUPERCharacterAIO>();
        if (player == null)
        {
            if (debugLogs)
            {
                Debug.Log($"[BumperTrap] Capsule touched '{other.name}', but no SUPERCharacterAIO was found in its parents.", this);
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
            Debug.LogError($"[BumperTrap] Player '{player.name}' has no Rigidbody for wind force.", player);
            return;
        }

        if (rb.isKinematic)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[BumperTrap] Player '{player.name}' has a kinematic Rigidbody; AddForce will not affect it.", player);
            }

            return;
        }

        Vector3 direction = player.transform.position - transform.position;
        direction.Normalize();

        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        Vector3 impulseForce = horizontalDirection * pushSpeedHorizontal +
                               Vector3.up * (direction.y * pushSpeedVertical);

        rb.AddForce(impulseForce, ForceMode.Impulse);

        spring.To(bumpScale);
        isBumping = true;
        isReturningToRest = false;

        if (debugLogs)
        {
            Debug.Log($"[BumperTrap] Collision hit player '{player.name}'; push={impulseForce}.", player);
        }
    }

    private void playSound()
    {
        if (bumperSound == null)
        {
            return;
        }

        if (NetworkAssetsManager.Instance == null)
        {
            bumperSound.Play();
            return;
        }

        NetworkAssetsManager.Instance.PlayForEveryone(bumperSound);
    }
}
