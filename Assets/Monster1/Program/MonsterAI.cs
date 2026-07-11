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

    [Tooltip("實際速度低於這個值時，不播放走路動畫")]
    public float minimumWalkVelocity = 0.08f;

    [Tooltip("走路動畫原本對應的移動速度，用來計算動畫播放倍率")]
    public float animationReferenceSpeed = 2f;

    [Tooltip("走路動畫最慢播放倍率")]
    public float minimumWalkAnimationSpeed = 0.75f;

    [Tooltip("走路動畫最快播放倍率")]
    public float maximumWalkAnimationSpeed = 1.5f;

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

    private bool currentIsLit = false;
    private bool currentIsWalking = false;
    private bool currentIsAttacking = false;

    void Start()
    {
        InitComponents();
        InitAudio();

        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        isStoppedByLight = IsHitByFlashlight();

        if (isStoppedByLight)
        {
            StopMonsterByLight();
            UpdateAnimationFromMovement();
            return;
        }

        if (!CanSeePlayer())
        {
            Idle();
            UpdateAnimationFromMovement();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }

        UpdateAnimationFromMovement();
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

        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
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

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > detectRange)
        {
            return false;
        }

        Vector3 origin =
            transform.position + Vector3.up * 1.5f;

        Vector3 target =
            player.position + Vector3.up * 1.0f;

        Vector3 direction = target - origin;

        if (Physics.Raycast(
            origin,
            direction.normalized,
            distance,
            obstacleLayer))
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

        Vector3 monsterPoint =
            transform.position + Vector3.up * 1.2f;

        Vector3 directionToMonster =
            monsterPoint - flashlight.position;

        float distanceToMonster =
            directionToMonster.magnitude;

        if (distanceToMonster > flashlightLight.range)
        {
            return false;
        }

        float angle = Vector3.Angle(
            flashlight.forward,
            directionToMonster
        );

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
            if (hit.transform != transform &&
                !hit.transform.IsChildOf(transform))
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

        currentIsLit = false;
        currentIsAttacking = false;
    }

    void AttackPlayer()
    {
        StopAgentImmediately();

        StopWalkSound();
        FacePlayer();

        currentIsLit = false;
        currentIsWalking = false;
        currentIsAttacking = true;

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
        StopAgentImmediately();
        StopWalkSound();

        currentIsLit = true;
        currentIsWalking = false;
        currentIsAttacking = false;
    }

    void Idle()
    {
        StopAgentImmediately();
        StopWalkSound();

        currentIsLit = false;
        currentIsWalking = false;
        currentIsAttacking = false;
    }

    void StopAgentImmediately()
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    void UpdateAnimationFromMovement()
    {
        if (animator == null)
        {
            return;
        }

        bool isActuallyMoving = false;
        float actualSpeed = 0f;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh &&
            !agent.isStopped)
        {
            actualSpeed = agent.velocity.magnitude;

            bool hasPath =
                agent.hasPath &&
                !agent.pathPending;

            bool hasDistanceToTravel =
                agent.remainingDistance >
                agent.stoppingDistance + 0.05f;

            isActuallyMoving =
                hasPath &&
                hasDistanceToTravel &&
                actualSpeed >= minimumWalkVelocity;
        }

        if (currentIsLit || currentIsAttacking)
        {
            isActuallyMoving = false;
        }

        currentIsWalking = isActuallyMoving;

        animator.SetBool("IsLit", currentIsLit);
        animator.SetBool("IsWalking", currentIsWalking);
        animator.SetBool("IsAttacking", currentIsAttacking);

        UpdateAnimatorPlaybackSpeed(actualSpeed);

        if (currentIsWalking)
        {
            PlayWalkSound();
        }
        else
        {
            StopWalkSound();
        }
    }

    void UpdateAnimatorPlaybackSpeed(float actualSpeed)
    {
        if (animator == null)
        {
            return;
        }

        if (!currentIsWalking)
        {
            animator.speed = 1f;
            return;
        }

        float referenceSpeed = Mathf.Max(
            animationReferenceSpeed,
            0.01f
        );

        float animationSpeedMultiplier =
            actualSpeed / referenceSpeed;

        animationSpeedMultiplier = Mathf.Clamp(
            animationSpeedMultiplier,
            minimumWalkAnimationSpeed,
            maximumWalkAnimationSpeed
        );

        animator.speed = animationSpeedMultiplier;
    }

    void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
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
        Gizmos.DrawWireSphere(
            transform.position,
            detectRange
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}