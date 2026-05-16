using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Detection")]
    public float detectRange = 10f;
    public float attackRange = 1.5f;
    public LayerMask obstacleLayer;

    [Header("Flashlight")]
    public Transform flashlight;
    public Light flashlightLight;
    public float flashlightStopAngle = 20f;
    public LayerMask monsterLayer;

    [Header("Movement")]
    public NavMeshAgent agent;

    [Header("Animation")]
    public Animator animator;

    [Header("Sound")]
    public AudioSource walkAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip walkSound;
    public AudioClip attackSound;

    private bool isStoppedByLight = false;
    private bool hasPlayedAttackSound = false;

    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (walkAudioSource != null)
        {
            walkAudioSource.clip = walkSound;
            walkAudioSource.loop = true;
            walkAudioSource.playOnAwake = false;
        }

        if (sfxAudioSource != null)
        {
            sfxAudioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        isStoppedByLight = IsHitByFlashlight();

        if (isStoppedByLight)
        {
            StopMonsterByLight();
            return;
        }

        if (CanSeePlayer())
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                AttackPlayer();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            Idle();
        }
    }

    bool CanSeePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectRange)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 direction = target - origin;

        if (Physics.Raycast(origin, direction.normalized, distance, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    bool IsHitByFlashlight()
    {
        if (flashlight == null || flashlightLight == null)
        {
            return false;
        }

        if (!flashlightLight.enabled)
        {
            return false;
        }

        Vector3 monsterPoint = transform.position + Vector3.up * 1.2f;
        Vector3 directionToMonster = monsterPoint - flashlight.position;

        float distanceToMonster = directionToMonster.magnitude;

        if (distanceToMonster > flashlightLight.range)
        {
            return false;
        }

        float angle = Vector3.Angle(flashlight.forward, directionToMonster);

        if (angle > flashlightStopAngle)
        {
            return false;
        }

        if (Physics.Raycast(flashlight.position, directionToMonster.normalized, out RaycastHit hit, distanceToMonster))
        {
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);

        PlayWalkSound();

        hasPlayedAttackSound = false;

        if (animator != null)
        {
            animator.SetBool("IsLit", false);
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsAttacking", false);
        }
    }

    void StopMonsterByLight()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        StopWalkSound();
        hasPlayedAttackSound = false;

        if (animator != null)
        {
            animator.SetBool("IsLit", true);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAttacking", false);
        }
    }

    void AttackPlayer()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        StopWalkSound();

        if (!hasPlayedAttackSound)
        {
            PlayAttackSound();
            hasPlayedAttackSound = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsLit", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAttacking", true);
        }

        Debug.Log("Attack Player");
    }

    void Idle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        StopWalkSound();
        hasPlayedAttackSound = false;

        if (animator != null)
        {
            animator.SetBool("IsLit", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAttacking", false);
        }
    }

    void PlayWalkSound()
    {
        if (walkAudioSource != null && walkSound != null)
        {
            if (!walkAudioSource.isPlaying)
            {
                walkAudioSource.Play();
            }
        }
    }

    void StopWalkSound()
    {
        if (walkAudioSource != null && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }

    void PlayAttackSound()
    {
        if (sfxAudioSource != null && attackSound != null)
        {
            sfxAudioSource.PlayOneShot(attackSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}