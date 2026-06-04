#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor 工具：自動建立 ClearPanel 所有子物件並排版
/// 使用方式：選中掛有 PuzzleClearPanel 的 GameObject
///           選單 → Tools → Laser Puzzle → Build Clear Panel UI
/// </summary>
public static class PuzzleClearPanelBuilder
{
    [MenuItem("Tools/Laser Puzzle/Build Clear Panel UI")]
    static void BuildClearPanel()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("錯誤", "請先在 Hierarchy 選中掛有 PuzzleClearPanel 的 GameObject", "OK");
            return;
        }

        var panel = selected.GetComponent<PuzzleClearPanel>();
        if (panel == null)
        {
            EditorUtility.DisplayDialog("錯誤", "選中的物件上沒有 PuzzleClearPanel 腳本", "OK");
            return;
        }

        // 確保有 CanvasGroup
        var cg = selected.GetComponent<CanvasGroup>();
        if (cg == null) cg = selected.AddComponent<CanvasGroup>();

        // 取得或設定 RectTransform（填滿父容器）
        var rt = selected.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 清除舊子物件（可選，避免重複建立）
        if (EditorUtility.DisplayDialog("清除舊子物件？",
            "是否先清除 ClearPanel 下的所有子物件再重建？", "清除重建", "保留現有"))
        {
            for (int i = selected.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(selected.transform.GetChild(i).gameObject);
        }

        // ── 建立各子物件 ──
        var bgOverlay   = CreateImage(selected, "BgOverlay",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.04f, 0.09f, 0.07f, 0.95f));
        // 確保 BgOverlay 在最底層
        bgOverlay.transform.SetAsFirstSibling();

        var border      = CreateBorder(selected);

        var scanLine    = CreateImage(selected, "ScanLine",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, -1.5f), new Vector2(0f, 1.5f),
            new Color(0.24f, 0.81f, 0.65f, 0.7f));

        var checkCircle = CreateCheckCircle(selected);

        var titleText   = CreateTMPText(selected, "TitleText",
            new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.58f),
            "PUZZLE CLEAR", 28, new Color(0.30f, 0.95f, 0.72f), FontStyles.Bold);

        var subtitleText = CreateTMPText(selected, "SubtitleText",
            new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.38f),
            "Laser reached the target!", 16, new Color(0.49f, 0.78f, 0.66f), FontStyles.Normal);

        var hintText    = CreateTMPText(selected, "HintText",
            new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.22f),
            "Press ESC to exit", 13, new Color(0.29f, 0.48f, 0.38f), FontStyles.Normal);

        // ── 用 SerializedObject 把欄位填入 ──
        var so = new SerializedObject(panel);
        so.FindProperty("rootGroup")    .objectReferenceValue = cg;
        so.FindProperty("bgOverlay")    .objectReferenceValue = bgOverlay.GetComponent<Image>();
        so.FindProperty("borderRect")   .objectReferenceValue = border.GetComponent<RectTransform>();
        so.FindProperty("borderImage")  .objectReferenceValue = border.GetComponent<Image>();
        so.FindProperty("scanLine")     .objectReferenceValue = scanLine.GetComponent<RectTransform>();
        so.FindProperty("scanLineImage").objectReferenceValue = scanLine.GetComponent<Image>();
        so.FindProperty("checkCircle")  .objectReferenceValue = checkCircle.GetComponent<RectTransform>();
        so.FindProperty("titleText")    .objectReferenceValue = titleText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("subtitleText") .objectReferenceValue = subtitleText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("hintText")     .objectReferenceValue = hintText.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        // 預設隱藏
        selected.SetActive(false);

        EditorUtility.SetDirty(selected);
        Debug.Log("[PuzzleClearPanelBuilder] ClearPanel 建立完成！所有欄位已自動填入。");
        EditorUtility.DisplayDialog("完成", "ClearPanel UI 建立完成！\n所有子物件與欄位已自動設定好。", "OK");
    }

    // ──────────────────────────────────────────
    // 輔助：建立 Image 物件
    // ──────────────────────────────────────────
    static GameObject CreateImage(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        go.GetComponent<Image>().color = color;
        return go;
    }

    // ──────────────────────────────────────────
    // 建立只有邊框的 Border
    // ──────────────────────────────────────────
    static GameObject CreateBorder(GameObject parent)
    {
        // 用四個細長 Image 拼出邊框，背景完全透明
        var container = new GameObject("Border", typeof(RectTransform));
        container.transform.SetParent(parent.transform, false);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.03f, 0.03f);
        rt.anchorMax = new Vector2(0.97f, 0.97f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Color borderCol = new Color(0.24f, 0.81f, 0.65f, 0f); // 初始透明，動畫控制
        float thickness = 2f;

        // 上
        CreateEdge(container, "Top",
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(0, -thickness), new Vector2(0, 0), borderCol);
        // 下
        CreateEdge(container, "Bottom",
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(0, 0), new Vector2(0, thickness), borderCol);
        // 左
        CreateEdge(container, "Left",
            new Vector2(0,0), new Vector2(0,1),
            new Vector2(0, 0), new Vector2(thickness, 0), borderCol);
        // 右
        CreateEdge(container, "Right",
            new Vector2(1,0), new Vector2(1,1),
            new Vector2(-thickness, 0), new Vector2(0, 0), borderCol);

        // 取第一個 Image 當 borderImage 給動畫控制顏色
        // PuzzleClearPanel 的 borderImage 控制整體顏色
        // 這裡在 container 上加一個透明 Image 作為控制用
        var controlImg = container.AddComponent<Image>();
        controlImg.color = new Color(0,0,0,0); // 完全透明，只用來傳顏色給動畫

        return container;
    }

    static void CreateEdge(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        go.GetComponent<Image>().color = color;
    }

    // ──────────────────────────────────────────
    // 建立 CheckCircle（圓圈 + 勾勾文字）
    // ──────────────────────────────────────────
    static GameObject CreateCheckCircle(GameObject parent)
    {
        // 外層容器
        var container = new GameObject("CheckCircle", typeof(RectTransform));
        container.transform.SetParent(parent.transform, false);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.55f);
        rt.anchorMax = new Vector2(0.5f, 0.55f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(80f, 80f);
        rt.anchoredPosition = Vector2.zero;

        // 圓圈背景
        var circle = new GameObject("Circle", typeof(RectTransform), typeof(Image));
        circle.transform.SetParent(container.transform, false);
        var crt = circle.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        var cImg = circle.GetComponent<Image>();
        cImg.color = new Color(0.07f, 0.22f, 0.16f, 0.9f);

        // 勾勾（用 TMP 文字 ✓）
        var check = new GameObject("CheckMark", typeof(RectTransform));
        check.transform.SetParent(container.transform, false);
        var checkTMP = check.AddComponent<TextMeshProUGUI>();
        var crt2 = check.GetComponent<RectTransform>();
        crt2.anchorMin = Vector2.zero; crt2.anchorMax = Vector2.one;
        crt2.offsetMin = new Vector2(4f, 0f); crt2.offsetMax = new Vector2(-4f, 0f);
        checkTMP.text      = "\u2713";
        checkTMP.fontSize  = 40;
        checkTMP.color     = new Color(0.24f, 0.81f, 0.65f);
        checkTMP.alignment = TextAlignmentOptions.Center;

        return container;
    }

    // ──────────────────────────────────────────
    // 建立 TMP 文字
    // ──────────────────────────────────────────
    static GameObject CreateTMPText(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        string text, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(color.r, color.g, color.b, 0f); // 初始透明

        return go;
    }
}
#endif