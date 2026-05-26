using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 負責把 LaserPuzzleData 渲染到 UI Canvas 上。
/// 掛在有 RawImage 的 GameObject 上（作為 Canvas 的子物件）。
/// 用 Texture2D 即時繪製，不需要額外資源。
/// </summary>
public class LaserPuzzleRenderer : MonoBehaviour
{
    [Header("資料來源")]
    [SerializeField] private LaserPuzzleController controller;

    [Header("UI 元件")]
    [SerializeField] private RawImage displayImage;   // 顯示謎題的 RawImage
    [SerializeField] private GameObject solvedPanel;  // 解謎成功顯示的 Panel（可選）

    [Header("外觀設定")]
    [SerializeField] private int cellPixels  = 60;    // 每格像素大小
    [SerializeField] private int borderPixels = 2;   // 格線寬度

    [Header("顏色")]
    [SerializeField] private Color bgColor       = new Color(0.12f, 0.12f, 0.14f);
    [SerializeField] private Color gridColor     = new Color(0.25f, 0.25f, 0.28f);
    [SerializeField] private Color emptyCellColor= new Color(0.18f, 0.18f, 0.20f);
    [SerializeField] private Color wallColor     = new Color(0.30f, 0.30f, 0.33f);
    [SerializeField] private Color mirrorColor   = new Color(0.36f, 0.78f, 0.65f);
    [SerializeField] private Color fixedTint     = new Color(0.20f, 0.50f, 0.40f, 0.3f);
    [SerializeField] private Color splitterColor = new Color(0.62f, 0.60f, 0.88f);
    [SerializeField] private Color sourceColor   = new Color(0.90f, 0.28f, 0.28f);
    [SerializeField] private Color targetColor   = new Color(0.39f, 0.60f, 0.13f);
    [SerializeField] private Color targetHitColor= new Color(0.59f, 0.90f, 0.25f);
    [SerializeField] private Color hoverColor    = new Color(1f,   1f,    1f,  0.08f);

    private Texture2D tex;
    private LaserPuzzleData Data => controller.Data;
    private Vector2Int hoveredCell = new(-1, -1);

    // ──────────────────────────────────────────
    void Awake()
    {
        CreateTexture();
        if (solvedPanel) solvedPanel.SetActive(false);
    }

    void CreateTexture()
    {
        if (Data == null) return;
        int texW = Data.Cols * cellPixels;
        int texH = Data.Rows * cellPixels;
        tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        displayImage.texture = tex;

        // 調整 RawImage 大小
        var rt = displayImage.rectTransform;
        rt.sizeDelta = new Vector2(texW, texH);
    }

    // 外部呼叫更新渲染
    public void Redraw()
    {
        if (tex == null) CreateTexture();
        DrawGrid();
        DrawPieces();
        DrawLasers();
        tex.Apply();

        if (solvedPanel)
            solvedPanel.SetActive(Data.IsSolved);
    }

    // ──────────────────────────────────────────
    // 繪製函式
    // ──────────────────────────────────────────
    void DrawGrid()
    {
        // 背景
        FillRect(0, 0, tex.width, tex.height, bgColor);

        for (int r = 0; r < Data.Rows; r++)
        for (int c = 0; c < Data.Cols; c++)
        {
            int px = c * cellPixels + borderPixels;
            int py = r * cellPixels + borderPixels;
            int sz = cellPixels - borderPixels * 2;

            var cell = Data.Grid[r, c];
            Color fill = cell.Type == LaserPuzzleData.CellType.Wall
                ? wallColor : emptyCellColor;

            FillRect(px, py, sz, sz, fill);

            // hover 高亮
            if (hoveredCell == new Vector2Int(r, c) && !cell.IsFixed && cell.Type != LaserPuzzleData.CellType.Wall)
                FillRect(px, py, sz, sz, hoverColor);

            // 格線
            DrawRectBorder(c * cellPixels, r * cellPixels, cellPixels, cellPixels, gridColor, borderPixels);
        }

        // Source 指示箭頭（左側）
        int sRow = Data.Source.Row;
        DrawArrow(0, sRow * cellPixels + cellPixels / 2, 16, 0, sourceColor);

        // Target 指示點（右側）
        foreach (var t in Data.Targets)
        {
            int ty = t.Row * cellPixels + cellPixels / 2;
            int tx = tex.width - 1;
            Color tc = t.IsHit ? targetHitColor : targetColor;
            DrawCircle(tx - 8, ty, 8, tc);
        }
    }

    void DrawPieces()
    {
        for (int r = 0; r < Data.Rows; r++)
        for (int c = 0; c < Data.Cols; c++)
        {
            var cell = Data.Grid[r, c];
            if (cell.Type == LaserPuzzleData.CellType.Empty || cell.Type == LaserPuzzleData.CellType.Wall)
                continue;

            int cx = c * cellPixels + cellPixels / 2;
            int cy = r * cellPixels + cellPixels / 2;
            int half = cellPixels / 2 - 6;

            if (cell.IsFixed)
                FillRect(c * cellPixels + borderPixels, r * cellPixels + borderPixels,
                         cellPixels - borderPixels * 2, cellPixels - borderPixels * 2, fixedTint);

            switch (cell.Type)
            {
                case LaserPuzzleData.CellType.MirrorSlash:
                    DrawLine(cx - half, cy + half, cx + half, cy - half, mirrorColor, 3);
                    break;
                case LaserPuzzleData.CellType.MirrorBack:
                    DrawLine(cx - half, cy - half, cx + half, cy + half, mirrorColor, 3);
                    break;
                case LaserPuzzleData.CellType.Splitter:
                    DrawLine(cx - half, cy + half, cx + half, cy - half, splitterColor, 2);
                    DrawLine(cx - half, cy - half, cx + half, cy + half, splitterColor, 2);
                    DrawCircle(cx, cy, 5, splitterColor);
                    break;
            }
        }
    }

    void DrawLasers()
    {
        foreach (var beam in Data.Beams)
        {
            Vector2Int from = beam.From;
            Vector2Int to   = beam.To;

            int x1 = GridToPixelX(from.y);
            int y1 = GridToPixelY(from.x);
            int x2 = GridToPixelX(to.y);
            int y2 = GridToPixelY(to.x);

            DrawLine(x1, y1, x2, y2, beam.Color, 2);
        }
    }

    // ──────────────────────────────────────────
    // 座標換算（grid row/col → 像素中心）
    // ──────────────────────────────────────────
    int GridToPixelX(int col)
    {
        if (col < 0) return 0;
        if (col >= Data.Cols) return tex.width;
        return col * cellPixels + cellPixels / 2;
    }

    int GridToPixelY(int row)
    {
        if (row < 0) return 0;
        if (row >= Data.Rows) return tex.height;
        return row * cellPixels + cellPixels / 2;
    }

    // ──────────────────────────────────────────
    // 滑鼠輸入轉換（供 Controller 呼叫）
    // ──────────────────────────────────────────
    public bool TryGetGridCell(Vector2 screenPos, out Vector2Int cell)
    {
        cell = new Vector2Int(-1, -1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            displayImage.rectTransform, screenPos,
            controller.PuzzleCamera, out Vector2 local);

        // local 以 Pivot 為原點（預設左下=0,0 in Unity UI...但 RawImage 預設 pivot 中心）
        // 調整：將 local 從中心轉換到左上角
        var rect = displayImage.rectTransform.rect;
        float lx = local.x + rect.width  / 2f;
        float ly = local.y + rect.height / 2f;

        int c = Mathf.FloorToInt(lx / cellPixels);
        int r = Mathf.FloorToInt(ly / cellPixels);

        // Texture Y 軸：Unity UI 由下往上，但 Texture2D SetPixel 由下往上也一樣
        // 所以需要反轉 row
        r = Data.Rows - 1 - r;

        if (r < 0 || r >= Data.Rows || c < 0 || c >= Data.Cols) return false;
        cell = new Vector2Int(r, c);
        return true;
    }

    public void SetHover(Vector2Int cell) { hoveredCell = cell; }
    public void ClearHover() { hoveredCell = new Vector2Int(-1, -1); }

    // ──────────────────────────────────────────
    // 底層繪圖工具（Texture2D）
    // ──────────────────────────────────────────
    void FillRect(int x, int y, int w, int h, Color c)
    {
        for (int dx = 0; dx < w; dx++)
        for (int dy = 0; dy < h; dy++)
        {
            int px = x + dx, py = tex.height - 1 - (y + dy);
            if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
            {
                Color existing = tex.GetPixel(px, py);
                tex.SetPixel(px, py, BlendAlpha(existing, c));
            }
        }
    }

    void DrawRectBorder(int x, int y, int w, int h, Color c, int thickness)
    {
        FillRect(x, y, w, thickness, c);
        FillRect(x, y + h - thickness, w, thickness, c);
        FillRect(x, y, thickness, h, c);
        FillRect(x + w - thickness, y, thickness, h, c);
    }

    void DrawLine(int x0, int y0, int x1, int y1, Color c, int thickness = 1)
    {
        // Bresenham
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            for (int ox = -thickness/2; ox <= thickness/2; ox++)
            for (int oy = -thickness/2; oy <= thickness/2; oy++)
                SetPixelSafe(x0 + ox, y0 + oy, c);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 <  dx) { err += dx; y0 += sy; }
        }
    }

    void DrawArrow(int x, int y, int size, int dir, Color c)
    {
        // dir=0: 右向箭頭
        for (int i = 0; i <= size; i++)
        {
            int half = size - i;
            for (int dy = -half; dy <= half; dy++)
                SetPixelSafe(x + i, y + dy, c);
        }
    }

    void DrawCircle(int cx, int cy, int r, Color c)
    {
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
            if (dx * dx + dy * dy <= r * r)
                SetPixelSafe(cx + dx, cy + dy, c);
    }

    void SetPixelSafe(int x, int y, Color c)
    {
        int py = tex.height - 1 - y;
        if (x < 0 || x >= tex.width || py < 0 || py >= tex.height) return;
        tex.SetPixel(x, py, c);
    }

    Color BlendAlpha(Color dst, Color src)
    {
        float a = src.a;
        return new Color(
            src.r * a + dst.r * (1 - a),
            src.g * a + dst.g * (1 - a),
            src.b * a + dst.b * (1 - a),
            dst.a + a * (1 - dst.a));
    }
}
