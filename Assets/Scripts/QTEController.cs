using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class QTEController : MonoBehaviour
{
    public static QTEController Instance { get; private set; }

    [Header("1. QTE Settings")]
    [Tooltip("ปุ่มที่ใช้สำหรับกดหยุดเข็ม (Default: Spacebar)")]
    public Key qteKey = Key.Space;
    
    [Header("2. UI References (Optional - จะสร้างอัตโนมัติหากเว้นว่าง)")]
    public GameObject qteCanvas;
    public GameObject qtePanel;
    public Image ringBgImage;
    public Image targetZoneImage;
    public RectTransform needleTransform;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;

    [Header("3. Audio Effects (Optional)")]
    public AudioClip hitSuccessSound;
    public AudioClip hitMissSound;
    public AudioClip qteWinSound;
    public AudioClip qteFailSound;

    // สถานะ QTE ปัจจุบัน
    private bool isQTEActive = false;
    private int requiredHits = 3;
    private int currentHits = 0;
    private int currentFailures = 0;
    private int maxFailures = 2;
    private float currentSpeed = 180f;
    private float speedIncrement = 40f;
    
    private float needleAngle = 0f; // มุมเข็ม (0-360 องศา)
    private float targetCenterAngle = 0f;
    private float targetWidthAngle = 35f;

    private Action onSuccessCallback;
    private Action onFailCallback;

    private PlayerController playerController;
    private PlayerInteraction playerInteraction;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        EnsureUIExists();

        if (qtePanel != null)
        {
            qtePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isQTEActive) return;

        // 1. หมุนเข็มต่อเนื่องตามความเร็ว
        needleAngle += currentSpeed * Time.deltaTime;
        if (needleAngle >= 360f)
        {
            needleAngle -= 360f;
        }

        // อัปเดตมุมการหมุนของเข็มบน UI (หมุนตามเข็มนาฬิกา)
        if (needleTransform != null)
        {
            needleTransform.localEulerAngles = new Vector3(0f, 0f, -needleAngle);
        }

        // 2. รับค่า Input กด Spacebar
        var keyboard = Keyboard.current;
        bool pressed = false;

        if (keyboard != null && keyboard[qteKey].wasPressedThisFrame)
        {
            pressed = true;
        }
        else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            pressed = true;
        }

        if (pressed)
        {
            EvaluateHit();
        }
    }

    /// <summary>
    /// เริ่มมินิเกม QTE แบบ Dead by Daylight
    /// </summary>
    public void StartQTE(int minHits, int maxHits, float baseSpeed, float speedInc, int maxFails, Action onSuccess, Action onFail)
    {
        if (isQTEActive) return;

        EnsureUIExists();

        // ค้นหา Player ในกรณีเปลี่ยนซีน
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        // สุ่มจำนวนครั้งที่ต้องกดจาก minHits ถึง maxHits (เช่น 3 ถึง 5)
        this.requiredHits = UnityEngine.Random.Range(minHits, maxHits + 1);
        this.currentHits = 0;
        this.currentFailures = 0;
        this.maxFailures = maxFails;
        this.currentSpeed = baseSpeed;
        this.speedIncrement = speedInc;
        this.onSuccessCallback = onSuccess;
        this.onFailCallback = onFail;

        this.needleAngle = 0f;
        GenerateNewTargetZone();

        // ล็อคการควบคุมของผู้เล่น
        LockPlayerControls(true);

        isQTEActive = true;
        if (qtePanel != null)
        {
            qtePanel.SetActive(true);
        }

        UpdateUI();
    }

    public bool IsQTEActive()
    {
        return isQTEActive;
    }

    /// <summary>
    /// ประมวลผลเมื่อผู้เล่นกดปุ่มหยุดเข็ม
    /// </summary>
    private void EvaluateHit()
    {
        float halfWidth = targetWidthAngle / 2f;
        float startAngle = NormalizeAngle(targetCenterAngle - halfWidth);
        float endAngle = NormalizeAngle(targetCenterAngle + halfWidth);

        bool isHit = IsAngleInZone(needleAngle, startAngle, endAngle);

        if (isHit)
        {
            // กดโดนหลอดขาว (Hit Success)
            currentHits++;
            currentSpeed += speedIncrement;

            PlaySound(hitSuccessSound);

            if (currentHits >= requiredHits)
            {
                // สำเร็จครบตามเป้าหมาย
                StartCoroutine(FinishQTERoutine(true));
            }
            else
            {
                // สุ่มตำแหน่งหลอดขาวใหม่สำหรับรอบถัดไป
                GenerateNewTargetZone();
                UpdateUI();
            }
        }
        else
        {
            // กดพลาดนอกหลอดขาว (Miss/Failure)
            currentFailures++;
            PlaySound(hitMissSound);

            if (currentFailures > maxFailures)
            {
                // ล้มเหลวเกินจำนวนที่กำหนด (เช่น ล้มเหลว > 2 ครั้ง)
                StartCoroutine(FinishQTERoutine(false));
            }
            else
            {
                // สุ่มตำแหน่งหลอดขาวใหม่ให้ลองแก้ตัวในรอบนี้
                GenerateNewTargetZone();
                UpdateUI();
            }
        }
    }

    private IEnumerator FinishQTERoutine(bool success)
    {
        isQTEActive = false;

        if (statusText != null)
        {
            statusText.text = success ? "<color=#00FF7F>สำเร็จ!</color>" : "<color=#FF4500>ล้มเหลว!</color>";
        }

        PlaySound(success ? qteWinSound : qteFailSound);

        yield return new WaitForSeconds(0.6f);

        if (qtePanel != null)
        {
            qtePanel.SetActive(false);
        }

        LockPlayerControls(false);

        if (success)
        {
            onSuccessCallback?.Invoke();
        }
        else
        {
            onFailCallback?.Invoke();
        }
    }

    private void GenerateNewTargetZone()
    {
        // สุ่มมุมตำแหน่งหลอดขาว (ให้อยู่ในช่วง 50 ถึง 310 องศา ห่างจากจุดเริ่มต้น)
        targetCenterAngle = UnityEngine.Random.Range(50f, 310f);

        if (targetZoneImage != null)
        {
            float fill = targetWidthAngle / 360f;
            targetZoneImage.fillAmount = fill;

            // หมุนภาพหลอดขาวไปยังมุมเริ่มต้นของโซน
            float startAngle = targetCenterAngle - (targetWidthAngle / 2f);
            targetZoneImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, -startAngle);
        }
    }

    private bool IsAngleInZone(float angle, float startAngle, float endAngle)
    {
        angle = NormalizeAngle(angle);

        if (startAngle <= endAngle)
        {
            return angle >= startAngle && angle <= endAngle;
        }
        else
        {
            // กรณีข้ามจุด 360/0 องศา
            return angle >= startAngle || angle <= endAngle;
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    private void UpdateUI()
    {
        if (promptText != null)
        {
            promptText.text = $"<color=#FFD700>📜 พิธีเขียนยันต์ (YANTRA RITUAL) ✨</color>\nกด <color=#00FFFF>[ SPACEBAR ]</color> ให้หยุดตรงหลอดสว่าง!";
        }

        if (progressText != null)
        {
            progressText.text = $"<color=#00FF88>SUCCESS: {currentHits}/{requiredHits}</color>  |  <color=#FF3555>MISS: {currentFailures}/{maxFailures}</color>";
        }

        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    private void LockPlayerControls(bool lockState)
    {
        if (playerController != null)
        {
            playerController.enabled = !lockState;
        }

        if (lockState)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// สร้าง Sprite วงกลมแบบ Smooth Antialiased เพื่อความคมชัดของ UI
    /// </summary>
    private Sprite CreateCircleSprite(int size = 256)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 1f);
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// สร้าง Canvas UI QTE อัตโนมัติในฉากสไตล์ Dead by Daylight Skill Check
    /// </summary>
    private void EnsureUIExists()
    {
        if (qtePanel != null && qteCanvas != null) return;

        // ค้นหา Canvas เดิมก่อน
        qteCanvas = GameObject.Find("QTE_Canvas");
        if (qteCanvas == null)
        {
            qteCanvas = new GameObject("QTE_Canvas");
            Canvas canvas = qteCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;

            CanvasScaler scaler = qteCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            qteCanvas.AddComponent<GraphicRaycaster>();
        }

        qtePanel = qteCanvas.transform.Find("QTEPanel")?.gameObject;
        if (qtePanel == null)
        {
            Sprite circleSprite = CreateCircleSprite(256);

            qtePanel = new GameObject("QTEPanel");
            qtePanel.transform.SetParent(qteCanvas.transform, false);

            RectTransform panelRect = qtePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 500);

            // 0. Dark Glass Overlay Background Card
            GameObject bgCardObj = new GameObject("GlassBG");
            bgCardObj.transform.SetParent(qtePanel.transform, false);
            Image bgCard = bgCardObj.AddComponent<Image>();
            bgCard.color = new Color(0.04f, 0.05f, 0.08f, 0.75f);
            bgCard.rectTransform.sizeDelta = new Vector2(440, 440);
            bgCard.sprite = circleSprite;

            // 1. วงแหวนขอบนอก (Outer Ring Frame)
            GameObject outerRingObj = new GameObject("OuterRing");
            outerRingObj.transform.SetParent(qtePanel.transform, false);
            Image outerRing = outerRingObj.AddComponent<Image>();
            outerRing.sprite = circleSprite;
            outerRing.color = new Color(0.2f, 0.25f, 0.35f, 0.85f);
            outerRing.rectTransform.sizeDelta = new Vector2(280, 280);

            // 2. วงกลมพื้นหลังรางล้อ (Track Background)
            GameObject ringObj = new GameObject("RingBG");
            ringObj.transform.SetParent(qtePanel.transform, false);
            ringBgImage = ringObj.AddComponent<Image>();
            ringBgImage.sprite = circleSprite;
            ringBgImage.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            ringBgImage.rectTransform.sizeDelta = new Vector2(265, 265);

            // 3. หลอดขาว Target Zone (Radial Fill)
            GameObject targetObj = new GameObject("TargetZone");
            targetObj.transform.SetParent(ringObj.transform, false);
            targetZoneImage = targetObj.AddComponent<Image>();
            targetZoneImage.sprite = circleSprite;
            targetZoneImage.type = Image.Type.Filled;
            targetZoneImage.fillMethod = Image.FillMethod.Radial360;
            targetZoneImage.fillOrigin = (int)Image.Origin360.Top;
            targetZoneImage.fillClockwise = true;
            targetZoneImage.color = new Color(1f, 0.95f, 0.85f, 0.98f); // Bright warm white
            targetZoneImage.rectTransform.sizeDelta = new Vector2(265, 265);

            // 4. วงกลมทับด้านในสร้างทรงวงแหวน (Inner Ring Mask)
            GameObject innerObj = new GameObject("InnerRing");
            innerObj.transform.SetParent(ringObj.transform, false);
            Image innerImage = innerObj.AddComponent<Image>();
            innerImage.sprite = circleSprite;
            innerImage.color = new Color(0.06f, 0.07f, 0.1f, 0.98f);
            innerImage.rectTransform.sizeDelta = new Vector2(195, 195);

            // 5. ขอบวงแหวนในสุด (Inner Ring Outline)
            GameObject innerOutlineObj = new GameObject("InnerOutline");
            innerOutlineObj.transform.SetParent(ringObj.transform, false);
            Image innerOutline = innerOutlineObj.AddComponent<Image>();
            innerOutline.sprite = circleSprite;
            innerOutline.color = new Color(0.25f, 0.3f, 0.4f, 0.6f);
            innerOutline.rectTransform.sizeDelta = new Vector2(198, 198);
            innerOutline.transform.SetAsFirstSibling();

            // 6. เข็มหมุน (Needle Line)
            GameObject needleObj = new GameObject("Needle");
            needleObj.transform.SetParent(ringObj.transform, false);
            needleTransform = needleObj.AddComponent<RectTransform>();
            needleTransform.sizeDelta = new Vector2(5, 125);
            needleTransform.pivot = new Vector2(0.5f, 0f); // จุดหมุนที่โคนเข็ม
            needleTransform.anchoredPosition = Vector2.zero;

            Image needleImage = needleObj.AddComponent<Image>();
            needleImage.color = new Color(1f, 0.15f, 0.25f, 1f); // Neon crimson red

            // 7. หัวเข็มเรืองแสง (Needle Pointer Tip)
            GameObject tipObj = new GameObject("NeedleTip");
            tipObj.transform.SetParent(needleObj.transform, false);
            Image tipImage = tipObj.AddComponent<Image>();
            tipImage.sprite = circleSprite;
            tipImage.color = new Color(1f, 0.4f, 0.4f, 1f);
            tipImage.rectTransform.sizeDelta = new Vector2(12, 12);
            tipImage.rectTransform.anchoredPosition = new Vector2(0, 120);

            // 8. จุดยึดตรงกลางเข็ม (Center Cap Hub)
            GameObject capObj = new GameObject("CenterCap");
            capObj.transform.SetParent(ringObj.transform, false);
            Image capImage = capObj.AddComponent<Image>();
            capImage.sprite = circleSprite;
            capImage.color = new Color(0.85f, 0.15f, 0.25f, 1f);
            capImage.rectTransform.sizeDelta = new Vector2(28, 28);

            // 9. ข้อความคำแนะนำ (Prompt Badge Text)
            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(qtePanel.transform, false);
            promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 22;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
            promptText.rectTransform.anchoredPosition = new Vector2(0, 175);
            promptText.rectTransform.sizeDelta = new Vector2(480, 70);

            // 10. ข้อความนับจำนวน (Progress Counter Text)
            GameObject progressObj = new GameObject("ProgressText");
            progressObj.transform.SetParent(qtePanel.transform, false);
            progressText = progressObj.AddComponent<TextMeshProUGUI>();
            progressText.fontSize = 20;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.fontStyle = FontStyles.Bold;
            progressText.color = Color.white;
            progressText.rectTransform.anchoredPosition = new Vector2(0, -175);
            progressText.rectTransform.sizeDelta = new Vector2(480, 50);

            // 11. ข้อความบอกผลลัพธ์ (Status Text ในกลางวงแหวน)
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(qtePanel.transform, false);
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 28;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontStyle = FontStyles.Bold;
            statusText.rectTransform.anchoredPosition = new Vector2(0, 0);
            statusText.rectTransform.sizeDelta = new Vector2(180, 60);
        }
    }
}
