using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// ระบบมินิเกม Smash Key (กดปุ่มรัวๆ แข่งกับเวลา)
/// ใช้สำหรับดิ้นหลุดจากการจับของผี หรือสถานการณ์ QTE เอาชีวิตรอด
/// ผู้เล่นต้องกด [Spacebar] ให้ครบตามจำนวนที่กำหนด (เช่น 20 ครั้ง) ภายในเวลา (เช่น 7 วินาที)
/// หากเว้นว่าง UI ระบบจะสร้าง Canvas และอินเทอร์เฟซที่สวยงามและตื่นเต้นให้อัตโนมัติ 100%
/// </summary>
public class SmashKeyMinigame : MonoBehaviour
{
    public static SmashKeyMinigame Instance { get; private set; }

    [Header("การตั้งค่าทั่วไป (Smash Settings)")]
    [Tooltip("จำนวนครั้งที่ต้องกดให้ครบ")]
    public int defaultTargetSmashCount = 20;

    [Tooltip("เวลาจำกัดในการกด (วินาที)")]
    public float defaultTimeLimit = 7.0f;

    [Tooltip("อัตราการลดลงของเกจหากหยุดกด (0 = ไม่ลด, ค่ายิ่งมากยิ่งท้าทาย)")]
    public float decayRatePerSecond = 0.5f;

    [Header("เสียงเอฟเฟกต์ (Audio FX)")]
    [Tooltip("เสียงตอนกดปุ่มแต่ละครั้ง (Struggle sound)")]
    public AudioClip smashHitSound;

    [Tooltip("เสียงเมื่อดิ้นหลุดสำเร็จ (Win sound)")]
    public AudioClip successSound;

    [Tooltip("เสียงเมื่อหมดเวลา / โดนจับ (Fail / Jumpscare sound)")]
    public AudioClip failSound;

    [Header("UI References (Optional - จะสร้างให้อัตโนมัติหากไม่ได้ลากใส่)")]
    [Tooltip("ความเข้มสูงสุดของขอบจอแดง (ปรับลดลงครึ่งหนึ่งเพื่อให้สบายตาขึ้น)")]
    [Range(0.05f, 0.6f)]
    public float maxVignetteAlpha = 0.22f;

    public Canvas minigameCanvas;
    public GameObject minigamePanel;
    public Image vignetteOverlay;
    public Slider progressBar;
    public Image progressFillImage;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;
    public RectTransform buttonPromptBox;

    public bool IsActive { get; private set; } = false;

    private int targetSmashCount = 20;
    private int currentSmashCount = 0;
    private float timeLimit = 7.0f;
    private float timeRemaining = 0f;

    private Action onSuccessCallback;
    private Action onFailCallback;

    private PlayerController playerController;
    private PlayerInteraction playerInteraction;
    private AudioSource audioSource;
    private Camera playerCam;
    private Vector3 originalCamLocalPos;
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.04f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitUI();
    }

    /// <summary>
    /// เริ่มต้นสร้าง UI อัตโนมัติหากยังไม่มีในฉาก
    /// </summary>
    public void InitUI()
    {
        if (minigameCanvas != null) return;

        // 1. สร้าง Canvas
        GameObject canvasObj = new GameObject("SmashKeyMinigame_Canvas");
        canvasObj.transform.SetParent(transform, false);
        minigameCanvas = canvasObj.AddComponent<Canvas>();
        minigameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        minigameCanvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Main Panel
        minigamePanel = new GameObject("MinigamePanel");
        minigamePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRT = minigamePanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // 3. Dark/Red Horror Vignette Overlay
        GameObject vigObj = new GameObject("VignetteOverlay");
        vigObj.transform.SetParent(minigamePanel.transform, false);
        RectTransform vigRT = vigObj.AddComponent<RectTransform>();
        vigRT.anchorMin = Vector2.zero;
        vigRT.anchorMax = Vector2.one;
        vigRT.offsetMin = Vector2.zero;
        vigRT.offsetMax = Vector2.zero;
        vignetteOverlay = vigObj.AddComponent<Image>();
        vignetteOverlay.color = new Color(0.45f, 0.02f, 0.02f, maxVignetteAlpha * 0.7f);
        vignetteOverlay.raycastTarget = false;

        // 4. Central Box Frame
        GameObject centerBox = new GameObject("CenterBox");
        centerBox.transform.SetParent(minigamePanel.transform, false);
        buttonPromptBox = centerBox.AddComponent<RectTransform>();
        buttonPromptBox.anchorMin = new Vector2(0.5f, 0.5f);
        buttonPromptBox.anchorMax = new Vector2(0.5f, 0.5f);
        buttonPromptBox.pivot = new Vector2(0.5f, 0.5f);
        buttonPromptBox.sizeDelta = new Vector2(700, 380);
        buttonPromptBox.anchoredPosition = new Vector2(0, 30);

        Image boxBg = centerBox.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);

        // 5. Title Prompt Text
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(centerBox.transform, false);
        RectTransform promptRT = promptObj.AddComponent<RectTransform>();
        promptRT.anchorMin = new Vector2(0.5f, 1f);
        promptRT.anchorMax = new Vector2(0.5f, 1f);
        promptRT.pivot = new Vector2(0.5f, 1f);
        promptRT.sizeDelta = new Vector2(660, 80);
        promptRT.anchoredPosition = new Vector2(0, -20);

        promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 32;
        promptText.fontStyle = FontStyles.Bold;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(1f, 0.25f, 0.25f);
        promptText.text = "⚠️ โดนผีจับตัว! กด [ SPACEBAR ] รัวๆ เพื่อดิ้นให้หลุด!";

        // 6. Big Key Prompt & Smash Counter Text
        GameObject counterObj = new GameObject("CounterText");
        counterObj.transform.SetParent(centerBox.transform, false);
        RectTransform countRT = counterObj.AddComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0.5f, 0.5f);
        countRT.anchorMax = new Vector2(0.5f, 0.5f);
        countRT.pivot = new Vector2(0.5f, 0.5f);
        countRT.sizeDelta = new Vector2(660, 90);
        countRT.anchoredPosition = new Vector2(0, 20);

        counterText = counterObj.AddComponent<TextMeshProUGUI>();
        counterText.fontSize = 50;
        counterText.fontStyle = FontStyles.Bold;
        counterText.alignment = TextAlignmentOptions.Center;
        counterText.color = Color.white;
        counterText.text = "0 / 20";

        // 7. Progress Bar
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(centerBox.transform, false);
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.5f, 0f);
        sliderRT.anchorMax = new Vector2(0.5f, 0f);
        sliderRT.pivot = new Vector2(0.5f, 0f);
        sliderRT.sizeDelta = new Vector2(560, 32);
        sliderRT.anchoredPosition = new Vector2(0, 85);

        progressBar = sliderObj.AddComponent<Slider>();
        progressBar.minValue = 0f;
        progressBar.maxValue = 1f;
        progressBar.value = 0f;

        // Progress Bar Background
        Image sliderBg = sliderObj.AddComponent<Image>();
        sliderBg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        // Fill Image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        progressFillImage = fillObj.AddComponent<Image>();
        progressFillImage.color = new Color(0.1f, 0.9f, 0.3f);
        progressBar.fillRect = fillRT;

        // 8. Timer Text
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(centerBox.transform, false);
        RectTransform timerRT = timerObj.AddComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0.5f, 0f);
        timerRT.anchorMax = new Vector2(0.5f, 0f);
        timerRT.pivot = new Vector2(0.5f, 0f);
        timerRT.sizeDelta = new Vector2(560, 45);
        timerRT.anchoredPosition = new Vector2(0, 25);

        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 26;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = new Color(1f, 0.85f, 0.2f);
        timerText.text = "⏱️ เวลาที่เหลือ: 7.0 วินาที";

        minigamePanel.SetActive(false);
    }

    /// <summary>
    /// เริ่มต้นมินิเกม Smash Key
    /// </summary>
    /// <param name="targetCount">จำนวนครั้งที่ต้องกดให้ครบ (Default 20)</param>
    /// <param name="time">เวลาจำกัด (Default 7 วิ)</param>
    /// <param name="onSuccess">ฟังก์ชันเมื่อทำสำเร็จ</param>
    /// <param name="onFail">ฟังก์ชันเมื่อล้มเหลว</param>
    /// <param name="customPrompt">ข้อความแจ้งเตือนด้านบน</param>
    public void StartSmashMinigame(int targetCount = 20, float time = 7.0f, Action onSuccess = null, Action onFail = null, string customPrompt = null)
    {
        if (IsActive) return;

        InitUI();

        targetSmashCount = targetCount > 0 ? targetCount : defaultTargetSmashCount;
        timeLimit = time > 0 ? time : defaultTimeLimit;
        timeRemaining = timeLimit;
        currentSmashCount = 0;
        onSuccessCallback = onSuccess;
        onFailCallback = onFail;
        IsActive = true;

        // ค้นหา Player และล็อคการเคลื่อนไหว
        playerController = FindFirstObjectByType<PlayerController>();
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (playerController != null)
        {
            playerController.isMovementLocked = true;
            if (playerController.playerCamera != null)
            {
                playerCam = playerController.playerCamera.GetComponent<Camera>();
                originalCamLocalPos = playerController.playerCamera.localPosition;
            }
        }

        if (playerCam == null)
        {
            playerCam = Camera.main;
            if (playerCam != null) originalCamLocalPos = playerCam.transform.localPosition;
        }

        if (customPrompt != null && promptText != null)
        {
            promptText.text = customPrompt;
        }
        else if (promptText != null)
        {
            promptText.text = "⚠️ โดนผีจับตัว! กด [ SPACEBAR ] รัวๆ เพื่อดิ้นให้หลุด!";
        }

        UpdateDisplay();

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(true);
        }

        Debug.Log($"[SmashKeyMinigame] 🔥 เริ่มมินิเกม Smash Key! ต้องกด Spacebar {targetSmashCount} ครั้ง ภายใน {timeLimit:F1} วินาที");
    }

    private void Update()
    {
        if (!IsActive) return;

        // 1. นับเวลาถอยหลัง
        timeRemaining -= Time.deltaTime;

        // 2. การลดเกจหากผู้เล่นหยุดกด
        if (decayRatePerSecond > 0f && currentSmashCount > 0)
        {
            float decay = decayRatePerSecond * Time.deltaTime;
            // ลดทีละน้อยหากไม่ได้กด
        }

        // 3. ตรวจจับการกด Spacebar (หรือปุ่ม A / South บน Gamepad)
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        bool spacePressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

        if (spacePressed)
        {
            RegisterSmash();
        }

        // 4. เขย่าหน้าจอ/กล้องเบาๆ ระหว่างดิ้น
        HandleCameraShake();

        // 5. เอฟเฟกต์กะพริบ Vignette สีแดง (ลดความเข้มลงครึ่งหนึ่งตามคำขอ)
        if (vignetteOverlay != null)
        {
            float baseA = maxVignetteAlpha * 0.65f;
            float pulseA = Mathf.PingPong(Time.time * 2f, maxVignetteAlpha * 0.35f);
            vignetteOverlay.color = new Color(0.5f, 0.02f, 0.02f, baseA + pulseA);
        }

        // 6. อัปเดตตัวเลขและเวลาบนจอ
        UpdateDisplay();

        // 7. ตรวจสอบเงื่อนไขชนะ / แพ้
        if (currentSmashCount >= targetSmashCount)
        {
            WinMinigame();
        }
        else if (timeRemaining <= 0f)
        {
            FailMinigame();
        }
    }

    private void RegisterSmash()
    {
        currentSmashCount++;

        // เล่นเสียงทุบ / ดิ้น
        if (smashHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(smashHitSound);
        }

        // เอฟเฟกต์กระตุกปุ่ม (Punch scale animation)
        if (buttonPromptBox != null)
        {
            StopCoroutine(nameof(PunchBoxRoutine));
            StartCoroutine(PunchBoxRoutine());
        }

        // เขย่ากล้อง
        shakeTimer = 0.12f;
    }

    private IEnumerator PunchBoxRoutine()
    {
        if (buttonPromptBox == null) yield break;
        buttonPromptBox.localScale = new Vector3(1.08f, 1.08f, 1f);
        yield return new WaitForSeconds(0.04f);
        if (buttonPromptBox != null)
        {
            buttonPromptBox.localScale = Vector3.one;
        }
    }

    private void HandleCameraShake()
    {
        if (playerCam == null) return;

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * shakeMagnitude;
            playerCam.transform.localPosition = originalCamLocalPos + randomOffset;
        }
        else
        {
            playerCam.transform.localPosition = originalCamLocalPos;
        }
    }

    private void UpdateDisplay()
    {
        float progress = Mathf.Clamp01((float)currentSmashCount / targetSmashCount);

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressFillImage != null)
        {
            // เปลี่ยนสีหลอดจากแดง -> ส้ม -> เขียว ตามความคืบหน้า
            progressFillImage.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.1f, 0.95f, 0.35f), progress);
        }

        if (counterText != null)
        {
            counterText.text = $"<size=120%><b>{currentSmashCount}</b></size> / {targetSmashCount} <size=60%>(กด SPACEBAR รัวๆ!)</size>";
        }

        if (timerText != null)
        {
            float displayTime = Mathf.Max(0f, timeRemaining);
            string timeColor = displayTime <= 2.0f ? "#FF2222" : "#FFD700";
            timerText.text = $"⏱️ เวลาที่เหลือ: <color={timeColor}>{displayTime:F1}</color> วินาที";
        }
    }

    private void WinMinigame()
    {
        IsActive = false;

        if (successSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(successSound);
        }

        ResetPlayerState();

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        Debug.Log("[SmashKeyMinigame] 🎉 ผู้เล่นดิ้นหลุดสำเร็จ!");
        onSuccessCallback?.Invoke();
    }

    private void FailMinigame()
    {
        IsActive = false;

        if (failSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(failSound);
        }

        ResetPlayerState();

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        Debug.Log("[SmashKeyMinigame] 💀 ผู้เล่นกดไม่ทัน! โดนผีจับตัวสำเร็จ!");
        onFailCallback?.Invoke();
    }

    public void StopMinigame()
    {
        IsActive = false;
        ResetPlayerState();
        if (minigamePanel != null) minigamePanel.SetActive(false);
    }

    private void ResetPlayerState()
    {
        if (playerCam != null)
        {
            playerCam.transform.localPosition = originalCamLocalPos;
        }

        if (playerController != null)
        {
            playerController.isMovementLocked = false;
        }
    }
}
