using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SoundEmitter : MonoBehaviour
{
    [Header("聲音屬性")]
    public float soundIntensity = 10f;
    public bool  destroyAfterEmit = true;   // 一次性聲音（如腳步用 false）

    SphereCollider trigger;

    void Awake()
    {
        trigger          = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius   = soundIntensity;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent<MonsterHearing>(out var monster)) return;

        float perceived = CalcPerceivedIntensity(monster.transform.position);
        monster.ReceiveSound(perceived, transform.position);
    }

    float CalcPerceivedIntensity(Vector3 targetPos)
    {
        float dist      = Vector3.Distance(transform.position, targetPos);
        float intensity = soundIntensity - (dist * dist / soundIntensity); // 反平方衰減

        Vector3 dir  = (targetPos - transform.position).normalized;
        var     hits = Physics.RaycastAll(transform.position, dir, dist);

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<SoundObstacle>(out var obs))
                intensity -= obs.dampening;
        }

        return Mathf.Max(0f, intensity);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, soundIntensity);
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, soundIntensity);
    }
}