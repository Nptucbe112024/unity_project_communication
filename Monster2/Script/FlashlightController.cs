using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("基本設定")]
    public KeyCode toggleKey = KeyCode.F; 
    public bool isOn = false;             

    [Header("偵測設定")]
    public float detectRange = 25f;       // 手電筒射程
    public float lightRadius = 3f;        // 光圈偵測寬度 (SphereCast 半徑)
    public LayerMask monsterLayer;        // 記得在 Inspector 選擇 "Monster" 層級

    private Light _lightSource;

    void Start()
    {
        _lightSource = GetComponent<Light>();
        
        // 初始狀態同步
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn;
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
        isOn = !isOn;
        if (_lightSource != null)
        {
            _lightSource.enabled = isOn;
        }
    }

    void ScanForMonster()
    {
        RaycastHit hit;
        
        // 使用 SphereCast 模擬錐形光束偵測
        // transform.position: 起點, lightRadius: 寬度, transform.forward: 方向, detectRange: 長度
        if (Physics.SphereCast(transform.position, lightRadius, transform.forward, out hit, detectRange, monsterLayer))
        {
            // 嘗試取得碰撞到的物件上的 Monster2 腳本
            Monster2 monster = hit.collider.GetComponent<Monster2>();
            
            if (monster != null)
            {
                // 呼叫怪物腳本中的 public 方法
                monster.BeIlluminated(); 
                Debug.Log("成功照到怪物：" + hit.collider.name);
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