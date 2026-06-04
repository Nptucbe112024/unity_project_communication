using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 解謎成功動畫面板（純 Coroutine，不用 Animator）
/// 
/// 動畫流程：
/// 1. Panel 淡入
/// 2. 外框從中心往外擴張
/// 3. 掃描線從上到下掃過
/// 4. 勾勾圓圈縮放彈跳出現
/// 5. 標題文字逐字打印
/// 6. 副標文字淡入
/// 7. 外框持續輕微閃爍
///
/// 使用方式：
///   呼叫 Show() 開始播放動畫
///   呼叫 Hide() 立刻隱藏
/// </summary>
public class PuzzleClearPanel : MonoBehaviour
{
    [Header("根節點")]
    [SerializeField] private CanvasGroup rootGroup;        // 整個 Panel 的淡入淡出

    [Header("背景")]
    [SerializeField] private Image bgOverlay;              // 半透明深色背景

    [Header("外框")]
    [SerializeField] private RectTransform borderRect;     // 外框 RectTransform
    [SerializeField] private Image borderImage;            // 外框 Image（顏色控制閃爍）

    [Header("掃描線")]
    [SerializeField] private RectTransform scanLine;       // 細長橫條，代表雷射掃描線
    [SerializeField] private Image scanLineImage;

    [Header("勾勾區域")]
    [SerializeField] private RectTransform checkCircle;    // 圓圈 + 勾勾的父容器

    [Header("文字")]
    [SerializeField] private TextMeshProUGUI titleText;    // 「解謎成功」
    [SerializeField] private TextMeshProUGUI subtitleText; // 「雷射順利抵達終點」
    [SerializeField] private TextMeshProUGUI hintText;     // 「按 ESC 離開」

    [Header("顏色設定")]
    [SerializeField] private Color laserColor   = new Color(0.24f, 0.81f, 0.65f); // 主色（青綠）
    [SerializeField] private Color scanColor    = new Color(0.24f, 0.81f, 0.65f, 0.6f);
    [SerializeField] private Color bgColor      = new Color(0.05f, 0.10f, 0.08f, 0.92f);

    [Header("時間設定")]
    [SerializeField] private float fadeInDuration     = 0.3f;
    [SerializeField] private float borderExpandDur    = 0.4f;
    [SerializeField] private float scanDuration       = 0.5f;
    [SerializeField] private float checkBounceDur     = 0.45f;
    [SerializeField] private float typewriterSpeed    = 0.06f; // 每個字的間隔秒數
    [SerializeField] private float subtitleFadeDur    = 0.4f;

    private Coroutine currentAnim;
    private string titleFull    = "PUZZLE CLEAR";
    private string subtitleFull = "Laser reached the target!";
    private string hintFull     = "Press ESC to exit";

    // ──────────────────────────────────────────
    void Awake()
    {
        gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────
    public void Show()
    {
        gameObject.SetActive(true);
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(PlayClearAnimation());
    }

    public void Hide()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────
    // 主動畫序列
    // ──────────────────────────────────────────
    IEnumerator PlayClearAnimation()
    {
        ResetAll();

        // ── 1. 背景淡入 ──
        yield return Fade(rootGroup, 0f, 1f, fadeInDuration);

        // ── 2. 外框從中心往外擴張 ──
        yield return ExpandBorder();

        // ── 3. 掃描線從上到下 ──
        yield return ScanLine();

        // ── 4. 勾勾彈跳出現 ──
        yield return BounceIn(checkCircle, checkBounceDur);

        // ── 5. 標題逐字打印 ──
        yield return Typewriter(titleText, titleFull, typewriterSpeed);

        // ── 6. 副標淡入 ──
        yield return FadeText(subtitleText, subtitleFull, subtitleFadeDur);
        yield return new WaitForSeconds(0.15f);
        yield return FadeText(hintText, hintFull, subtitleFadeDur);

        // ── 7. 外框持續閃爍 ──
        currentAnim = StartCoroutine(BorderFlicker());
    }

    // ──────────────────────────────────────────
    // 動畫片段
    // ──────────────────────────────────────────

    IEnumerator ExpandBorder()
    {
        if (!borderRect) yield break;
        float t = 0f;

        // 收集四條邊的 Image
        var edges = new System.Collections.Generic.List<Image>();
        foreach (Transform child in borderRect)
        {
            var img = child.GetComponent<Image>();
            if (img) edges.Add(img);
        }

        while (t < borderExpandDur)
        {
            t += Time.deltaTime;
            float p = EaseOutCubic(t / borderExpandDur);
            Color c = new Color(laserColor.r, laserColor.g, laserColor.b, p);
            foreach (var e in edges) e.color = c;
            if (borderImage) borderImage.color = new Color(laserColor.r, laserColor.g, laserColor.b, p);
            yield return null;
        }
        Color final = new Color(laserColor.r, laserColor.g, laserColor.b, 1f);
        foreach (var e in edges) e.color = final;
    }

    IEnumerator ScanLine()
    {
        if (!scanLine) yield break;
        scanLine.gameObject.SetActive(true);

        RectTransform parent = scanLine.parent as RectTransform;
        float panelH = parent ? parent.rect.height : 300f;
        float startY =  panelH / 2f + 10f;
        float endY   = -panelH / 2f - 10f;

        float t = 0f;
        while (t < scanDuration)
        {
            t += Time.deltaTime;
            float p  = t / scanDuration;
            float py = Mathf.Lerp(startY, endY, p);
            scanLine.anchoredPosition = new Vector2(0f, py);

            // 掃描線靠近中間時最亮，靠近邊緣時漸暗
            float alpha = Mathf.Sin(p * Mathf.PI) * 0.85f;
            if (scanLineImage)
                scanLineImage.color = new Color(scanColor.r, scanColor.g, scanColor.b, alpha);

            yield return null;
        }
        scanLine.gameObject.SetActive(false);
    }

    IEnumerator BounceIn(RectTransform target, float duration)
    {
        if (!target) yield break;
        target.gameObject.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            // Elastic overshoot：超過 1 再縮回來
            float scale = ElasticOut(p);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator Typewriter(TextMeshProUGUI label, string fullText, float charDelay)
    {
        if (!label) yield break;
        label.gameObject.SetActive(true);
        label.text = "";
        label.color = new Color(label.color.r, label.color.g, label.color.b, 1f);

        foreach (char c in fullText)
        {
            label.text += c;
            // 每打一個字閃一下游標感
            yield return new WaitForSeconds(charDelay);
        }
    }

    IEnumerator FadeText(TextMeshProUGUI label, string fullText, float duration)
    {
        if (!label) yield break;
        label.gameObject.SetActive(true);
        label.text = fullText;

        float t = 0f;
        Color c = label.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            label.color = new Color(c.r, c.g, c.b, t / duration);
            yield return null;
        }
        label.color = new Color(c.r, c.g, c.b, 1f);
    }

    IEnumerator BorderFlicker()
    {
        if (!borderRect) yield break;
        var edges = new System.Collections.Generic.List<Image>();
        foreach (Transform child in borderRect)
        {
            var img = child.GetComponent<Image>();
            if (img) edges.Add(img);
        }

        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 1.8f;
            float alpha = 0.6f + Mathf.Sin(t * Mathf.PI) * 0.4f;
            Color c = new Color(laserColor.r, laserColor.g, laserColor.b, alpha);
            foreach (var e in edges) e.color = c;
            if (borderImage) borderImage.color = c;
            yield return null;
        }
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (!group) yield break;
        float t = 0f;
        group.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    // ──────────────────────────────────────────
    // 初始化
    // ──────────────────────────────────────────
    void ResetAll()
    {
        if (rootGroup)    rootGroup.alpha = 0f;

        // 背景：深色半透明，確保蓋過底層
        if (bgOverlay)
        {
            bgOverlay.color = bgColor;
            bgOverlay.raycastTarget = true;
        }

        // 邊框：從零大小開始擴張
        if (borderImage)
        {
            borderImage.color = new Color(laserColor.r, laserColor.g, laserColor.b, 0f);
        }
        if (borderRect)
        {
            _targetBorderSize = GetTargetBorderSize();
            borderRect.sizeDelta = Vector2.zero;
        }

        if (scanLine)     scanLine.gameObject.SetActive(false);
        if (checkCircle)  { checkCircle.gameObject.SetActive(false); checkCircle.localScale = Vector3.zero; }
        if (titleText)    { titleText.text = ""; titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, 0f); titleText.gameObject.SetActive(false); }
        if (subtitleText) { subtitleText.text = ""; subtitleText.color = new Color(subtitleText.color.r, subtitleText.color.g, subtitleText.color.b, 0f); subtitleText.gameObject.SetActive(false); }
        if (hintText)     { hintText.text = ""; hintText.color = new Color(hintText.color.r, hintText.color.g, hintText.color.b, 0f); hintText.gameObject.SetActive(false); }
    }

    Vector2 _targetBorderSize;

    Vector2 GetTargetBorderSize()
    {
        var selfRt = GetComponent<RectTransform>();
        if (selfRt != null) return selfRt.rect.size;
        if (borderRect && borderRect.parent is RectTransform p) return p.rect.size;
        return new Vector2(340f, 200f);
    }

    // ──────────────────────────────────────────
    // Easing 函式
    // ──────────────────────────────────────────
    static float EaseOutCubic(float t) =>
        1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

    static float ElasticOut(float t)
    {
        t = Mathf.Clamp01(t);
        if (t == 0f || t == 1f) return t;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
}