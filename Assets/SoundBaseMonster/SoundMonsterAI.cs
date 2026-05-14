using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MonsterHearing))]
public class SoundMonsterAI : MonoBehaviour
{
    public enum State { Patrol, Alert, Chase }

    [Header("狀態")]
    public State currentState = State.Patrol;

    [Header("移動速度")]
    public float patrolSpeed = 2f;
    public float alertSpeed  = 3f;
    public float chaseSpeed  = 5f;

    [Header("巡邏設定")]
    public float patrolRadius     = 12f;  // 隨機點取樣半徑
    public float waypointArriveDistance = 1f;

    [Header("狀態切換距離")]
    public float chaseDistance = 8f;    // 聲源距離 < 此值 → 進攻
    public float disengageDistance = 14f; // 進攻中距離 > 此值 → 降回警戒

    [Header("警戒逾時")]
    public float alertTimeout = 5f;

    // 內部
    NavMeshAgent   agent;
    MonsterHearing hearing;
    Renderer       rend;

    Vector3 lastSoundPos;
    bool    hasLastSoundPos;
    float   alertTimer;
    Vector3 patrolTarget;
    bool    hasPatrolTarget;

    void Awake()
    {
        agent   = GetComponent<NavMeshAgent>();
        hearing = GetComponent<MonsterHearing>();
        rend    = GetComponent<Renderer>();
        rend.material.color = Color.red;
    }

    void Update()
    {
        // 每幀先收音
        bool heard = hearing.HasHeardSound(out float intensity, out Vector3 soundPos);

        if (heard)
        {
            lastSoundPos    = soundPos;
            hasLastSoundPos = true;
            HandleSoundInput(intensity, soundPos);
        }

        switch (currentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Alert:  UpdateAlert(heard);  break;
            case State.Chase:  UpdateChase();  break;
        }
    }

    // ─── 聲音觸發邏輯 ──────────────────────────────

    void HandleSoundInput(float intensity, Vector3 soundPos)
    {
        float dist = Vector3.Distance(transform.position, soundPos);

        if (currentState == State.Patrol)
        {
            // 巡邏中：任何聲音 → 警戒
            EnterAlert(soundPos);
        }
        else if (currentState == State.Alert)
        {
            // 警戒中：距離夠近 → 進攻
            if (dist < chaseDistance)
                EnterChase(soundPos);
        }
        else if (currentState == State.Chase)
        {
            // 進攻中：持續更新目標
            agent.SetDestination(soundPos);
        }
    }

    // ─── 狀態更新 ──────────────────────────────────

    void UpdatePatrol()
    {
        agent.speed = patrolSpeed;

        if (!hasPatrolTarget || agent.remainingDistance < waypointArriveDistance)
            SetRandomPatrolPoint();
    }

    void UpdateAlert(bool heardThisFrame)
    {
        agent.speed = alertSpeed;

        if (heardThisFrame)
        {
            alertTimer = 0f; // 聽到聲音重置計時
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

        // 抵達上次聲源後原地待機（等計時歸零）
        if (hasLastSoundPos && agent.remainingDistance < waypointArriveDistance)
            agent.ResetPath();
    }

    void UpdateChase()
    {
        agent.speed = chaseSpeed;

        if (!hasLastSoundPos) return;

        // 距離拉開 → 降級
        float dist = Vector3.Distance(transform.position, lastSoundPos);
        if (dist > disengageDistance)
        {
            EnterAlert(lastSoundPos);
        }
    }

    // ─── 狀態切換 ──────────────────────────────────

    void EnterPatrol()
    {
        currentState   = State.Patrol;
        hasPatrolTarget = false;
        alertTimer     = 0f;
        rend.material.color = Color.white;
        MonsterDebugger.Log(gameObject.name, State.Patrol, "5 秒無聲，回巡邏");
    }

    void EnterAlert(Vector3 target)
    {
        currentState = State.Alert;
        alertTimer   = 0f;
        agent.SetDestination(target);
        rend.material.color = new Color(1f, 0.5f, 0f);
        MonsterDebugger.Log(gameObject.name, State.Alert, $"往聲源移動 {target}");
    }

    void EnterChase(Vector3 target)
    {
        currentState = State.Chase;
        agent.SetDestination(target);
        rend.material.color = new Color(0.6f, 0f, 0f);
        MonsterDebugger.Log(gameObject.name, State.Chase, $"距離近，進攻！目標 {target}");
    }

    // ─── 巡邏隨機點 ────────────────────────────────

    void SetRandomPatrolPoint()
    {
        // NavMesh.SamplePosition 在半徑內找最近可行走點
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir    = Random.insideUnitSphere * patrolRadius;
            randomDir           += transform.position;
            randomDir.y          = transform.position.y;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolTarget    = hit.position;
                hasPatrolTarget = true;
                agent.SetDestination(patrolTarget);
                return;
            }
        }
        // 10 次都失敗則原地等一下
        agent.ResetPath();
    }

    // ─── Gizmos（Scene 視圖輔助線）─────────────────

    void OnDrawGizmosSelected()
    {
        // 巡邏半徑
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // 進攻距離
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // 脫戰距離
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, disengageDistance);

        // 當前目標
        if (hasPatrolTarget && currentState == State.Patrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(patrolTarget, 0.3f);
            Gizmos.DrawLine(transform.position, patrolTarget);
        }
        if (hasLastSoundPos)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastSoundPos, 0.4f);
            Gizmos.DrawLine(transform.position, lastSoundPos);
        }
    }
}