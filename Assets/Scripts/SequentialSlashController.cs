using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// ตัวจัดการมินิเกมตวัดดาบ/ฟันยันต์สะบั้นมาร (Sequential Slash QTE)
/// - สุ่มจำนวนครั้งที่ต้องฟัน 3 ถึง 5 ครั้ง
/// - เวลาต่อรอยฟันจะเร็วขึ้นเรื่อยๆ (รอยฟันแรก 6.0 วินาที ➔ รอยฟันสุดท้าย 3.0 วินาที)
/// - พลาดได้ไม่เกิน 2 ครั้ง (พลาดครั้งที่ 3 = ล้มเหลวทันที)
/// - สร้าง UI Canvas + OnGUI Fallback และเสียงตวัดดาบสังเคราะห์ (Procedural Synth SFX)
/// </summary>
public class SequentialSlashController : MonoBehaviour
{
    public static SequentialSlashController Instance { get; private set; }

    [Header("UI References (Optional)")]
    public GameObject slashCanvas;
    public GameObject slashPanel;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;

    [Header("Audio SFX (Optional)")]
    public AudioClip slashSound;
    public AudioClip winSound;
    public AudioClip failSound;

    // Runtime State
    private bool isSlashActive = false;
    private int requiredSlashes = 3;
    private int currentSlashIndex = 0;
    private int currentFailures = 0;
    private int maxFailures = 3;

    // Timer per slash (6.0s down to 3.0s)
    private float initialMaxTime = 6.0f;
    private float minMaxTime = 3.0f;
    private float currentMaxTime = 6.0f;
    private float currentTimer = 6.0f;

    // Slash Target Vectors (Screen Normalized 0..1 or Pixels)
    private Vector2 slashStartPos;
    private Vector2 slashEndPos;
    private float slashAngle = 0f;
    private bool isDragging = false;
    private Vector2 dragStartPos;
    private bool hasPassedStartPoint = false;

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

        // ซ่อนหน้า UI เฉพาะตอนที่ยังไม่ได้เริ่มเล่น
        // (ตัวควบคุมถูกสร้างแล้วเริ่มเกมทันทีในเฟรมเดียวกัน → Start() มาทีหลังแล้วเคยไปซ่อน UI ทิ้ง ทำให้หน้าจอไม่ขึ้น)
        if (slashPanel != null && !isSlashActive)
        {
            slashPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isSlashActive) return;

        // 1. อัปเดตเวลาถอยหลังสำหรับรอยฟันปัจจุบัน
        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            TriggerSlashFail("หมดเวลาตวัดดาบ!");
            return;
        }

        // 2. รับค่า Input การลากเมาส์ / Touch / Gamepad
        HandleSlashInput();
    }

    /// <summary>
    /// เริ่มเล่นมินิเกม Sequential Slash QTE
    /// </summary>
    public void StartSlashGame(int minHits, int maxHits, Action onSuccess, Action onFail)
    {
        if (isSlashActive) return;

        EnsureUIExists();

        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        // สุ่มจำนวนครั้งที่ต้องฟัน (3 ถึง 5 ครั้ง)
        this.requiredSlashes = UnityEngine.Random.Range(Mathf.Clamp(minHits, 3, 5), Mathf.Clamp(maxHits, 3, 5) + 1);
        this.currentSlashIndex = 0;
        this.currentFailures = 0;
        this.maxFailures = 3;
        this.onSuccessCallback = onSuccess;
        this.onFailCallback = onFail;

        LockPlayerControls(true);

        isSlashActive = true;
        if (slashCanvas != null) slashCanvas.SetActive(true);
        if (slashPanel != null) slashPanel.SetActive(true);

        // เริ่มต้นสร้างทิศทางฟันรอยแรก
        PrepareNextSlash();
    }

    public bool IsSlashActive()
    {
        return isSlashActive;
    }

    /// <summary>
    /// เตรียมทิศทางและเวลาสำหรับรอยฟันถัดไป
    /// </summary>
    private void PrepareNextSlash()
    {
        // คำนวณเวลาถอยหลัง: รอยแรก 6.0s ➔ รอยสุดท้าย 3.0s
        float progressFraction = requiredSlashes > 1 ? (float)currentSlashIndex / (requiredSlashes - 1) : 0f;
        float baseTime = Mathf.Lerp(initialMaxTime, minMaxTime, progressFraction);

        // ดึงบัฟเวลาจากโต๊ะหมู่บูชา (ถ้ามี)
        float altarTimeBonus = 0f;
        if (BuddhistAltarManager.Instance != null)
        {
            altarTimeBonus = (BuddhistAltarManager.Instance.currentLevel - 1) * 0.4f;
        }

        currentMaxTime = baseTime + altarTimeBonus;
        currentTimer = currentMaxTime;
        isDragging = false;
        hasPassedStartPoint = false;

        // สุ่มมุมทิศทางการฟัน (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°)
        float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
        slashAngle = angles[UnityEngine.Random.Range(0, angles.Length)];

        // สุ่มจุดศูนย์กลางรอยฟันตรงกลางจอ
        float screenW = Screen.width;
        float screenH = Screen.height;
        Vector2 center = new Vector2(screenW / 2f, screenH / 2f);

        float lineLength = Mathf.Min(screenW, screenH) * 0.35f;
        Vector2 dir = new Vector2(Mathf.Cos(slashAngle * Mathf.Deg2Rad), Mathf.Sin(slashAngle * Mathf.Deg2Rad));

        slashStartPos = center - dir * (lineLength / 2f);
        slashEndPos = center + dir * (lineLength / 2f);

        try { RefreshGuide(); }
        catch (Exception e) { Debug.LogError("[SequentialSlash] สร้างเส้นนำทางไม่สำเร็จ: " + e); }
        UpdateUI();
    }

    /// <summary>
    /// ตรวจสอบการลากเมาส์ / การกดตวัดดาบ
    /// </summary>
    private void HandleSlashInput()
    {
        var mouse = Mouse.current;
        var gamepad = Gamepad.current;

        Vector2 currentMousePos = Vector2.zero;
        bool isPressing = false;
        bool isJustPressed = false;
        bool isJustReleased = false;

        if (mouse != null)
        {
            currentMousePos = mouse.position.ReadValue();
            isPressing = mouse.leftButton.isPressed;
            isJustPressed = mouse.leftButton.wasPressedThisFrame;
            isJustReleased = mouse.leftButton.wasReleasedThisFrame;
        }

        // กรณีใช้ Gamepad Stick / Button
        if (gamepad != null && gamepad.rightStick.magnitude > 0.5f)
        {
            Vector2 stickDir = gamepad.rightStick.ReadValue().normalized;
            Vector2 targetDir = (slashEndPos - slashStartPos).normalized;

            if (Vector2.Dot(stickDir, targetDir) > 0.75f)
            {
                TriggerSlashSuccess();
                return;
            }
        }

        // ตรวจจับการกดลากเมาส์
        if (isJustPressed)
        {
            float distToStart = Vector2.Distance(currentMousePos, slashStartPos);
            // ขยายรัศมีตามบารมีโต๊ะหมู่บูชา
            float hitRadius = 80f;
            if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.currentLevel > 1)
            {
                hitRadius += (BuddhistAltarManager.Instance.currentLevel - 1) * 20f;
            }

            if (distToStart <= hitRadius)
            {
                isDragging = true;
                hasPassedStartPoint = true;
                dragStartPos = currentMousePos;
            }
        }

        if (isDragging && hasPassedStartPoint && isPressing)
        {
            float distToEnd = Vector2.Distance(currentMousePos, slashEndPos);
            float hitRadius = 90f;

            if (distToEnd <= hitRadius)
            {
                // ตวัดดาบผ่านจุดสิ้นสุดสำเร็จ!
                isDragging = false;
                hasPassedStartPoint = false;
                TriggerSlashSuccess();
            }
        }

        if (isDragging && isPressing) AddTrail(currentMousePos);
        lastTrailPos = currentMousePos;

        if (isJustReleased)
        {
            isDragging = false;
        }
    }

    private void TriggerSlashSuccess()
    {
        SpawnCutMark(slashStartPos, slashEndPos);
        currentSlashIndex++;
        PlaySound(slashSound, 600f + (currentSlashIndex * 80f), 0.18f);

        if (currentSlashIndex >= requiredSlashes)
        {
            // ทำครบทุกรอยฟันแล้ว ➔ สำเร็จ!
            StartCoroutine(FinishSlashRoutine(true));
        }
        else
        {
            // ไปยังรอยฟันถัดไป
            PrepareNextSlash();
        }
    }

    private void TriggerSlashFail(string reason)
    {
        currentFailures++;
        PlaySound(failSound, 160f, 0.3f);

        if (currentFailures > maxFailures)
        {
            // พลาดเกิน 2 ครั้ง ➔ ล้มเหลว!
            StartCoroutine(FinishSlashRoutine(false));
        }
        else
        {
            // ให้ลองฟันใหม่ในรอยเดิม
            PrepareNextSlash();
        }
    }

    private IEnumerator FinishSlashRoutine(bool success)
    {
        isSlashActive = false;

        if (statusText != null)
        {
            statusText.text = success 
                ? "<color=#E8DCC4>ตัดอาคมขาดสะบั้น</color>" 
                : "<color=#B0473A>อาคมยังไม่ขาด</color>";
        }

        PlaySound(success ? winSound : failSound, success ? 880f : 120f, success ? 0.45f : 0.5f);

        yield return new WaitForSeconds(0.8f);

        if (slashPanel != null) slashPanel.SetActive(false);
        if (slashCanvas != null) slashCanvas.SetActive(false);

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

    private void UpdateUI()
    {
        if (promptText != null)
        {
            string speedHint = currentSlashIndex == 0 ? "(เริ่ม 6.0 วิ)" : $"(เร่งสปีด! {currentMaxTime:F1} วิ)";
            promptText.text = $"<color=#E8DCC4><size=120%>พิธีตวัดดาบฟันอาคม</size></color>\n<size=80%><color=#A8977A>กดค้างที่ด้ามดาบ แล้วลากตามรอยชาดไปจนถึงตราประทับ</color></size>";
        }

        if (progressText != null)
        {
            progressText.text = $"<color=#E8DCC4>รอยฟัน {currentSlashIndex}/{requiredSlashes}</color>" + (currentFailures > 0 ? $"   <color=#B0473A>พลาด {currentFailures}/{maxFailures}</color>" : "");
        }

        if (timerText != null)
        {
            timerText.text = $"<color={(currentTimer <= 1.5f ? "#B0473A" : "#FFB35C")}>{currentTimer:F1}</color> <size=70%><color=#A8977A>วินาที</color></size>"; UpdateTimerBar();
        }
    }

    // ══════════ หน้าตาใหม่: รอยชาดนำทาง / ด้ามดาบ / ตราประทับ / แสงดาบ / รอยแผล ══════════
    private RectTransform guideRoot;
    private Sprite dotSprite;
    private Image timerBarFill;
    private Vector2 lastTrailPos;
    private static readonly Color Vermilion = new Color(0.66f, 0.18f, 0.12f, 1f);
    private static readonly Color Cream = new Color(0.91f, 0.86f, 0.77f, 1f);

    private Vector2 ScreenToPanel(Vector2 screen)
    {
        RectTransform pr = slashPanel.GetComponent<RectTransform>();
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(pr, screen, null, out local);
        return local;
    }

    private Image MakeImg(Transform parent, string name, Vector2 size, Vector2 pos, Color c, Sprite sprite = null, float rot = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = c;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = size;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
        return img;
    }

    private void EnsureGuideRoot()
    {
        if (slashPanel == null) return;
        if (dotSprite == null) dotSprite = CreateCircleSprite(64);
        if (guideRoot == null)
        {
            var go = new GameObject("Guide", typeof(RectTransform));
            go.transform.SetParent(slashPanel.transform, false);
            guideRoot = go.GetComponent<RectTransform>();
        }
        if (timerBarFill == null)
        {
            // แถบเวลาเป็นเส้นธูปที่ค่อยๆ มอดไหม้ (ใต้ตัวเลขเวลา)
            MakeImg(slashPanel.transform, "IncenseBG", new Vector2(300, 6), new Vector2(0, 160), new Color(0.2f, 0.13f, 0.08f, 0.9f));
            timerBarFill = MakeImg(slashPanel.transform, "IncenseFill", new Vector2(300, 6), new Vector2(-150, 160), new Color(0.75f, 0.35f, 0.15f, 1f));
            timerBarFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        }
    }

    private void UpdateTimerBar()
    {
        if (timerBarFill == null || currentMaxTime <= 0f) return;
        float k = Mathf.Clamp01(currentTimer / currentMaxTime);
        timerBarFill.rectTransform.sizeDelta = new Vector2(300f * k, 6f);
    }

    /// <summary>วาดเส้นนำแบบรอยชาดประ + ด้ามดาบที่จุดเริ่ม + ตราประทับที่จุดจบ</summary>
    private void RefreshGuide()
    {
        EnsureGuideRoot();
        if (guideRoot == null) return;
        for (int i = guideRoot.childCount - 1; i >= 0; i--) Destroy(guideRoot.GetChild(i).gameObject);

        Vector2 a = ScreenToPanel(slashStartPos);
        Vector2 b = ScreenToPanel(slashEndPos);
        Vector2 d = b - a;
        float len = d.magnitude;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        int dashes = Mathf.Max(6, Mathf.RoundToInt(len / 26f));
        for (int i = 0; i < dashes; i++)
        {
            float t = (i + 0.5f) / dashes;
            MakeImg(guideRoot, "Dash", new Vector2(14, 4), Vector2.Lerp(a, b, t), new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.75f), null, ang);
        }

        // ด้ามดาบ (จุดเริ่ม)
        MakeImg(guideRoot, "HiltRing", new Vector2(58, 58), a, new Color(Cream.r, Cream.g, Cream.b, 0.35f), dotSprite);
        MakeImg(guideRoot, "HiltCore", new Vector2(20, 20), a, Cream, dotSprite);
        MakeImg(guideRoot, "Handle", new Vector2(40, 8), a - d.normalized * 34f, new Color(0.35f, 0.22f, 0.12f, 1f), null, ang);
        MakeImg(guideRoot, "Guard", new Vector2(6, 26), a - d.normalized * 12f, new Color(0.62f, 0.45f, 0.22f, 1f), null, ang);

        // ตราประทับ (จุดจบ)
        MakeImg(guideRoot, "SealOuter", new Vector2(62, 62), b, new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.9f), dotSprite);
        MakeImg(guideRoot, "SealInner", new Vector2(50, 50), b, new Color(0.45f, 0.10f, 0.07f, 1f), dotSprite);
        var lblGo = new GameObject("SealText");
        lblGo.transform.SetParent(guideRoot, false);
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        if (promptText != null) lbl.font = promptText.font;
        lbl.text = "ฟัน";
        lbl.fontSize = 20;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Cream;
        lbl.raycastTarget = false;
        lbl.rectTransform.sizeDelta = new Vector2(60, 40);
        lbl.rectTransform.anchoredPosition = b;
    }

    /// <summary>แสงดาบตามเมาส์ขณะลาก</summary>
    private void AddTrail(Vector2 screenPos)
    {
        if (slashPanel == null || guideRoot == null) return;
        Vector2 p0 = ScreenToPanel(lastTrailPos);
        Vector2 p1 = ScreenToPanel(screenPos);
        Vector2 d = p1 - p0;
        if (d.sqrMagnitude < 4f) return;
        var seg = MakeImg(slashPanel.transform, "BladeTrail", new Vector2(d.magnitude + 4f, 5f), (p0 + p1) * 0.5f,
                          new Color(0.92f, 0.94f, 1f, 0.85f), null, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        StartCoroutine(FadeAndDestroy(seg, 0.25f));
    }

    /// <summary>ฟันสำเร็จ: รอยแผลสีแดงคล้ำพาดตามแนวที่ฟัน</summary>
    private void SpawnCutMark(Vector2 startScreen, Vector2 endScreen)
    {
        if (slashPanel == null) return;
        Vector2 a = ScreenToPanel(startScreen);
        Vector2 b = ScreenToPanel(endScreen);
        Vector2 d = b - a;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        var cut = MakeImg(slashPanel.transform, "CutMark", new Vector2(d.magnitude * 1.15f, 7f), (a + b) * 0.5f, new Color(0.55f, 0.06f, 0.05f, 0.95f), null, ang);
        var glow = MakeImg(slashPanel.transform, "CutGlow", new Vector2(d.magnitude * 1.15f, 2f), (a + b) * 0.5f, new Color(1f, 0.9f, 0.8f, 1f), null, ang);
        StartCoroutine(FadeAndDestroy(cut, 0.7f));
        StartCoroutine(FadeAndDestroy(glow, 0.3f));
    }

    private IEnumerator FadeAndDestroy(Image img, float life)
    {
        float t = 0f;
        Color c0 = img.color;
        while (t < life && img != null)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - t / life));
            yield return null;
        }
        if (img != null) Destroy(img.gameObject);
    }

    private void LockPlayerControls(bool lockState)
    {
        if (playerController != null) playerController.enabled = !lockState;

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
            PlaySynthSlashBeep(synthFreq, duration);
        }
    }

    private void PlaySynthSlashBeep(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * (t / duration)); // Pitch swoosh
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.2f;
            samples[i] = (Mathf.Sin(2 * Mathf.PI * (frequency + (1.0f - t/duration) * 300f) * t) * 0.7f + noise) * envelope * 0.45f;
        }

        AudioClip clip = AudioClip.Create("SlashSynthSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip, 0.5f);
    }

    private Sprite CreateCircleSprite(int size = 128)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - dist + 1f);
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void EnsureUIExists()
    {
        if (slashPanel != null && slashCanvas != null) return;

        slashCanvas = GameObject.Find("Slash_Canvas");
        if (slashCanvas == null)
        {
            slashCanvas = new GameObject("Slash_Canvas");
            Canvas canvas = slashCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = slashCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            slashCanvas.AddComponent<GraphicRaycaster>();
        }
        else
        {
            Canvas c = slashCanvas.GetComponent<Canvas>();
            if (c != null) c.sortingOrder = 999;
        }

        slashPanel = slashCanvas.transform.Find("SlashPanel")?.gameObject;
        if (slashPanel == null)
        {
            Sprite circleSprite = CreateCircleSprite(128);

            slashPanel = new GameObject("SlashPanel");
            slashPanel.transform.SetParent(slashCanvas.transform, false);

            RectTransform panelRect = slashPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(600, 600);

            // Background Card
            GameObject bgCardObj = new GameObject("GlassBG");
            bgCardObj.transform.SetParent(slashPanel.transform, false);
            Image bgCard = bgCardObj.AddComponent<Image>();
            bgCard.color = new Color(0f, 0f, 0f, 0.35f); // จางๆ ให้เห็นหลัง NPC ที่จะฟัน
            bgCard.rectTransform.sizeDelta = new Vector2(560, 560);
            bgCard.sprite = circleSprite;

            // Header Prompt Text
            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(slashPanel.transform, false);
            promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 22;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color32(0xE8, 0xDC, 0xC4, 0xFF);
            promptText.rectTransform.anchoredPosition = new Vector2(0, 230);
            promptText.rectTransform.sizeDelta = new Vector2(540, 70);

            // Timer Text
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(slashPanel.transform, false);
            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.fontSize = 20;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = new Color(1f, 0.85f, 0.2f);
            timerText.rectTransform.anchoredPosition = new Vector2(0, 185);
            timerText.rectTransform.sizeDelta = new Vector2(540, 40);

            // Progress Text
            GameObject progressObj = new GameObject("ProgressText");
            progressObj.transform.SetParent(slashPanel.transform, false);
            progressText = progressObj.AddComponent<TextMeshProUGUI>();
            progressText.fontSize = 18;
            progressText.fontStyle = FontStyles.Bold;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = new Color32(0xE8, 0xDC, 0xC4, 0xFF);
            progressText.rectTransform.anchoredPosition = new Vector2(0, -230);
            progressText.rectTransform.sizeDelta = new Vector2(540, 40);

            // Status Text
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(slashPanel.transform, false);
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 28;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontStyle = FontStyles.Bold;
            statusText.rectTransform.anchoredPosition = new Vector2(0, 0);
            statusText.rectTransform.sizeDelta = new Vector2(300, 60);
        }
    }

    // ==========================================
    // OnGUI Fallback Rendering (การันตีการวาด 100%)
    // ==========================================
    void OnGUI()
    {
        if (!isSlashActive) return;
        // Canvas แบบใหม่ทำงานอยู่ → ไม่ต้องวาดสำรองทับ
        // ระบบวาดสำรองแบบเก่า (START/END, SLASH: 0/4) ปิดถาวร — ใช้หน้า UI แบบใหม่เท่านั้น
        return;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // 1. กรอบพื้นหลังการ์ดกระจกดำ
        GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        GUI.Box(new Rect(centerX - 270f, centerY - 270f, 540f, 540f), GUIContent.none);

        // 2. หัวข้อและคำแนะนำ
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(centerX - 250f, centerY - 250f, 500f, 32f), "⚔️ พิธีตวัดดาบฟันยันต์ (SEQUENTIAL SLASH) ✨", titleStyle);

        // เวลาถอยหลังและระดับสปีด
        GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        timerStyle.normal.textColor = currentTimer <= 1.5f ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 1f, 0.5f);
        GUI.Label(new Rect(centerX - 250f, centerY - 215f, 500f, 25f), $"⏱️ เวลาตวัด: {currentTimer:F1}s / {currentMaxTime:F1}s (รอยฟันที่ {currentSlashIndex + 1}/{requiredSlashes})", timerStyle);

        // 3. วาดเส้นและจุดรอยฟัน (Start ➔ End Arrow Line)
        float startX = slashStartPos.x;
        float startY = Screen.height - slashStartPos.y; // Flip Y for IMGUI
        float endX = slashEndPos.x;
        float endY = Screen.height - slashEndPos.y;

        // วาดจุดเริ่ม (🟢 Start Circle)
        GUI.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        GUI.Box(new Rect(startX - 20f, startY - 20f, 40f, 40f), "▶ START");

        // วาดจุดปลายทาง (🔴 End Target Circle)
        GUI.color = new Color(1f, 0.25f, 0.25f, 0.9f);
        GUI.Box(new Rect(endX - 22.5f, endY - 22.5f, 45f, 45f), "🎯 END");

        // วาดเส้นแนวทิศทางรอยฟัน (Glowing Crimson Line)
        GUI.color = new Color(1f, 0.15f, 0.25f, 0.85f);
        DrawLine(new Vector2(startX, startY), new Vector2(endX, endY), 4f);

        // 4. แสดงข้อความนับจำนวนความคืบหน้า
        GUIStyle progStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        progStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(centerX - 250f, centerY + 225f, 500f, 30f), $"SLASH: {currentSlashIndex}/{requiredSlashes}  |  FAIL: {currentFailures}/{maxFailures} (ห้ามพลาดเกิน 3 ครั้ง)", progStyle);

        GUI.color = Color.white;
    }

    private void DrawLine(Vector2 pointA, Vector2 pointB, float width)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color savedColor = GUI.color;

        float angle = Vector2.Angle(pointB - pointA, Vector2.right);
        if (pointA.y > pointB.y) angle = -angle;

        GUIUtility.ScaleAroundPivot(new Vector2((pointB - pointA).magnitude, width), new Vector2(pointA.x, pointA.y + 0.5f));
        GUIUtility.RotateAroundPivot(angle, pointA);

        GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1, 1), Texture2D.whiteTexture);
        GUI.matrix = matrix;
        GUI.color = savedColor;
    }
}
