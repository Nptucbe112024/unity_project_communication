using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 保證有解的關卡自動生成器（反向生成法）
/// 
/// 演算法：
/// 1. 從 Source 出發，隨機放置鏡子讓雷射反射，直到抵達 Target
/// 2. 記錄這條「解答路徑」上的所有鏡子位置與類型
/// 3. 從解答中隨機移除幾個鏡子 → 給玩家放置
/// 4. 加入若干固定鏡子與牆壁增加難度
/// 5. 驗證初始狀態未通關
/// </summary>
public static class PuzzleLevelGenerator
{
    // ──────────────────────────────────────────
    public struct GeneratorConfig
    {
        public int Rows;
        public int Cols;
        public int SourceRow;       // 雷射來源列
        public int TargetRow;       // 目標列（右側射出）
        public int MinMirrors;      // 解答路徑最少鏡子數
        public int MaxMirrors;      // 解答路徑最多鏡子數
        public int WallCount;       // 隨機牆壁數量
        public int DecoyMirrors;    // 假鏡子（不在解答路徑上）
        public int MaxAttempts;     // 生成失敗重試上限

        public static GeneratorConfig Default => new GeneratorConfig
        {
            Rows = 7, Cols = 10,
            SourceRow = -1,  // -1 = 隨機
            TargetRow = -1,  // -1 = 隨機
            MinMirrors = 2, MaxMirrors = 5,
            WallCount = 4, DecoyMirrors = 2,
            MaxAttempts = 200
        };
    }

    // ──────────────────────────────────────────
    /// <summary>
    /// 生成一個保證有解、且初始未通關的關卡。
    /// 失敗時回傳 null（極少發生，可重試）。
    /// </summary>
    public static LaserPuzzleData Generate(GeneratorConfig cfg)
    {
        for (int attempt = 0; attempt < cfg.MaxAttempts; attempt++)
        {
            var result = TryGenerate(cfg);
            if (result != null) return result;
        }
        Debug.LogWarning("[PuzzleLevelGenerator] 達到重試上限，回傳預設關卡");
        return BuildFallback(cfg);
    }

    // ──────────────────────────────────────────
    static LaserPuzzleData TryGenerate(GeneratorConfig cfg)
    {
        // 隨機 Source / Target（-1 表示隨機）
        int sourceRow = cfg.SourceRow >= 0 ? cfg.SourceRow : Random.Range(0, cfg.Rows);
        int targetRow = cfg.TargetRow >= 0 ? cfg.TargetRow : Random.Range(0, cfg.Rows);

        var data = new LaserPuzzleData(cfg.Rows, cfg.Cols);
        data.SetSource(sourceRow, PuzzleDirection.Right);
        data.AddTarget(targetRow);

        // cfg 的 row 先暫存回去，讓後續函式用
        cfg.SourceRow = sourceRow;
        cfg.TargetRow = targetRow;

        // Step 1：加牆壁（避開來源列與目標列）
        var walls = PlaceWalls(data, cfg);

        // Step 2：反向生成解答路徑
        var solution = BuildSolutionPath(data, cfg, walls);
        if (solution == null || solution.Count < cfg.MinMirrors) return null;

        // Step 3：把解答鏡子全部放進 grid（先設為固定，之後再決定哪些移除）
        foreach (var m in solution)
            data.SetCell(m.Row, m.Col, m.Type, false);

        // Step 4：驗證此時已通關
        data.Trace();
        if (!data.IsSolved) return null;

        // Step 5：決定哪些鏡子固定（Fixed）、哪些讓玩家放
        // 保留 1~2 個固定，其餘清除給玩家擺
        int keepFixed = Mathf.Clamp(Random.Range(1, 3), 1, solution.Count - 1);
        var fixedIndices = new HashSet<int>();
        while (fixedIndices.Count < keepFixed)
            fixedIndices.Add(Random.Range(0, solution.Count));

        // 清除玩家要放的格子
        for (int i = 0; i < solution.Count; i++)
        {
            var m = solution[i];
            if (fixedIndices.Contains(i))
                data.SetCell(m.Row, m.Col, m.Type, true);   // 固定
            else
                data.SetCell(m.Row, m.Col, CellType.Empty, false); // 清除給玩家
        }

        // Step 6：加入假鏡子（增加干擾）
        PlaceDecoys(data, cfg, walls, solution);

        // Step 7：驗證初始狀態未通關
        data.Trace();
        if (data.IsSolved) return null; // 固定鏡子已經通關了，重來

        return data;
    }

    // ──────────────────────────────────────────
    // 從 Source 出發，隨機反射直到抵達 Target 右側
    // 回傳路徑上每個鏡子的位置與類型
    static List<MirrorInfo> BuildSolutionPath(LaserPuzzleData data, GeneratorConfig cfg,
                                               HashSet<Vector2Int> walls)
    {
        var mirrors = new List<MirrorInfo>();
        int r = cfg.SourceRow, c = 0;
        int dr = 0, dc = 1; // 初始向右
        int steps = 0;
        int maxSteps = cfg.Rows * cfg.Cols * 2;

        // 用來追蹤已訪問的格子+方向，防無限迴圈
        var visited = new HashSet<(int, int, int, int)>();

        while (steps++ < maxSteps)
        {
            // 移動一步
            r += dr; c += dc;

            // 抵達右側邊界 → 確認是否在目標列
            if (c >= cfg.Cols)
            {
                if (r == cfg.TargetRow && dc == 1)
                    return mirrors.Count >= cfg.MinMirrors ? mirrors : null;
                return null;
            }

            // 超出邊界（上下左）→ 失敗
            if (r < 0 || r >= cfg.Rows || c < 0) return null;

            // 撞牆 → 失敗
            if (walls.Contains(new Vector2Int(r, c))) return null;

            // 防無限迴圈
            var state = (r, c, dr, dc);
            if (visited.Contains(state)) return null;
            visited.Add(state);

            // 如果鏡子數已達上限，只能直走到終點
            if (mirrors.Count >= cfg.MaxMirrors) continue;

            // 隨機決定：直走 or 放鏡子反射
            // 越接近邊界越傾向直走
            float reflectChance = 0.45f;
            if (c > cfg.Cols - 3) reflectChance = 0.1f; // 快到右邊就直走

            if (Random.value < reflectChance)
            {
                // 選擇鏡子類型：讓雷射轉 90 度
                CellType mirrorType;
                int newDr, newDc;

                // / 鏡：(dr,dc) → (-dc,-dr)
                // \ 鏡：(dr,dc) → (dc,dr)
                if (Random.value < 0.5f)
                {
                    mirrorType = CellType.MirrorSlash;
                    newDr = -dc; newDc = -dr;
                }
                else
                {
                    mirrorType = CellType.MirrorBack;
                    newDr = dc; newDc = dr;
                }

                // 確認轉向後不會立刻出界或撞牆
                int nr = r + newDr, nc = c + newDc;
                bool nextOk = nr >= 0 && nr < cfg.Rows && nc >= 0 &&
                              !walls.Contains(new Vector2Int(nr, nc));

                if (nextOk)
                {
                    mirrors.Add(new MirrorInfo { Row = r, Col = c, Type = mirrorType });
                    dr = newDr; dc = newDc;
                }
            }
        }
        return null;
    }

    // ──────────────────────────────────────────
    static HashSet<Vector2Int> PlaceWalls(LaserPuzzleData data, GeneratorConfig cfg)
    {
        var walls = new HashSet<Vector2Int>();
        int attempts = 0;
        while (walls.Count < cfg.WallCount && attempts < 100)
        {
            attempts++;
            int r = Random.Range(0, cfg.Rows);
            int c = Random.Range(1, cfg.Cols - 1); // 不放邊緣

            // 避開來源列左側入口 & 目標列右側出口
            if (r == cfg.SourceRow && c <= 1) continue;
            if (r == cfg.TargetRow && c >= cfg.Cols - 2) continue;

            var pos = new Vector2Int(r, c);
            if (!walls.Contains(pos))
            {
                walls.Add(pos);
                data.SetCell(r, c, CellType.Wall);
            }
        }
        return walls;
    }

    static void PlaceDecoys(LaserPuzzleData data, GeneratorConfig cfg,
                            HashSet<Vector2Int> walls, List<MirrorInfo> solution)
    {
        var solutionPos = new HashSet<Vector2Int>();
        foreach (var m in solution) solutionPos.Add(new Vector2Int(m.Row, m.Col));

        var types = new[] { CellType.MirrorSlash, CellType.MirrorBack, CellType.Splitter };
        int placed = 0, attempts = 0;

        while (placed < cfg.DecoyMirrors && attempts < 100)
        {
            attempts++;
            int r = Random.Range(0, cfg.Rows);
            int c = Random.Range(1, cfg.Cols - 1);
            var pos = new Vector2Int(r, c);

            if (walls.Contains(pos)) continue;
            if (solutionPos.Contains(pos)) continue;
            if (data.Grid[r, c].Type != CellType.Empty) continue;

            data.SetCell(r, c, types[Random.Range(0, types.Length)], false);
            placed++;
        }
    }

    // ──────────────────────────────────────────
    static LaserPuzzleData BuildFallback(GeneratorConfig cfg)
    {
        int sr = cfg.SourceRow >= 0 ? cfg.SourceRow : cfg.Rows / 2;
        int tr = cfg.TargetRow >= 0 ? cfg.TargetRow : cfg.Rows / 2;
        var d = new LaserPuzzleData(cfg.Rows, cfg.Cols);
        d.SetSource(sr, PuzzleDirection.Right);
        d.AddTarget(tr);
        d.SetCell(0, 2, CellType.MirrorSlash, true);
        d.SetCell(6, 2, CellType.MirrorBack,  true);
        d.SetCell(0, 7, CellType.MirrorBack,  true);
        d.SetCell(6, 8, CellType.MirrorSlash, true);
        return d;
    }

    struct MirrorInfo { public int Row, Col; public CellType Type; }
}
