using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 雷射解謎資料結構與雷射追蹤邏輯
/// 純 C# 資料層，不依賴 MonoBehaviour
/// </summary>
// enums 搬到 class 外，所有腳本可直接引用
public enum CellType  { Empty, Wall, MirrorSlash, MirrorBack, Splitter }
public enum PuzzleDirection { Up, Down, Left, Right }

public class LaserPuzzleData
{

    public class Cell
    {
        public CellType Type = CellType.Empty;
        public bool IsFixed = false; // 固定方塊，玩家不可修改
    }

    public struct LaserBeam
    {
        public Vector2Int From; // grid 座標（-1 或 Cols 代表邊界外）
        public Vector2Int To;
        public Color Color;
        public bool IsActive;
    }

    public struct LaserSource
    {
        public int Row;
        public PuzzleDirection Dir;
    }

    public struct LaserTarget
    {
        public int Row;
        public bool IsHit;
    }

    // ──────────────────────────────────────────
    // Grid 資料
    // ──────────────────────────────────────────
    public int Rows { get; private set; }
    public int Cols { get; private set; }
    public Cell[,] Grid { get; private set; }
    public LaserSource Source { get; private set; }
    public List<LaserTarget> Targets { get; private set; }

    // 追蹤結果（每次呼叫 Trace 後更新）
    public List<LaserBeam> Beams { get; private set; } = new();
    public bool IsSolved { get; private set; }

    private static readonly Color LaserColor = new Color(1f, 0.25f, 0.25f, 0.9f);
    private static readonly Color SplitColor  = new Color(1f, 0.6f,  0.1f, 0.9f);

    // ──────────────────────────────────────────
    // 初始化
    // ──────────────────────────────────────────
    public LaserPuzzleData(int rows, int cols)
    {
        Rows = rows; Cols = cols;
        Grid = new Cell[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                Grid[r, c] = new Cell();
        Targets = new List<LaserTarget>();
    }

    public void SetSource(int row, PuzzleDirection dir) =>
        Source = new LaserSource { Row = row, Dir = dir };

    public void AddTarget(int row) =>
        Targets.Add(new LaserTarget { Row = row, IsHit = false });

    public void SetCell(int r, int c, CellType type, bool isFixed = false)
    {
        if (!InBounds(r, c)) return;
        Grid[r, c].Type = type;
        Grid[r, c].IsFixed = isFixed;
    }

    public bool TryPlaceCell(int r, int c, CellType type)
    {
        if (!InBounds(r, c)) return false;
        if (Grid[r, c].IsFixed) return false;
        if (Grid[r, c].Type == CellType.Wall) return false;
        Grid[r, c].Type = type;
        return true;
    }

    public bool TryRemoveCell(int r, int c)
    {
        if (!InBounds(r, c)) return false;
        if (Grid[r, c].IsFixed) return false;
        Grid[r, c].Type = CellType.Empty;
        return true;
    }

    // ──────────────────────────────────────────
    // 雷射追蹤
    // ──────────────────────────────────────────
    public void Trace()
    {
        Beams.Clear();
        // 重置目標
        for (int i = 0; i < Targets.Count; i++)
        {
            var t = Targets[i];
            t.IsHit = false;
            Targets[i] = t;
        }

        var startPos = Source.Dir switch
        {
            PuzzleDirection.Right => new Vector2Int(Source.Row, -1),
            PuzzleDirection.Left  => new Vector2Int(Source.Row, Cols),
            PuzzleDirection.Down  => new Vector2Int(-1, Source.Row),
            PuzzleDirection.Up    => new Vector2Int(Rows, Source.Row),
            _ => new Vector2Int(Source.Row, -1)
        };
        var startDir = DirToVec(Source.Dir);
        TraceRay(startPos, startDir, LaserColor, 0);

        IsSolved = Targets.TrueForAll(t => t.IsHit);
    }

    private void TraceRay(Vector2Int pos, Vector2Int dir, Color color, int depth)
    {
        if (depth > 50) return; // 防無限迴圈

        Vector2Int cur = pos + dir;
        Vector2Int segStart = pos;

        while (InBoundsOrBorder(cur))
        {
            if (!InBounds(cur.x, cur.y))
            {
                // 離開 grid → 記錄線段，檢查目標
                AddBeam(segStart, cur, color);
                CheckTargetExit(cur, dir);
                return;
            }

            var cell = Grid[cur.x, cur.y];

            if (cell.Type == CellType.Wall)
            {
                AddBeam(segStart, cur, color);
                return;
            }

            if (cell.Type != CellType.Empty)
            {
                AddBeam(segStart, cur, color);
                HandleMirror(cur, dir, cell.Type, color, depth);
                return;
            }

            cur += dir;
        }

        AddBeam(segStart, cur - dir, color);
    }

    private void HandleMirror(Vector2Int pos, Vector2Int dir, CellType type, Color color, int depth)
    {
        switch (type)
        {
            case CellType.MirrorSlash:
                // / 鏡：(dr,dc) → (-dc,-dr)
                TraceRay(pos, new Vector2Int(-dir.y, -dir.x), color, depth + 1);
                break;
            case CellType.MirrorBack:
                // \ 鏡：(dr,dc) → (dc,dr)
                TraceRay(pos, new Vector2Int(dir.y, dir.x), color, depth + 1);
                break;
            case CellType.Splitter:
                // 分光：直穿 + 兩側反射
                TraceRay(pos, dir,                              color,      depth + 1);
                TraceRay(pos, new Vector2Int(-dir.y, -dir.x),  color,      depth + 1);
                TraceRay(pos, new Vector2Int(dir.y,  dir.x),   SplitColor, depth + 1);
                break;
        }
    }

    private void CheckTargetExit(Vector2Int exitPos, Vector2Int dir)
    {
        // 目前預設：Source 從左射出，Target 在右側
        // 可依需求擴充為四方向目標
        if (exitPos.y >= Cols && dir.y == 1)
        {
            for (int i = 0; i < Targets.Count; i++)
            {
                var t = Targets[i];
                if (t.Row == exitPos.x)
                {
                    t.IsHit = true;
                    Targets[i] = t;
                }
            }
        }
    }

    private void AddBeam(Vector2Int from, Vector2Int to, Color color)
    {
        Beams.Add(new LaserBeam { From = from, To = to, Color = color, IsActive = true });
    }

    private static Vector2Int DirToVec(PuzzleDirection d) => d switch
    {
        PuzzleDirection.Up    => new Vector2Int(-1, 0),
        PuzzleDirection.Down  => new Vector2Int( 1, 0),
        PuzzleDirection.Left  => new Vector2Int( 0,-1),
        PuzzleDirection.Right => new Vector2Int( 0, 1),
        _ => Vector2Int.right
    };

    private bool InBounds(int r, int c) =>
        r >= 0 && r < Rows && c >= 0 && c < Cols;

    private bool InBoundsOrBorder(Vector2Int v) =>
        v.x >= -1 && v.x <= Rows && v.y >= -1 && v.y <= Cols;
    /// <summary>
    /// 深拷貝：複製整個 Grid 狀態，用於重置快照
    /// </summary>
    public LaserPuzzleData Clone()
    {
        var copy = new LaserPuzzleData(Rows, Cols);
        copy.Source  = this.Source;
        copy.Targets = new System.Collections.Generic.List<LaserTarget>();
        foreach (var t in this.Targets)
            copy.Targets.Add(t);

        for (int r = 0; r < Rows; r++)
        for (int c = 0; c < Cols; c++)
        {
            copy.Grid[r, c].Type    = this.Grid[r, c].Type;
            copy.Grid[r, c].IsFixed = this.Grid[r, c].IsFixed;
        }
        return copy;
    }

}