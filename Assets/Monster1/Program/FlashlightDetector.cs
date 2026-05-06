using UnityEngine;

public class FlashlightDetector : MonoBehaviour
{
    [Header("Flashlight")]
    public Light flashlight;
    public float lightRange = 15f;
    public float lightAngle = 60f;

    [Header("Raycast")]
    public Transform rayOrigin;   // 建議指定 Main Camera 或手電筒頭的位置
    public LayerMask hitMask;     // 包含怪物、牆壁、地板等會擋光的物件

    private MonsterAI[] monsters;

    void Start()
    {
        monsters = FindObjectsOfType<MonsterAI>();

        if (rayOrigin == null)
        {
            rayOrigin = transform;
        }
    }

    void Update()
    {
        if (monsters == null || monsters.Length == 0)
        {
            monsters = FindObjectsOfType<MonsterAI>();
        }

        foreach (MonsterAI monster in monsters)
        {
            bool lit = IsMonsterLit(monster);
            monster.SetFrozen(lit);
        }
    }

    bool IsMonsterLit(MonsterAI monster)
    {
        if (monster == null) return false;
        if (flashlight == null) return false;
        if (!flashlight.enabled) return false;

        Vector3 origin = rayOrigin.position;

        // 稍微瞄怪物胸口/上半身，比瞄腳底穩
        Vector3 targetPos = monster.transform.position + Vector3.up * 1.0f;
        Vector3 dirToMonster = targetPos - origin;

        float distance = dirToMonster.magnitude;
        if (distance > lightRange) return false;

        float angle = Vector3.Angle(rayOrigin.forward, dirToMonster);
        if (angle > lightAngle * 0.5f) return false;

        RaycastHit hit;
        if (Physics.Raycast(origin, dirToMonster.normalized, out hit, lightRange, hitMask))
        {
            Debug.DrawRay(origin, dirToMonster.normalized * distance, Color.red);

            MonsterAI hitMonster = hit.collider.GetComponentInParent<MonsterAI>();
            if (hitMonster != null && hitMonster == monster)
            {
                Debug.Log("Flashlight hit monster: " + monster.name);
                return true;
            }
        }
        else
        {
            Debug.DrawRay(origin, dirToMonster.normalized * distance, Color.yellow);
        }

        return false;
    }
}