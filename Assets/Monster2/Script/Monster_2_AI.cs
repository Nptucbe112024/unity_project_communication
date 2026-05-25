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
    public float hitRange = 2.5f;        // 真正揮爪時能打到玩家的極限距離
    public float hitAngle = 60f;         // 真正揮爪時能打到玩家的角度扇形範圍（度）

    [Header("狀態冷靜時間")]
    public float calmDownTime = 3.0f; 
    private float detectTimer = 0f;  
    private bool isDetected = false;

    // ====== 【鎖定玩家移動與轉向變數】 ======
    private bool isFreezingPlayer = false; // 目前是否正在定身玩家
    private Vector3 frozenPlayerPos;       // 被抓住時玩家的固定位置

    [Header("視角對準設定")]
    public float lookAtSpeed = 12.0f;    // 鏡頭強行轉向怪物的速度（數值越大轉越快）

    // ====== 【鏡頭抖動設定】 ======
    [Header("玩家受撞擊抖動設定")]
    public float shakeIntensity = 0.15f; // 抖動的劇烈程度（數值越大晃越大，建議 0.1 ~ 0.3）
    private Vector3 originalCamLocalPos;  // 記錄相機原本的局部座標
    private bool hasSavedCamPos = false;  // 是否已經記錄相機初始位置

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

        // 計時器與定身邏輯
        if (detectTimer > 0) {
            isDetected = true;
            detectTimer -= Time.deltaTime;

            // ====== 【核心修改：玩家原地卡死面朝怪物並抖動，怪物留在原地不移動】 ======
            if (isFreezingPlayer && player != null) {
                // 1. 玩家位置處理：死死固定在原地
                player.position = frozenPlayerPos;

                Camera playerCam = player.GetComponentInChildren<Camera>();

                // 2. 玩家轉向處理：強迫臉朝怪物
                Vector3 lookDir = (transform.position + Vector3.up * 1.2f) - player.position;
                if (lookDir != Vector3.zero) {
                    Vector3 lookDirYaw = lookDir;
                    lookDirYaw.y = 0;
                    if (lookDirYaw != Vector3.zero) {
                        player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(lookDirYaw), Time.deltaTime * lookAtSpeed);
                    }

                    if (playerCam != null) {
                        Quaternion targetCamRot = Quaternion.LookRotation(lookDir);
                        playerCam.transform.rotation = Quaternion.Slerp(playerCam.transform.rotation, targetCamRot, Time.deltaTime * lookAtSpeed);
                    }
                }

                // 3. 怪物動作處理：【已修改】取消衝刺撞擊，讓怪物原地停下並切換成 IDLE
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                UpdateAnimation(IDLE);

                // 確保怪物在原地也會面向玩家
                Vector3 monsterLook = player.position - transform.position;
                monsterLook.y = 0;
                if (monsterLook != Vector3.zero) {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(monsterLook), Time.deltaTime * 15f);
                }

                // 4. 執行鏡頭隨機抖動（怪物在原地，玩家因為恐懼而全身發抖）
                if (playerCam != null) {
                    if (!hasSavedCamPos) {
                        originalCamLocalPos = playerCam.transform.localPosition;
                        hasSavedCamPos = true;
                    }

                    float randomX = Random.Range(-1f, 1f) * shakeIntensity;
                    float randomY = Random.Range(-1f, 1f) * shakeIntensity;
                    float randomZ = Random.Range(-1f, 1f) * (shakeIntensity * 0.3f);

                    playerCam.transform.localPosition = originalCamLocalPos + new Vector3(randomX, randomY, randomZ);
                }
            }
        } else {
            isDetected = false;
            hasPlayedSpotted = false;  

            // 時間到了，解鎖玩家並還原鏡頭
            if (isFreezingPlayer) {
                isFreezingPlayer = false;

                // 還原相機位置
                if (player != null && hasSavedCamPos) {
                    Camera playerCam = player.GetComponentInChildren<Camera>();
                    if (playerCam != null) {
                        playerCam.transform.localPosition = originalCamLocalPos;
                    }
                }
                hasSavedCamPos = false;

                TogglePlayerController(true); 
                Debug.Log("🔓 釋放玩家，原地定身與抖動結束。");
            }
        }

        // 核心 AI 切換（非定身狀態下）
        if (!isFreezingPlayer) {
            if (isDetected) {
                ChaseAndAttack();
            } else {
                Patrol();
            }
        }
    }

    public void BeIlluminated() {
        if (isFreezingPlayer) return; 
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

    public void OnAttackHit() {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (distance <= hitRange && angle <= (hitAngle / 2f)) {
            
            Debug.Log("💥 攻擊成功命中！啟動原地定身、強制面朝怪物與驚恐抖動機制！");

            UltimateFlashlightController flashlight = player.GetComponentInChildren<UltimateFlashlightController>();
            if (flashlight != null) {
                flashlight.RequestTurnOff();
            }

            detectTimer = calmDownTime;
            isFreezingPlayer = true;
            frozenPlayerPos = player.position; 

            TogglePlayerController(false);

        } else {
            Debug.Log("💨 玩家成功走位，躲開了怪物的攻擊！");
        }
    }

    void TogglePlayerController(bool enable) {
        if (player == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = enable;

        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts) {
            string scriptName = script.GetType().Name.ToLower();
            if (scriptName.Contains("firstperson") || scriptName.Contains("playercontroller") || scriptName.Contains("mouselook")) {
                script.enabled = enable;
            }
        }

        MonoBehaviour[] camScripts = player.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in camScripts) {
            string scriptName = script.GetType().Name.ToLower();
            if (scriptName.Contains("mouselook") || scriptName.Contains("rotation") || scriptName.Contains("camera")) {
                if (!scriptName.Contains("flashlight") && !scriptName.Contains("monster")) {
                    script.enabled = enable;
                }
            }
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