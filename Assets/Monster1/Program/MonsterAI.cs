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

    [Header("Movement")]
    public NavMeshAgent agent;
    public float rotateSpeed = 8f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;

    [Header("Animation")]
    public Animator animator;

    [Header("Sound")]
    public AudioSource walkAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip walkSound;
    public AudioClip attackSound;

    private bool isStoppedByLight = false;

    void Start()
    {
        InitComponents();
        InitAudio();
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        isStoppedByLight = IsHitByFlashlight();

        if (isStoppedByLight)
        {
            StopMonsterByLight();
            return;
        }

        if (!CanSeePlayer())
        {
            Idle();
            return;
        }

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

    void InitComponents()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void InitAudio()
    {
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

    bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

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

        if (Physics.Raycast(
            flashlight.position,
            directionToMonster.normalized,
            out RaycastHit hit,
            distanceToMonster))
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
        if (agent == null || player == null)
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);

        PlayWalkSound();

        SetAnimation(
            isLit: false,
            isWalking: true,
            isAttacking: false
        );
    }

    void AttackPlayer()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        StopWalkSound();
        FacePlayer();

        SetAnimation(
            isLit: false,
            isWalking: false,
            isAttacking: true
        );

        if (attackTimer <= 0f)
        {
            PlayAttackSound();

            if (animator != null)
            {
                animator.SetTrigger("AttackTrigger");
            }

            Debug.Log("Attack Player");

            attackTimer = attackCooldown;
        }
    }

    void StopMonsterByLight()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        StopWalkSound();

        SetAnimation(
            isLit: true,
            isWalking: false,
            isAttacking: false
        );
    }

    void Idle()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        StopWalkSound();

        SetAnimation(
            isLit: false,
            isWalking: false,
            isAttacking: false
        );
    }

    void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    void SetAnimation(bool isLit, bool isWalking, bool isAttacking)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("IsLit", isLit);
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsAttacking", isAttacking);
    }

    void PlayWalkSound()
    {
        if (walkAudioSource == null || walkSound == null)
        {
            return;
        }

        if (!walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }
    }

    void StopWalkSound()
    {
        if (walkAudioSource == null)
        {
            return;
        }

        if (walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }

    void PlayAttackSound()
    {
        if (sfxAudioSource == null || attackSound == null)
        {
            return;
        }

        sfxAudioSource.PlayOneShot(attackSound);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}