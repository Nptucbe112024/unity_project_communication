using UnityEngine;
using System.Collections; // 必須引入，才能使用協程 (Coroutine)

[RequireComponent(typeof(Light))] // 確保此物件身上一定有 Light 組件
public class FlashlightController : MonoBehaviour
{
    [Header("基本設定")]
    public KeyCode toggleKey = KeyCode.F; // 開關手電筒的按鍵
    public bool isOn = false;             // 目前手電筒是否開啟

    [Header("攻擊後關燈設定")]
    public float delayTime = 0.5f;        // 被怪物抓到後，延遲關燈的秒數（可在 Inspector 調整）
    private Coroutine delayTurnOffCoroutine; // 記錄正在執行的倒數，防止重複疊加

    [Header("音效設定")]
    public AudioSource audioSource;       // 播放開關燈音效的組件
    public AudioClip turnOnSound;        // 開燈音效
    public AudioClip turnOffSound;       // 關燈音效

    private Light _lightSource;

    void Start()
    {
        _lightSource = GetComponent<Light>();
        
        // 初始狀態同步
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn; // 根據初始設定決定一開始是開還是關
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>(); // 如果沒拉組件，自動嘗試取得身上的 AudioSource
        }
    }

    void Update()
    {
        // 按下設定的按鍵（預設 F）開關手電筒
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }

        // ====== 【全面改版優化：核心合併修改點】 ======
        // 這裡已經徹底移除原本會報錯的 ScanForMonster() 呼叫。
        // 原本手電筒面板上的 Detect Range、Light Radius、Monster Layer 變數已被移除。
        // 現在全部改由兩隻怪物（MonsterAI 與 Monster2）各自獨立使用自身的 IsHitByFlashlight() 雷達去進行精準判定。
        // 這樣做不僅更省效能，也永久解決了隔著厚牆會讓蜘蛛怪（Monster2）暴走的 Bug。
    }

    void ToggleFlashlight()
    {
        // 如果手電筒正處於被怪物強行關閉的倒數中，玩家此時若主動按下 F 鍵，就直接取消倒數，由玩家接管
        if (delayTurnOffCoroutine != null)
        {
            StopCoroutine(delayTurnOffCoroutine);
            delayTurnOffCoroutine = null;
        }

        isOn = !isOn;
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn;
        }

        // 播放開關燈音效
        if (audioSource != null)
        {
            if (isOn && turnOnSound != null) audioSource.PlayOneShot(turnOnSound); //
            else if (!isOn && turnOffSound != null) audioSource.PlayOneShot(turnOffSound); //
        }
    }

    // ====== 統一接口：提供給蜘蛛怪（Monster2）或其他具備懲罰機制的 AI 攻擊命中時呼叫 ======
    public void RequestTurnOff()
    {
        // 如果手電筒本來就是關的，就不用再關一次
        if (!isOn) return;

        // 防止多隻怪物同時攻擊，或者同一隻怪連續命中導致倒數協程重複啟動
        if (delayTurnOffCoroutine != null)
        {
            StopCoroutine(delayTurnOffCoroutine);
        }

        // 啟動倒數協程，延遲一段時間後自動熄滅玩家的手電筒
        delayTurnOffCoroutine = StartCoroutine(DelayTurnOffRoutine());
    }

    // 延遲關燈的協程處理
    private IEnumerator DelayTurnOffRoutine()
    {
        // 等待設定的 delayTime 秒數（預設 0.5 秒，製造出被咬到/抓到後燈光閃爍故障熄滅的效果）
        yield return new WaitForSeconds(delayTime);

        if (isOn)
        {
            isOn = false;
            if (_lightSource != null) _lightSource.enabled = false; //
            
            // 播放關燈音效，營造恐怖氛圍
            if (audioSource != null && turnOffSound != null)
            {
                audioSource.PlayOneShot(turnOffSound); //
            }
        }

        delayTurnOffCoroutine = null; // 結束後清空協程紀錄
    }
}