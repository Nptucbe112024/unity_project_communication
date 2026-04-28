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

        // 自動抓取 AudioSource 如果沒拉的話
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }
    
    void Update () {
        // 安全檢查
        if (agent == null || !agent.isOnNavMesh) return;

        // 計時器邏輯
        if (detectTimer > 0) {
            isDetected = true;
            detectTimer -= Time.deltaTime;
        } else {
            isDetected = false;
            hasPlayedSpotted = false; // 失去目標後重置尖叫開關
        }

        // 核心 AI 切換
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
        // 如果狀態沒變，就直接跳過，不重複執行動畫和音效邏輯
        if (stateValue == lastState) return;
        lastState = stateValue;

        // --- 音效處理邏輯 ---
        HandleSound(stateValue);

        // --- 動畫處理邏輯 ---
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
                // 照到光瞬間尖叫 (只播一次)
                if (!hasPlayedSpotted && spottedSound != null) {
                    audioSource.PlayOneShot(spottedSound);
                    hasPlayedSpotted = true;
                }
                // 切換成追逐持續音
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