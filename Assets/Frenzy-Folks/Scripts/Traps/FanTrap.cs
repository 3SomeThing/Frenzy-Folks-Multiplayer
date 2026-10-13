using System.Collections.Generic;
using Frenzy_Folks.Scripts.Player;
using UnityEngine;

public class FanTrap : MonoBehaviour
{
    [SerializeField] private ParticleSystem windParticles;
    [SerializeField] private AudioSource windSound;
    [SerializeField] private CapsuleCollider windTrigger;
    [SerializeField, Min(0.1f)] private float blowingDuration = 7f;
    [SerializeField, Min(0.1f)] private float pauseDuration = 2f;
    [SerializeField, Range(0f, 90f)] private float sweepAngle = 45f;
    [SerializeField, Min(0.1f)] private float pushInterval = 0.25f;
    [SerializeField, Min(0f)] private float pushSpeed = 8f;
    [SerializeField] private bool debugLogs = true;

    private readonly Dictionary<FrenzyThirdPersonController, float> nextPushTime =
        new Dictionary<FrenzyThirdPersonController, float>();
    private Quaternion initialHeadRotation;
    private float cycleStartTime;
    private float currentSweepAngle;
    private int playerLayer;
    private bool wasBlowing;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
        {
            Debug.LogError("[FanTrap] Unity layer 'Player' does not exist. Add it in Project Settings > Tags and Layers.", this);
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

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        initialHeadRotation = transform.rotation;
        ParticleSystem.MainModule windMain = windParticles.main;
        toggleParticlesAndSound(true);
        wasBlowing = false;
        cycleStartTime = Time.time;
        
        if (debugLogs)
        {
            Debug.Log($"[FanTrap] Ready: trigger='{windTrigger.name}', wind emitter='{windParticles.name}' at local position {windParticles.transform.localPosition}; particle simulation={windParticles.main.simulationSpace}.", this);
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
            if (isBlowing)
            {
                toggleParticlesAndSound(true);
                
                if (debugLogs)
                {
                    Debug.Log($"[FanTrap] Wind ON; sweeping from {-sweepAngle:0.#}° to {sweepAngle:0.#}°.", this);
                }
            }
            else
            {
                toggleParticlesAndSound(false);
                
                if (debugLogs)
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
        pushInterval = Mathf.Max(0.1f, pushInterval);
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

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != playerLayer)
        {
            return;
        }

        FrenzyThirdPersonController player = other.GetComponentInParent<FrenzyThirdPersonController>();
        if (player != null)
        {
            nextPushTime.Remove(player);
        }
    }

    private void TryPushPlayer(Collider other)
    {
        if (other.gameObject.layer != playerLayer)
        {
            return;
        }

        if (!wasBlowing)
        {
            return;
        }

        FrenzyThirdPersonController player = other.GetComponentInParent<FrenzyThirdPersonController>();
        if (player == null)
        {
            if (debugLogs)
            {
                Debug.Log($"[FanTrap] Capsule touched '{other.name}', but no FrenzyThirdPersonController was found in its parents.", this);
            }
            return;
        }

        if (nextPushTime.TryGetValue(player, out float nextTime) && Time.time < nextTime)
        {
            return;
        }

        nextPushTime[player] = Time.time + pushInterval;
        Vector3 direction = Quaternion.AngleAxis(currentSweepAngle, transform.up) * windTrigger.transform.up;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 impulse = direction.normalized * pushSpeed;
        player.ApplyExternalImpulse(impulse);
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
