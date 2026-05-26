using UnityEngine;

/// <summary>
/// 掛在謎題物件上（monitor 等）。
/// 使用兩個 Camera：玩家相機（FPSController 上）與謎題相機（Monitor 上）。
/// 開謎題時停用玩家相機 Component，啟用謎題相機。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class InteractionZone : MonoBehaviour
{
    [Header("謎題")]
    [SerializeField] private LaserPuzzleController puzzleController;

    [Header("相機（拖入 Camera 組件，不是 GameObject）")]
    [SerializeField] private Camera playerCamera;   // FPSController 子物件的 Camera
    [SerializeField] private Camera puzzleCamera;   // Monitor 上的 Camera

    [Header("玩家移動")]
    [SerializeField] private MonoBehaviour playerMovementScript;  // FPSController 腳本
    [SerializeField] private MonoBehaviour playerLookScript;      // MouseLook 腳本（可留空）

    [Header("互動提示 UI")]
    [SerializeField] private GameObject interactPromptUI;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("範圍設定")]
    [SerializeField] private float radius = 2.5f;

    // ──────────────────────────────────────────
    private bool playerInRange = false;
    private bool puzzleOpen    = false;
    private bool isReady       = false;

    void Awake()
    {
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;

        if (interactPromptUI) interactPromptUI.SetActive(false);

        // 謎題相機預設關閉
        if (puzzleCamera) puzzleCamera.enabled = false;

        if (puzzleController)
        {
            puzzleController.OnPuzzleClosed += HandlePuzzleClosed;
            puzzleController.OnPuzzleSolved += HandlePuzzleSolved;
        }

        Invoke(nameof(SetReady), 0.3f);
    }

    void SetReady() => isReady = true;

    void Update()
    {
        if (!playerInRange || puzzleOpen) return;
        if (Input.GetKeyDown(interactKey)) OpenPuzzle();
    }

    // ──────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!isReady) return;
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactPromptUI) interactPromptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPromptUI) interactPromptUI.SetActive(false);
        if (puzzleOpen) ClosePuzzle();
    }

    // ──────────────────────────────────────────
    void OpenPuzzle()
    {
        puzzleOpen = true;
        if (interactPromptUI) interactPromptUI.SetActive(false);

        // 停用玩家相機（保留 GameObject，只關 Component）
        if (playerCamera) playerCamera.enabled = false;

        // 啟用謎題相機
        if (puzzleCamera) puzzleCamera.enabled = true;

        // 停用移動與視角腳本
        if (playerMovementScript) playerMovementScript.enabled = false;
        if (playerLookScript)     playerLookScript.enabled     = false;

        // 解鎖滑鼠
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        puzzleController?.OpenPuzzle();
    }

    void ClosePuzzle()
    {
        puzzleOpen = false;

        // 還原玩家相機
        if (playerCamera) playerCamera.enabled = true;

        // 關閉謎題相機
        if (puzzleCamera) puzzleCamera.enabled = false;

        // 還原移動與視角腳本
        if (playerMovementScript) playerMovementScript.enabled = true;
        if (playerLookScript)     playerLookScript.enabled     = true;

        // 還原滑鼠鎖定
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (playerInRange && interactPromptUI)
            interactPromptUI.SetActive(true);
    }

    void HandlePuzzleClosed() => ClosePuzzle();
    void HandlePuzzleSolved() => Debug.Log($"[{gameObject.name}] 謎題解開！");

    void OnDestroy()
    {
        if (puzzleController)
        {
            puzzleController.OnPuzzleClosed -= HandlePuzzleClosed;
            puzzleController.OnPuzzleSolved -= HandlePuzzleSolved;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}