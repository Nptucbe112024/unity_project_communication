using UnityEngine;
using System.Collections; // 必須引入，才能使用協程 (Coroutine)

public class UltimateFlashlightController : MonoBehaviour
{
    [Header("基本設定")]
    public KeyCode toggleKey = KeyCode.F; // 開關手電筒的按鍵
    public bool isOn = false;             // 目前手電筒是否開啟

    // ====== 【光的部分：直接控制底下的 WhiteLight】 ======
    [Header("指定光源 (請拖入底下的 WhiteLight)")]
    public Light _lightSource;            

    [Header("攻擊後關燈設定")]
    public float delayTime = 0.5f;        // 被怪物抓到後，延遲關燈的秒數（可在 Inspector 調整）
    private Coroutine delayTurnOffCoroutine; // 記錄正在執行的倒數，防止重複疊加

    [Header("音效設定")]
    public AudioSource audioSource;       // 播放開關燈音效的組件
    public AudioClip turnOnSound;        // 開燈音效
    public AudioClip turnOffSound;       // 關燈音效

    void Start()
    {
        // 移除原本的 GetComponent<Light>()，這樣就不會再去抓父物件身上那個錯的燈了！
        
        // 初始狀態同步
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn; // 根據初始設定決定子光源一開始是開還是關
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>(); // 自動嘗試取得身上的 AudioSource
        }
    }

    void Update()
    {
        // 按下設定的按鍵（預設 F）開關手電筒
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }

        // ====== 【全面改版優化：大功告成】 ======
        // 這裡已經徹底移除原本在第 125 行會導致報白字的 ScanForMonster() 函數與呼叫。
        // 現在改由兩隻怪物（MonsterAI 與 Monster2）在它們各自的 Update 內，
        // 透過 IsHitByFlashlight() 雷達去主動判定這盞 _lightSource 的開關狀態、位置與角度。
        // 這不只消除了 CS1061 的錯誤，更完美達成了您的自主偵測型 AI 規劃！
    }

    void ToggleFlashlight()
    {
        // 如果手電筒正處於被怪物強行關閉的倒數中，玩家此時若主動按下 F 鍵，就直接取消倒數
        if (delayTurnOffCoroutine != null)
        {
            StopCoroutine(delayTurnOffCoroutine);
            delayTurnOffCoroutine = null;
        }

        isOn = !isOn;
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn; // 控制子光源
        }

        // 播放開關燈音效
        if (audioSource != null)
        {
            if (isOn && turnOnSound != null) audioSource.PlayOneShot(turnOnSound);
            else if (!isOn && turnOffSound != null) audioSource.PlayOneShot(turnOffSound);
        }
    }

    // ====== 統一接口：提供給怪物 AI 攻擊命中玩家時，強行關燈呼叫 ======
    public void RequestTurnOff()
    {
        if (!isOn) return;

        if (delayTurnOffCoroutine != null)
        {
            StopCoroutine(delayTurnOffCoroutine);
        }

        delayTurnOffCoroutine = StartCoroutine(DelayTurnOffRoutine());
    }

    // 延遲關燈的協程處理
    private IEnumerator DelayTurnOffRoutine()
    {
        yield return new WaitForSeconds(delayTime);

        if (isOn)
        {
            isOn = false;
            if (_lightSource != null) _lightSource.enabled = false; // 關閉子光源
            
            if (audioSource != null && turnOffSound != null)
            {
                audioSource.PlayOneShot(turnOffSound);
            }
        }

        delayTurnOffCoroutine = null; 
    }
}