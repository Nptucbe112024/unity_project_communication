using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Distance Settings")]
    public float detectionDistance = 10f;
    public float attackDistance = 2f;
    public float facePlayerDistance = 5f;

    [Header("State")]
    public bool isFrozen = false;

    [Header("Audio")]
    public AudioSource footstepSource; // �}�B�n
    public AudioSource sfxSource;      // ��������

    public AudioClip footstepSound;
    public AudioClip attackSound;

    private NavMeshAgent agent;
    private Animator animator;

    private bool isAttackingNow = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // �}�B�n��l��
        if (footstepSource != null)
        {
            footstepSource.clip = footstepSound;
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (agent == null || animator == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // ===== Frozen =====
        if (isFrozen)
        {
            StopMovement();

            animator.SetBool("isFrozen", true);
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", false);

            StopFootsteps();

            isAttackingNow = false;
            return;
        }

        animator.SetBool("isFrozen", false);

        // ===== ���b�d�� =====
        if (distance > detectionDistance)
        {
            StopMovement();

            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", false);

            StopFootsteps();

            isAttackingNow = false;
            return;
        }

        // ===== ���� =====
        if (distance <= attackDistance)
        {
            StopMovement();

            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", true);

            StopFootsteps();

            if (!isAttackingNow)
            {
                PlayAttackSound();
                isAttackingNow = true;
            }

            AttackPlayer();
        }
        // ===== �l�� =====
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            animator.SetBool("isMoving", true);
            animator.SetBool("isAttacking", false);

            PlayFootsteps();

            isAttackingNow = false;
        }

        // ===== ���V���a =====
        if (distance <= facePlayerDistance)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * 5f
                );
            }
        }
    }

    void StopMovement()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    void PlayFootsteps()
    {
        Debug.Log(name + " PlayFootsteps");
        if (footstepSource != null &&
            footstepSound != null &&
            !footstepSource.isPlaying)
        {
            footstepSource.Play();
        }
    }

    void StopFootsteps()
    {
        if (footstepSource != null && footstepSource.isPlaying)
        {
            Debug.Log(name + " StopFootsteps");
            footstepSource.Stop();
        }
    }

    void PlayAttackSound()
    {
        if (sfxSource != null && attackSound != null)
        {
            sfxSource.PlayOneShot(attackSound);
        }
    }

    void AttackPlayer()
    {
        // ����[����
    }

    public void SetFrozen(bool value)
    {
        if (isFrozen != value)
        {
            Debug.Log(name + " SetFrozen = " + value);
        }

        isFrozen = value;
    }

    void OnDrawGizmosSelected()
    {
        return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}