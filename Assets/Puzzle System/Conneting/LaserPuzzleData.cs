using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject：在 Inspector 或程式碼中定義關卡。
/// 建立方式：Assets 右鍵 → Create → Laser Puzzle → Level Data
/// </summary>
[CreateAssetMenu(menuName = "Laser Puzzle/Level Data", fileName = "NewPuzzleLevel")]
public class PuzzleLevelData : ScriptableObject
{
    [Header("格子大小")]
    public int rows = 7;
    public int cols = 10;

    [Header("雷射來源（從左側射出）")]
    public int sourceRow = 3;
    public LaserPuzzleData.Direction sourceDir = LaserPuzzleData.Direction.Right;

    [Header("目標（到達右側哪一行）")]
    public List<int> targetRows = new() { 3 };

    [Header("牆壁")]
    public List<Vector2Int> walls = new();

    [Header("固定方塊（玩家不可移動）")]
    public List<FixedCell> fixedCells = new();

    [Header("隨機初始方塊")]
    public bool randomize = false;
    [Tooltip("隨機放置幾個可移動方塊")]
    public int randomCount = 3;

    [System.Serializable]
    public class FixedCell
    {
        public Vector2Int pos;
        public LaserPuzzleData.CellType type;
    }

    // ──────────────────────────────────────────
    public LaserPuzzleData Build()
    {
        var d = new LaserPuzzleData(rows, cols);
        d.SetSource(sourceRow, sourceDir);
        foreach (var r in targetRows) d.AddTarget(r);
        foreach (var w in walls)      d.SetCell(w.x, w.y, LaserPuzzleData.CellType.Wall);
        foreach (var f in fixedCells) d.SetCell(f.pos.x, f.pos.y, f.type, true);

        if (randomize) PlaceRandom(d);
        return d;
    }

    void PlaceRandom(LaserPuzzleData d)
    {
        var types = new[]
        {
            LaserPuzzleData.CellType.MirrorSlash,
            LaserPuzzleData.CellType.MirrorBack,
            LaserPuzzleData.CellType.Splitter
        };

        int placed = 0, attempts = 0;
        while (placed < randomCount && attempts < 200)
        {
            attempts++;
            int r = Random.Range(0, rows);
            int c = Random.Range(1, cols - 1); // 避免邊緣
            var cell = d.Grid[r, c];
            if (cell.Type != LaserPuzzleData.CellType.Empty) continue;
            var t = types[Random.Range(0, types.Length)];
            d.SetCell(r, c, t, false);
            placed++;
        }
    }
}