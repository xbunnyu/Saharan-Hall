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
    private int maxFailures = 3;
    private float currentSpeed = 180f;
    private float speedIncrement = 40f;
    
    private float needleAngle = 0f; // มุมเข็ม (0-360 องศา)
    private float targetCenterAngle = 0f;
    private float targetWidthAngle = 35f;
    private string customTitleText = "";

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

        // 2. ระบบ Auto-Catch จากบารมีโต๊ะหมู่บูชา Level 4
        if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.ActiveAutoCatch)
        {
            float halfWidth = targetWidthAngle / 2f;
            float startAngle = NormalizeAngle(targetCenterAngle - halfWidth);
            float endAngle = NormalizeAngle(targetCenterAngle + halfWidth);

            if (IsAngleInZone(needleAngle, startAngle, endAngle))
            {
                EvaluateHit();
                return;
            }
        }

        // 3. รับค่า Input กด Spacebar หรือ Gamepad
        var keyboard = Keyboard.current;
        bool pressed = false;

        if (keyboard != null && keyboard[qteKey].wasPressedThisFrame)
        {
            pressed = true;
        }
        else if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            pressed = true;
        }

        if (pressed)
        {
            EvaluateHit();
        }
    }

    /// <summary>
    /// เริ่มมินิเกม QTE แบบ Dead by Daylight / พิธีกรรม
    /// </summary>
    public void StartQTE(int minHits, int maxHits, float baseSpeed, float speedInc, int maxFails, Action onSuccess, Action onFail, string title = "")
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
        this.speedIncrement = speedInc;
        this.onSuccessCallback = onSuccess;
        this.onFailCallback = onFail;
        this.customTitleText = title;

        // ดึงบัฟช่วยเหลือจากโต๊ะหมู่บูชา (BuddhistAltarManager)
        if (BuddhistAltarManager.Instance != null)
        {
            float windowMult = BuddhistAltarManager.Instance.ActiveQteWindow / 0.25f; // L1=1.0, L2=1.6, L3=2.2, L4=3.2
            this.targetWidthAngle = Mathf.Clamp(35f * windowMult, 35f, 115f);

            float speedMult = BuddhistAltarManager.Instance.ActiveQteSpeed / 1.8f; // L1=1.0, L2=0.77, L3=0.55, L4=0.39
            this.currentSpeed = baseSpeed * speedMult;
        }
        else
        {
            this.targetWidthAngle = 35f;
            this.currentSpeed = baseSpeed;
        }

        this.needleAngle = 0f;
        GenerateNewTargetZone();

        // ล็อคการควบคุมของผู้เล่น
        LockPlayerControls(true);

        isQTEActive = true;
        if (qteCanvas != null)
        {
            qteCanvas.SetActive(true);
        }
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

            PlaySound(hitSuccessSound, 587.33f, 0.15f);

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
            PlaySound(hitMissSound, 180f, 0.25f);

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
            statusText.text = success ? "<color=#3B2A12>ยันต์สำเร็จ</color>" : "<color=#8E1B12>ยันต์ขาด</color>";
        }

        PlaySound(success ? qteWinSound : qteFailSound, success ? 880f : 130f, success ? 0.4f : 0.5f);

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

            // ปรับสีหากมี Visual Aid จากโต๊ะหมู่บูชา
            if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.ActiveVisualAid)
            {
                targetZoneImage.color = new Color(0.85f, 0.55f, 0.15f, 1f); // ชาดทอง (บารมีโต๊ะหมู่)
            }
            else
            {
                targetZoneImage.color = YantraVermilion;
            }

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

    // รายการอักขระยันต์ศักดิ์สิทธิ์สำหรับสุ่มแสดงตรงกลางวงแหวนเขียนยันต์
    private readonly string[] sacredYantraRunes = { "นะ", "โม", "พุธ", "ธา", "ยะ", "ฤ", "ฤา", "ฦ", "ฦา", "อะ", "อุ", "มะ" };
    private TextMeshProUGUI centerRuneText;

    // ── สีธีม "เขียนยันต์": กระดาษสา / หมึกดำ / ชาดแดง ──
    private static readonly Color YantraPaper = new Color(0.86f, 0.78f, 0.61f, 0.97f);
    private static readonly Color YantraInk = new Color(0.16f, 0.10f, 0.07f, 1f);
    private static readonly Color YantraVermilion = new Color(0.66f, 0.18f, 0.12f, 0.95f);

    /// <summary>กรอบยันต์: ขอบสี่เหลี่ยมสองชั้น + เส้นทแยง จางๆ บนกระดาษ</summary>
    private void BuildYantraFrame(Transform parent)
    {
        Color faint = new Color(YantraInk.r, YantraInk.g, YantraInk.b, 0.55f);
        Color fainter = new Color(YantraInk.r, YantraInk.g, YantraInk.b, 0.18f);
        void Line(string name, Vector2 size, Vector2 pos, float rot, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
        Line("FrameTop", new Vector2(450, 4), new Vector2(0, 245), 0, faint);
        Line("FrameBottom", new Vector2(450, 4), new Vector2(0, -245), 0, faint);
        Line("FrameLeft", new Vector2(4, 494), new Vector2(-223, 0), 0, faint);
        Line("FrameRight", new Vector2(4, 494), new Vector2(223, 0), 0, faint);
        Line("InnerTop", new Vector2(420, 2), new Vector2(0, 232), 0, faint);
        Line("InnerBottom", new Vector2(420, 2), new Vector2(0, -232), 0, faint);
        Line("InnerLeft", new Vector2(2, 466), new Vector2(-210, 0), 0, faint);
        Line("InnerRight", new Vector2(2, 466), new Vector2(210, 0), 0, faint);
        Line("Diag1", new Vector2(2, 600), Vector2.zero, 41f, fainter);
        Line("Diag2", new Vector2(2, 600), Vector2.zero, -41f, fainter);
        Line("Cross1", new Vector2(420, 2), Vector2.zero, 0, fainter);
        Line("Cross2", new Vector2(2, 466), Vector2.zero, 0, fainter);
    }

    private void UpdateUI()
    {
        if (promptText != null)
        {
            string header = !string.IsNullOrEmpty(customTitleText)
                ? customTitleText
                : "พิธีเขียนยันต์";

            string altarAidTag = "";
            if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.currentLevel > 1)
            {
                altarAidTag = $"\n<size=75%><color=#6B4A1E>บารมีโต๊ะหมู่บูชา Lv.{BuddhistAltarManager.Instance.currentLevel}: {BuddhistAltarManager.GetLevelBenefitText(BuddhistAltarManager.Instance.currentLevel)}</color></size>";
            }

            promptText.text = $"<size=120%>{header}</size>\n<size=80%>กด <color=#8E1B12>Spacebar</color> ตวัดพู่กันเมื่อเข็มอยู่ในช่องชาด</size>{altarAidTag}";
        }

        if (progressText != null)
        {
            // อักขระที่ลงไปแล้วเรียงต่อกันเหมือนเขียนจริง
            string written = "";
            for (int i = 0; i < currentHits && i < sacredYantraRunes.Length; i++) written += sacredYantraRunes[i] + " ";
            string misses = currentFailures > 0 ? $"   <color=#8E1B12>หมึกหยด {currentFailures}/{maxFailures}</color>" : "";
            progressText.text = $"{written.Trim()}\n<size=75%>ลงอักขระ {currentHits}/{requiredHits}{misses}</size>";
        }

        if (statusText != null)
        {
            statusText.text = "";
        }

        if (centerRuneText != null && currentHits < sacredYantraRunes.Length)
        {
            int index = Mathf.Clamp(currentHits, 0, sacredYantraRunes.Length - 1);
            centerRuneText.text = sacredYantraRunes[index];
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

    private void PlaySound(AudioClip clip, float synthFreq = 520f, float duration = 0.2f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            return;
        }

        if (audioSource != null)
        {
            PlaySynthBeep(synthFreq, duration);
        }
    }

    private void PlaySynthBeep(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1.0f - (t / duration);
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("QTESynthBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip, 0.5f);
    }

    private Texture2D guiCircleTex;
    private Texture2D GetGuiCircleTex()
    {
        if (guiCircleTex == null)
        {
            int size = 128;
            guiCircleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            guiCircleTex.SetPixels(colors);
            guiCircleTex.Apply();
        }
        return guiCircleTex;
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
        if (qtePanel != null && qteCanvas != null && targetZoneImage != null && needleTransform != null && centerRuneText != null) return;

        // ค้นหา Canvas เดิมก่อน
        qteCanvas = GameObject.Find("QTE_Canvas");
        if (qteCanvas == null)
        {
            qteCanvas = new GameObject("QTE_Canvas");
            Canvas canvas = qteCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = qteCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            qteCanvas.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(qteCanvas);
        }
        else
        {
            Canvas canvas = qteCanvas.GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 999;
        }

        qtePanel = qteCanvas.transform.Find("QTEPanel")?.gameObject;
        
        // หาก qtePanel มีอยู่เดิมแต่ขาด Component วงกลม/เข็มสำคัญ ให้ทำลายและสร้างใหม่เพื่อให้แสดงผลเป็นวงกลมสมบูรณ์
        if (qtePanel != null && (targetZoneImage == null || needleTransform == null || centerRuneText == null))
        {
            DestroyImmediate(qtePanel);
            qtePanel = null;
        }

        if (qtePanel == null)
        {
            Sprite circleSprite = CreateCircleSprite(256);

            qtePanel = new GameObject("QTEPanel");
            qtePanel.transform.SetParent(qteCanvas.transform, false);

            RectTransform panelRect = qtePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 500);

            // 0. Dark Glass Overlay Background Card (Circular)
            GameObject bgCardObj = new GameObject("GlassBG");
            bgCardObj.transform.SetParent(qtePanel.transform, false);
            Image bgCard = bgCardObj.AddComponent<Image>();
            bgCard.color = YantraPaper; // กระดาษยันต์
            bgCard.rectTransform.sizeDelta = new Vector2(480, 520);
            bgCard.sprite = null; // แผ่นสี่เหลี่ยม
            BuildYantraFrame(qtePanel.transform);

            // 1. วงแหวนขอบนอก (Outer Ring Frame)
            GameObject outerRingObj = new GameObject("OuterRing");
            outerRingObj.transform.SetParent(qtePanel.transform, false);
            Image outerRing = outerRingObj.AddComponent<Image>();
            outerRing.sprite = circleSprite;
            outerRing.color = YantraInk;
            outerRing.rectTransform.sizeDelta = new Vector2(280, 280);

            // 2. วงกลมพื้นหลังรางล้อ (Track Background)
            GameObject ringObj = new GameObject("RingBG");
            ringObj.transform.SetParent(qtePanel.transform, false);
            ringBgImage = ringObj.AddComponent<Image>();
            ringBgImage.sprite = circleSprite;
            ringBgImage.color = new Color(0.74f, 0.64f, 0.46f, 1f);
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
            targetZoneImage.color = YantraVermilion; // ช่องชาด
            targetZoneImage.rectTransform.sizeDelta = new Vector2(265, 265);

            // 4. วงกลมทับด้านในสร้างทรงวงแหวน (Inner Ring Mask)
            GameObject innerObj = new GameObject("InnerRing");
            innerObj.transform.SetParent(ringObj.transform, false);
            Image innerImage = innerObj.AddComponent<Image>();
            innerImage.sprite = circleSprite;
            innerImage.color = YantraPaper;
            innerImage.rectTransform.sizeDelta = new Vector2(195, 195);

            // 5. ขอบวงแหวนในสุด (Inner Ring Outline)
            GameObject innerOutlineObj = new GameObject("InnerOutline");
            innerOutlineObj.transform.SetParent(ringObj.transform, false);
            Image innerOutline = innerOutlineObj.AddComponent<Image>();
            innerOutline.sprite = circleSprite;
            innerOutline.color = new Color(YantraInk.r, YantraInk.g, YantraInk.b, 0.6f);
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
            needleImage.color = YantraInk; // ด้ามพู่กันหมึก

            // 7. หัวเข็มเรืองแสง (Needle Pointer Tip)
            GameObject tipObj = new GameObject("NeedleTip");
            tipObj.transform.SetParent(needleObj.transform, false);
            Image tipImage = tipObj.AddComponent<Image>();
            tipImage.sprite = circleSprite;
            tipImage.color = YantraInk;
            tipImage.rectTransform.sizeDelta = new Vector2(12, 12);
            tipImage.rectTransform.anchoredPosition = new Vector2(0, 120);

            // 8. จุดยึดตรงกลางเข็ม และอักขระยันต์ศักดิ์สิทธิ์ (Center Cap Hub & Yantra Rune)
            GameObject capObj = new GameObject("CenterCap");
            capObj.transform.SetParent(ringObj.transform, false);
            Image capImage = capObj.AddComponent<Image>();
            capImage.sprite = circleSprite;
            capImage.color = YantraInk;
            capImage.rectTransform.sizeDelta = new Vector2(28, 28);

            GameObject runeObj = new GameObject("CenterRuneText");
            runeObj.transform.SetParent(ringObj.transform, false);
            centerRuneText = runeObj.AddComponent<TextMeshProUGUI>();
            centerRuneText.fontSize = 46;
            centerRuneText.alignment = TextAlignmentOptions.Center;
            centerRuneText.fontStyle = FontStyles.Bold;
            centerRuneText.color = new Color(YantraInk.r, YantraInk.g, YantraInk.b, 0.92f); // อักขระหมึก
            centerRuneText.rectTransform.anchoredPosition = new Vector2(0, 48);
            centerRuneText.rectTransform.sizeDelta = new Vector2(100, 60);

            // 9. ข้อความคำแนะนำ (Prompt Badge Text)
            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(qtePanel.transform, false);
            promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 22;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = YantraInk;
            promptText.rectTransform.anchoredPosition = new Vector2(0, 192);
            promptText.rectTransform.sizeDelta = new Vector2(420, 90);

            // 10. ข้อความนับจำนวน (Progress Counter Text)
            GameObject progressObj = new GameObject("ProgressText");
            progressObj.transform.SetParent(qtePanel.transform, false);
            progressText = progressObj.AddComponent<TextMeshProUGUI>();
            progressText.fontSize = 20;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.fontStyle = FontStyles.Bold;
            progressText.color = YantraInk;
            progressText.rectTransform.anchoredPosition = new Vector2(0, -192);
            progressText.rectTransform.sizeDelta = new Vector2(420, 90);

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

    // ==========================================
    // OnGUI Rendering Fallback (สำรองการวาดหาก UI Canvas ถูกซ่อน)
    // ==========================================
    void OnGUI()
    {
        if (!isQTEActive) return;

        // หาก UI Canvas ทำงานและแสดงผลอยู่แล้ว ให้ข้าม OnGUI Fallback
        if (qtePanel != null && qtePanel.activeInHierarchy && qteCanvas != null && qteCanvas.activeInHierarchy && targetZoneImage != null && needleTransform != null)
        {
            return;
        }

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // 1. กรอบพื้นหลังการ์ดกระจกดำทรงวงกลม (Dark Glass Overlay)
        GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
        GUI.DrawTexture(new Rect(centerX - 220f, centerY - 220f, 440f, 440f), GetGuiCircleTex());

        // 2. หัวข้อและคำแนะนำ
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        string header = !string.IsNullOrEmpty(customTitleText) ? customTitleText : "📜 พิธีเขียนยันต์มหาเวทย์ (YANTRA DRAWING) ✨";
        GUI.Label(new Rect(centerX - 220f, centerY - 180f, 440f, 32f), header, titleStyle);

        GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        promptStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(centerX - 220f, centerY - 145f, 440f, 25f), "กด [ SPACEBAR / BUTTON ] ตวัดพู่กันหยุดตรงช่วงอักขระสว่าง!", promptStyle);

        // 3. วาดวงกลมรางล้อเป้าหมาย QTE (Outer Ring Track Frame)
        GUI.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
        GUI.DrawTexture(new Rect(centerX - 130f, centerY - 130f, 260f, 260f), GetGuiCircleTex());

        // 4. วาดแถบขาว Target Zone บนรางล้อ (White Target Zone Arc)
        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(targetCenterAngle, new Vector2(centerX, centerY));
        GUI.color = (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.ActiveVisualAid)
            ? new Color(1f, 0.85f, 0.2f, 0.95f)
            : new Color(1f, 0.95f, 0.85f, 0.98f);
        float widthPx = Mathf.Clamp(targetWidthAngle * 1.6f, 35f, 160f);
        GUI.DrawTexture(new Rect(centerX - (widthPx / 2f), centerY - 132f, widthPx, 38f), GetGuiCircleTex());
        GUI.matrix = savedMatrix;

        // 5. วงกลมรางล้อใน (Inner Ring Mask)
        GUI.color = new Color(0.06f, 0.07f, 0.1f, 0.98f);
        GUI.DrawTexture(new Rect(centerX - 95f, centerY - 95f, 190f, 190f), GetGuiCircleTex());

        // 6. วาดเข็มหมุนสีแดง (Rotating Neon Red Needle Line & Pointer Tip)
        savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(needleAngle, new Vector2(centerX, centerY));
        GUI.color = new Color(1f, 0.15f, 0.25f, 1f); // Neon Crimson Red Needle
        GUI.DrawTexture(new Rect(centerX - 3f, centerY - 120f, 6f, 120f), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.4f, 0.4f, 1f); // Needle Tip Pointer
        GUI.DrawTexture(new Rect(centerX - 6f, centerY - 124f, 12f, 12f), GetGuiCircleTex());
        GUI.matrix = savedMatrix;

        // 7. Center Cap Hub
        GUI.color = new Color(0.85f, 0.15f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(centerX - 14f, centerY - 14f, 28f, 28f), GetGuiCircleTex());

        // 8. แสดงอักขระยันต์ตรงกลาง
        GUIStyle runeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 44,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        runeStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        int runeIdx = Mathf.Clamp(currentHits, 0, sacredYantraRunes.Length - 1);
        GUI.Label(new Rect(centerX - 60f, centerY - 30f, 120f, 60f), sacredYantraRunes[runeIdx], runeStyle);

        // 9. แสดงนับจำนวนความสำเร็จ
        GUIStyle progStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        progStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(centerX - 220f, centerY + 150f, 440f, 30f), $"ลงยันต์สำเร็จ: {currentHits}/{requiredHits}  |  หลุดจังหวะ: {currentFailures}/{maxFailures}", progStyle);

        GUI.color = Color.white;
    }
}
