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
    public AudioSource audioSource;
    public AudioClip detectSound;
    public AudioClip attackSound;

    private NavMeshAgent agent;
    private Animator animator;

    private bool hasDetectedPlayer = false; // 防止一直重播 detect sound
    private bool isAttackingNow = false;    // 防止 attack sound 每幀播放

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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
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
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();

            animator.SetBool("isFrozen", true);
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", false);

            hasDetectedPlayer = false;
            isAttackingNow = false;

            return;
        }

        animator.SetBool("isFrozen", false);

        // ===== 不在範圍 =====
        if (distance > detectionDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();

            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", false);

            hasDetectedPlayer = false;
            isAttackingNow = false;

            return;
        }

        // ===== 第一次發現玩家 → 播音效 =====
        if (!hasDetectedPlayer)
        {
            PlaySound(detectSound);
            hasDetectedPlayer = true;
        }

        // ===== 攻擊 =====
        if (distance <= attackDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();

            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", true);

            if (!isAttackingNow)
            {
                PlaySound(attackSound);
                isAttackingNow = true;
            }

            AttackPlayer();
        }
        // ===== 追擊 =====
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            animator.SetBool("isMoving", true);
            animator.SetBool("isAttacking", false);

            isAttackingNow = false;
        }

        // ===== 面向玩家 =====
        if (distance <= facePlayerDistance)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }

    void AttackPlayer()
    {
        // 之後加扣血
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}