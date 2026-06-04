using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 解謎控制器。
/// - 工具列用 OnGUI (IMGUI) 繪製，不需要額外建 Canvas 按鈕
/// - 關卡用 PuzzleLevel ScriptableObject 或直接 SetupLevel() 程式碼設定
/// - Canvas 設為 World Space 貼在 Monitor 上
/// </summary>
public class LaserPuzzleController : MonoBehaviour
{
    // ──────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────
    [Header("元件")]
    [SerializeField] private LaserPuzzleRenderer puzzleRenderer;
    [SerializeField] private GameObject puzzlePanel;          // World Space Canvas 根節點
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Clear 動畫面板")]
    [SerializeField] private PuzzleClearPanel clearPanel;

    [Header("關卡設定")]
    [SerializeField] private PuzzleLevelData levelData;  // 手動設定（留空則自動生成）
    [SerializeField] private bool autoGenerate = true;   // 自動生成關卡
    [SerializeField] private int generatorSeed = 0;      // 0 = 每次隨機；其他值固定結果

    [Header("相機（拖入 Monitor 上的 Camera）")]
    [SerializeField] private Camera puzzleCamera;

    [Header("IMGUI 工具列外觀")]
    [SerializeField] private int toolbarHeight = 36;
    [SerializeField] private int toolbarFontSize = 14;

    // ──────────────────────────────────────────
    // 公開屬性
    // ──────────────────────────────────────────
    public LaserPuzzleData Data { get; private set; }
    public Camera PuzzleCamera => puzzleCamera != null ? puzzleCamera : Camera.main;

    public System.Action OnPuzzleSolved;
    public System.Action OnPuzzleClosed;

    // ──────────────────────────────────────────
    // 內部狀態
    // ──────────────────────────────────────────
    private CellType selectedType = CellType.MirrorSlash;
    private bool eraserMode  = false;
    private bool guiVisible  = false;
    private LaserPuzzleData savedInitialData = null; // 記錄初始關卡狀態

    private GUIStyle btnStyle;
    private GUIStyle btnSelectedStyle;
    private bool stylesInit = false;

    // ──────────────────────────────────────────
    void Awake()
    {
        LoadLevel();
        puzzlePanel.SetActive(false);
    }

    // ──────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────

    public void OpenPuzzle()
    {
        puzzlePanel.SetActive(true);
        guiVisible = true;
        RefreshPuzzle();
        UpdateStatusText();
    }

    public void ClosePuzzle()
    {
        clearPanel?.Hide();
        puzzlePanel.SetActive(false);
        guiVisible = false;
        OnPuzzleClosed?.Invoke();
    }

    /// <summary>
    /// 用程式碼直接載入關卡。
    /// 範例：
    ///   var data = new LaserPuzzleData(7, 10);
    ///   data.SetSource(3, PuzzleDirection.Right);
    ///   data.AddTarget(3);
    ///   data.SetCell(2, 2, CellType.Wall);
    ///   data.SetCell(0, 2, CellType.MirrorSlash, true);
    ///   controller.LoadCustomLevel(data);
    /// </summary>
    public void LoadCustomLevel(LaserPuzzleData data)
    {
        Data = data;
        Data.Trace();
        if (puzzlePanel.activeSelf) puzzleRenderer.Redraw();
    }

    // ──────────────────────────────────────────
    // IMGUI 工具列
    // ──────────────────────────────────────────
    void OnGUI()
    {
        if (!guiVisible) return;
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        // 工具列固定在畫面底部
        float barW = 480f;
        float barX = (sw - barW) / 2f;
        float barY = sh - toolbarHeight - 10f;

        GUI.Box(new Rect(barX - 4, barY - 4, barW + 8, toolbarHeight + 8),
                GUIContent.none, GUI.skin.box);

        float x = barX;
        float bw = 90f;

        DrawToolBtn(ref x, barY, bw, "╱ 鏡子/",
            selectedType == CellType.MirrorSlash && !eraserMode,
            () => { selectedType = CellType.MirrorSlash; eraserMode = false; });

        DrawToolBtn(ref x, barY, bw, "╲ 鏡子\\",
            selectedType == CellType.MirrorBack && !eraserMode,
            () => { selectedType = CellType.MirrorBack; eraserMode = false; });

        DrawToolBtn(ref x, barY, bw, "✦ 分光器",
            selectedType == CellType.Splitter && !eraserMode,
            () => { selectedType = CellType.Splitter; eraserMode = false; });

        DrawToolBtn(ref x, barY, bw, "✕ 清除",
            eraserMode,
            () => eraserMode = true);

        x += 8f;

        DrawToolBtn(ref x, barY, 70f, "↺ 重置",
            false,
            ResetPuzzle);

        DrawToolBtn(ref x, barY, 50f, "✕ 離開",
            false,
            ClosePuzzle);

        UpdateStatusText();
    }

    void DrawToolBtn(ref float x, float y, float w, string label, bool selected, System.Action onClick)
    {
        var style = selected ? btnSelectedStyle : btnStyle;
        if (GUI.Button(new Rect(x, y, w, toolbarHeight), label, style))
            onClick?.Invoke();
        x += w + 4f;
    }

    void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = toolbarFontSize,
            fontStyle = FontStyle.Normal,
        };
        btnStyle.normal.textColor  = Color.white;

        btnSelectedStyle = new GUIStyle(btnStyle);
        btnSelectedStyle.normal.background  = MakeTex(2, 2, new Color(0.11f, 0.62f, 0.46f));
        btnSelectedStyle.normal.textColor   = Color.white;
        btnSelectedStyle.hover.background   = btnSelectedStyle.normal.background;
        btnSelectedStyle.active.background  = btnSelectedStyle.normal.background;
    }

    Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pix); tex.Apply();
        return tex;
    }

    // ──────────────────────────────────────────
    // Update：處理滑鼠點擊謎題格
    // ──────────────────────────────────────────
    void Update()
    {
        if (!guiVisible) return;

        if (puzzleRenderer.TryGetGridCell(Input.mousePosition, out var cell))
        {
            puzzleRenderer.SetHover(cell);

            if (Input.GetMouseButtonDown(0))
            {
                bool changed;
                if (eraserMode)
                {
                    changed = Data.TryRemoveCell(cell.x, cell.y);
                    if (changed)
                        Debug.Log($"[{gameObject.name}] 清除格子 ({cell.x},{cell.y}) | 雷射通關：{Data.IsSolved}");
                }
                else
                {
                    changed = Data.TryPlaceCell(cell.x, cell.y, selectedType);
                    if (changed)
                        Debug.Log($"[{gameObject.name}] 放置 {selectedType} 於 ({cell.x},{cell.y}) | 雷射通關：{Data.IsSolved}");
                }
                if (changed) RefreshPuzzle();
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (Data.TryRemoveCell(cell.x, cell.y))
                {
                    Debug.Log($"[{gameObject.name}] 右鍵清除 ({cell.x},{cell.y}) | 雷射通關：{Data.IsSolved}");
                    RefreshPuzzle();
                }
            }
        }
        else
        {
            puzzleRenderer.ClearHover();
        }

        puzzleRenderer.Redraw();
    }

    // ──────────────────────────────────────────
    // 內部
    // ──────────────────────────────────────────
    void LoadLevel()
    {
        if (levelData != null)
        {
            Data = levelData.Build();
        }
        else if (autoGenerate)
        {
            if (generatorSeed != 0) Random.InitState(generatorSeed);
            var cfg = PuzzleLevelGenerator.GeneratorConfig.Default;
            Data = PuzzleLevelGenerator.Generate(cfg);
            if (Data == null) Data = BuildDefaultLevel(); // 萬一失敗才用預設
        }
        else
        {
            Data = BuildDefaultLevel();
        }
        Data.Trace();
        savedInitialData = Data.Clone(); // 存快照，Reset 用
    }

    /// <summary>
    /// 重新生成一個新關卡（可在外部呼叫，例如按鈕）
    /// </summary>
    public void GenerateNewLevel(int seed = 0)
    {
        if (seed != 0) Random.InitState(seed);
        var cfg = PuzzleLevelGenerator.GeneratorConfig.Default;
        Data = PuzzleLevelGenerator.Generate(cfg) ?? BuildDefaultLevel();
        Data.Trace();
        puzzleRenderer.Redraw();
        UpdateStatusText();
    }

    void RefreshPuzzle()
    {
        Data.Trace();
        puzzleRenderer.Redraw();
        UpdateStatusText();

        if (Data.IsSolved)
        {
            OnPuzzleSolved?.Invoke();
            if (statusText) statusText.text = "✓ 解謎成功！";
            clearPanel?.Show();
        }
    }

    void ResetPuzzle()
    {
        // 還原到初始快照，不重新生成關卡
        if (savedInitialData != null)
        {
            Data = savedInitialData.Clone();
            Data.Trace();
        }
        else
        {
            LoadLevel();
        }
        puzzleRenderer.Redraw();
        UpdateStatusText();
        Debug.Log($"[{gameObject.name}] 關卡已重置");
    }

    void UpdateStatusText()
    {
        if (!statusText || Data.IsSolved) return;
        string mode = eraserMode ? "清除模式" : selectedType switch
        {
            CellType.MirrorSlash => "放置 / 鏡子",
            CellType.MirrorBack  => "放置 \\ 鏡子",
            CellType.Splitter    => "放置分光器",
            _ => ""
        };
        if (statusText) statusText.text = $"{mode}　左鍵放置・右鍵清除・ESC 關閉";
    }

    // ──────────────────────────────────────────
    // 預設關卡（程式碼範例）
    // ──────────────────────────────────────────
    static LaserPuzzleData BuildDefaultLevel()
    {
        var d = new LaserPuzzleData(7, 10);
        d.SetSource(3, PuzzleDirection.Right);
        d.AddTarget(3);

        // 牆壁
        foreach (var (r,c) in new[]{(2,2),(2,3),(4,6),(4,7),(1,7),(5,3)})
            d.SetCell(r, c, CellType.Wall);

        // 固定鏡子
        d.SetCell(0, 2, CellType.MirrorSlash, true);
        d.SetCell(6, 2, CellType.MirrorBack,  true);
        d.SetCell(0, 7, CellType.MirrorBack,  true);
        d.SetCell(6, 8, CellType.MirrorSlash, true);

        return d;
    }
}