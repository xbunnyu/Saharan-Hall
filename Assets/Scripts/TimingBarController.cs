using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// ตัวจัดการมินิเกมแถบจังหวะโปรยข้าวสารไล่ผี (Horizontal Timing Bar QTE)
/// - ตัวชี้เลื่อนสลับซ้าย-ขวาบนแถบแนวนอน (PingPong Movement)
/// - กด [ Spacebar ] เพื่อหยุดตัวชี้ให้อยู่ในโซนเป้าหมาย PERFECT
/// - สุ่มกดให้สำเร็จ 4-6 ครั้ง | พลาดได้สูงสุด 2 ครั้ง | ความเร็วค่อยๆ เพิ่มขึ้นทุกรอบ
/// - รองรับ Dual Rendering (Canvas UI + OnGUI Fallback)
/// </summary>
public class TimingBarController : MonoBehaviour
{
    public static TimingBarController Instance { get; private set; }

    [Header("1. Settings")]
    public Key timingKey = Key.Space;

    [Header("2. UI References (Optional - จะสร้างให้อัตโนมัติหากเว้นว่าง)")]
    public GameObject canvasObj;
    public GameObject mainPanel;
    public RectTransform barFrameRect;
    public RectTransform targetZoneRect;
    public RectTransform indicatorRect;
    public Image targetZoneImage;
    public Image indicatorImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI nodesText;

    [Header("3. Audio (Optional)")]
    public AudioClip hitSuccessSound;
    public AudioClip hitMissSound;
    public AudioClip winSound;
    public AudioClip failSound;

    // Runtime state
    private bool isActive = false;
    private int requiredHits = 4;
    private int currentHits = 0;
    private int currentFailures = 0;
    private int maxFailures = 3;

    private float baseSpeed = 1.2f; // ความเร็วพื้นฐานของการวิ่งสลับ (Cycles per sec)
    private float currentSpeed = 1.2f;
    private float speedIncrement = 0.35f;

    private float pingPongTime = 0f;
    private float currentNormalizedPos = 0.5f; // 0.0 = ซ้ายสุด, 1.0 = ขวาสุด

    private float targetCenterNormalized = 0.5f;
    private float targetWidthNormalized = 0.22f; // 22% ของความกว้างแถบ
    private string customTitle = "";

    private Action onSuccessCallback;
    private Action onFailCallback;

    private PlayerController playerController;
    private PlayerInteraction playerInteraction;
    private AudioSource audioSource;

    private const float BAR_WIDTH = 540f; // ความกว้างแถบพิกเซล

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

        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!isActive) return;

        // 1. คำนวณอนิเมชันการวิ่งสลับซ้าย-ขวา (PingPong Movement)
        pingPongTime += Time.deltaTime * currentSpeed;
        currentNormalizedPos = Mathf.PingPong(pingPongTime, 1.0f);

        // อัปเดตตำแหน่งพิกเซลของตัวชี้บน Canvas UI
        if (indicatorRect != null)
        {
            float startX = -BAR_WIDTH / 2f;
            float targetX = Mathf.Lerp(startX, -startX, currentNormalizedPos);
            indicatorRect.anchoredPosition = new Vector2(targetX, indicatorRect.anchoredPosition.y);
        }

        // 2. ระบบ Auto-Catch จากบารมีโต๊ะหมู่บูชา Level 4
        if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.ActiveAutoCatch)
        {
            float halfWidth = targetWidthNormalized / 2f;
            if (currentNormalizedPos >= targetCenterNormalized - halfWidth && currentNormalizedPos <= targetCenterNormalized + halfWidth)
            {
                EvaluateHit();
                return;
            }
        }

        // 3. รับ Input ปุ่มกด
        var keyboard = Keyboard.current;
        bool pressed = false;

        if (keyboard != null && keyboard[timingKey].wasPressedThisFrame)
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
    /// เริ่มต้นมินิเกม Horizontal Timing Bar QTE
    /// </summary>
    public void StartTimingBarGame(int minHits, int maxHits, float baseSpd, float speedInc, int maxFails, Action onSuccess, Action onFail, string title = "")
    {
        if (isActive) return;

        EnsureUIExists();

        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        this.requiredHits = UnityEngine.Random.Range(minHits, maxHits + 1);
        this.currentHits = 0;
        this.currentFailures = 0;
        this.maxFailures = maxFails;
        this.speedIncrement = speedInc;
        this.onSuccessCallback = onSuccess;
        this.onFailCallback = onFail;
        this.customTitle = title;

        // ดึงบัฟจากโต๊ะหมู่บูชา (BuddhistAltarManager)
        if (BuddhistAltarManager.Instance != null)
        {
            float windowMult = BuddhistAltarManager.Instance.ActiveQteWindow / 0.25f;
            this.targetWidthNormalized = Mathf.Clamp(0.22f * windowMult, 0.20f, 0.45f);

            float speedMult = BuddhistAltarManager.Instance.ActiveQteSpeed / 1.8f;
            this.baseSpeed = baseSpd * speedMult;
        }
        else
        {
            this.targetWidthNormalized = 0.22f;
            this.baseSpeed = baseSpd;
        }

        this.currentSpeed = this.baseSpeed;
        this.pingPongTime = 0f;

        GenerateNewTargetZone();

        LockPlayerControls(true);

        isActive = true;
        if (canvasObj != null) canvasObj.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(true);

        UpdateUI();
    }

    public bool IsActive() => isActive;

    private void EvaluateHit()
    {
        float halfWidth = targetWidthNormalized / 2f;
        bool isSuccess = currentNormalizedPos >= (targetCenterNormalized - halfWidth) 
                      && currentNormalizedPos <= (targetCenterNormalized + halfWidth);

        if (isSuccess)
        {
            // กดโดนโซน PERFECT
            currentHits++;
            currentSpeed += speedIncrement;

            PlaySound(hitSuccessSound, 650f, 0.15f);

            if (currentHits >= requiredHits)
            {
                StartCoroutine(FinishGameRoutine(true));
            }
            else
            {
                GenerateNewTargetZone();
                UpdateUI();
            }
        }
        else
        {
            // กดพลาดนอกโซน PERFECT
            currentFailures++;

            PlaySound(hitMissSound, 180f, 0.25f);

            if (currentFailures > maxFailures)
            {
                StartCoroutine(FinishGameRoutine(false));
            }
            else
            {
                GenerateNewTargetZone();
                UpdateUI();
            }
        }
    }

    private IEnumerator FinishGameRoutine(bool success)
    {
        isActive = false;

        if (statusText != null)
        {
            statusText.text = success ? "<color=#00FF7F>สำเร็จ!</color>" : "<color=#FF4500>ล้มเหลว!</color>";
        }

        PlaySound(success ? winSound : failSound, success ? 880f : 130f, success ? 0.4f : 0.5f);

        yield return new WaitForSeconds(0.6f);

        if (mainPanel != null) mainPanel.SetActive(false);

        LockPlayerControls(false);

        if (success) onSuccessCallback?.Invoke();
        else onFailCallback?.Invoke();
    }

    private void GenerateNewTargetZone()
    {
        // สุ่มตำแหน่งจุดศูนย์กลางของโซน PERFECT ช่วง 25% ถึง 75%
        targetCenterNormalized = UnityEngine.Random.Range(0.25f, 0.75f);

        if (targetZoneRect != null)
        {
            float widthPx = targetWidthNormalized * BAR_WIDTH;
            targetZoneRect.sizeDelta = new Vector2(widthPx, targetZoneRect.sizeDelta.y);

            float startX = -BAR_WIDTH / 2f;
            float targetX = Mathf.Lerp(startX, -startX, targetCenterNormalized);
            targetZoneRect.anchoredPosition = new Vector2(targetX, 0f);
        }

        if (targetZoneImage != null)
        {
            if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.ActiveVisualAid)
            {
                targetZoneImage.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            }
            else
            {
                targetZoneImage.color = new Color(1f, 0.92f, 0.4f, 0.92f);
            }
        }
    }

    private void UpdateUI()
    {
        if (titleText != null)
        {
            string header = !string.IsNullOrEmpty(customTitle)
                ? customTitle
                : "🌾 พิธีโปรยข้าวสารไล่ผี (EXORCISM RICE TOSSING) 👻";

            string altarAidTag = "";
            if (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.currentLevel > 1)
            {
                altarAidTag = $"\n<size=80%><color=#FFD700>⛩️ บารมีโต๊ะหมู่บูชา Lv.{BuddhistAltarManager.Instance.currentLevel}: {BuddhistAltarManager.GetLevelBenefitText(BuddhistAltarManager.Instance.currentLevel)}</color></size>";
            }

            titleText.text = $"<color=#FFD700>{header}</color>{altarAidTag}";
        }

        if (promptText != null)
        {
            promptText.text = "กด <color=#00FFFF>[ SPACEBAR / BUTTON ]</color> ให้ตัวชี้หยุดตรงโซน PERFECT!";
        }

        if (progressText != null)
        {
            progressText.text = $"<color=#00FF88>ลงจังหวะสำเร็จ: {currentHits}/{requiredHits}</color>  |  <color=#FF3555>หลุดจังหวะ: {currentFailures}/{maxFailures}</color>";
        }

        if (nodesText != null)
        {
            string nodes = "";
            for (int i = 0; i < requiredHits; i++)
            {
                if (i < currentHits) nodes += "<color=#FFD700>● </color>";
                else nodes += "<color=#778899>○ </color>";
            }
            nodesText.text = nodes.TrimEnd();
        }

        if (statusText != null)
        {
            statusText.text = "";
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

        AudioClip clip = AudioClip.Create("TimingBarSynthBeep", sampleCount, 1, sampleRate, false);
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

    private Sprite CreateCircleSprite(int size = 128)
    {
        Texture2D tex = GetGuiCircleTex();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private void EnsureUIExists()
    {
        if (mainPanel != null && canvasObj != null && barFrameRect != null && targetZoneRect != null && indicatorRect != null) return;

        canvasObj = GameObject.Find("TimingBar_Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("TimingBar_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }

        mainPanel = canvasObj.transform.Find("TimingBarPanel")?.gameObject;
        if (mainPanel != null && (barFrameRect == null || targetZoneRect == null || indicatorRect == null))
        {
            DestroyImmediate(mainPanel);
            mainPanel = null;
        }

        if (mainPanel == null)
        {
            Sprite circleSprite = CreateCircleSprite();

            mainPanel = new GameObject("TimingBarPanel");
            mainPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.5f, 0.5f);
            mainRect.anchorMax = new Vector2(0.5f, 0.5f);
            mainRect.sizeDelta = new Vector2(700, 320);

            // 0. Dark Glass Overlay Card
            GameObject glassBg = new GameObject("GlassBG");
            glassBg.transform.SetParent(mainPanel.transform, false);
            Image bgImg = glassBg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            bgImg.rectTransform.sizeDelta = new Vector2(680, 300);

            // 1. Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(mainPanel.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.rectTransform.anchoredPosition = new Vector2(0, 105);
            titleText.rectTransform.sizeDelta = new Vector2(650, 50);

            // 2. Prompt Text
            GameObject promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(mainPanel.transform, false);
            promptText = promptObj.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 16;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
            promptText.rectTransform.anchoredPosition = new Vector2(0, 60);
            promptText.rectTransform.sizeDelta = new Vector2(650, 30);

            // 3. Node Progress Dots Text (● ● ○ ○)
            GameObject nodesObj = new GameObject("NodesText");
            nodesObj.transform.SetParent(mainPanel.transform, false);
            nodesText = nodesObj.AddComponent<TextMeshProUGUI>();
            nodesText.fontSize = 22;
            nodesText.alignment = TextAlignmentOptions.Center;
            nodesText.rectTransform.anchoredPosition = new Vector2(0, 32);
            nodesText.rectTransform.sizeDelta = new Vector2(650, 30);

            // 4. Bar Frame Background Track
            GameObject barFrameObj = new GameObject("BarFrame");
            barFrameObj.transform.SetParent(mainPanel.transform, false);
            barFrameRect = barFrameObj.AddComponent<RectTransform>();
            barFrameRect.sizeDelta = new Vector2(BAR_WIDTH, 44);
            barFrameRect.anchoredPosition = new Vector2(0, -15);

            Image barFrameImg = barFrameObj.AddComponent<Image>();
            barFrameImg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);

            // Left & Right Arrow Badges ◀ ▶
            GameObject leftArrow = new GameObject("LeftArrow");
            leftArrow.transform.SetParent(barFrameObj.transform, false);
            var leftText = leftArrow.AddComponent<TextMeshProUGUI>();
            leftText.text = "◀";
            leftText.fontSize = 20;
            leftText.alignment = TextAlignmentOptions.Center;
            leftText.color = new Color(1f, 0.85f, 0.2f);
            leftText.rectTransform.anchoredPosition = new Vector2(-BAR_WIDTH / 2f + 18, 0);
            leftText.rectTransform.sizeDelta = new Vector2(30, 30);

            GameObject rightArrow = new GameObject("RightArrow");
            rightArrow.transform.SetParent(barFrameObj.transform, false);
            var rightText = rightArrow.AddComponent<TextMeshProUGUI>();
            rightText.text = "▶";
            rightText.fontSize = 20;
            rightText.alignment = TextAlignmentOptions.Center;
            rightText.color = new Color(1f, 0.85f, 0.2f);
            rightText.rectTransform.anchoredPosition = new Vector2(BAR_WIDTH / 2f - 18, 0);
            rightText.rectTransform.sizeDelta = new Vector2(30, 30);

            // 5. Target Zone (Golden PERFECT Section)
            GameObject targetZoneObj = new GameObject("TargetZone");
            targetZoneObj.transform.SetParent(barFrameObj.transform, false);
            targetZoneRect = targetZoneObj.AddComponent<RectTransform>();
            targetZoneRect.sizeDelta = new Vector2(BAR_WIDTH * targetWidthNormalized, 44);
            targetZoneRect.anchoredPosition = Vector2.zero;

            targetZoneImage = targetZoneObj.AddComponent<Image>();
            targetZoneImage.color = new Color(1f, 0.92f, 0.4f, 0.92f);

            // Target Zone Label "PERFECT"
            GameObject perfectLabel = new GameObject("PerfectLabel");
            perfectLabel.transform.SetParent(targetZoneObj.transform, false);
            var perfText = perfectLabel.AddComponent<TextMeshProUGUI>();
            perfText.text = "PERFECT";
            perfText.fontSize = 14;
            perfText.fontStyle = FontStyles.Bold;
            perfText.alignment = TextAlignmentOptions.Center;
            perfText.color = new Color(0.1f, 0.08f, 0f, 0.95f);
            perfText.rectTransform.sizeDelta = new Vector2(120, 30);

            // 6. Indicator Pointer (Triangle ▼)
            GameObject indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(barFrameObj.transform, false);
            indicatorRect = indicatorObj.AddComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(24, 24);
            indicatorRect.anchoredPosition = new Vector2(0, 32);

            var indicatorText = indicatorObj.AddComponent<TextMeshProUGUI>();
            indicatorText.text = "▼";
            indicatorText.fontSize = 26;
            indicatorText.alignment = TextAlignmentOptions.Center;
            indicatorText.color = new Color(1f, 0.2f, 0.3f, 1f);

            // 7. Progress Text
            GameObject progressObj = new GameObject("ProgressText");
            progressObj.transform.SetParent(mainPanel.transform, false);
            progressText = progressObj.AddComponent<TextMeshProUGUI>();
            progressText.fontSize = 16;
            progressText.fontStyle = FontStyles.Bold;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.rectTransform.anchoredPosition = new Vector2(0, -68);
            progressText.rectTransform.sizeDelta = new Vector2(650, 35);

            // 8. Status Text
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(mainPanel.transform, false);
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 26;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.rectTransform.anchoredPosition = new Vector2(0, -105);
            statusText.rectTransform.sizeDelta = new Vector2(250, 45);
        }
    }

    // ==========================================
    // OnGUI Fallback Rendering
    // ==========================================
    void OnGUI()
    {
        if (!isActive) return;

        if (mainPanel != null && mainPanel.activeInHierarchy && canvasObj != null && canvasObj.activeInHierarchy && barFrameRect != null && targetZoneRect != null && indicatorRect != null)
        {
            return;
        }

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // 1. Dark Glass Card Overlay
        GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        GUI.DrawTexture(new Rect(centerX - 340f, centerY - 150f, 680f, 300f), GetGuiCircleTex());

        // 2. Title & Prompt
        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleSt.normal.textColor = new Color(1f, 0.85f, 0.2f);
        string header = !string.IsNullOrEmpty(customTitle) ? customTitle : "🌾 พิธีโปรยข้าวสารไล่ผี (EXORCISM RICE TOSSING) 👻";
        GUI.Label(new Rect(centerX - 320f, centerY - 130f, 640f, 30f), header, titleSt);

        GUIStyle promptSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        promptSt.normal.textColor = Color.white;
        GUI.Label(new Rect(centerX - 320f, centerY - 95f, 640f, 25f), "กด [ SPACEBAR / BUTTON ] ให้ตัวชี้หยุดตรงโซน PERFECT!", promptSt);

        // 3. Horizontal Bar Track
        float barW = 540f;
        float barH = 40f;
        float barX = centerX - (barW / 2f);
        float barY = centerY - 15f;

        GUI.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);

        // 4. Target Zone (PERFECT Section)
        float targetW = targetWidthNormalized * barW;
        float targetX = barX + (targetCenterNormalized * barW) - (targetW / 2f);

        GUI.color = (BuddhistAltarManager.Instance != null && BuddhistAltarManager.Instance.ActiveVisualAid)
            ? new Color(1f, 0.85f, 0.2f, 0.95f)
            : new Color(1f, 0.92f, 0.4f, 0.95f);
        GUI.DrawTexture(new Rect(targetX, barY, targetW, barH), Texture2D.whiteTexture);

        GUIStyle perfSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        perfSt.normal.textColor = new Color(0.1f, 0.08f, 0f);
        GUI.Label(new Rect(targetX, barY + 5f, targetW, barH - 10f), "PERFECT", perfSt);

        // 5. Indicator Pointer (▼)
        float indX = barX + (currentNormalizedPos * barW) - 10f;
        float indY = barY - 26f;

        GUIStyle indSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        indSt.normal.textColor = new Color(1f, 0.2f, 0.3f);
        GUI.Label(new Rect(indX, indY, 20f, 25f), "▼", indSt);

        // 6. Progress Text
        GUIStyle progSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        progSt.normal.textColor = Color.white;
        GUI.Label(new Rect(centerX - 320f, centerY + 50f, 640f, 30f), $"ลงจังหวะสำเร็จ: {currentHits}/{requiredHits}  |  หลุดจังหวะ: {currentFailures}/{maxFailures}", progSt);

        GUI.color = Color.white;
    }
}
