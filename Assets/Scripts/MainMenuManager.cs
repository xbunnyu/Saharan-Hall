using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ผู้จัดการหน้า Main Menu (เมนูหลัก) สำหรับเกม Saharan Hall
/// รองรับ:
/// 1. ปุ่มเริ่มเกม (Start Game) - โหลดฉาก GamePlay พร้อมแถบโหลด (Loading Bar)
/// 2. ปุ่มตั้งค่า (Settings) - ปรับระดับเสียง (Master/BGM/SFX), ความไวเมาส์, คุณภาพกราฟิก, โหมดเต็มจอ, ความละเอียด
/// 3. ปุ่มออกจากเกม (Quit Game) - พร้อมหน้าต่างยืนยัน (Confirmation Dialog)
/// 4. รองรับทั้ง Canvas UI (uGUI / TextMeshPro) และ Fallback OnGUI ที่สวยงามในตัว
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("--- UI Mode ---")]
    [Tooltip("เปิดใช้งาน UI จำลองแบบ OnGUI (ให้ติ๊กออกถ้าต้องการจัดวาง Canvas เองใน Unity Editor)")]
    public bool useBuiltInOnGUI = false;

    [Header("--- Scene Loading ---")]
    [Tooltip("ชื่อฉากที่ต้องการโหลดเมื่อกดปุ่มเริ่มเกม")]
    public string gameplaySceneName = "GamePlay";
    [Tooltip("Panel หน้าต่างแสดงสถานะการโหลด (ไม่ใส่ก็ได้)")]
    public GameObject loadingPanel;
    [Tooltip("Slider แถบแสดงเปอร์เซ็นต์การโหลด (ไม่ใส่ก็ได้)")]
    public Slider loadingProgressBar;
    [Tooltip("ข้อความแสดงเปอร์เซ็นต์การโหลด (ไม่ใส่ก็ได้)")]
    public TextMeshProUGUI loadingProgressText;

    [Header("--- Main Menu Buttons ---")]
    [Tooltip("ปุ่มเริ่มเกม")]
    public Button playButton;
    [Tooltip("ปุ่มตั้งค่า")]
    public Button settingsButton;
    [Tooltip("ปุ่มออกจากเกม")]
    public Button quitButton;

    [Header("--- Panels ---")]
    [Tooltip("Panel เมนูหลัก")]
    public GameObject mainMenuPanel;
    [Tooltip("Panel หน้าต่างตั้งค่า")]
    public GameObject settingsPanel;
    [Tooltip("Panel หน้าต่างยืนยันการออกจากเกม (ถ้าไม่ใช้จะออกจากเกมทันทีเมื่อกดปุ่มออก)")]
    public GameObject quitConfirmPanel;

    [Header("--- Settings - Audio ---")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeValueText;
    public Slider bgmVolumeSlider;
    public TextMeshProUGUI bgmVolumeValueText;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeValueText;

    [Header("--- Settings - Graphics & Controls ---")]
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI mouseSensitivityValueText;
    public Button closeSettingsButton;
    public Button resetSettingsButton;

    [Header("--- Quit Confirmation Dialog ---")]
    public Button confirmQuitButton;
    public Button cancelQuitButton;

    [Header("--- Audio / Sound Effects (Optional) ---")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickClip;
    public AudioClip buttonHoverClip;

    // Internal State
    private bool isSettingsOpen = false;
    private bool isQuitConfirmOpen = false;
    private bool isLoading = false;
    private Resolution[] availableResolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();

    // Constants for PlayerPrefs keys
    public const string PREF_MASTER_VOLUME = "MasterVolume";
    public const string PREF_BGM_VOLUME = "BGMVolume";
    public const string PREF_SFX_VOLUME = "SFXVolume";
    public const string PREF_QUALITY_LEVEL = "QualityLevel";
    public const string PREF_FULLSCREEN = "Fullscreen";
    public const string PREF_RESOLUTION_INDEX = "ResolutionIndex";
    public const string PREF_MOUSE_SENSITIVITY = "MouseSensitivity";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // ปลดล็อคและแสดงเคอร์เซอร์เมาส์ในหน้าเมนูเสมอ
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        EnsureAudioSource();
    }

    private void Start()
    {
        InitializeResolutions();
        LoadAndApplySettings();
        BindButtonEvents();

        // สถานะเริ่มต้นของ Panels
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private void Update()
    {
        // จัดการคีย์ลัด Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isSettingsOpen)
            {
                CloseSettings();
            }
            else if (isQuitConfirmOpen)
            {
                CloseQuitConfirm();
            }
            else
            {
                OpenQuitConfirm();
            }
        }
    }

    #region Event Binding
    private void BindButtonEvents()
    {
        // ปุ่มเมนูหลัก
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null)
            quitButton.onClick.AddListener(OpenQuitConfirm);

        // ปุ่มยืนยันออกจากเกม
        if (confirmQuitButton != null)
            confirmQuitButton.onClick.AddListener(ConfirmQuitGame);
        if (cancelQuitButton != null)
            cancelQuitButton.onClick.AddListener(CloseQuitConfirm);

        // ปุ่มในการตั้งค่า
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);
        if (resetSettingsButton != null)
            resetSettingsButton.onClick.AddListener(ResetSettingsToDefault);

        // Sliders & Toggles ในการตั้งค่า
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQualityLevel);
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }
    #endregion

    #region Play / Scene Loading
    public void OnPlayButtonClicked()
    {
        PlayClickSound();
        if (isLoading) return;

        StartCoroutine(LoadGameplaySceneRoutine());
    }

    private IEnumerator LoadGameplaySceneRoutine()
    {
        isLoading = true;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // เริ่มโหลดฉากแบบ Asynchronous
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameplaySceneName);
        if (asyncLoad == null)
        {
            // หากหาตามชื่อไม่พบ ให้ลองโหลด Scene ลำดับที่ 1
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                asyncLoad = SceneManager.LoadSceneAsync(1);
            }
            else
            {
                Debug.LogError($"[MainMenuManager] ไม่พบฉาก '{gameplaySceneName}' ใน Build Settings!");
                isLoading = false;
                if (loadingPanel != null) loadingPanel.SetActive(false);
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                yield break;
            }
        }

        asyncLoad.allowSceneActivation = false;

        float targetProgress = 0f;
        while (!asyncLoad.isDone)
        {
            // asyncLoad.progress มีค่าตั้งแต่ 0 ถึง 0.9 ระหว่างโหลด
            targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = targetProgress;
            }

            if (loadingProgressText != null)
            {
                loadingProgressText.text = $"กำลังโหลด... {(int)(targetProgress * 100f)}%";
            }

            // เมื่อโหลดเสร็จแล้ว
            if (asyncLoad.progress >= 0.9f)
            {
                if (loadingProgressText != null)
                {
                    loadingProgressText.text = "พร้อมแล้ว!";
                }

                yield return new WaitForSeconds(0.4f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
    #endregion

    #region Settings Menu
    public void OpenSettings()
    {
        PlayClickSound();
        isSettingsOpen = true;

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        PlayClickSound();
        isSettingsOpen = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        PlayerPrefs.Save();
    }

    public void ResetSettingsToDefault()
    {
        PlayClickSound();

        SetMasterVolume(1.0f);
        SetBGMVolume(0.8f);
        SetSFXVolume(1.0f);
        SetMouseSensitivity(0.15f);
        SetFullscreen(true);

        int defaultQuality = QualitySettings.names.Length > 2 ? 2 : QualitySettings.names.Length - 1;
        SetQualityLevel(defaultQuality);

        // Sync UI Sliders / Toggles
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1.0f;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = 0.8f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1.0f;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = 0.15f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (qualityDropdown != null) qualityDropdown.value = defaultQuality;

        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, volume);

        if (masterVolumeValueText != null)
            masterVolumeValueText.text = $"{(int)(volume * 100)}%";
    }

    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_BGM_VOLUME, volume);

        if (bgmVolumeValueText != null)
            bgmVolumeValueText.text = $"{(int)(volume * 100)}%";
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, volume);

        if (sfxVolumeValueText != null)
            sfxVolumeValueText.text = $"{(int)(volume * 100)}%";
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        sensitivity = Mathf.Clamp(sensitivity, 0.05f, 1.0f);
        PlayerPrefs.SetFloat(PREF_MOUSE_SENSITIVITY, sensitivity);

        if (mouseSensitivityValueText != null)
            mouseSensitivityValueText.text = sensitivity.ToString("F2");
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
    }

    public void SetQualityLevel(int qualityIndex)
    {
        if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
            PlayerPrefs.SetInt(PREF_QUALITY_LEVEL, qualityIndex);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        if (filteredResolutions != null && resolutionIndex >= 0 && resolutionIndex < filteredResolutions.Count)
        {
            Resolution res = filteredResolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            PlayerPrefs.SetInt(PREF_RESOLUTION_INDEX, resolutionIndex);
        }
    }

    private void InitializeResolutions()
    {
        availableResolutions = Screen.resolutions;
        filteredResolutions.Clear();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution res = availableResolutions[i];
            // กรองเอา RefreshRate ปกติเพื่อไม่ให้ตัวเลือกซ้ำซ้อน
            string resString = $"{res.width} x {res.height}";
            if (!options.Contains(resString))
            {
                options.Add(resString);
                filteredResolutions.Add(res);

                if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
                {
                    currentResIndex = filteredResolutions.Count - 1;
                }
            }
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            int savedIndex = PlayerPrefs.GetInt(PREF_RESOLUTION_INDEX, currentResIndex);
            resolutionDropdown.value = Mathf.Clamp(savedIndex, 0, options.Count - 1);
            resolutionDropdown.RefreshShownValue();
        }

        // ตั้งค่า Quality Dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> qualityNames = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(qualityNames);
            int currentQuality = PlayerPrefs.GetInt(PREF_QUALITY_LEVEL, QualitySettings.GetQualityLevel());
            qualityDropdown.value = Mathf.Clamp(currentQuality, 0, qualityNames.Count - 1);
            qualityDropdown.RefreshShownValue();
        }
    }

    private void LoadAndApplySettings()
    {
        // 1. Audio
        float masterVol = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1.0f);
        float bgmVol = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
        float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1.0f);

        AudioListener.volume = masterVol;
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (masterVolumeValueText != null) masterVolumeValueText.text = $"{(int)(masterVol * 100)}%";

        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVol;
        if (bgmVolumeValueText != null) bgmVolumeValueText.text = $"{(int)(bgmVol * 100)}%";

        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (sfxVolumeValueText != null) sfxVolumeValueText.text = $"{(int)(sfxVol * 100)}%";

        // 2. Mouse Sensitivity
        float sensitivity = PlayerPrefs.GetFloat(PREF_MOUSE_SENSITIVITY, 0.15f);
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = sensitivity;
        if (mouseSensitivityValueText != null) mouseSensitivityValueText.text = sensitivity.ToString("F2");

        // 3. Fullscreen
        bool isFull = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = isFull;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFull;

        // 4. Quality
        int qualityIndex = PlayerPrefs.GetInt(PREF_QUALITY_LEVEL, QualitySettings.GetQualityLevel());
        if (qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
            if (qualityDropdown != null) qualityDropdown.value = qualityIndex;
        }
    }
    #endregion

    #region Quit Game
    public void OpenQuitConfirm()
    {
        PlayClickSound();
        isQuitConfirmOpen = true;

        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(true);
    }

    public void CloseQuitConfirm()
    {
        PlayClickSound();
        isQuitConfirmOpen = false;

        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
    }

    public void ConfirmQuitGame()
    {
        PlayClickSound();
        Debug.Log("[MainMenuManager] ออกจากเกมเรียบร้อยแล้ว");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region Audio Helper
    private void EnsureAudioSource()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
            }
            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f; // 2D UI Sound
        }
    }

    public void PlayClickSound()
    {
        if (uiAudioSource == null) EnsureAudioSource();

        if (buttonClickClip != null)
        {
            float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1.0f);
            uiAudioSource.PlayOneShot(buttonClickClip, sfxVol);
        }
        else
        {
            // หากไม่ได้ใส่ไฟล์เสียง ให้เล่นเสียงคลิกสังเคราะห์นุ่มๆ
            PlayProceduralBeep(880f, 0.05f);
        }
    }

    public void PlayHoverSound()
    {
        if (uiAudioSource == null) EnsureAudioSource();

        if (buttonHoverClip != null)
        {
            float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1.0f);
            uiAudioSource.PlayOneShot(buttonHoverClip, sfxVol * 0.5f);
        }
    }

    private void PlayProceduralBeep(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - ((float)i / sampleCount); // Fade out
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("UIClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1.0f);
        uiAudioSource.PlayOneShot(clip, sfxVol);
    }
    #endregion

    #region Fallback Built-in OnGUI (หากต้องการใช้ UI ชั่วคราว)
    private void OnGUI()
    {
        // หากปิดการใช้งาน OnGUI หรือมี Canvas UI อยู่แล้ว จะไม่วาดทับ
        if (!useBuiltInOnGUI)
        {
            return;
        }

        if (mainMenuPanel != null || playButton != null)
        {
            return;
        }

        // วาด Background เต็มจอ (Dark Atmosphere)
        GUI.color = new Color(0.06f, 0.08f, 0.1f, 0.96f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // วาดแถบข้างเมนูหลัก (Left Menu Column)
        float menuWidth = Mathf.Min(420f, Screen.width * 0.45f);
        float menuHeight = Screen.height;
        GUI.color = new Color(0.09f, 0.12f, 0.16f, 0.98f);
        GUI.DrawTexture(new Rect(0, 0, menuWidth, menuHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // เส้นขอบสีทองเรืองรอง
        GUI.color = new Color(0.85f, 0.65f, 0.2f, 0.4f);
        GUI.DrawTexture(new Rect(menuWidth - 2, 0, 2, menuHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ชื่อเกม (Title)
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        GUI.Label(new Rect(40, 50, menuWidth - 60, 48), "Saharan Hall", titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft
        };
        subTitleStyle.normal.textColor = new Color(0.75f, 0.8f, 0.85f, 0.85f);
        GUI.Label(new Rect(42, 98, menuWidth - 60, 28), "ศาลาหลอน • สวดมนต์ • ไขปริศนา", subTitleStyle);

        // เส้นแบ่ง
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(new Rect(40, 135, menuWidth - 80, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ปุ่มเมนูหลัก 3 ปุ่ม
        float btnStartY = 160f;
        float btnWidth = menuWidth - 80f;
        float btnHeight = 52f;
        float btnSpacing = 16f;

        GUIStyle menuBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        // 1. ปุ่มเริ่มเกม
        GUI.backgroundColor = new Color(0.18f, 0.65f, 0.35f);
        if (GUI.Button(new Rect(40, btnStartY, btnWidth, btnHeight), "▶  เริ่มเกม (Start Game)", menuBtnStyle))
        {
            OnPlayButtonClicked();
        }

        // 2. ปุ่มตั้งค่า
        GUI.backgroundColor = isSettingsOpen ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.2f, 0.3f, 0.45f);
        if (GUI.Button(new Rect(40, btnStartY + (btnHeight + btnSpacing), btnWidth, btnHeight), "⚙  ตั้งค่า (Settings)", menuBtnStyle))
        {
            if (isSettingsOpen) CloseSettings();
            else OpenSettings();
        }

        // 3. ปุ่มออกจากเกม
        GUI.backgroundColor = new Color(0.65f, 0.2f, 0.2f);
        if (GUI.Button(new Rect(40, btnStartY + (btnHeight + btnSpacing) * 2, btnWidth, btnHeight), "✕  ออกจากเกม (Quit)", menuBtnStyle))
        {
            OpenQuitConfirm();
        }
        GUI.backgroundColor = Color.white;

        // คำแนะนำคีย์ลัด
        GUIStyle tipStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };
        tipStyle.normal.textColor = new Color(0.6f, 0.65f, 0.7f, 0.7f);
        GUI.Label(new Rect(40, menuHeight - 50, menuWidth - 80, 30), "Saharan Hall v1.0 • กด Esc เพื่อยกเลิก/ย้อนกลับ", tipStyle);

        // หน้าต่างตั้งค่าแบบ OnGUI Modal
        if (isSettingsOpen)
        {
            DrawOnGUISettingsModal();
        }

        // หน้าต่างยืนยันออกจากเกมแบบ OnGUI Modal
        if (isQuitConfirmOpen)
        {
            DrawOnGUIQuitModal();
        }
    }

    private void DrawOnGUISettingsModal()
    {
        float panelW = 520f;
        float panelH = 460f;
        float panelX = (Screen.width - panelW) / 2f + 100f;
        float panelY = (Screen.height - panelH) / 2f;

        // กล่องหน้าต่าง
        GUI.color = new Color(0.12f, 0.15f, 0.2f, 0.98f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.65f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, 3), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        GUI.Label(new Rect(panelX + 25, panelY + 20, 300, 32), "⚙ ตั้งค่า (Settings)", titleStyle);

        float curY = panelY + 65;
        float labelW = 160;
        float valW = 60;
        float sliderW = panelW - labelW - valW - 70;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        labelStyle.normal.textColor = Color.white;
        GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight };
        valStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

        // Master Volume
        float curMaster = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1.0f);
        GUI.Label(new Rect(panelX + 25, curY, labelW, 24), "🔊 เสียงหลัก (Master):", labelStyle);
        float newMaster = GUI.HorizontalSlider(new Rect(panelX + 25 + labelW, curY + 6, sliderW, 20), curMaster, 0f, 1f);
        GUI.Label(new Rect(panelX + 25 + labelW + sliderW + 10, curY, valW, 24), $"{(int)(newMaster * 100)}%", valStyle);
        if (Mathf.Abs(newMaster - curMaster) > 0.01f) SetMasterVolume(newMaster);
        curY += 40;

        // BGM Volume
        float curBGM = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.8f);
        GUI.Label(new Rect(panelX + 25, curY, labelW, 24), "🎵 เสียงเพลง (BGM):", labelStyle);
        float newBGM = GUI.HorizontalSlider(new Rect(panelX + 25 + labelW, curY + 6, sliderW, 20), curBGM, 0f, 1f);
        GUI.Label(new Rect(panelX + 25 + labelW + sliderW + 10, curY, valW, 24), $"{(int)(newBGM * 100)}%", valStyle);
        if (Mathf.Abs(newBGM - curBGM) > 0.01f) SetBGMVolume(newBGM);
        curY += 40;

        // SFX Volume
        float curSFX = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1.0f);
        GUI.Label(new Rect(panelX + 25, curY, labelW, 24), "🔔 เอฟเฟกต์ (SFX):", labelStyle);
        float newSFX = GUI.HorizontalSlider(new Rect(panelX + 25 + labelW, curY + 6, sliderW, 20), curSFX, 0f, 1f);
        GUI.Label(new Rect(panelX + 25 + labelW + sliderW + 10, curY, valW, 24), $"{(int)(newSFX * 100)}%", valStyle);
        if (Mathf.Abs(newSFX - curSFX) > 0.01f) SetSFXVolume(newSFX);
        curY += 40;

        // Mouse Sensitivity
        float curSens = PlayerPrefs.GetFloat(PREF_MOUSE_SENSITIVITY, 0.15f);
        GUI.Label(new Rect(panelX + 25, curY, labelW, 24), "🖱 ความไวเมาส์:", labelStyle);
        float newSens = GUI.HorizontalSlider(new Rect(panelX + 25 + labelW, curY + 6, sliderW, 20), curSens, 0.05f, 0.5f);
        GUI.Label(new Rect(panelX + 25 + labelW + sliderW + 10, curY, valW, 24), newSens.ToString("F2"), valStyle);
        if (Mathf.Abs(newSens - curSens) > 0.005f) SetMouseSensitivity(newSens);
        curY += 45;

        // Fullscreen Toggle
        bool isFull = Screen.fullScreen;
        GUI.Label(new Rect(panelX + 25, curY, labelW, 24), "🖥 โหมดหน้าจอ:", labelStyle);
        bool newFull = GUI.Toggle(new Rect(panelX + 25 + labelW, curY, 200, 24), isFull, " เต็มจอ (Fullscreen)");
        if (newFull != isFull) SetFullscreen(newFull);
        curY += 45;

        // Quality Level
        GUI.Label(new Rect(panelX + 25, curY, labelW, 24), "✨ คุณภาพกราฟิก:", labelStyle);
        int curQuality = QualitySettings.GetQualityLevel();
        string qualityText = QualitySettings.names.Length > curQuality ? QualitySettings.names[curQuality] : "Default";
        if (GUI.Button(new Rect(panelX + 25 + labelW, curY, 180, 28), $"ระดับ: {qualityText} ⟳"))
        {
            int nextQuality = (curQuality + 1) % QualitySettings.names.Length;
            SetQualityLevel(nextQuality);
        }
        curY += 60;

        // ปุ่มด้านล่าง (ปิด / รีเซ็ต)
        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
        if (GUI.Button(new Rect(panelX + 25, curY, 160, 40), "บันทึกและปิด"))
        {
            CloseSettings();
        }

        GUI.backgroundColor = new Color(0.4f, 0.4f, 0.45f);
        if (GUI.Button(new Rect(panelX + 200, curY, 140, 40), "รีเซ็ตเริ่มต้น"))
        {
            ResetSettingsToDefault();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawOnGUIQuitModal()
    {
        // กล่องยืนยันกึ่งกลางจอ
        float modalW = 420f;
        float modalH = 220f;
        float modalX = (Screen.width - modalW) / 2f;
        float modalY = (Screen.height - modalH) / 2f;

        // พื้นหลังดำโปร่ง
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        // กล่อง
        GUI.color = new Color(0.14f, 0.16f, 0.22f, 0.98f);
        GUI.DrawTexture(new Rect(modalX, modalY, modalW, modalH), Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.3f, 0.3f, 0.9f);
        GUI.DrawTexture(new Rect(modalX, modalY, modalW, 4), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.85f);
        GUI.Label(new Rect(modalX, modalY + 25, modalW, 30), "ยืนยันการออกจากเกม?", titleStyle);

        GUIStyle descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        descStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);
        GUI.Label(new Rect(modalX + 20, modalY + 65, modalW - 40, 30), "คุณต้องการออกจากเกม Saharan Hall หรือไม่?", descStyle);

        // ปุ่ม ใช่ (ออก) / ไม่ (ยกเลิก)
        float btnW = 150f;
        float btnH = 44f;
        float btnY = modalY + 130f;

        GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
        if (GUI.Button(new Rect(modalX + 45, btnY, btnW, btnH), "ใช่, ออกจากเกม"))
        {
            ConfirmQuitGame();
        }

        GUI.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
        if (GUI.Button(new Rect(modalX + modalW - 45 - btnW, btnY, btnW, btnH), "ยกเลิก (กลับ)"))
        {
            CloseQuitConfirm();
        }
        GUI.backgroundColor = Color.white;
    }
    #endregion
}
