using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// ประเภทฉากจบทั้งหมดของเกม Saharan Hall (3 ฉากจบ)
/// 1. GhostGameOver : แพ้เกม (โดนผีเกาะครบ 3 ตัว)
/// 2. GoodEnding    : จบแบบดี (ค่าความดี Karma >= 50 เมื่อจบวันที่ 3)
/// 3. BadEnding     : จบแบบชั่ว (ค่าความดี Karma < 50 เมื่อจบวันที่ 3)
/// </summary>
public enum GameEndingType
{
    GhostGameOver,    // แพ้เกม (Game Over - โดนผีเกาะครบ 3 ตัว)
    GoodEnding,       // จบแบบดี (Good Ending - ค่าความดี >= 50)
    BadEnding         // จบแบบชั่ว (Bad Ending - ค่าความดี < 50)
}

/// <summary>
/// โครงสร้างข้อมูลจังหวะเนื้อเรื่องคัตซีน (Beat / Act)
/// </summary>
[System.Serializable]
public class EndingCutsceneBeat
{
    [Tooltip("ชื่อบทหรือองก์ เช่น องก์ที่ ๑ : สัมผัสเยียบเย็น")]
    public string actTitle;

    [TextArea(3, 7)]
    [Tooltip("ข้อความบรรยายเรื่องราวในฉากนี้")]
    public string narrationText;

    [Tooltip("ภาพประกอบเสริม (ถ้ามี ไม่ใส่จะใช้บรรยากาศสีธีม)")]
    public Sprite illustration;

    [Tooltip("เสียงเอฟเฟกต์เฉพาะช่วงนี้ (ถ้ามี)")]
    public AudioClip customSfx;
}

/// <summary>
/// โครงสร้างข้อมูลชุดคัตซีนของแต่ละฉากจบ
/// </summary>
[System.Serializable]
public class EndingCutsceneSequence
{
    public GameEndingType endingType;

    [Tooltip("ป้ายสถานะหรือฉายาฉากจบ")]
    public string endingBadge;

    [Tooltip("ชื่อฉากจบทางการ")]
    public string endingName;

    [TextArea(2, 5)]
    [Tooltip("ข้อความสรุปฉากจบสำหรับแสดงบนหน้าต่างผลสรุป (Result Panel)")]
    public string epilogueSummary;

    [Tooltip("สีธีมหลักของฉากจบนี้")]
    public Color themeColor = Color.white;

    [Tooltip("สีบรรยากาศมืด/ไอหมอกของฉากจบนี้")]
    public Color atmosphereTint = new Color(0.08f, 0.02f, 0.09f, 0.95f);

    [Tooltip("เพลงประกอบคัตซีน (ถ้าไม่มี ระบบจะสร้างเสียงบรรยากาศจำลองให้อัตโนมัติ)")]
    public AudioClip cutsceneBGM;

    [Tooltip("ลำดับฉากย่อย/องก์บรรยาย")]
    public List<EndingCutsceneBeat> beats = new List<EndingCutsceneBeat>();
}

/// <summary>
/// ผู้จัดการฉากจบและระบบ Cutscene ศูนย์กลางสำหรับ 3 ฉากจบ
/// รองรับทั้งการเล่นไฟล์วิดิโอฉากจบ (VideoCutscene) และระบบคัตซีนบรรยายเนื้อเรื่องในตัว
/// </summary>
public class GameEndingManager : MonoBehaviour
{
    private static GameEndingManager _instance;
    public static GameEndingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameEndingManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameEndingManager");
                    _instance = go.AddComponent<GameEndingManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    // ==========================================
    // 🎥 Video Cutscenes (วิดิโอฉากจบ)
    // ==========================================
    [Header("🎥 Video Cutscenes (วิดิโอฉากจบ)")]
    [Tooltip("เปิดใช้งานการเล่นไฟล์วิดิโอสำหรับฉากจบ (ถ้ามีคลิปจะเล่นวิดิโอทันทีก่อนเข้าหน้าสรุปผล)")]
    public bool useVideoCutscenes = true;

    [Tooltip("วิดิโอฉากแพ้เกม (Ghost Game Over) - โดนผีครบ 3 ตัว")]
    public VideoClip ghostGameOverVideo;

    [Tooltip("วิดิโอฉากจบแบบดี (Good Ending) - สัจจะบารมี")]
    public VideoClip goodEndingVideo;

    [Tooltip("วิดิโอฉากจบแบบชั่ว (Bad Ending) - วิบัติอวิชชา")]
    public VideoClip badEndingVideo;

    [Header("📁 หรือระบุ URL / Path ไฟล์วิดิโอในเครื่อง (กรณีไม่ได้แปลงเป็น VideoClip)")]
    public string ghostGameOverVideoPath = "";
    public string goodEndingVideoPath = "";
    public string badEndingVideoPath = "";

    [Tooltip("VideoPlayer component (ระบบสร้างให้อัตโนมัติหากเว้นว่าง)")]
    public VideoPlayer videoPlayer;

    // ==========================================
    // UI References (Inspector)
    // ==========================================
    [Header("UI References (Inspector)")]
    [Tooltip("UI Canvas หลักสำหรับฉากจบ (จบแบบดี / จบแบบชั่ว)")]
    public GameObject endingCanvas;

    [Tooltip("UI Canvas หรือ Panel สำหรับฉากแพ้เกม (Game Over)")]
    public GameObject gameOverCanvas;

    [Tooltip("UI Panel สำหรับฉากจบแบบดี (Good Ending)")]
    public GameObject goodEndingPanel;

    [Tooltip("UI Panel สำหรับฉากจบแบบชั่ว (Bad Ending)")]
    public GameObject badEndingPanel;

    [HideInInspector]
    public GameObject kumanThongEndingPanel; // คงฟิลด์ไว้ป้องกัน Missing Reference เก่า

    [Header("UI Components (ค้นหาอัตโนมัติจาก Canvas ถ้าเว้นว่าง)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button restartButton;
    public Button mainMenuButton;
    public Image bgImage;

    // ==========================================
    // Cutscene Settings
    // ==========================================
    [Header("🎬 ระบบ Text Cutscene (เล่นเมื่อไม่มีวิดิโอ)")]
    [Tooltip("เปิดใช้งานคัตซีนบรรยายเรื่องราวก่อนแสดงหน้าสรุปฉากจบ (กรณีไม่มีไฟล์วิดิโอ)")]
    public bool enableCutscenes = true;

    [Tooltip("ความเร็วพิมพ์ดีด (วินาทีต่อ 1 ตัวอักษร)")]
    public float typewriterSpeed = 0.032f;

    [Tooltip("ฟอนต์ภาษาไทยสำหรับคัตซีน (ถ้าเว้นว่างจะค้นหา K2D จากในฉากให้อัตโนมัติ)")]
    public TMP_FontAsset customThaiFont;

    [Tooltip("เสียงเอฟเฟกต์เคาะพิมพ์ดีด (ถ้าไม่ใส่จะใช้เสียงสังเคราะห์ในตัว)")]
    public AudioClip typewriterTickSfx;

    [Header("📚 ลำดับเนื้อเรื่องคัตซีน (ปรับแต่งได้ใน Inspector)")]
    public List<EndingCutsceneSequence> cutsceneSequences = new List<EndingCutsceneSequence>();

    // ==========================================
    // State
    // ==========================================
    [Header("State")]
    public bool isGameEnding = false;
    public GameEndingType currentEndingType = GameEndingType.BadEnding;

    private PlayerController playerController;
    private AudioSource cutsceneAudioSource;
    private AudioSource typewriterAudioSource;
    private AudioClip proceduralTickClip;

    // Video Runtime UI
    private GameObject videoCanvasGO;
    private CanvasGroup videoCanvasGroup;
    private RawImage videoRawImage;
    private RenderTexture videoRenderTexture;
    private bool isVideoPlaying = false;
    private bool skipVideoRequested = false;

    // Text Cutscene Runtime UI
    private GameObject cutsceneCanvasGO;
    private CanvasGroup cutsceneCanvasGroup;
    private RectTransform topLetterbox;
    private RectTransform bottomLetterbox;
    private Image backdropImage;
    private Image cardBg;
    private Image cardGlowBorder;
    private TextMeshProUGUI cutsceneBadgeText;
    private TextMeshProUGUI cutsceneActTitleText;
    private TextMeshProUGUI cutsceneStoryText;
    private TextMeshProUGUI cutscenePageIndicatorText;
    private TextMeshProUGUI cutscenePromptText;
    private Button cutsceneSkipButton;
    private Image illustrationImage;

    // Cutscene Control Flags
    private bool isCutsceneActive = false;
    private bool isTyping = false;
    private bool skipTypingRequested = false;
    private bool advanceBeatRequested = false;
    private bool skipCutsceneRequested = false;
    private Coroutine activeCutsceneCoroutine;
    private Coroutine cameraMotionCoroutine;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        cutsceneAudioSource = gameObject.GetComponent<AudioSource>();
        if (cutsceneAudioSource == null) cutsceneAudioSource = gameObject.AddComponent<AudioSource>();

        typewriterAudioSource = gameObject.AddComponent<AudioSource>();

        EnsureDefaultCutscenesPopulated();
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        EnsureEndingCanvas();
        BindUIComponents();

        // ซ่อน Canvas และ Panel ฉากจบ/Game Over ไว้ก่อนเริ่มเกม
        if (endingCanvas != null) endingCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (goodEndingPanel != null) goodEndingPanel.SetActive(false);
        if (badEndingPanel != null) badEndingPanel.SetActive(false);
    }

    void Update()
    {
        // 1. ตรวจสอบการกดข้ามในโหมดวิดิโอ
        if (isVideoPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                SkipVideoCutscene();
            }
            return;
        }

        // 2. ตรวจสอบการควบคุมในโหมด Text Cutscene
        if (!isCutsceneActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SkipCutscene();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                skipTypingRequested = true;
            }
            else
            {
                advanceBeatRequested = true;
            }
        }
    }

    // ==========================================
    // ประเมินผลฉากจบวันที่ 3 (จบแบบดี หรือ จบแบบชั่ว)
    // ==========================================

    /// <summary>
    /// ประเมินและดึงฉากจบหลังจากกดนอนจบวันที่ 3
    /// - ค่าความดี Karma >= 50 -> จบแบบดี (GoodEnding)
    /// - ค่าความดี Karma < 50  -> จบแบบชั่ว (BadEnding)
    /// </summary>
    public void EvaluateDay3Ending()
    {
        int karma = 40;
        int targetGood = 50;
        if (KarmaManager.Instance != null)
        {
            karma = KarmaManager.Instance.karma;
            targetGood = KarmaManager.Instance.targetGoodKarma;
        }

        Debug.Log($"[GameEndingManager] 📊 ประเมินผลฉากจบวันที่ 3 | ค่าความดี (Karma): {karma} (เกณฑ์จบดี: >={targetGood})");

        if (karma >= targetGood)
        {
            TriggerEnding(GameEndingType.GoodEnding);
        }
        else
        {
            TriggerEnding(GameEndingType.BadEnding);
        }
    }

    /// <summary>
    /// Alias สำหรับรองรับโค้ดเก่า
    /// </summary>
    public void EvaluateDay2Ending() => EvaluateDay3Ending();

    // ==========================================
    // สั่งจบเกมและเรียก Cutscene (Video หรือ Text)
    // ==========================================

    /// <summary>
    /// สั่งจบเกมและเริ่มเล่น Cutscene ตามประเภทฉากจบ (แพ้เกม, จบแบบดี, จบแบบชั่ว)
    /// </summary>
    public void TriggerEnding(GameEndingType endingType)
    {
        if (isGameEnding) return;
        isGameEnding = true;
        currentEndingType = endingType;

        string endingLabel = endingType switch
        {
            GameEndingType.GhostGameOver => "แพ้เกม (Game Over)",
            GameEndingType.GoodEnding => "จบแบบดี (Good Ending)",
            GameEndingType.BadEnding => "จบแบบชั่ว (Bad Ending)",
            _ => endingType.ToString()
        };

        Debug.Log($"[GameEndingManager] 🎬 เรียกฉากจบ: {endingLabel}");

        // หยุดเวลาและหยุดการควบคุมผู้เล่น
        Time.timeScale = 0f;
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ปิดการแสดงผล UI, มินิเกม, เป้าเล็ง, กล่อง Prompt และ HUD อื่นๆ ทั้งหมดที่ไม่เกี่ยวข้อง
        HideAllGameplayAndIrrelevantUI();

        // ปิดจอมืด Fade อื่นๆ ในฉาก
        ClearAllFadeOverlays();

        // ตรวจสอบ Canvas ฉากจบ
        EnsureEndingCanvas();

        // ตรวจสอบว่ามีไฟล์วิดิโอสำหรับฉากจบนี้หรือไม่
        GetVideoForEnding(endingType, out VideoClip videoClip, out string videoPath);
        bool hasVideo = (videoClip != null) || (!string.IsNullOrEmpty(videoPath));

        if (useVideoCutscenes && hasVideo)
        {
            if (activeCutsceneCoroutine != null) StopCoroutine(activeCutsceneCoroutine);
            activeCutsceneCoroutine = StartCoroutine(PlayVideoCutsceneRoutine(videoClip, videoPath, endingType));
        }
        else if (enableCutscenes)
        {
            if (activeCutsceneCoroutine != null) StopCoroutine(activeCutsceneCoroutine);
            activeCutsceneCoroutine = StartCoroutine(PlayCutsceneSequenceRoutine(endingType));
        }
        else
        {
            ShowFinalEndingUI(endingType);
        }
    }

    private void GetVideoForEnding(GameEndingType endingType, out VideoClip clip, out string path)
    {
        clip = null;
        path = "";
        switch (endingType)
        {
            case GameEndingType.GhostGameOver:
                clip = ghostGameOverVideo;
                path = ghostGameOverVideoPath;
                break;
            case GameEndingType.GoodEnding:
                clip = goodEndingVideo;
                path = goodEndingVideoPath;
                break;
            case GameEndingType.BadEnding:
                clip = badEndingVideo;
                path = badEndingVideoPath;
                break;
        }
    }

    // ==========================================
    // 🎥 วิดิโอ Cutscene Coroutine
    // ==========================================

    private IEnumerator PlayVideoCutsceneRoutine(VideoClip clip, string customUrl, GameEndingType endingType)
    {
        isVideoPlaying = true;
        skipVideoRequested = false;

        BuildVideoCanvasUI();
        videoCanvasGO.SetActive(true);
        videoCanvasGroup.alpha = 1f;

        // กำหนดแหล่งวิดิโอ
        if (clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = customUrl;
        }

        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;

        // ตั้งค่าระบบเสียงวิดิโอ
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, cutsceneAudioSource);

        Debug.Log($"[GameEndingManager] 🎥 กำลังโหลดวิดิโอฉากจบ: {(clip != null ? clip.name : customUrl)}");

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared && !skipVideoRequested)
        {
            yield return null;
        }

        if (!skipVideoRequested)
        {
            videoPlayer.Play();
            if (cutsceneAudioSource != null) cutsceneAudioSource.Play();

            // รอจนกว่าวิดิโอจะเล่นจบ หรือผู้เล่นสั่งข้าม
            while (videoPlayer.isPlaying && !skipVideoRequested)
            {
                yield return null;
            }
        }

        // ค่อยๆ เฟดวิดิโอออก
        videoPlayer.Stop();
        if (cutsceneAudioSource != null) cutsceneAudioSource.Stop();

        float fadeDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (videoCanvasGroup != null) videoCanvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        if (videoCanvasGO != null) videoCanvasGO.SetActive(false);
        isVideoPlaying = false;

        ShowFinalEndingUI(endingType);
    }

    public void SkipVideoCutscene()
    {
        if (!isVideoPlaying) return;
        Debug.Log("[GameEndingManager] ⏭️ ผู้เล่นกดข้ามวิดิโอฉากจบ (Skip Video)");
        skipVideoRequested = true;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (cutsceneAudioSource != null) cutsceneAudioSource.Stop();
        if (videoCanvasGO != null) videoCanvasGO.SetActive(false);
        isVideoPlaying = false;

        if (activeCutsceneCoroutine != null)
        {
            StopCoroutine(activeCutsceneCoroutine);
            activeCutsceneCoroutine = null;
        }

        ShowFinalEndingUI(currentEndingType);
    }

    private void BuildVideoCanvasUI()
    {
        if (videoCanvasGO != null) return;

        // สร้าง RenderTexture สำหรับวิดิโอ
        if (videoRenderTexture == null)
        {
            videoRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            videoRenderTexture.name = "EndingVideo_RT";
            videoRenderTexture.Create();
        }

        videoCanvasGO = new GameObject("EndingVideo_Canvas");
        DontDestroyOnLoad(videoCanvasGO);

        Canvas canvas = videoCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4900;

        CanvasScaler scaler = videoCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        videoCanvasGO.AddComponent<GraphicRaycaster>();
        videoCanvasGroup = videoCanvasGO.AddComponent<CanvasGroup>();

        // ฉากหลังดำสนิท
        GameObject bgGO = new GameObject("Video_Backdrop");
        bgGO.transform.SetParent(videoCanvasGO.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        SetFullScreenAnchor(bgRT);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;

        // RawImage สำหรับแสดงผลวิดิโอเต็มจอ
        GameObject rawGO = new GameObject("Video_RawImage");
        rawGO.transform.SetParent(videoCanvasGO.transform, false);
        RectTransform rawRT = rawGO.AddComponent<RectTransform>();
        SetFullScreenAnchor(rawRT);
        videoRawImage = rawGO.AddComponent<RawImage>();
        videoRawImage.texture = videoRenderTexture;

        // ปุ่มข้ามวิดิโอ (Skip Button) มุมขวาบน
        GameObject skipGO = new GameObject("Video_SkipButton");
        skipGO.transform.SetParent(videoCanvasGO.transform, false);
        RectTransform skipRT = skipGO.AddComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1f, 1f);
        skipRT.anchorMax = new Vector2(1f, 1f);
        skipRT.pivot = new Vector2(1f, 1f);
        skipRT.anchoredPosition = new Vector2(-40f, -40f);
        skipRT.sizeDelta = new Vector2(210f, 50f);

        Image skipBg = skipGO.AddComponent<Image>();
        skipBg.color = new Color(0.1f, 0.1f, 0.14f, 0.85f);
        Button skipBtn = skipGO.AddComponent<Button>();
        skipBtn.targetGraphic = skipBg;
        skipBtn.onClick.AddListener(SkipVideoCutscene);

        GameObject skipTextGO = new GameObject("Skip_Text");
        skipTextGO.transform.SetParent(skipGO.transform, false);
        RectTransform skipTextRT = skipTextGO.AddComponent<RectTransform>();
        SetFullScreenAnchor(skipTextRT);
        TextMeshProUGUI skipTMP = skipTextGO.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset thaiFont = GetThaiFontAsset();
        if (thaiFont != null) skipTMP.font = thaiFont;
        skipTMP.text = "ข้ามวิดิโอ ⏭️ [Esc]";
        skipTMP.fontSize = 20;
        skipTMP.alignment = TextAlignmentOptions.Center;
        skipTMP.color = Color.white;

        // สร้าง VideoPlayer
        if (videoPlayer == null)
        {
            videoPlayer = videoCanvasGO.AddComponent<VideoPlayer>();
        }

        videoCanvasGO.SetActive(false);
    }

    // ==========================================
    // Text Cutscene Coroutine (Fallback)
    // ==========================================

    private IEnumerator PlayCutsceneSequenceRoutine(GameEndingType endingType)
    {
        isCutsceneActive = true;
        skipCutsceneRequested = false;

        EndingCutsceneSequence sequence = GetSequence(endingType);
        if (sequence == null || sequence.beats == null || sequence.beats.Count == 0)
        {
            EnsureDefaultCutscenesPopulated();
            sequence = GetSequence(endingType);
        }

        BuildCutsceneCanvasUI();
        cutsceneCanvasGO.SetActive(true);
        cutsceneCanvasGroup.alpha = 0f;

        PlayAtmosphericAudio(sequence);

        if (cameraMotionCoroutine != null) StopCoroutine(cameraMotionCoroutine);
        cameraMotionCoroutine = StartCoroutine(AnimateCinematicCamera(35f));

        if (backdropImage != null) backdropImage.color = sequence.atmosphereTint;
        if (cardGlowBorder != null) cardGlowBorder.color = sequence.themeColor;
        if (cutsceneBadgeText != null)
        {
            cutsceneBadgeText.text = sequence.endingBadge;
            cutsceneBadgeText.color = sequence.themeColor;
        }

        // เลื่อนแถบ Letterbox เข้ามา
        float introTime = 0.75f;
        float elapsed = 0f;
        while (elapsed < introTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / introTime);
            cutsceneCanvasGroup.alpha = t;
            if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(120f, 0f, t));
            if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(-120f, 0f, t));
            yield return null;
        }
        cutsceneCanvasGroup.alpha = 1f;

        int totalBeats = sequence.beats.Count;
        for (int i = 0; i < totalBeats; i++)
        {
            if (skipCutsceneRequested) break;

            EndingCutsceneBeat beat = sequence.beats[i];

            if (cutsceneActTitleText != null) cutsceneActTitleText.text = beat.actTitle;
            if (cutscenePageIndicatorText != null)
            {
                string dots = "";
                for (int d = 0; d < totalBeats; d++)
                {
                    dots += (d == i) ? "<color=#FFD700>●</color> " : "<color=#888888>○</color> ";
                }
                cutscenePageIndicatorText.text = $"หน้า {i + 1} / {totalBeats}  {dots}";
            }

            if (illustrationImage != null)
            {
                if (beat.illustration != null)
                {
                    illustrationImage.sprite = beat.illustration;
                    illustrationImage.gameObject.SetActive(true);
                }
                else
                {
                    illustrationImage.gameObject.SetActive(false);
                }
            }

            if (beat.customSfx != null && cutsceneAudioSource != null)
            {
                cutsceneAudioSource.PlayOneShot(beat.customSfx);
            }

            advanceBeatRequested = false;
            if (cutscenePromptText != null) cutscenePromptText.gameObject.SetActive(false);

            yield return StartCoroutine(TypewriterRoutine(beat.narrationText, cutsceneStoryText));

            if (skipCutsceneRequested) break;

            if (cutscenePromptText != null)
            {
                cutscenePromptText.gameObject.SetActive(true);
                cutscenePromptText.text = (i == totalBeats - 1)
                    ? "[ คลิกเมาส์ หรือกด Space เพื่อเข้าสู่บทสรุป ➔ ]"
                    : "[ คลิกเมาส์ หรือกด Space เพื่ออ่านต่อ ▶ ]";
            }

            advanceBeatRequested = false;
            while (!advanceBeatRequested && !skipCutsceneRequested)
            {
                if (cutscenePromptText != null)
                {
                    float glowAlpha = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
                    cutscenePromptText.alpha = glowAlpha;
                }
                yield return null;
            }

            if (endingType == GameEndingType.GhostGameOver || endingType == GameEndingType.BadEnding)
            {
                yield return StartCoroutine(PulseScreenShake(0.18f, 6f));
            }
        }

        // ค่อยๆ เฟดคัตซีนออก
        float outroTime = 0.6f;
        elapsed = 0f;
        while (elapsed < outroTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outroTime);
            cutsceneCanvasGroup.alpha = 1f - t;
            yield return null;
        }

        cutsceneCanvasGO.SetActive(false);
        isCutsceneActive = false;

        if (cameraMotionCoroutine != null)
        {
            StopCoroutine(cameraMotionCoroutine);
            cameraMotionCoroutine = null;
        }

        ShowFinalEndingUI(endingType);
    }

    public void SkipCutscene()
    {
        if (!isCutsceneActive) return;
        Debug.Log("[GameEndingManager] ⏭️ ผู้เล่นสั่งข้ามคัตซีน (Skip)");
        skipCutsceneRequested = true;
        skipTypingRequested = true;
        advanceBeatRequested = true;

        if (activeCutsceneCoroutine != null)
        {
            StopCoroutine(activeCutsceneCoroutine);
            activeCutsceneCoroutine = null;
        }

        if (cutsceneCanvasGO != null) cutsceneCanvasGO.SetActive(false);
        isCutsceneActive = false;

        if (cameraMotionCoroutine != null)
        {
            StopCoroutine(cameraMotionCoroutine);
            cameraMotionCoroutine = null;
        }

        ShowFinalEndingUI(currentEndingType);
    }

    // ==========================================
    // Typewriter Routine
    // ==========================================

    private IEnumerator TypewriterRoutine(string fullText, TextMeshProUGUI targetText)
    {
        isTyping = true;
        skipTypingRequested = false;
        targetText.text = "";

        int totalChars = fullText.Length;
        for (int i = 0; i < totalChars; i++)
        {
            if (skipTypingRequested || skipCutsceneRequested)
            {
                targetText.text = fullText;
                break;
            }

            if (fullText[i] == '<')
            {
                int closeTagIndex = fullText.IndexOf('>', i);
                if (closeTagIndex != -1)
                {
                    i = closeTagIndex;
                    targetText.text = fullText.Substring(0, i + 1);
                    continue;
                }
            }

            targetText.text = fullText.Substring(0, i + 1);

            if (!char.IsWhiteSpace(fullText[i]) && i % 2 == 0)
            {
                PlayTypewriterTick();
            }

            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        isTyping = false;
        skipTypingRequested = false;
    }

    private void PlayTypewriterTick()
    {
        if (typewriterAudioSource == null) return;

        if (typewriterTickSfx != null)
        {
            typewriterAudioSource.PlayOneShot(typewriterTickSfx, 0.35f);
        }
        else
        {
            if (proceduralTickClip == null) proceduralTickClip = GenerateProceduralTick();
            if (proceduralTickClip != null)
            {
                typewriterAudioSource.pitch = Random.Range(0.92f, 1.08f);
                typewriterAudioSource.PlayOneShot(proceduralTickClip, 0.22f);
            }
        }
    }

    private IEnumerator AnimateCinematicCamera(float maxDuration)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.position;
        Quaternion originalRot = cam.transform.rotation;

        float timer = 0f;
        while (isCutsceneActive && timer < maxDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / Mathf.Max(maxDuration, 1f);

            float dolly = Mathf.Sin(progress * Mathf.PI * 0.5f) * 0.35f;
            float swayX = Mathf.Sin(timer * 0.7f) * 0.04f;
            float swayY = Mathf.Cos(timer * 0.5f) * 0.025f;

            cam.transform.position = originalPos + (cam.transform.forward * dolly) + (cam.transform.right * swayX) + (cam.transform.up * swayY);
            yield return null;
        }

        cam.transform.position = originalPos;
        cam.transform.rotation = originalRot;
    }

    private IEnumerator PulseScreenShake(float duration, float magnitude)
    {
        if (cardBg == null) yield break;
        RectTransform rt = cardBg.rectTransform;
        Vector2 originalAnchored = rt.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float percent = 1f - (elapsed / duration);
            float offsetX = Random.Range(-1f, 1f) * magnitude * percent;
            float offsetY = Random.Range(-1f, 1f) * magnitude * percent;
            rt.anchoredPosition = originalAnchored + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rt.anchoredPosition = originalAnchored;
    }

    // ==========================================
    // หน้าจอสรุปผลฉากจบ (Result UI)
    // ==========================================

    private void ShowFinalEndingUI(GameEndingType endingType)
    {
        EnsureEndingCanvas();
        HideAllGameplayAndIrrelevantUI();

        bool isGhostGameOver = (endingType == GameEndingType.GhostGameOver);
        EndingCutsceneSequence seq = GetSequence(endingType);

        if (badEndingPanel != null)
        {
            badEndingPanel.SetActive(endingType == GameEndingType.BadEnding);
        }

        if (goodEndingPanel != null)
        {
            goodEndingPanel.SetActive(endingType == GameEndingType.GoodEnding);
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(isGhostGameOver);
            if (isGhostGameOver && endingCanvas != null && gameOverCanvas != endingCanvas && gameOverCanvas.transform.parent != endingCanvas.transform)
            {
                endingCanvas.SetActive(false);
            }
        }

        if (isGhostGameOver && gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }
        else if (endingCanvas != null)
        {
            endingCanvas.SetActive(true);
        }

        BindUIComponents();

        if (titleText != null && seq != null)
        {
            titleText.text = seq.endingName;
            titleText.color = seq.themeColor;
        }

        if (descriptionText != null && seq != null)
        {
            descriptionText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(seq.themeColor)}>{seq.endingBadge}</color>\n\n{seq.epilogueSummary}";
        }
    }

    // ==========================================
    // Dynamic Cutscene Canvas UI
    // ==========================================

    private void BuildCutsceneCanvasUI()
    {
        if (cutsceneCanvasGO != null) return;

        TMP_FontAsset thaiFont = GetThaiFontAsset();

        cutsceneCanvasGO = new GameObject("EndingCutscene_Canvas");
        DontDestroyOnLoad(cutsceneCanvasGO);

        Canvas canvas = cutsceneCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;

        CanvasScaler scaler = cutsceneCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        cutsceneCanvasGO.AddComponent<GraphicRaycaster>();
        cutsceneCanvasGroup = cutsceneCanvasGO.AddComponent<CanvasGroup>();

        GameObject backdropGO = new GameObject("Backdrop");
        backdropGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform bdRT = backdropGO.AddComponent<RectTransform>();
        SetFullScreenAnchor(bdRT);
        backdropImage = backdropGO.AddComponent<Image>();
        backdropImage.color = new Color(0.04f, 0.02f, 0.02f, 0.94f);
        backdropImage.raycastTarget = false;

        GameObject topBar = new GameObject("Letterbox_Top");
        topBar.transform.SetParent(cutsceneCanvasGO.transform, false);
        topLetterbox = topBar.AddComponent<RectTransform>();
        topLetterbox.anchorMin = new Vector2(0f, 1f);
        topLetterbox.anchorMax = new Vector2(1f, 1f);
        topLetterbox.pivot = new Vector2(0.5f, 1f);
        topLetterbox.sizeDelta = new Vector2(0f, 110f);
        Image topImg = topBar.AddComponent<Image>();
        topImg.color = Color.black;
        topImg.raycastTarget = false;

        GameObject bottomBar = new GameObject("Letterbox_Bottom");
        bottomBar.transform.SetParent(cutsceneCanvasGO.transform, false);
        bottomLetterbox = bottomBar.AddComponent<RectTransform>();
        bottomLetterbox.anchorMin = new Vector2(0f, 0f);
        bottomLetterbox.anchorMax = new Vector2(1f, 0f);
        bottomLetterbox.pivot = new Vector2(0.5f, 0f);
        bottomLetterbox.sizeDelta = new Vector2(0f, 110f);
        Image bottomImg = bottomBar.AddComponent<Image>();
        bottomImg.color = Color.black;
        bottomImg.raycastTarget = false;

        GameObject skipGO = new GameObject("Cutscene_SkipButton");
        skipGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform skipRT = skipGO.AddComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1f, 1f);
        skipRT.anchorMax = new Vector2(1f, 1f);
        skipRT.pivot = new Vector2(1f, 1f);
        skipRT.anchoredPosition = new Vector2(-40f, -40f);
        skipRT.sizeDelta = new Vector2(190f, 48f);

        Image skipBg = skipGO.AddComponent<Image>();
        skipBg.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);
        cutsceneSkipButton = skipGO.AddComponent<Button>();
        cutsceneSkipButton.targetGraphic = skipBg;
        cutsceneSkipButton.onClick.AddListener(SkipCutscene);

        GameObject skipTextGO = new GameObject("Skip_Text");
        skipTextGO.transform.SetParent(skipGO.transform, false);
        RectTransform skipTextRT = skipTextGO.AddComponent<RectTransform>();
        SetFullScreenAnchor(skipTextRT);
        TextMeshProUGUI skipTMP = skipTextGO.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) skipTMP.font = thaiFont;
        skipTMP.text = "ข้ามคัตซีน ⏭️ [Esc]";
        skipTMP.fontSize = 20;
        skipTMP.alignment = TextAlignmentOptions.Center;
        skipTMP.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        GameObject cardGO = new GameObject("Cutscene_StoryCard");
        cardGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0f);
        cardRT.anchorMax = new Vector2(0.5f, 0f);
        cardRT.pivot = new Vector2(0.5f, 0f);
        cardRT.anchoredPosition = new Vector2(0f, 125f);
        cardRT.sizeDelta = new Vector2(1380f, 320f);

        cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.05f, 0.08f, 0.92f);

        GameObject glowGO = new GameObject("GlowBorder");
        glowGO.transform.SetParent(cardGO.transform, false);
        RectTransform glowRT = glowGO.AddComponent<RectTransform>();
        SetFullScreenAnchor(glowRT);
        cardGlowBorder = glowGO.AddComponent<Image>();
        cardGlowBorder.color = new Color(1f, 0.8f, 0.3f, 0.25f);
        cardGlowBorder.raycastTarget = false;

        GameObject badgeGO = new GameObject("EndingBadge_Text");
        badgeGO.transform.SetParent(cardGO.transform, false);
        RectTransform badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0f, 1f);
        badgeRT.anchorMax = new Vector2(1f, 1f);
        badgeRT.pivot = new Vector2(0.5f, 1f);
        badgeRT.anchoredPosition = new Vector2(0f, -16f);
        badgeRT.sizeDelta = new Vector2(-60f, 32f);

        cutsceneBadgeText = badgeGO.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) cutsceneBadgeText.font = thaiFont;
        cutsceneBadgeText.fontSize = 21;
        cutsceneBadgeText.fontStyle = FontStyles.Bold;
        cutsceneBadgeText.alignment = TextAlignmentOptions.Center;

        GameObject actGO = new GameObject("ActTitle_Text");
        actGO.transform.SetParent(cardGO.transform, false);
        RectTransform actRT = actGO.AddComponent<RectTransform>();
        actRT.anchorMin = new Vector2(0f, 1f);
        actRT.anchorMax = new Vector2(1f, 1f);
        actRT.pivot = new Vector2(0.5f, 1f);
        actRT.anchoredPosition = new Vector2(0f, -50f);
        actRT.sizeDelta = new Vector2(-60f, 38f);

        cutsceneActTitleText = actGO.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) cutsceneActTitleText.font = thaiFont;
        cutsceneActTitleText.fontSize = 26;
        cutsceneActTitleText.fontStyle = FontStyles.Bold;
        cutsceneActTitleText.alignment = TextAlignmentOptions.Center;
        cutsceneActTitleText.color = Color.white;

        GameObject storyGO = new GameObject("Story_Text");
        storyGO.transform.SetParent(cardGO.transform, false);
        RectTransform storyRT = storyGO.AddComponent<RectTransform>();
        storyRT.anchorMin = new Vector2(0f, 0f);
        storyRT.anchorMax = new Vector2(1f, 1f);
        storyRT.offsetMin = new Vector2(50f, 48f);
        storyRT.offsetMax = new Vector2(-50f, -94f);

        cutsceneStoryText = storyGO.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) cutsceneStoryText.font = thaiFont;
        cutsceneStoryText.fontSize = 24;
        cutsceneStoryText.lineSpacing = 18f;
        cutsceneStoryText.alignment = TextAlignmentOptions.TopLeft;
        cutsceneStoryText.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        GameObject pageGO = new GameObject("Page_Text");
        pageGO.transform.SetParent(cardGO.transform, false);
        RectTransform pageRT = pageGO.AddComponent<RectTransform>();
        pageRT.anchorMin = new Vector2(0f, 0f);
        pageRT.anchorMax = new Vector2(0f, 0f);
        pageRT.pivot = new Vector2(0f, 0f);
        pageRT.anchoredPosition = new Vector2(50f, 14f);
        pageRT.sizeDelta = new Vector2(300f, 30f);

        cutscenePageIndicatorText = pageGO.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) cutscenePageIndicatorText.font = thaiFont;
        cutscenePageIndicatorText.fontSize = 18;
        cutscenePageIndicatorText.alignment = TextAlignmentOptions.Left;
        cutscenePageIndicatorText.color = new Color(0.7f, 0.7f, 0.7f, 0.85f);

        GameObject promptGO = new GameObject("Prompt_Text");
        promptGO.transform.SetParent(cardGO.transform, false);
        RectTransform promptRT = promptGO.AddComponent<RectTransform>();
        promptRT.anchorMin = new Vector2(1f, 0f);
        promptRT.anchorMax = new Vector2(1f, 0f);
        promptRT.pivot = new Vector2(1f, 0f);
        promptRT.anchoredPosition = new Vector2(-50f, 14f);
        promptRT.sizeDelta = new Vector2(600f, 30f);

        cutscenePromptText = promptGO.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) cutscenePromptText.font = thaiFont;
        cutscenePromptText.fontSize = 20;
        cutscenePromptText.fontStyle = FontStyles.Bold;
        cutscenePromptText.alignment = TextAlignmentOptions.Right;
        cutscenePromptText.color = new Color(1f, 0.85f, 0.4f, 1f);

        GameObject illusGO = new GameObject("Illustration");
        illusGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform illusRT = illusGO.AddComponent<RectTransform>();
        illusRT.anchorMin = new Vector2(0.5f, 0.5f);
        illusRT.anchorMax = new Vector2(0.5f, 0.5f);
        illusRT.pivot = new Vector2(0.5f, 0.5f);
        illusRT.anchoredPosition = new Vector2(0f, 130f);
        illusRT.sizeDelta = new Vector2(620f, 360f);
        illustrationImage = illusGO.AddComponent<Image>();
        illustrationImage.preserveAspect = true;
        illustrationImage.raycastTarget = false;
        illusGO.SetActive(false);

        cutsceneCanvasGO.SetActive(false);
    }

    private void SetFullScreenAnchor(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private TMP_FontAsset GetThaiFontAsset()
    {
        if (customThaiFont != null) return customThaiFont;

        var allTmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allTmps)
        {
            if (t != null && t.font != null && t.font.name.ToLower().Contains("k2d"))
            {
                customThaiFont = t.font;
                return customThaiFont;
            }
        }

        foreach (var t in allTmps)
        {
            if (t != null && t.font != null)
            {
                customThaiFont = t.font;
                return customThaiFont;
            }
        }

        return null;
    }

    // ==========================================
    // ระบบเสียงบรรยากาศ (Procedural Audio)
    // ==========================================

    private void PlayAtmosphericAudio(EndingCutsceneSequence seq)
    {
        if (cutsceneAudioSource == null) return;

        if (seq != null && seq.cutsceneBGM != null)
        {
            cutsceneAudioSource.clip = seq.cutsceneBGM;
            cutsceneAudioSource.loop = true;
            cutsceneAudioSource.volume = 0.85f;
            cutsceneAudioSource.Play();
            return;
        }

        AudioClip proceduralClip = GenerateProceduralTone(seq != null ? seq.endingType : currentEndingType);
        if (proceduralClip != null)
        {
            cutsceneAudioSource.clip = proceduralClip;
            cutsceneAudioSource.loop = true;
            cutsceneAudioSource.volume = 0.65f;
            cutsceneAudioSource.Play();
        }
    }

    private AudioClip GenerateProceduralTone(GameEndingType endingType)
    {
        int sampleRate = 44100;
        int lengthSamples = sampleRate * 3;
        AudioClip clip = AudioClip.Create("EndingAmbientTone", lengthSamples, 1, sampleRate, false);
        float[] samples = new float[lengthSamples];

        float baseFreq = 55f;
        if (endingType == GameEndingType.GoodEnding) baseFreq = 216f;

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float wave1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
            float wave2 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t) * 0.4f;
            float wave3 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.01f) * t) * 0.2f;

            float envelope = Mathf.Sin((float)i / lengthSamples * Mathf.PI);
            samples[i] = (wave1 + wave2 + wave3) * envelope * 0.45f;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateProceduralTick()
    {
        int sampleRate = 44100;
        int lengthSamples = (int)(sampleRate * 0.02f);
        AudioClip clip = AudioClip.Create("TypewriterTick", lengthSamples, 1, sampleRate, false);
        float[] samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / lengthSamples;
            float freq = Mathf.Lerp(1200f, 400f, t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / sampleRate)) * (1f - t) * 0.4f;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    // ==========================================
    // ค้นหาและเตรียมข้อมูล Cutscenes 3 ฉากจบ
    // ==========================================

    private EndingCutsceneSequence GetSequence(GameEndingType type)
    {
        foreach (var seq in cutsceneSequences)
        {
            if (seq != null && seq.endingType == type) return seq;
        }
        return null;
    }

    private void EnsureDefaultCutscenesPopulated()
    {
        if (cutsceneSequences != null && cutsceneSequences.Count >= 3) return;
        if (cutsceneSequences == null) cutsceneSequences = new List<EndingCutsceneSequence>();

        // 1. แพ้เกม (GhostGameOver / Game Over)
        if (GetSequence(GameEndingType.GhostGameOver) == null)
        {
            var ghostSeq = new EndingCutsceneSequence
            {
                endingType = GameEndingType.GhostGameOver,
                endingBadge = "💀 จุดจบ : แพ้เกม",
                endingName = "แพ้เกม - จบชีพกลางอาถรรพ์",
                themeColor = new Color(1f, 0.25f, 0.25f, 1f),
                atmosphereTint = new Color(0.12f, 0.01f, 0.01f, 0.96f),
                epilogueSummary = "วิญญาณร้ายครบทั้งสามตนที่เกาะกุมร่างได้ดึงดูดพลังชีวิตจนหมดสิ้น กรงเล็บแห่งความตายพรากลมหายใจสุดท้ายของท่านไป ตำหนักสารหันต์เงียบงันลงชั่วนิรันดร์...",
                beats = new List<EndingCutsceneBeat>
                {
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๑ : สัมผัสเยียบเย็น ◈",
                        narrationText = "ลมพายุพัดกระโชกวูบหนึ่ง... ประตูและหน้าต่างตำหนักปิดสนิทลงพร้อมกัน ความหนาวเหน็บที่ผิดธรรมชาติแผ่ซ่านจับขั้วหัวใจ เงาดำทั้งสามตนที่เคยเกาะกุมแผ่นหลัง บัดนี้เริ่มเคลื่อนไหว..."
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๒ : เสียงกระซิบแห่งความตาย ◈",
                        narrationText = "เสียงกระซิบสาปแช่งนับร้อยพันกรีดร้องดังกึกก้องในโสตประสาท เลือดลมในกายเย็นเฉียบจนแข็งค้าง เงาทึบของภูตผีทั้งสามตนทอดร่างบิดเบี้ยว ดวงตาสีแดงก่ำนับสิบคู่จ้องมองลงมาจากเพดานมืดมิด"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๓ : พันธนาการสู่ห้วงอเวจี ◈",
                        narrationText = "กรงเล็บดำทมิฬพุ่งแทงทะลวงผ่านร่าง กระชากดวงวิญญาณให้หลุดลอยจากสังขาร ท่านพยายามเอ่ยบทสวดหรือคว้าสิ่งศักดิ์สิทธิ์ แต่กลับไร้เรี่ยวแรง... ความมืดมิดกลืนกินทุกสรรพสิ่ง"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๔ : ซากหุ่นเชิดไร้วิญญาณ ◈",
                        narrationText = "รุ่งอรุณไม่เคยมาถึงสำหรับท่าน... ตำหนักสารหันต์กลายเป็นสุสานรกร้าง ไร้ซึ่งเสียงลมหายใจของผู้สืบทอด มีเพียงซากร่างไร้วิญญาณที่ถูกวิญญาณร้ายสิงสถิตตลอดกาล"
                    }
                }
            };
            cutsceneSequences.Add(ghostSeq);
        }

        // 2. จบแบบดี (GoodEnding)
        if (GetSequence(GameEndingType.GoodEnding) == null)
        {
            var goodSeq = new EndingCutsceneSequence
            {
                endingType = GameEndingType.GoodEnding,
                endingBadge = "✨ ปลดล็อค : จบแบบดี (Good Ending)",
                endingName = "จบแบบดี : สัจจะบารมีนำทาง",
                themeColor = new Color(1f, 0.88f, 0.4f, 1f),
                atmosphereTint = new Color(0.04f, 0.06f, 0.1f, 0.94f),
                epilogueSummary = "ตลอด 3 วันแห่งการบำเพ็ญตน ด้วยผลกรรมดีและบารมีธรรมอันเปี่ยมล้น (Karma >= 50) ท่านได้ช่วยเหลือผู้คนจนพ้นเคราะห์ ตำหนักสารหันต์หลุดพ้นจากเงามืด และท่านได้กลายเป็นอาจารย์ผู้ชี้ทางสว่างอย่างแท้จริง",
                beats = new List<EndingCutsceneBeat>
                {
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๑ : รุ่งอรุณแห่งความหวัง ◈",
                        narrationText = "ราตรีกาลอันยาวนานของวันที่สามสิ้นสุดลง... แสงทองแรกแห่งอรุณรุ่งสาดส่องทะลุผ่านม่านหมอกหนา กระทบสู่โต๊ะหมู่บูชาและพระพุทธรูปศักดิ์สิทธิ์ กลิ่นดอกบัวและควันธูปหอมสะอาดอบอวลไปทั่วบริเวณ"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๒ : บารมีแห่งความเมตตา ◈",
                        narrationText = "ตลอดระยะเวลาสามวันที่ผ่านมา ผลกรรมดีและสัจจะที่ท่านได้เพียรช่วยเหลือผู้ตกทุกข์ได้ยาก โดยมิหวังลาภยศหรือหลอกลวง ได้กลายเป็นเกราะแก้วคุ้มภัย เสนียดจัญไรและคำสาปอาถรรพ์ต่างมลายสิ้นไปตามแสงธรรม"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๓ : เสียงสรรเสริญแห่งศรัทธา ◈",
                        narrationText = "ชาวบ้านจากทั่วสารทิศต่างเดินทางมากราบไหว้ด้วยความเลื่อมใส เสียงสวดมนต์แว่วหวานดังก้องไปทั่วคุ้งน้ำ ตำหนักแห่งนี้มิใช่ที่พึ่งของอวิชชาหรือความหวาดกลัวอีกต่อไป แต่เป็นประทีปส่องสว่างชี้ทางพ้นทุกข์"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๔ : สืบทอดวิถีผู้ทรงศีล ◈",
                        narrationText = "ท่านมองออกไปนอกหน้าต่างด้วยหัวใจที่สงบนิ่งและเบิกบาน... หน้าที่ผู้สืบทอดตำหนักได้รับการชำระให้บริสุทธิ์ บารมีธรรมที่สั่งสมจะคุ้มครองรักษาแผ่นดินนี้สืบไปชั่วกาลนาน"
                    }
                }
            };
            cutsceneSequences.Add(goodSeq);
        }

        // 3. จบแบบชั่ว (BadEnding)
        if (GetSequence(GameEndingType.BadEnding) == null)
        {
            var badSeq = new EndingCutsceneSequence
            {
                endingType = GameEndingType.BadEnding,
                endingBadge = "💀 ปลดล็อค : จบแบบชั่ว (Bad Ending)",
                endingName = "จบแบบชั่ว : วิบัติอวิชชา สนองกรรม",
                themeColor = new Color(0.85f, 0.35f, 0.95f, 1f),
                atmosphereTint = new Color(0.08f, 0.02f, 0.09f, 0.96f),
                epilogueSummary = "ตลอด 3 วันแห่งความโลภและบาปหนา การหลงใหลในลาภยศ ประกอบพิธีกรรมหลอกลวง และพึ่งพามนตร์ดำ (Karma < 50) นำพาความพินาศมาสู่ตน บาปกรรมที่ก่อไว้สะท้อนกลับมาทำลายล้างผู้ประกอบพิธีจนสิ้นซาก...",
                beats = new List<EndingCutsceneBeat>
                {
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๑ : กลิ่นคาวเลือดในความมืด ◈",
                        narrationText = "เมื่อค่ำคืนของวันที่สามสิ้นสุดลง ท้องฟ้ากลับมืดมิดไร้แสงจันทร์ ลมกรรโชกพัดเปลวเทียนดำวูบไหว กลิ่นเหม็นเน่าและควันพิธีไสยดำที่สั่งสมมาตลอดสามวัน เริ่มย้อนกลับมาเกาะกุมรอบตัว"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๒ : หนี้กรรมที่ไม่อาจหลีกเลี่ยง ◈",
                        narrationText = "'สิ่งที่เจ้าหลอกลวงตลอดเวลาที่ผ่านมา... เงินทองที่แลกมาด้วยบาปหนา และวิญญาณที่เจ้ากักขังไว้...' เสียงกระซิบเย็นเยียบดังก้องจากมุมมืด บรรดาเงาของเจ้ากรรมนายเวรค่อยๆ ปรากฏกายล้อมรอบเตียง"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๓ : เปลวเพลิงอเวจี ◈",
                        narrationText = "กระจกทุกบานในห้องนอนแตกร้าวเป็นเสี่ยงๆ เงามืดพุ่งเข้าฉีกทึ้งความทรงจำและพลังอาคมที่เคยภาคภูมิใจ เวทมนตร์ดำที่ไร้ศีลธรรมกำกับ ได้หวนกลับมาเผาผลาญผู้เป็นนายอย่างสาสม"
                    },
                    new EndingCutsceneBeat
                    {
                        actTitle = "◈ องก์ที่ ๔ : สุสานมนตร์ดำ ◈",
                        narrationText = "เสียงกรีดร้องด้วยความทรมานค่อยๆ เงียบหายไปในความมืด... เช้าวันถัดมา ตำหนักปิดตายลงอย่างถาวร มีเพียงเรื่องเล่าสยองขวัญของอาจารย์ผู้หลงระเริงในอำนาจมืด ที่ต้องชดใช้ด้วยชีวิตและวิญญาณ"
                    }
                }
            };
            cutsceneSequences.Add(badSeq);
        }
    }

    // ==========================================
    // ตรวจสอบและค้นหา UI Canvas ในฉาก
    // ==========================================

    private void EnsureEndingCanvas()
    {
        if (endingCanvas == null)
        {
            GameObject foundCanvas = GameObject.Find("EndingCanvas");
            if (foundCanvas == null) foundCanvas = GameObject.Find("EnedingCanvas");
            if (foundCanvas == null) foundCanvas = GameObject.Find("GameEndingCanvas");
            if (foundCanvas == null)
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    if (c != null && c.gameObject.name.ToLower().Contains("ending") &&
                        !c.gameObject.name.ToLower().Contains("gameover") &&
                        c.gameObject.name != "EndingCutscene_Canvas" &&
                        c.gameObject.name != "EndingVideo_Canvas")
                    {
                        foundCanvas = c.gameObject;
                        break;
                    }
                }
            }

            if (foundCanvas != null) endingCanvas = foundCanvas;
        }

        if (gameOverCanvas == null)
        {
            GameObject foundGOCanvas = GameObject.Find("GameOverCanvas");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("Game over EndingCanvas");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("GameOverPanel");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("GameOver");
            if (foundGOCanvas == null)
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    string nameLower = c.gameObject.name.ToLower();
                    if (c != null && (nameLower.Contains("gameover") || nameLower.Contains("game over")))
                    {
                        foundGOCanvas = c.gameObject;
                        break;
                    }
                }
            }

            if (foundGOCanvas == null && endingCanvas != null)
            {
                Transform goChild = endingCanvas.transform.Find("GameOverPanel");
                if (goChild == null) goChild = endingCanvas.transform.Find("GameOver");
                if (goChild != null) foundGOCanvas = goChild.gameObject;
            }

            if (foundGOCanvas != null) gameOverCanvas = foundGOCanvas;
        }

        if (badEndingPanel == null && endingCanvas != null)
        {
            Transform t = endingCanvas.transform.Find("BadEndingPanel");
            if (t == null) t = endingCanvas.transform.Find("BadEnding");
            if (t == null) t = endingCanvas.transform.Find("Game over EndingCanvas");
            if (t != null) badEndingPanel = t.gameObject;
        }

        if (goodEndingPanel == null && endingCanvas != null)
        {
            Transform t = endingCanvas.transform.Find("GoodEndingPanel");
            if (t == null) t = endingCanvas.transform.Find("GoodEnding");
            if (t == null) t = endingCanvas.transform.Find("Good EndingCanvas ");
            if (t != null) goodEndingPanel = t.gameObject;
        }
    }

    private void BindUIComponents()
    {
        GameObject targetCanvas = (currentEndingType == GameEndingType.GhostGameOver && gameOverCanvas != null)
            ? gameOverCanvas
            : endingCanvas;

        if (targetCanvas == null) targetCanvas = endingCanvas;
        if (targetCanvas == null) return;

        if (titleText == null)
        {
            var tmps = targetCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.transform.parent != null && t.transform.parent.GetComponent<Button>() != null) continue;
                string tName = t.gameObject.name.ToLower();
                if (tName.Contains("title") || tName.Contains("heading") || tName.Contains("หัวข้อ"))
                {
                    titleText = t;
                    break;
                }
            }
            if (titleText == null && tmps.Length > 0)
            {
                foreach (var t in tmps)
                {
                    if (t.transform.parent != null && t.transform.parent.GetComponent<Button>() != null) continue;
                    titleText = t;
                    break;
                }
            }
        }

        if (descriptionText == null)
        {
            var tmps = targetCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t == titleText) continue;
                if (t.transform.parent != null && t.transform.parent.GetComponent<Button>() != null) continue;
                string tName = t.gameObject.name.ToLower();
                if (tName.Contains("desc") || tName.Contains("detail") || tName.Contains("รายละเอียด"))
                {
                    descriptionText = t;
                    break;
                }
            }
        }

        if (restartButton == null || mainMenuButton == null)
        {
            var buttons = targetCanvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string bName = b.gameObject.name.ToLower();
                if (restartButton == null && (bName.Contains("restart") || bName.Contains("retry") || bName.Contains("เริ่มใหม่")))
                {
                    restartButton = b;
                }
                else if (mainMenuButton == null && (bName.Contains("menu") || bName.Contains("return") || bName.Contains("main") || bName.Contains("หน้าหลัก")))
                {
                    mainMenuButton = b;
                }
            }

            if (restartButton == null && buttons.Length > 0) restartButton = buttons[0];
            if (mainMenuButton == null && buttons.Length > 1) mainMenuButton = buttons[1];
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (bgImage == null)
        {
            var images = targetCanvas.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                string iName = img.gameObject.name.ToLower();
                if (iName.Contains("panel") || iName.Contains("bg") || iName.Contains("background"))
                {
                    bgImage = img;
                    break;
                }
            }
            if (bgImage == null && images.Length > 0) bgImage = images[0];
        }
    }

    /// <summary>
    /// ปิดการแสดงผล UI, มินิเกม, เป้าเล็ง, กล่อง Prompt และ HUD ทั้งหมดที่ไม่เกี่ยวข้องกับฉากจบ
    /// ป้องกันไม่ให้ UI มินิเกมหรือการโต้ตอบซ้อนทับกับฉากจบเมื่อหน้าหันไปโดนวัตถุ
    /// </summary>
    public void HideAllGameplayAndIrrelevantUI()
    {
        // 1. ปิดการควบคุมผู้เล่นและการโต้ตอบ (PlayerController & PlayerInteraction)
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        else
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;
        }

        PlayerInteraction playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.enabled = false;
        }

        // 2. ปิดและเคลียร์ UI การโต้ตอบทั้งหมด (Prompt, Crosshair, Reading, Inventory, etc.)
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdatePrompt(null, true, false);
            if (InteractionUIManager.Instance.promptPanel != null) InteractionUIManager.Instance.promptPanel.SetActive(false);
            if (InteractionUIManager.Instance.crosshairImage != null) InteractionUIManager.Instance.crosshairImage.gameObject.SetActive(false);
            if (InteractionUIManager.Instance.readingPanel != null) InteractionUIManager.Instance.readingPanel.SetActive(false);
            if (InteractionUIManager.Instance.notificationPanel != null) InteractionUIManager.Instance.notificationPanel.SetActive(false);
            if (InteractionUIManager.Instance.inventoryPanel != null) InteractionUIManager.Instance.inventoryPanel.SetActive(false);
            if (InteractionUIManager.Instance.heldItemHUDPanel != null) InteractionUIManager.Instance.heldItemHUDPanel.SetActive(false);
            if (InteractionUIManager.Instance.itemDetailPanel != null) InteractionUIManager.Instance.itemDetailPanel.SetActive(false);
        }

        // 3. ปิด Highlight ของไอเทมทั้งหมดในฉาก
        var highlighters = FindObjectsByType<ItemHighlighter>(FindObjectsSortMode.None);
        foreach (var h in highlighters)
        {
            if (h != null) h.SetHovered(false);
        }

        // 4. สั่งหยุดและปิดมินิเกมทุกชนิดที่อาจเปิดค้างอยู่
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.isMinigameActive = false;
        }

        if (QTEController.Instance != null)
        {
            if (QTEController.Instance.qtePanel != null) QTEController.Instance.qtePanel.SetActive(false);
            if (QTEController.Instance.qteCanvas != null) QTEController.Instance.qteCanvas.SetActive(false);
        }

        if (TimingBarController.Instance != null)
        {
            if (TimingBarController.Instance.mainPanel != null) TimingBarController.Instance.mainPanel.SetActive(false);
            if (TimingBarController.Instance.canvasObj != null) TimingBarController.Instance.canvasObj.SetActive(false);
        }

        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.isPlaying = false;
        }

        if (SequentialSlashController.Instance != null)
        {
            if (SequentialSlashController.Instance.slashPanel != null) SequentialSlashController.Instance.slashPanel.SetActive(false);
            if (SequentialSlashController.Instance.slashCanvas != null) SequentialSlashController.Instance.slashCanvas.SetActive(false);
        }

        if (SmashKeyMinigame.Instance != null)
        {
            if (SmashKeyMinigame.Instance.minigamePanel != null) SmashKeyMinigame.Instance.minigamePanel.SetActive(false);
        }

        // 5. ปิดหน้าต่างเควส, ร้านค้า, ศาลกุมาร, ป้ายประกาศ
        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.CloseDialog();
        }

        if (ShopController.Instance != null)
        {
            ShopController.Instance.CloseShop();
        }

        if (KumanThongUIController.Instance != null)
        {
            KumanThongUIController.Instance.CloseUI();
        }

        if (AltarUpgradeUI.Instance != null)
        {
            AltarUpgradeUI.Instance.CloseUI();
        }

        if (DailyNewsBulletin.Instance != null)
        {
            DailyNewsBulletin.Instance.CloseBulletin();
        }

        // 6. ปิด Canvas และ HUD อื่นๆ ทั้งหมดในฉาก ยกเว้น Canvas ของฉากจบ
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c == null) continue;
            GameObject cgo = c.gameObject;

            // ข้าม Canvas ที่เกี่ยวข้องกับฉากจบ
            if (cgo == endingCanvas || cgo == gameOverCanvas ||
                cgo == cutsceneCanvasGO || cgo == videoCanvasGO ||
                cgo == goodEndingPanel || cgo == badEndingPanel)
            {
                continue;
            }

            if (endingCanvas != null && c.transform.IsChildOf(endingCanvas.transform)) continue;
            if (gameOverCanvas != null && c.transform.IsChildOf(gameOverCanvas.transform)) continue;

            // ปิด Canvas อื่นๆ ที่ไม่เกี่ยวข้อง
            c.enabled = false;
        }
    }

    private void ClearAllFadeOverlays()
    {
        var fadeObjs = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in fadeObjs)
        {
            if (c.gameObject != null && c.gameObject != endingCanvas && c.gameObject != gameOverCanvas &&
                c.gameObject.name != "EndingCutscene_Canvas" &&
                c.gameObject.name != "EndingVideo_Canvas" &&
                c.gameObject.name.ToLower().Contains("fade"))
            {
                c.gameObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // การนำทางและเริ่มเกมใหม่
    // ==========================================

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameEnding = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isGameEnding = false;
        SceneManager.LoadScene("MainMenu");
    }

    // ==========================================
    // ContextMenu สำหรับทดสอบ 3 ฉากจบใน Unity Editor
    // ==========================================

    [ContextMenu("🧪 ทดสอบ Cutscene: แพ้เกม (GhostGameOver)")]
    public void TestTriggerGhostGameOver()
    {
        isGameEnding = false;
        TriggerEnding(GameEndingType.GhostGameOver);
    }

    [ContextMenu("🧪 ทดสอบ Cutscene: จบแบบดี (GoodEnding)")]
    public void TestTriggerGoodEnding()
    {
        isGameEnding = false;
        TriggerEnding(GameEndingType.GoodEnding);
    }

    [ContextMenu("🧪 ทดสอบ Cutscene: จบแบบชั่ว (BadEnding)")]
    public void TestTriggerBadEnding()
    {
        isGameEnding = false;
        TriggerEnding(GameEndingType.BadEnding);
    }

    [ContextMenu("🧪 ทดสอบ: ประเมินผลฉากจบวันที่ 3 (EvaluateDay3Ending)")]
    public void TestEvaluateDay3Ending() => EvaluateDay3Ending();

    public void TestEvaluateDay2Ending() => EvaluateDay3Ending();
}
