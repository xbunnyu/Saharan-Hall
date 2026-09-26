using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// คอมโพเนนต์สำหรับโต้ตอบกับเตียงนอน (Bed Interactable)
/// - เมื่อผู้เล่นเดินมาที่เตียงและกด [E] จะทำการนอนหลับและขึ้นวันใหม่ (Advance Day)
/// - ป้องกันการนอนหากตำหนักยังเปิดอยู่ (ต้องให้บริการลูกค้าให้ครบก่อน)
/// - Fade จอมืด -> DayManager.AdvanceDay() -> แสดง '☀️ เช้าวันที่ X' -> Fade จอสว่างกลับคืนมา
/// - ติดตั้งตัวเองลงบน GameObject ชื่อ "Bed" อัตโนมัติเมื่อเริ่มเกม
/// </summary>
public class BedInteractable : InteractableItem
{
    [Header("Bed Interaction Settings")]
    [Tooltip("ระยะเวลา Fade ดำ (วินาที)")]
    public float fadeDuration = 0.8f;

    [Tooltip("เสียงนอนหลับ (ถ้ามี)")]
    public AudioClip sleepSound;

    [Tooltip("ตำแหน่งสำหรับผู้เล่นเมื่อตื่นนอน (ถ้าเว้นว่างจะยืนข้างเตียง)")]
    public Transform wakeUpPoint;

    private bool isSleeping = false;

    // Fade Canvas UI (สร้างและแชร์อัตโนมัติ)
    private static GameObject fadeCanvasGO;
    private static Image fadeImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToBed()
    {
        // ค้นหา GameObject ชื่อ "Bed" ในฉากอัตโนมัติ
        GameObject bedObj = GameObject.Find("Bed");
        if (bedObj != null)
        {
            if (bedObj.GetComponent<BedInteractable>() == null)
            {
                bedObj.AddComponent<BedInteractable>();
                Debug.Log("[BedInteractable] ✅ ตรวจพบเตียงนอน (Bed) — ติดตั้งระบบนอนหลับขึ้นวันใหม่อัตโนมัติเรียบร้อยแล้ว");
            }
        }
    }

    private void Awake()
    {
        itemName = "เตียงนอน";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "นอนหลับ (ขึ้นวันใหม่)";
        readTitle = "";
        readDescription = "";
    }

    private void Reset()
    {
        itemName = "เตียงนอน";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "นอนหลับ (ขึ้นวันใหม่)";
        readTitle = "";
        readDescription = "";
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(itemName)) itemName = "เตียงนอน";
        if (string.IsNullOrEmpty(customReadPromptText)) customReadPromptText = "นอนหลับ (ขึ้นวันใหม่)";
        EnsureFadeCanvas();
    }

    /// <summary>
    /// เมื่อผู้เล่นมองที่เตียงแล้วกด [E]
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        if (isSleeping) return;

        // 1. ตรวจสอบว่าตำหนักยังเปิดอยู่หรือไม่ (หากเปิดอยู่ต้องให้บริการลูกค้าให้ครบก่อน)
        if (HallManager.Instance != null && HallManager.Instance.isHallOpen)
        {
            int target = HallManager.Instance.maxNpcPerSession > 0 ? HallManager.Instance.maxNpcPerSession : 5;
            int served = HallManager.Instance.npcsServedThisSession;

            interactor.ShowNotification(
                $"<color=#FF4500>⚠️ ตำหนักยังเปิดอยู่ ไม่สามารถเข้านอนได้!</color>\nต้องต้อนรับผู้มาเยือนให้ครบก่อน ({served}/{target} คน)", 
                3.5f
            );
            return;
        }

        // 2. ปิดหน้าต่างอ่าน (ถ้ามี)
        interactor.CloseReading();

        // 3. เริ่มกระบวนการนอนหลับ
        StartCoroutine(SleepRoutine(interactor));
    }

    private IEnumerator SleepRoutine(PlayerInteraction interactor)
    {
        isSleeping = true;

        // ล็อคการเคลื่อนที่ของผู้เล่นชั่วคราวขณะหลับ
        PlayerController pc = interactor != null ? interactor.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        // เล่นเสียงนอนหลับ (ถ้าใส่ไฟล์เสียงไว้)
        if (sleepSound != null)
        {
            AudioSource.PlayClipAtPoint(sleepSound, transform.position);
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification("<color=#FFD700>💤 กำลังนอนหลับพักผ่อน...</color>", 2.0f);
        }

        // 4. Fade to Black (หน้าจอมืดสนิท)
        EnsureFadeCanvas();
        fadeCanvasGO.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
            }
            yield return null;
        }

        if (fadeImage != null) fadeImage.color = Color.black;

        // พักจอมืดสั้นๆ ให้ความรู้สึกเหมือนหลับข้ามคืน
        yield return new WaitForSeconds(0.5f);

        // 5. สั่ง DayManager ขึ้นวันใหม่
        if (DayManager.Instance != null)
        {
            DayManager.Instance.AdvanceDay();
        }
        else if (HallManager.Instance != null)
        {
            // Fallback กรณีไม่มี DayManager ในฉาก
            HallManager.Instance.OnNewDay();
        }

        // 6. จัดตำแหน่งผู้เล่นตื่นนอน (ถ้ามีการกำหนดจุดยืนข้างเตียงไว้)
        if (wakeUpPoint != null && interactor != null)
        {
            CharacterController cc = interactor.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            interactor.transform.position = wakeUpPoint.position;
            interactor.transform.rotation = wakeUpPoint.rotation;
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }

        // ปลดล็อคการควบคุมของผู้เล่น
        if (pc != null) pc.enabled = true;

        // 7. Fade จอสว่างกลับคืนมา
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(elapsed / fadeDuration));
            }
            yield return null;
        }

        if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeCanvasGO.SetActive(false);

        isSleeping = false;
        Debug.Log("[BedInteractable] ☀️ ผู้เล่นตื่นนอนและขึ้นวันใหม่เรียบร้อยแล้ว");
    }

    private static void EnsureFadeCanvas()
    {
        if (fadeCanvasGO != null) return;

        fadeCanvasGO = new GameObject("BedSleepFade_Canvas");
        DontDestroyOnLoad(fadeCanvasGO);

        Canvas canvas = fadeCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;

        CanvasScaler scaler = fadeCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        fadeCanvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("SleepFadeImage");
        imgGO.transform.SetParent(fadeCanvasGO.transform, false);

        RectTransform rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        fadeCanvasGO.SetActive(false);
    }
}
