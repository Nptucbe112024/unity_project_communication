using UnityEngine;
using System.Collections; // 【必須引入】使用協程需要這個命名空間

[RequireComponent(typeof(Light))]
public class UltimateFlashlightController : MonoBehaviour
{
    [Header("基本設定")]
    public KeyCode toggleKey = KeyCode.F; 
    public bool isOn = false;             

    [Header("偵測設定")]
    public float detectRange = 25f;       
    public float lightRadius = 3f;        
    public LayerMask monsterLayer;        

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;

    private Light _lightSource;

    void Start()
    {
        _lightSource = GetComponent<Light>();
        if (_lightSource != null) _lightSource.enabled = isOn;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) ToggleFlashlight();
        if (isOn) ScanForMonster();
    }

    void ToggleFlashlight()
    {
        isOn = !isOn;
        if (_lightSource != null) _lightSource.enabled = isOn;
        PlayToggleSound();
    }

    // 【修改】提供給外部呼叫的延遲關燈啟動器
    public void TurnOffFlashlightWithDelay(float delayTime)
    {
        if (isOn)
        {
            // 啟動協程來處理延遲
            StartCoroutine(DelayTurnOffRoutine(delayTime));
        }
    }

    // 【新增】實際執行動態延遲的協程
    private IEnumerator DelayTurnOffRoutine(float delayTime)
    {
        // 程式走到這邊後，會在這裡停頓指定的秒數（例如 1 秒），期間遊戲其他東西照常運作
        yield return new WaitForSeconds(delayTime);

        // 再次確認手電筒是否還是開著的（防止玩家中途自己先關了）
        if (isOn)
        {
            isOn = false;
            if (_lightSource != null)
            {
                _lightSource.enabled = false;
            }
            
            // 播放關燈音效
            if (audioSource != null && turnOffSound != null)
            {
                audioSource.PlayOneShot(turnOffSound);
            }
        }
    }

    void PlayToggleSound()
    {
        if (audioSource != null)
        {
            AudioClip clipToPlay = isOn ? turnOnSound : turnOffSound;
            if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
        }
    }

    void ScanForMonster()
    {
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, lightRadius, transform.forward, out hit, detectRange, monsterLayer))
        {
            Monster2 monster = hit.collider.GetComponent<Monster2>();
            if (monster != null) monster.BeIlluminated(); 
        }
    }
}