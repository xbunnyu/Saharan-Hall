using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// วางบนประตู/ทางเข้าห้องนอน — ผู้เล่นกด [E] เพื่อเปลี่ยนซีน
/// ใช้ InteractableItem เป็นฐาน เหมือนกับ HallTrigger / QTEInteractable
/// </summary>
public class BedroomDoorInteractable : InteractableItem
{
    [Header("Scene Transition Settings")]
    [Tooltip("ชื่อซีนห้องนอนที่ต้องการเปลี่ยนไป")]
    public string targetSceneName = "BedroomScene";

    [Tooltip("เวลา Fade Out ก่อนเปลี่ยนซีน (วินาที)")]
    public float fadeDuration = 0.8f;

    [Tooltip("แสดงข้อความเตือนถ้าเควสทุกอย่างยังไม่เสร็จ (ถ้าจะใช้เงื่อนไข)")]
    public bool requireAllQuestsDone = false;

    [Tooltip("เสียงเปิดประตู (Optional)")]
    public AudioClip doorOpenSound;

    private bool isTransitioning = false;

    // ── Fade UI (สร้างอัตโนมัติ) ─────────────────────────────
    private static GameObject fadeCanvasGO;
    private static Image fadeImage;

    // ─────────────────────────────────────────────────────────

    void Reset()
    {
        itemName             = "ประตูห้องนอน";
        canRead              = true;
        canCollect           = false;
        customReadPromptText = "เข้าห้องนอน";
        readTitle            = "";
        readDescription      = "";
    }

    void Start()
    {
        // ตั้งค่าข้อมูลเริ่มต้น (กรณีไม่ได้ตั้งใน Inspector)
        if (string.IsNullOrEmpty(itemName))          itemName             = "ประตูห้องนอน";
        if (string.IsNullOrEmpty(customReadPromptText)) customReadPromptText = "เข้าห้องนอน";

        EnsureFadeCanvas();
    }

    // ─────────────────────────────────────────────────────────

    public override void OnRead(PlayerInteraction interactor)
    {
        if (isTransitioning) return;

        // ตรวจเงื่อนไขเควส (ถ้าเปิดใช้งาน)
        if (requireAllQuestsDone && QuestUIManager.Instance != null
            && QuestUIManager.Instance.activeQuests.Count > 0)
        {
            interactor.ShowNotification(
                "<color=#FF9900>⚠️ ยังมีภาระกิจค้างอยู่ กรุณาทำให้เสร็จก่อนเข้านอน</color>", 3.0f);
            return;
        }

        // เล่นเสียงเปิดประตู
        if (doorOpenSound != null)
        {
            AudioSource.PlayClipAtPoint(doorOpenSound, transform.position);
        }

        // ประเมินผล Karma ก่อนนอน (ถ้ามี KarmaManager)
        if (KarmaManager.Instance != null)
        {
            KarmaManager.Instance.EvaluateKarmaEnding();
        }

        interactor.CloseReading();

        // ขึ้นวันใหม่ — เรียก DayManager.AdvanceDay() พร้อมเอฟเฟคจอ
        if (DayManager.Instance != null)
        {
            StartCoroutine(SleepAndAdvanceDay());
        }
        else
        {
            // Fallback: เปลี่ยนซีนถ้าไม่มี DayManager
            StartCoroutine(FadeAndLoadScene());
        }
    }

    // ══════════════════════════════════════════════════════════
    // Fade & Scene Load
    // ══════════════════════════════════════════════════════════

    private IEnumerator FadeAndLoadScene()
    {
        isTransitioning = true;

        // ล็อคการเคลื่อนที่ผู้เล่น
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        // Fade to Black
        EnsureFadeCanvas();
        fadeCanvasGO.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        // โหลดซีน
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetSceneName);
    }

    // เมื่อกดนอน: Fade ดำ, เรียก AdvanceDay, Fade กลับและปลด Lock
    private IEnumerator SleepAndAdvanceDay()
    {
        isTransitioning = true;

        // ล็อคการเคลื่อนที่ผู้เล่น
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        // Fade to Black
        EnsureFadeCanvas();
        fadeCanvasGO.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, 1f);

        // Hold สั้นๆ และเรียก AdvanceDay (DayManager จะ Fade In overlay)
        yield return new WaitForSeconds(0.3f);
        DayManager.Instance.AdvanceDay();

        // ปลด Lock ผู้เล่น
        if (pc != null) pc.enabled = true;

        // Fade Back from Black
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeCanvasGO.SetActive(false);

        isTransitioning = false;
    }

    // ══════════════════════════════════════════════════════════
    // Fade Canvas (สร้างแค่ครั้งเดียวสำหรับทุก Instance)
    // ══════════════════════════════════════════════════════════

    private static void EnsureFadeCanvas()
    {
        if (fadeCanvasGO != null) return;

        fadeCanvasGO = new GameObject("SceneFade_Canvas");
        DontDestroyOnLoad(fadeCanvasGO);

        Canvas canvas = fadeCanvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = fadeCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        fadeCanvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(fadeCanvasGO.transform, false);

        RectTransform rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage       = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        fadeCanvasGO.SetActive(false);
    }
}
