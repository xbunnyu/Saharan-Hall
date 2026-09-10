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

        if (slashPanel != null)
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

        if (isJustReleased)
        {
            isDragging = false;
        }
    }

    private void TriggerSlashSuccess()
    {
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
                ? "<color=#00FF7F>⚔️ ฟันยันต์สะบั้นสำเร็จ!</color>" 
                : "<color=#FF4500>💀 พิธีกรรมล้มเหลว!</color>";
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
            promptText.text = $"<color=#FFD700>⚔️ พิธีตวัดดาบฟันยันต์ (SEQUENTIAL SLASH) ✨</color>\nลากเมาส์ตวัดผ่าทิศทางลูกศรให้ครบ! {speedHint}";
        }

        if (progressText != null)
        {
            progressText.text = $"<color=#00FF88>จำนวนรอยฟัน: {currentSlashIndex}/{requiredSlashes}</color>  |  <color=#FF3555>หลุดจังหวะ: {currentFailures}/{maxFailures}</color>";
        }

        if (timerText != null)
        {
            timerText.text = $"⏱️ เวลาตวัด: <color=#FFD700>{currentTimer:F1}s</color> / {currentMaxTime:F1}s";
        }
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
            bgCard.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
            bgCard.rectTransform.sizeDelta = new Vector2(560, 560);
            bgCard.sprite = circleSprite;

            // Header Prompt Text
            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(slashPanel.transform, false);
            promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 22;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
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
            progressText.color = Color.white;
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
