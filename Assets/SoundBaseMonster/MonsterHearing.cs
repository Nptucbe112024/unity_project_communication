using UnityEngine;

public class MonsterHearing : MonoBehaviour
{
    [Header("聽覺閾值")]
    public float patrolThreshold = 5f;  // 巡邏時需要更大聲才觸發
    public float alertThreshold  = 2f;  // 警戒時更靈敏

    SoundMonsterAI  ai;
    bool       heardThisFrame;
    float      heardIntensity;
    Vector3    heardPosition;

    void Awake() => ai = GetComponent<SoundMonsterAI>();

    // 由 SoundEmitter 呼叫
    public void ReceiveSound(float intensity, Vector3 sourcePos)
    {
        float threshold = ai.currentState == SoundMonsterAI.State.Alert
                        ? alertThreshold
                        : patrolThreshold;

        if (intensity >= threshold)
        {
            heardThisFrame = true;
            // 同幀多個聲源取最強的
            if (intensity > heardIntensity)
            {
                heardIntensity = intensity;
                heardPosition  = sourcePos;
            }
        }
    }

    // MonsterAI 每幀呼叫一次
    public bool HasHeardSound(out float intensity, out Vector3 pos)
    {
        intensity = heardIntensity;
        pos       = heardPosition;
        bool result   = heardThisFrame;
        // 消費掉
        heardThisFrame = false;
        heardIntensity = 0f;
        return result;
    }
}