using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MonsterHearing2))]
public class SoundMonsterAI2 : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Alert,
        Chase,
        Attack
    }

    [Header("目標")]
    public Transform player;

    [Header("狀態")]
    public State currentState = State.Patrol;

    [Header("移動速度")]
    public float patrolSpeed = 2f;
    public float alertSpeed = 3f;
    public float chaseSpeed = 6f;

    [Header("巡邏設定")]
    public float patrolRadius = 12f;
    public float waypointArriveDistance = 1f;

    [Header("追擊設定")]
    public float chaseDistance = 30f;
    public float disengageDistance = 50f;

    [Header("攻擊 / 停止距離")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;

    [Header("追擊放棄設定")]
    public float losePlayerAfterNoSound = 1f;

    [Tooltip("只有大於這個強度的聲音，才會刷新追擊時間。建議比 Breath Intensity 大。")]
    public float chaseKeepIntensity = 3f;

    [Tooltip("大於這個強度時，怪物會直接進入追擊。")]
    public float chaseTriggerIntensity = 5f;

    [Header("警戒逾時")]
    public float alertTimeout = 3f;

    [Header("動畫狀態名稱")]
    public string idleAnim = "Idle";
    public string walk1Anim = "Walk1";
    public string walk2Anim = "Walk2";
    public string biteAnim = "Bite";

    NavMeshAgent agent;
    MonsterHearing2 hearing;
    Renderer rend;
    Animator animator;

    Vector3 lastSoundPos;
    bool hasLastSoundPos;

    float lastStrongSoundTime;
    float alertTimer;
    float attackTimer;

    Vector3 patrolTarget;
    bool hasPatrolTarget;

    string currentAnim = "";

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hearing = GetComponent<MonsterHearing2>();
        rend = GetComponentInChildren<Renderer>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        agent.stoppingDistance = attackRange;
        EnterPatrol();
    }

    void Update()
    {
        bool heard = hearing.HasHeardSound(out float intensity, out Vector3 soundPos);

        if (heard)
        {
            lastSoundPos = soundPos;
            hasLastSoundPos = true;

            if (intensity >= chaseKeepIntensity)
            {
                lastStrongSoundTime = Time.time;
            }

            Debug.Log($"[MONSTER] Heard sound intensity:{intensity:F1} pos:{soundPos}");

            HandleSoundInput(intensity, soundPos);
        }

        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                break;

            case State.Alert:
                UpdateAlert(heard);
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Attack:
                UpdateAttack();
                break;
        }
    }

    void HandleSoundInput(float intensity, Vector3 soundPos)
    {
        float distToSound = Vector3.Distance(transform.position, soundPos);

        if (currentState == State.Patrol)
        {
            if (ShouldChase(intensity, distToSound))
            {
                EnterChase();
            }
            else
            {
                EnterAlert(soundPos);
            }
        }
        else if (currentState == State.Alert)
        {
            if (ShouldChase(intensity, distToSound))
            {
                EnterChase();
            }
            else
            {
                agent.SetDestination(soundPos);
            }
        }
        else if (currentState == State.Chase)
        {
            if (player != null)
            {
                agent.SetDestination(player.position);
            }
        }
    }

    bool ShouldChase(float intensity, float distToSound)
    {
        if (intensity >= chaseTriggerIntensity)
        {
            return true;
        }

        if (distToSound <= chaseDistance && intensity >= chaseKeepIntensity)
        {
            return true;
        }

        return false;
    }

    void UpdatePatrol()
    {
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f;

        PlayAnim(walk1Anim);

        if (!hasPatrolTarget || (!agent.pathPending && agent.remainingDistance <= waypointArriveDistance))
        {
            SetRandomPatrolPoint();
        }
    }

    void UpdateAlert(bool heardThisFrame)
    {
        agent.speed = alertSpeed;
        agent.stoppingDistance = 0f;

        if (agent.hasPath && agent.remainingDistance > waypointArriveDistance)
        {
            PlayAnim(walk2Anim);
        }
        else
        {
            PlayAnim(idleAnim);
        }

        if (heardThisFrame)
        {
            alertTimer = 0f;
        }
        else
        {
            alertTimer += Time.deltaTime;

            if (alertTimer >= alertTimeout)
            {
                EnterPatrol();
                return;
            }
        }

        if (hasLastSoundPos && !agent.pathPending && agent.remainingDistance <= waypointArriveDistance)
        {
            agent.ResetPath();
            PlayAnim(idleAnim);
        }
    }

    void UpdateChase()
    {
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange;

        if (Time.time - lastStrongSoundTime >= losePlayerAfterNoSound)
        {
            Debug.Log("[MONSTER] Lost player: no strong sound for 1 second.");
            EnterAlert(lastSoundPos);
            return;
        }

        if (player == null)
        {
            if (hasLastSoundPos)
            {
                agent.SetDestination(lastSoundPos);
                PlayAnim(walk2Anim);
            }

            return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= attackRange)
        {
            EnterAttack();
            return;
        }

        agent.SetDestination(player.position);
        FacePlayerSoft();
        PlayAnim(walk2Anim);

        if (distToPlayer > disengageDistance)
        {
            EnterAlert(lastSoundPos);
        }
    }

    void UpdateAttack()
    {
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        FacePlayer();

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;

            if (Time.time - lastStrongSoundTime >= losePlayerAfterNoSound)
            {
                EnterAlert(lastSoundPos);
                return;
            }

            if (player == null)
            {
                EnterAlert(lastSoundPos);
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, player.position);

            if (distToPlayer > attackRange)
            {
                EnterChase();
                return;
            }

            PlayAnim(biteAnim);
        }
    }

    void EnterPatrol()
    {
        currentState = State.Patrol;

        alertTimer = 0f;
        hasPatrolTarget = false;

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0f;

        PlayAnim(walk1Anim);

        if (rend != null)
        {
            rend.material.color = Color.white;
        }

        Debug.Log("[MONSTER] State = Patrol");
    }

    void EnterAlert(Vector3 target)
    {
        currentState = State.Alert;

        alertTimer = 0f;

        agent.speed = alertSpeed;
        agent.stoppingDistance = 0f;

        if (hasLastSoundPos)
        {
            agent.SetDestination(target);
            PlayAnim(walk2Anim);
        }
        else
        {
            PlayAnim(idleAnim);
        }

        if (rend != null)
        {
            rend.material.color = new Color(1f, 0.5f, 0f);
        }

        Debug.Log("[MONSTER] State = Alert");
    }

    void EnterChase()
    {
        currentState = State.Chase;

        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange;

        lastStrongSoundTime = Time.time;

        if (player != null)
        {
            agent.SetDestination(player.position);
        }
        else if (hasLastSoundPos)
        {
            agent.SetDestination(lastSoundPos);
        }

        PlayAnim(walk2Anim);

        if (rend != null)
        {
            rend.material.color = Color.red;
        }

        Debug.Log("[MONSTER] State = Chase");
    }

    void EnterAttack()
    {
        currentState = State.Attack;

        agent.ResetPath();
        agent.velocity = Vector3.zero;

        attackTimer = 0f;

        FacePlayer();
        PlayAnim(biteAnim);

        if (rend != null)
        {
            rend.material.color = new Color(0.4f, 0f, 0f);
        }

        Debug.Log("[MONSTER] State = Attack");
    }

    void SetRandomPatrolPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir += transform.position;
            randomDir.y = transform.position.y;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolTarget = hit.position;
                hasPatrolTarget = true;
                agent.SetDestination(patrolTarget);
                return;
            }
        }

        agent.ResetPath();
        PlayAnim(idleAnim);
    }

    void FacePlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = lookRotation;
    }

    void FacePlayerSoft()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * 5f
        );
    }

    void PlayAnim(string animName)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(animName)) return;
        if (currentAnim == animName) return;

        currentAnim = animName;
        animator.CrossFade(animName, 0.15f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, disengageDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (hasLastSoundPos)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastSoundPos, 0.4f);
            Gizmos.DrawLine(transform.position, lastSoundPos);
        }
    }
}