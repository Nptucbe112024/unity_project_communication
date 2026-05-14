using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
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
    public float flashlightStopAngle = 20f;
    public LayerMask monsterLayer;

    [Header("Movement")]
    public NavMeshAgent agent;

    private bool isStoppedByLight = false;

    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    void Update()
    {
        isStoppedByLight = IsHitByFlashlight();

        if (isStoppedByLight)
        {
            StopMonster();
            return;
        }

        if (CanSeePlayer())
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                AttackPlayer();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            Idle();
        }
    }

    bool CanSeePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // 1. 玩家不在範圍內
        if (distance > detectRange)
        {
            return false;
        }

        // 2. 從怪物眼睛位置往玩家發射射線
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 direction = target - origin;

        // 3. 如果中間打到牆壁，代表看不到玩家
        if (Physics.Raycast(origin, direction.normalized, distance, obstacleLayer))
        {
            return false;
        }

        return true;
    }

    bool IsHitByFlashlight()
    {
        if (flashlight == null || flashlightLight == null)
        {
            return false;
        }

        // 手電筒沒開就不會停止怪物
        if (!flashlightLight.enabled)
        {
            return false;
        }

        Vector3 monsterPoint = transform.position + Vector3.up * 1.2f;
        Vector3 directionToMonster = monsterPoint - flashlight.position;

        float distanceToMonster = directionToMonster.magnitude;

        // 超出手電筒照射距離
        if (distanceToMonster > flashlightLight.range)
        {
            return false;
        }

        // 判斷怪物是否在手電筒照射角度內
        float angle = Vector3.Angle(flashlight.forward, directionToMonster);

        if (angle > flashlightStopAngle)
        {
            return false;
        }

        // 判斷手電筒與怪物之間有沒有牆壁
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
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void StopMonster()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void AttackPlayer()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Debug.Log("Attack Player");
        // 這裡之後可以放扣血、播放攻擊動畫
    }

    void Idle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 這裡之後可以改成巡邏
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}