using UnityEngine;
using UnityEngine.AI;

public class Monster2 : MonoBehaviour {
    private Animator anim;
    private NavMeshAgent agent; 
    
    [Header("目標設定")]
    public Transform player;      

    [Header("移動數值")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.5f;
    public float attackRange = 2.2f; 

    [Header("攻擊命中設定")]
    public float hitRange = 2.5f;        // 【新增】真正揮爪時能打到玩家的極限距離
    public float hitAngle = 60f;         // 【新增】真正揮爪時能打到玩家的角度扇形範圍（度）

    [Header("狀態冷靜時間")]
    public float calmDownTime = 3.0f; 
    private float detectTimer = 0f;  
    private bool isDetected = false;

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip wanderSound;   // 走路/巡邏聲
    public AudioClip spottedSound;  // 突然發現玩家（照到光）的叫聲
    public AudioClip chaseSound;    // 追逐時的急促聲
    public AudioClip attackSound;   // 攻擊聲

    private int lastState = -1;     // 紀錄上一個狀態，防止重複執行
    private bool hasPlayedSpotted = false; // 確保被照到時的尖叫只播一次

    // 狀態常數
    private const int IDLE = 0;
    private const int WALK = 1;
    private const int RUN = 2;
    private const int ATTACK = 3;

    void Start () {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true; 

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
    
    void Update () {
        if (agent == null || !agent.isOnNavMesh) return;

        if (detectTimer > 0) {
            isDetected = true;
            detectTimer -= Time.deltaTime;
        } else {
            isDetected = false;
            hasPlayedSpotted = false; 
        }

        if (isDetected) {
            ChaseAndAttack();
        } else {
            Patrol();
        }
    }

    public void BeIlluminated() {
        detectTimer = calmDownTime;
    }

    void ChaseAndAttack() {
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance > attackRange) {
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(player.position);
            UpdateAnimation(RUN);
        } else {
            Attack();
        }
    }

    void Attack() {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        Vector3 targetDir = player.position - transform.position;
        targetDir.y = 0;
        if (targetDir != Vector3.zero) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * 10f);
        }

        UpdateAnimation(ATTACK);
    }

    // ====================================================================
    // 【核心新增】由「動畫事件 (Animation Event)」在怪物揮下爪子的精準瞬間呼叫
    // ====================================================================
    public void OnAttackHit() {
        if (player == null) return;

        // 1. 計算當前玩家跟怪物的距離
        float distance = Vector3.Distance(transform.position, player.position);

        // 2. 計算玩家是否在怪物的正前方扇形範圍內（防止玩家繞背躲開）
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        // 3. 【準確命中判斷】：只有距離夠近、且角度在前方，才算真正打中玩家！
        if (distance <= hitRange && angle <= (hitAngle / 2f)) {
            
            Debug.Log("💥 怪物精準命中玩家！手電筒即將關閉！");

            // 這裡可以執行玩家扣血，例如：player.GetComponent<PlayerHealth>().TakeDamage(20);

            // 呼叫手電筒腳本，開始延遲 1 秒熄滅的處理
            UltimateFlashlightController flashlight = player.GetComponentInChildren<UltimateFlashlightController>();
            if (flashlight != null) {
                flashlight.RequestTurnOff();
            }
        } else {
            Debug.Log("💨 玩家成功走位，躲開了怪物的攻擊！");
        }
    }

    void Patrol() {
        agent.isStopped = false;
        agent.speed = walkSpeed;

        if (!agent.hasPath || agent.remainingDistance < 0.5f) {
             Vector3 randomDest = transform.position + new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
             agent.SetDestination(randomDest);
        }
        UpdateAnimation(WALK);
    }

    void UpdateAnimation(int stateValue) {
        if (stateValue == lastState) return;
        lastState = stateValue;

        HandleSound(stateValue);

        anim.SetInteger("state", stateValue);
        anim.SetInteger("moving", (stateValue == 1) ? 1 : 0);
        anim.SetInteger("run",    (stateValue == 2) ? 1 : 0);
        anim.SetInteger("attack", (stateValue == 3) ? 1 : 0);
    }

    void HandleSound(int state) {
        if (audioSource == null) return;

        switch (state) {
            case WALK:
                audioSource.clip = wanderSound;
                audioSource.loop = true;
                audioSource.Play();
                break;

            case RUN:
                if (!hasPlayedSpotted && spottedSound != null) {
                    audioSource.PlayOneShot(spottedSound);
                    hasPlayedSpotted = true;
                }
                audioSource.clip = chaseSound;
                audioSource.loop = true;
                audioSource.Play();
                break;

            case ATTACK:
                if (attackSound != null) {
                    audioSource.PlayOneShot(attackSound);
                }
                break;

            case IDLE:
                audioSource.Stop();
                break;
        }
    }
}