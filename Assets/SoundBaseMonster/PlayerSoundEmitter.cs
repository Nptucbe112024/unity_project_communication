using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
public class PlayerSoundEmitter : MonoBehaviour
{
    [Header("聲音強度")]
    public float breathIntensity = 0.8f;
    public float crouchIntensity = 1.5f;
    public float walkIntensity   = 4.0f;
    public float sprintIntensity = 9.0f;

    [Header("腳步間隔（秒）")]
    public float crouchInterval  = 0.75f;
    public float walkInterval    = 0.50f;
    public float sprintInterval  = 0.25f;

    [Header("呼吸間隔（秒）")]
    public float breathInterval  = 3.0f;

    // ── 內部 ─────────────────────────────────────────
    FirstPersonController fpc;  // 新增
    StarterAssetsInputs input;
    CharacterController cc;

    float footstepTimer;
    float breathTimer;
    
    public InputAction crouchAction;

    [HideInInspector] public float  lastIntensity;
    [HideInInspector] public string lastType = "-";

    
    void Awake()
    {
        fpc   = GetComponent<FirstPersonController>();  // 新增
        input = GetComponent<StarterAssetsInputs>();
        cc    = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleBreath();
        HandleFootstep();
    }

    // ── 呼吸（常駐，不管有沒有在移動）──────────────────

    void HandleBreath()
    {
        breathTimer += Time.deltaTime;
        if (breathTimer < breathInterval) return;
        breathTimer = 0f;
        Emit(breathIntensity, "Breath");
    }

    // ── 腳步（移動中才觸發）────────────────────────────

    void HandleFootstep()
    {
        // 改成讀 fpc 的速度，不用 cc.isGrounded
        float speed = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
        bool isMoving = speed > 0.1f;

        if (!isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        bool isCrouch = crouchAction.IsPressed();
        bool isSprint = input.sprint;

        float intensity = isCrouch ? crouchIntensity
                        : isSprint ? sprintIntensity
                        :            walkIntensity;

        float interval  = isCrouch ? crouchInterval
                        : isSprint ? sprintInterval
                        :            walkInterval;

        footstepTimer += Time.deltaTime;
        if (footstepTimer < interval) return;
        footstepTimer = 0f;

        string type = isCrouch ? "Crouch" : isSprint ? "Sprint" : "Walk";
        Emit(intensity, type);
    }

    // ── 核心發聲 ────────────────────────────────────────

    void Emit(float intensity, string type)
    {
        lastIntensity = intensity;
        lastType      = type;

        var go = new GameObject($"SoundPulse_{type}");
        go.transform.position = transform.position;

        var emitter            = go.AddComponent<SoundEmitter>();
        emitter.soundIntensity = intensity;
        Destroy(go, 0.12f);

        MonsterDebugger.LogSound($"Player/{type}", intensity, transform.position);
    }

    // ── Debug HUD ───────────────────────────────────────

    // PlayerSoundEmitter.cs 的 OnGUI 換成這個版本

void OnGUI()
{
    if (!MonsterDebugger.enabled) return;

    bool isCrouch = crouchAction.IsPressed();
    bool isSprint = input != null && input.sprint;
    float speed   = cc != null
                  ? new Vector2(cc.velocity.x, cc.velocity.z).magnitude
                  : 0f;

    string mode   = isCrouch ? "CROUCH" : isSprint ? "SPRINT" : "WALK";
    string moving = speed > 0.1f ? "YES" : "NO";

    // 主要資訊框
    GUI.color = Color.black;
    GUI.Box(new Rect(9, 9, 234, 130), "");
    GUI.color = Color.white;

    GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 13 };

    GUI.Label(new Rect(14, 14,  220, 20), $"Mode     : {mode}",                     style);
    GUI.Label(new Rect(14, 32,  220, 20), $"Moving   : {moving}  ({speed:F2} m/s)", style);
    GUI.Label(new Rect(14, 50,  220, 20), $"Last     : {lastType} ({lastIntensity:F1})", style);
    GUI.Label(new Rect(14, 68,  220, 20), $"Breath   : {breathInterval - breathTimer:F1}s", style);

    // 強度視覺化 bar
    GUI.Label(new Rect(14, 88, 220, 20), "Intensity:", style);

    float maxIntensity = sprintIntensity;
    float barWidth     = 200f * (lastIntensity / maxIntensity);

    // 背景
    GUI.color = new Color(0.3f, 0.3f, 0.3f);
    GUI.DrawTexture(new Rect(14, 106, 200, 16), Texture2D.whiteTexture);

    // 強度 bar（顏色依強度變化）
    float ratio   = lastIntensity / maxIntensity;
    GUI.color     = Color.Lerp(Color.green, Color.red, ratio);
    GUI.DrawTexture(new Rect(14, 106, barWidth, 16), Texture2D.whiteTexture);

    GUI.color = Color.white;
    GUI.Label(new Rect(220, 104, 40, 20), $"{lastIntensity:F1}", style);
}

    // ── Gizmos（選取時顯示最後發聲範圍）──────────────────

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
        Gizmos.DrawSphere(transform.position, lastIntensity);
        Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, lastIntensity);
    }
}