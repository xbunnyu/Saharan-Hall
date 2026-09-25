using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบจัดการวัน (Day Manager)
/// - นับวันปัจจุบัน
/// - แสดง HUD วันบนมุมบนซ้าย (ต้องลาก Text UI มาใส่)
/// - แสดง Overlay "เช้าวันที่ X" เมื่อขึ้นวันใหม่
/// - สั่ง HallManager reset เมื่อขึ้นวันใหม่
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    [Header("วันปัจจุบัน")]
    [Tooltip("วันเริ่มต้นของเกม")]
    public int currentDay = 1;

    // ─────────────────────────────────────────────
    [Header("HUD แสดงวัน (ลาก Text UI มาใส่)")]
    [Tooltip("TextMeshPro สำหรับแสดงวันปัจจุบันบนหน้าจอ (ถ้าไม่ใส่จะสร้างอัตโนมัติ)")]
    public TMP_Text dayHUDText;

    // ─────────────────────────────────────────────
    [Header("Overlay ขึ้นวันใหม่")]
    [Tooltip("CanvasGroup ที่ครอบคลุมหน้าจอ สำหรับแสดง 'เช้าวันที่ X' (ถ้าไม่ใส่จะสร้างอัตโนมัติ)")]
    public CanvasGroup newDayOverlay;
    [Tooltip("Text ที่อยู่ใน Overlay (ถ้าไม่ใส่จะสร้างอัตโนมัติ)")]
    public TMP_Text newDayText;
    [Tooltip("เวลาที่ Overlay แสดง (วินาที)")]
    public float overlayDuration = 2.5f;
    [Tooltip("เวลา Fade In/Out (วินาที)")]
    public float fadeDuration = 0.6f;

    // ─────────────────────────────────────────────
    private bool isShowingOverlay = false;

    // ══════════════════════════════════════════════
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        EnsureHUDExists();
        UpdateDayHUD();
    }

    // ══════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════

    /// <summary>
    /// เพิ่มวัน 1 วัน — เรียกจาก BedroomDoorInteractable เมื่อกดนอน
    /// </summary>
    public void AdvanceDay()
    {
        currentDay++;
        Debug.Log($"[DayManager] ☀️ ขึ้นวันที่ {currentDay}!");

        // Reset Hall ให้เปิดได้ใหม่
        if (HallManager.Instance != null)
        {
            HallManager.Instance.OnNewDay();
        }

        UpdateDayHUD();

        if (!isShowingOverlay)
        {
            StartCoroutine(ShowNewDayOverlay());
        }
    }

    // ══════════════════════════════════════════════
    // HUD
    // ══════════════════════════════════════════════

    private void UpdateDayHUD()
    {
        if (dayHUDText != null)
        {
            dayHUDText.text = $"วันที่  {currentDay}";
        }
    }

    // ══════════════════════════════════════════════
    // Overlay
    // ══════════════════════════════════════════════

    private IEnumerator ShowNewDayOverlay()
    {
        isShowingOverlay = true;
        EnsureOverlayExists();

        if (newDayText != null)
            newDayText.text = $"☀️  เช้าวันที่  {currentDay}";

        // Fade In
        newDayOverlay.alpha = 0f;
        newDayOverlay.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            newDayOverlay.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        newDayOverlay.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(overlayDuration);

        // Fade Out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            newDayOverlay.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        newDayOverlay.alpha = 0f;
        newDayOverlay.gameObject.SetActive(false);

        isShowingOverlay = false;
    }

    // ══════════════════════════════════════════════
    // Auto-create UI if not assigned
    // ══════════════════════════════════════════════

    private void EnsureHUDExists()
    {
        if (dayHUDText != null) return;

        Canvas targetCanvas = GetOrCreateCanvas("DayManager_Canvas", 100);

        // HUD Text (วันที่ มุมบนซ้าย)
        GameObject hudGO = new GameObject("DayHUD_Text");
        hudGO.transform.SetParent(targetCanvas.transform, false);
        RectTransform hudRT = hudGO.AddComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(0f, 1f);
        hudRT.anchorMax = new Vector2(0f, 1f);
        hudRT.pivot     = new Vector2(0f, 1f);
        hudRT.anchoredPosition = new Vector2(20f, -20f);
        hudRT.sizeDelta = new Vector2(300f, 60f);

        dayHUDText = hudGO.AddComponent<TextMeshProUGUI>();
        dayHUDText.fontSize = 28;
        dayHUDText.fontStyle = FontStyles.Bold;
        dayHUDText.color = new Color(1f, 0.9f, 0.5f, 1f); // สีทอง
        dayHUDText.alignment = TextAlignmentOptions.Left;

        // เพิ่มเงาให้อ่านง่าย
        var shadow = hudGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private void EnsureOverlayExists()
    {
        if (newDayOverlay != null) return;

        Canvas targetCanvas = GetOrCreateCanvas("DayManager_Canvas", 100);

        // Overlay Panel (เต็มหน้าจอ)
        GameObject overlayGO = new GameObject("NewDay_Overlay");
        overlayGO.transform.SetParent(targetCanvas.transform, false);
        RectTransform overlayRT = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // พื้นหลังดำ
        Image bg = overlayGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        newDayOverlay = overlayGO.AddComponent<CanvasGroup>();
        newDayOverlay.blocksRaycasts = false;

        // ข้อความ "เช้าวันที่ X"
        GameObject textGO = new GameObject("NewDay_Text");
        textGO.transform.SetParent(overlayGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot     = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(700f, 120f);

        newDayText = textGO.AddComponent<TextMeshProUGUI>();
        newDayText.fontSize = 52;
        newDayText.fontStyle = FontStyles.Bold;
        newDayText.color = new Color(1f, 0.92f, 0.55f, 1f);
        newDayText.alignment = TextAlignmentOptions.Center;

        overlayGO.SetActive(false);
    }

    private Canvas GetOrCreateCanvas(string canvasName, int sortingOrder)
    {
        // ลองหา Canvas ที่มีอยู่ก่อน
        GameObject existing = GameObject.Find(canvasName);
        if (existing != null)
        {
            Canvas c = existing.GetComponent<Canvas>();
            if (c != null) return c;
        }

        // สร้าง Canvas ใหม่
        GameObject canvasGO = new GameObject(canvasName);
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }
}
