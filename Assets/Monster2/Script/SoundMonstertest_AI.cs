using UnityEngine;
using UnityEngine.AI;

public class SoundMonstertest_AI : MonoBehaviour
{
    public enum State { Idle, Searching, Attack }
    public State currentState = State.Idle;

    [Header("移動速度")]
    public float searchSpeed = 3.5f;

    [Header("攻擊設定")]
    public float attackDistance = 1.8f; // 接觸或接近此距離即攻擊
    public float attackCooldown = 2f;    // 攻擊冷卻時間
    private float lastAttackTime;

    private NavMeshAgent agent;
    private MonsterHearing hearing;
    private Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        hearing = GetComponent<MonsterHearing>();
        animator = GetComponentInChildren<Animator>(); // 根據 YAML 結構，Animator 在子物件
    }

    void Update()
    {
        // 1. 偵測聲音
        bool heard = hearing.HasHeardSound(out float intensity, out Vector3 soundPos);
        
        if (heard)
        {
            currentState = State.Searching;
            agent.SetDestination(soundPos);
        }

        // 2. 狀態行為判斷
        switch (currentState)
        {
            case State.Idle:
                UpdateIdleState();
                break;
            case State.Searching:
                UpdateSearchingState();
                break;
        }

        // 3. 攻擊判定（每一幀檢查是否與玩家足夠近）
        CheckForPlayerContact();

        // 4. 同步動畫參數
        UpdateAnimator();
    }

    void UpdateIdleState()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void UpdateSearchingState()
    {
        agent.isStopped = false;
        agent.speed = searchSpeed;

        // 如果抵達聲音來源點，回到 Idle 狀態
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentState = State.Idle;
        }
    }

    void CheckForPlayerContact()
    {
        // 尋找標籤為 "Player" 的物件（請確保你的玩家物件 Tag 設為 Player）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= attackDistance && Time.time > lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }
    }

    void PerformAttack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack"); // 觸發攻擊動畫
        agent.isStopped = true;        // 攻擊時原地站立
        
        // 攻擊完後可視需求決定要停留在 Idle 還是繼續 Search
        // currentState = State.Idle; 
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // 同步速度 ( agent.velocity.magnitude 是物理移動速度 )
        animator.SetFloat("Speed", agent.velocity.magnitude);
        
        // 同步是否有聲音目標
        animator.SetBool("HeardSound", currentState == State.Searching);
    }

    // 可選：如果你想用碰撞偵測來攻擊，也可以改用這個
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PerformAttack();
        }
    }
}