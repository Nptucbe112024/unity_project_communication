using UnityEngine;
using System.Collections; // 必須引入，才能使用協程 (Coroutine)

[RequireComponent(typeof(Light))]
public class UltimateFlashlightController : MonoBehaviour
{
    [Header("基本設定")]
    public KeyCode toggleKey = KeyCode.F; 
    public bool isOn = false;             

    [Header("攻擊後關燈設定")]
    public float delayTime = 0.5f;        // 可以在 Inspector 直接修改延遲秒數（預設 1 秒）
    private Coroutine delayTurnOffCoroutine; // 用來記錄正在執行的倒數，防止重複重疊

    [Header("偵測設定 (適用於發瘋蜘蛛怪 Monster2)")]
    public float detectRange = 25f;       // 手電筒射程
    public float lightRadius = 3f;        // 光圈偵測寬度 (SphereCast 半徑)
    public LayerMask monsterLayer;        // 記得在 Inspector 選擇 "Monster" 層級

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;

    private Light _lightSource;

    void Start()
    {
        _lightSource = GetComponent<Light>();
        
        // 初始狀態同步
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        // 按下設定的按鍵開關手電筒
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }

        // 當手電筒開啟時，持續偵測前方是否有怪物
        if (isOn)
        {
            ScanForMonster();
        }
    }

    void ToggleFlashlight()
    {
        // 如果手電筒被強制關閉的倒數還在跑，玩家此時若手動切換開關，就將倒數取消
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
            if (isOn && turnOnSound != null) audioSource.PlayOneShot(turnOnSound);
            else if (!isOn && turnOffSound != null) audioSource.PlayOneShot(turnOffSound);
        }
    }

    // ====== 【核心功能】提供給所有怪物 AI 呼叫的統一接口 ======
    public void RequestTurnOff()
    {
        // 如果手電筒本來就是關的，就什麼都不做
        if (!isOn) return;

        // 防止多隻怪物同時攻擊，或者同一隻怪連續攻擊導致倒數重複啟動
        if (delayTurnOffCoroutine != null)
        {
            StopCoroutine(delayTurnOffCoroutine);
        }

        // 啟動內部的倒數協程
        delayTurnOffCoroutine = StartCoroutine(DelayTurnOffRoutine());
    }

    // 手電筒內部的延遲關燈協程
    private IEnumerator DelayTurnOffRoutine()
    {
        // 讀取上面設定的 delayTime（1秒）
        yield return new WaitForSeconds(delayTime);

        if (isOn)
        {
            isOn = false;
            if (_lightSource != null) _lightSource.enabled = false;
            if (audioSource != null && turnOffSound != null)
            {
                audioSource.PlayOneShot(turnOffSound);
            }
        }

        delayTurnOffCoroutine = null; // 結束後清空紀錄
    }

    void ScanForMonster()
    {
        RaycastHit hit;
        
        // 使用 SphereCast 模擬錐形光束偵測
        if (Physics.SphereCast(transform.position, lightRadius, transform.forward, out hit, detectRange, monsterLayer))
        {
            // 嘗試取得第一隻怪物的組件
            Monster2 monster = hit.collider.GetComponent<Monster2>();
            if (monster != null)
            {
                monster.BeIlluminated(); 
                Debug.Log("成功照到蜘蛛怪：" + hit.collider.name);
            }
        }
    }

    // 在 Scene 視窗畫出偵測範圍 (方便 Debug)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * detectRange);
        Gizmos.DrawWireSphere(transform.position + transform.forward * detectRange, lightRadius);
    }
}