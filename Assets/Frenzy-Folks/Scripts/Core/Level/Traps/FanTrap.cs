using SUPERCharacter;
using UnityEngine;

public class FanTrap : MonoBehaviour
{
    [SerializeField, Min(0f)] public float pushSpeed = 100f;
    [SerializeField, Min(0.1f)] public float blowingDuration = 7f;
    [SerializeField, Min(0.1f)] public float pauseDuration = 2f;
    [SerializeField, Range(0f, 90f)] public float sweepAngle = 45f;
    [SerializeField] private ParticleSystem windParticles;
    [SerializeField] private AudioSource windSound;
    [SerializeField] private CapsuleCollider windTrigger;
    [SerializeField] private bool debugLogs = false;

    private Quaternion initialHeadRotation;
    private float cycleStartTime;
    private float currentSweepAngle;
    private int playerLayer; //TODO: Generique partout
    private bool wasBlowing;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player"); //TODO: Generique partout
        if (playerLayer < 0)
        {
            Debug.LogError(
                "[FanTrap] Unity layer 'Player' does not exist. Add it in Project Settings > Tags and Layers.", this);
            enabled = false;
            return;
        }

        if (windParticles == null)
        {
            windParticles = GetComponentInChildren<ParticleSystem>(true);
            windSound = GetComponentInChildren<AudioSource>(true);
        }

        if (windParticles == null)
        {
            if (debugLogs)
            {
                Debug.LogError($"[FanTrap] No wind ParticleSystem assigned or found under '{name}'.", this);
            }

            enabled = false;
            return;
        }

        if (windTrigger == null)
        {
            windTrigger = GetComponentInChildren<CapsuleCollider>(true);
        }

        if (windTrigger == null)
        {
            if (debugLogs)
            {
                Debug.LogError($"[FanTrap] No CapsuleCollider assigned or found under '{name}'.", this);
            }

            enabled = false;
            return;
        }

        initialHeadRotation = transform.rotation;
        toggleParticlesAndSound(true);
        wasBlowing = false;
        cycleStartTime = Time.time;

        if (debugLogs)
        {
            Debug.Log(
                $"[FanTrap] Ready: trigger='{windTrigger.name}', wind emitter='{windParticles.name}' at local position {windParticles.transform.localPosition}; particle simulation={windParticles.main.simulationSpace}.",
                this);
        }
    }

    private void Update()
    {
        float cycleDuration = blowingDuration + pauseDuration;
        float cycleTime = Mathf.Repeat(Time.time - cycleStartTime, cycleDuration);
        bool isBlowing = cycleTime < blowingDuration;

        if (isBlowing != wasBlowing)
        {
            wasBlowing = isBlowing;
            toggleParticlesAndSound(isBlowing);

            if (debugLogs)
            {
                if (isBlowing)
                {
                    Debug.Log($"[FanTrap] Wind ON; sweeping from {-sweepAngle:0.#}° to {sweepAngle:0.#}°.", this);
                }
                else
                {
                    Debug.Log("[FanTrap] Wind OFF.", this);
                }
            }
        }

        if (isBlowing)
        {
            float sweepProgress = Mathf.PingPong(cycleTime / blowingDuration * 2f, 1f);
            currentSweepAngle = Mathf.Lerp(-sweepAngle, sweepAngle, sweepProgress);
            transform.localRotation =
                initialHeadRotation * Quaternion.Euler(0f, currentSweepAngle, 0f);
        }
    }

    private void OnValidate()
    {
        blowingDuration = Mathf.Max(0.1f, blowingDuration);
        pauseDuration = Mathf.Max(0.1f, pauseDuration);
        pushSpeed = Mathf.Max(0f, pushSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPushPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPushPlayer(other);
    }

    private void TryPushPlayer(Collider other)
    {
        if (!wasBlowing)
        {
            return;
        }

        SUPERCharacterAIO player = other.GetComponentInParent<SUPERCharacterAIO>();
        if (player == null)
        {
            if (debugLogs)
            {
                Debug.Log($"[FanTrap] Capsule touched '{other.name}', but no SUPERCharacterAIO was found in its parents.", this);
            }
            return;
        }

        if (player.gameObject.layer != playerLayer)
        {
            return;
        }

        Vector3 direction = Quaternion.AngleAxis(currentSweepAngle, transform.up) * windTrigger.transform.up;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 impulse = direction.normalized * pushSpeed;
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[FanTrap] Player '{player.name}' has no Rigidbody for wind force.", player);
            return;
        }

        if (rb.isKinematic)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[FanTrap] Player '{player.name}' has a kinematic Rigidbody; AddForce will not affect it.", player);
            }

            return;
        }

        rb.AddForce(impulse, ForceMode.Force);
        if (debugLogs)
        {
            Debug.Log($"[FanTrap] Capsule hit player '{player.name}'; push={impulse}, sweep={currentSweepAngle:0.#}°.", player);
        }
    }

    private void toggleParticlesAndSound(bool status)
    {
        if (status)
        {
            windParticles.Play(true);
            windSound.Play();
        }
        else
        {
            windParticles.Stop(true);
            windSound.Stop();
        }
    }
}
