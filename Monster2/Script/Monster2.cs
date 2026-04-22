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

    // 定義狀態對應的 Integer 數值
    // 請檢查 Animator 箭頭上的 Conditions 是否對應這些數字
    private const int IDLE = 0;
    private const int WALK = 1;
    private const int RUN = 2;
    private const int ATTACK = 3;

    void Start () {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        // 防止旋轉衝突，讓 NavMeshAgent 處理轉向
        agent.updateRotation = true; 
    }
    
    void Update () {
        // 計時器邏輯
        if (detectTimer > 0) {
            isDetected = true;
            detectTimer -= Time.deltaTime;
        } else {
            isDetected = false;
            
        }

        // 核心 AI 切換
        if (isDetected) {
            ChaseAndAttack();
        } else {
            Patrol();
        }
    }

    // 由手電筒腳本遠端呼叫
    public void BeIlluminated() {
        detectTimer = calmDownTime;
    }

    void ChaseAndAttack() {
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance > attackRange) {
            // 追逐玩家
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(player.position);
            
            UpdateAnimation(RUN);
        } else {
            // 進入攻擊
            Attack();
        }
    }

    void Attack() {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        // 攻擊時強制轉向玩家
        Vector3 targetDir = player.position - transform.position;
        targetDir.y = 0; // 保持水平旋轉
        if (targetDir != Vector3.zero) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * 10f);
        }

        UpdateAnimation(ATTACK);
    }

    void Patrol() {
        agent.isStopped = false;
        agent.speed = walkSpeed;

        // 簡單的巡邏：如果沒路徑或快走到了，就換一個點
        if (!agent.hasPath || agent.remainingDistance < 0.5f) {
             Vector3 randomDest = transform.position + new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
             agent.SetDestination(randomDest);
        }

        UpdateAnimation(WALK);
    }

    void UpdateAnimation(int stateValue) {
        // 同步更新你所有的 Animator 參數
        anim.SetInteger("state", stateValue);

        // 根據 stateValue (0:Idle, 1:Walk, 2:Run, 3:Attack) 分別設定對應參數
        anim.SetInteger("moving", (stateValue == 1) ? 1 : 0);
        anim.SetInteger("run",    (stateValue == 2) ? 1 : 0);
        anim.SetInteger("attack", (stateValue == 3) ? 1 : 0);
        
        // 如果 state 為 0 (Idle)，確保其他所有動畫參數都回歸 0
        if (stateValue == 0) {
            anim.SetInteger("moving", 0);
            anim.SetInteger("run", 0);
            anim.SetInteger("attack", 0);
        }
    }
}