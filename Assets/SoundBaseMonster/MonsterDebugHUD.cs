using UnityEngine;

// 需要在 Edit > Project Settings > Player 開啟 Legacy Text Mesh
public class MonsterDebugHUD : MonoBehaviour
{
    SoundMonsterAI      ai;
    MonsterHearing hearing;
    TextMesh       label;

    void Awake()
    {
        ai      = GetComponent<SoundMonsterAI>();
        hearing = GetComponent<MonsterHearing>();

        // 動態建立懸浮文字
        var go  = new GameObject("DebugLabel");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0, 2.5f, 0);

        label           = go.AddComponent<TextMesh>();
        label.fontSize  = 28;
        label.alignment = TextAlignment.Center;
        label.anchor    = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        // 標籤永遠面向攝影機
        label.transform.LookAt(Camera.main.transform);
        label.transform.Rotate(0, 180, 0);

        string stateStr = ai.currentState switch
        {
            SoundMonsterAI.State.Patrol => "🟢 PATROL",
            SoundMonsterAI.State.Alert  => "🟠 ALERT",
            SoundMonsterAI.State.Chase  => "🔴 CHASE",
            _                      => "?"
        };

        label.text = $"{stateStr}\nspd:{ai.GetComponent<UnityEngine.AI.NavMeshAgent>().velocity.magnitude:F1}";
    }
}