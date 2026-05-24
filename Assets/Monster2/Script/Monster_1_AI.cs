using UnityEngine;
using UnityEngine.AI;

public class Monster_1_AI : MonoBehaviour
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
    public float flashlightStopAngle = 25f; // 【稍微放大角度防漏】20度有點太嚴苛，改成25-30度體驗更好

    [Header("Movement")]
    public NavMeshAgent agent;
    public float rotateSpeed = 8f;

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

    void Start()
    {
        InitComponents();
        InitAudio();
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        // 1. 先計算跟玩家的距離
        float distance = Vector3.Distance(transform.position, player.position);

        // 2. 進行光照判別
        isStoppedByLight = IsHitByFlashlight(distance);

        // 3. 【強光擁有最高優先權】只要被照到，不管是遠是近，一律強制不准動、不准攻擊！
        if (isStoppedByLight)
        {
            StopMonsterByLight();
            return; // 這裡直接切斷，後面的攻擊代碼絕對執行不到
        }

        if (!CanSeePlayer())
        {
            Idle();
            return;
        }

        // 4. 沒被光照到，才允許判斷攻擊或追逐
        if (distance <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }
    }

    void InitComponents()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void InitAudio()
    {
        if (walkAudioSource != null)
        {
            walkAudioSource.clip = walkSound;
            walkAudioSource.loop = true;
            walkAudioSource.playOnAwake = false;
        }
        if (sfxAudioSource != null) sfxAudioSource.playOnAwake = false;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectRange) return false;

        // 視線起點也同步稍微拉高防穿幫
        Vector3 origin = transform.position + Vector3.up * 2.0f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 direction = target - origin;

        if (Physics.Raycast(origin, direction.normalized, distance, obstacleLayer))
        {
            return false;
        }
        return true;
    }

    // 【修改】傳入目前距離，以便做貼身安全防範
    bool IsHitByFlashlight(float distanceToPlayer)
    {
        if (flashlight == null || flashlightLight == null) return false;
        if (!flashlightLight.enabled) return false; // 手電筒沒開，免談

        // ======= 【貼身安全保護】=======
        // 如果怪物已經貼在你臉上(小於攻擊距離+0.5)，且玩家相機幾乎正對著怪物
        if (distanceToPlayer <= (attackRange + 0.5f))
        {
            Vector3 dirToMonster = (transform.position - player.position).normalized;
            float facingAngle = Vector3.Angle(player.forward, dirToMonster);
            
            // 只要玩家的鏡頭正前方看著怪(角度在45度內)，貼身狀態下直接判斷被照到！防止射線插進模型內部穿幫
            if (facingAngle < 45f) return true; 
        }
        // ===============================

        // 【修改】將 1.2f 改為 2.0f 提高偵測點至蜘蛛怪的胸口高度
        Vector3 monsterPoint = transform.position + Vector3.up * 2.0f;
        Vector3 directionToMonster = monsterPoint - flashlight.position;
        float distanceToMonster = directionToMonster.magnitude;

        if (distanceToMonster > flashlightLight.range) return false;

        float angle = Vector3.Angle(flashlight.forward, directionToMonster);
        if (angle > flashlightStopAngle) return false;

        if (Physics.Raycast(flashlight.position, directionToMonster.normalized, out RaycastHit hit, distanceToMonster))
        {
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }
        return true;
    }

    void ChasePlayer()
    {
        if (agent == null || player == null) return;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        PlayWalkSound();
        SetAnimation(isLit: false, isWalking: true, isAttacking: false);
    }

    void AttackPlayer()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        StopWalkSound();
        FacePlayer();

        SetAnimation(isLit: false, isWalking: false, isAttacking: true);

        if (attackTimer <= 0f)
        {
            PlayAttackSound();
            if (animator != null) animator.SetTrigger("AttackTrigger");

            // 呼叫改寫後的手電筒
            /*if (player != null)
            {
                UltimateFlashlightController flashlightCtrl = player.GetComponentInChildren<UltimateFlashlightController>();
                if (flashlightCtrl != null)
                {
                    flashlightCtrl.RequestTurnOff();
                }
            }*/
            attackTimer = attackCooldown;
        }
    }

    void StopMonsterByLight()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        StopWalkSound();
        SetAnimation(isLit: true, isWalking: false, isAttacking: false);
    }

    void Idle()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        StopWalkSound();
        SetAnimation(isLit: false, isWalking: false, isAttacking: false);
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    void SetAnimation(bool isLit, bool isWalking, bool isAttacking)
    {
        if (animator == null) return;
        animator.SetBool("IsLit", isLit);
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsAttacking", isAttacking);
    }

    void PlayWalkSound()
    {
        if (walkAudioSource == null || walkSound == null) return;
        if (!walkAudioSource.isPlaying) walkAudioSource.Play();
    }

    void StopWalkSound()
    {
        if (walkAudioSource == null) return;
        if (walkAudioSource.isPlaying) walkAudioSource.Stop();
    }

    void PlayAttackSound()
    {
        if (sfxAudioSource == null || attackSound == null) return;
        sfxAudioSource.PlayOneShot(attackSound);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 在 Scene 視窗畫出胸口高度的綠點，方便你確認有沒有對準
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.0f, 0.2f);
    }
}