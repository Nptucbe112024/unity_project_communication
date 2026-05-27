using UnityEngine;

public class MonsterHearing2 : MonoBehaviour
{
    [Header("Å¥Ä±ìH­È")]
    public float patrolThreshold = 5f;
    public float alertThreshold = 2f;

    SoundMonsterAI2 ai;

    bool heardThisFrame;
    float heardIntensity;
    Vector3 heardPosition;

    void Awake()
    {
        ai = GetComponent<SoundMonsterAI2>();
    }

    public void ReceiveSound(float intensity, Vector3 sourcePos)
    {
        if (ai == null) return;

        float threshold = ai.currentState == SoundMonsterAI2.State.Alert
            ? alertThreshold
            : patrolThreshold;

        if (intensity >= threshold)
        {
            heardThisFrame = true;

            if (intensity > heardIntensity)
            {
                heardIntensity = intensity;
                heardPosition = sourcePos;
            }
        }
    }

    public bool HasHeardSound(out float intensity, out Vector3 pos)
    {
        intensity = heardIntensity;
        pos = heardPosition;

        bool result = heardThisFrame;

        heardThisFrame = false;
        heardIntensity = 0f;

        return result;
    }
}