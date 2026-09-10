#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuSetupTool : EditorWindow
{
    private static TMP_FontAsset cachedThaiFont;

    [MenuItem("Tools/Saharan Hall/Setup Main Menu in Current Scene")]
    public static void SetupMainMenuInScene()
    {
        // โหลดฟอนต์ภาษาไทย (Thasadith-Bold SDF)
        cachedThaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Thasadith/Thasadith-Bold SDF.asset");

        // 1. ตรวจสอบหรือสร้าง EventSystem
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            eventSystem = esGO.AddComponent<EventSystem>();

            // ใช้ InputSystemUIInputModule หากรองรับ
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                esGO.AddComponent(inputModuleType);
            }
            else
            {
                esGO.AddComponent<StandaloneInputModule>();
            }
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        // 2. สร้าง Canvas หลัก
        GameObject canvasGO = new GameObject("MainMenu_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create MainMenu Canvas");

        // 3. เพิ่ม MainMenuManager Component
        MainMenuManager manager = canvasGO.AddComponent<MainMenuManager>();

        // 4. Background (ความมืดบรรยากาศวัดร้าง)
        GameObject bgGO = CreateUIObject("Background", canvasGO.transform);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.07f, 0.09f, 1f);
        StretchFull(bgGO.GetComponent<RectTransform>());

        // 5. Main Menu Panel
        GameObject mainPanelGO = CreateUIObject("MainMenu_Panel", canvasGO.transform);
        StretchFull(mainPanelGO.GetComponent<RectTransform>());
        manager.mainMenuPanel = mainPanelGO;

        // Side Banner Decoration (Left column)
        GameObject sideColGO = CreateUIObject("SideColumn", mainPanelGO.transform);
        RectTransform sideRT = sideColGO.GetComponent<RectTransform>();
        sideRT.anchorMin = new Vector2(0, 0);
        sideRT.anchorMax = new Vector2(0, 1);
        sideRT.pivot = new Vector2(0, 0.5f);
        sideRT.sizeDelta = new Vector2(500, 0);
        sideRT.anchoredPosition = Vector2.zero;
        Image sideImg = sideColGO.AddComponent<Image>();
        sideImg.color = new Color(0.08f, 0.11f, 0.14f, 0.95f);

        // Gold Accent Line
        GameObject goldLineGO = CreateUIObject("GoldAccentLine", sideColGO.transform);
        RectTransform lineRT = goldLineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(1, 0);
        lineRT.anchorMax = new Vector2(1, 1);
        lineRT.pivot = new Vector2(1, 0.5f);
        lineRT.sizeDelta = new Vector2(3, 0);
        lineRT.anchoredPosition = Vector2.zero;
        Image lineImg = goldLineGO.AddComponent<Image>();
        lineImg.color = new Color(0.85f, 0.65f, 0.2f, 0.5f);

        // Game Title
        GameObject titleGO = CreateUIObject("GameTitle", sideColGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(-80, 80);
        titleRT.anchoredPosition = new Vector2(0, -70);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Saharan Hall";
        titleTMP.fontSize = 44;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.85f, 0.35f);
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Subtitle
        GameObject subTitleGO = CreateUIObject("SubTitle", sideColGO.transform);
        RectTransform subTitleRT = subTitleGO.GetComponent<RectTransform>();
        subTitleRT.anchorMin = new Vector2(0, 1);
        subTitleRT.anchorMax = new Vector2(1, 1);
        subTitleRT.pivot = new Vector2(0.5f, 1);
        subTitleRT.sizeDelta = new Vector2(-80, 36);
        subTitleRT.anchoredPosition = new Vector2(0, -145);
        TextMeshProUGUI subTitleTMP = subTitleGO.AddComponent<TextMeshProUGUI>();
        subTitleTMP.text = "ศาลาหลอน • สวดมนต์ • ไขปริศนา";
        subTitleTMP.fontSize = 18;
        subTitleTMP.color = new Color(0.75f, 0.8f, 0.85f, 0.85f);
        subTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Divider
        GameObject divGO = CreateUIObject("Divider", sideColGO.transform);
        RectTransform divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 1);
        divRT.anchorMax = new Vector2(1, 1);
        divRT.pivot = new Vector2(0.5f, 1);
        divRT.sizeDelta = new Vector2(-80, 2);
        divRT.anchoredPosition = new Vector2(0, -190);
        Image divImg = divGO.AddComponent<Image>();
        divImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Main Menu Buttons Container
        GameObject btnContainerGO = CreateUIObject("ButtonsContainer", sideColGO.transform);
        RectTransform btnContRT = btnContainerGO.GetComponent<RectTransform>();
        btnContRT.anchorMin = new Vector2(0, 1);
        btnContRT.anchorMax = new Vector2(1, 1);
        btnContRT.pivot = new Vector2(0.5f, 1);
        btnContRT.sizeDelta = new Vector2(-80, 240);
        btnContRT.anchoredPosition = new Vector2(0, -220);

        // 1. ปุ่มเริ่มเกม (Play Button)
        manager.playButton = CreateStyledButton(
            "Play_Button", btnContainerGO.transform,
            new Vector2(0, 0), new Vector2(400, 56),
            "▶  เริ่มเกม (Start Game)",
            new Color(0.16f, 0.62f, 0.35f),
            new Color(0.22f, 0.78f, 0.44f)
        );

        // 2. ปุ่มตั้งค่า (Settings Button)
        manager.settingsButton = CreateStyledButton(
            "Settings_Button", btnContainerGO.transform,
            new Vector2(0, -72), new Vector2(400, 56),
            "⚙  ตั้งค่า (Settings)",
            new Color(0.2f, 0.3f, 0.45f),
            new Color(0.28f, 0.42f, 0.62f)
        );

        // 3. ปุ่มออกจากเกม (Quit Button)
        manager.quitButton = CreateStyledButton(
            "Quit_Button", btnContainerGO.transform,
            new Vector2(0, -144), new Vector2(400, 56),
            "✕  ออกจากเกม (Quit)",
            new Color(0.65f, 0.2f, 0.2f),
            new Color(0.82f, 0.25f, 0.25f)
        );

        // Version Note
        GameObject noteGO = CreateUIObject("VersionNote", sideColGO.transform);
        RectTransform noteRT = noteGO.GetComponent<RectTransform>();
        noteRT.anchorMin = new Vector2(0, 0);
        noteRT.anchorMax = new Vector2(1, 0);
        noteRT.pivot = new Vector2(0.5f, 0);
        noteRT.sizeDelta = new Vector2(-80, 30);
        noteRT.anchoredPosition = new Vector2(0, 30);
        TextMeshProUGUI noteTMP = noteGO.AddComponent<TextMeshProUGUI>();
        noteTMP.text = "Saharan Hall v1.0 • พัฒนาด้วย Unity";
        noteTMP.fontSize = 14;
        noteTMP.color = new Color(0.5f, 0.55f, 0.6f, 0.7f);
        noteTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // 6. Settings Panel
        CreateSettingsModal(canvasGO.transform, manager);

        // 7. Quit Confirmation Modal
        CreateQuitConfirmModal(canvasGO.transform, manager);

        // 8. Loading Panel
        CreateLoadingPanel(canvasGO.transform, manager);

        // ปิด Modal เริ่มต้น
        if (manager.settingsPanel != null) manager.settingsPanel.SetActive(false);
        if (manager.quitConfirmPanel != null) manager.quitConfirmPanel.SetActive(false);
        if (manager.loadingPanel != null) manager.loadingPanel.SetActive(false);

        // กำหนด Font ภาษาไทย (Thasadith) ให้กับ TextMeshProUGUI ทุกตัว
        if (cachedThaiFont != null)
        {
            foreach (var tmp in canvasGO.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.font = cachedThaiFont;
            }
        }

        Selection.activeGameObject = canvasGO;
        EditorUtility.SetDirty(canvasGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("[Saharan Hall] สร้าง Main Menu Canvas พร้อมปุ่มเริ่มเกม, ตั้งค่า, ออกจากเกม สำเร็จเรียบร้อยแล้ว!");
    }

    private static void CreateSettingsModal(Transform parent, MainMenuManager manager)
    {
        GameObject modalGO = CreateUIObject("Settings_Panel", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());
        manager.settingsPanel = modalGO;

        // Dim Background
        Image dimImg = modalGO.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f);

        // Dialog Box
        GameObject boxGO = CreateUIObject("DialogBox", modalGO.transform);
        RectTransform boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(620, 560);
        Image boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.11f, 0.14f, 0.18f, 0.98f);

        // Header Gold Line
        GameObject headerLine = CreateUIObject("HeaderLine", boxGO.transform);
        RectTransform hlRT = headerLine.GetComponent<RectTransform>();
        hlRT.anchorMin = new Vector2(0, 1);
        hlRT.anchorMax = new Vector2(1, 1);
        hlRT.pivot = new Vector2(0.5f, 1);
        hlRT.sizeDelta = new Vector2(0, 4);
        Image hlImg = headerLine.AddComponent<Image>();
        hlImg.color = new Color(0.85f, 0.65f, 0.2f, 0.9f);

        // Modal Title
        GameObject modalTitleGO = CreateUIObject("Title", boxGO.transform);
        RectTransform mtRT = modalTitleGO.GetComponent<RectTransform>();
        mtRT.anchorMin = new Vector2(0, 1);
        mtRT.anchorMax = new Vector2(1, 1);
        mtRT.pivot = new Vector2(0.5f, 1);
        mtRT.sizeDelta = new Vector2(-60, 45);
        mtRT.anchoredPosition = new Vector2(0, -25);
        TextMeshProUGUI mtTMP = modalTitleGO.AddComponent<TextMeshProUGUI>();
        mtTMP.text = "⚙  การตั้งค่า (Settings)";
        mtTMP.fontSize = 24;
        mtTMP.fontStyle = FontStyles.Bold;
        mtTMP.color = new Color(1f, 0.85f, 0.35f);

        float currentY = -90;

        // 1. Master Volume
        CreateSliderRow(boxGO.transform, "MasterVolume_Row", "🔊 เสียงหลัก (Master):", currentY, out manager.masterVolumeSlider, out manager.masterVolumeValueText);
        currentY -= 55;

        // 2. BGM Volume
        CreateSliderRow(boxGO.transform, "BGMVolume_Row", "🎵 เสียงเพลง (BGM):", currentY, out manager.bgmVolumeSlider, out manager.bgmVolumeValueText);
        currentY -= 55;

        // 3. SFX Volume
        CreateSliderRow(boxGO.transform, "SFXVolume_Row", "🔔 เอฟเฟกต์ (SFX):", currentY, out manager.sfxVolumeSlider, out manager.sfxVolumeValueText);
        currentY -= 55;

        // 4. Mouse Sensitivity
        CreateSliderRow(boxGO.transform, "Sensitivity_Row", "🖱 ความไวเมาส์:", currentY, out manager.mouseSensitivitySlider, out manager.mouseSensitivityValueText);
        currentY -= 60;

        // 5. Fullscreen Toggle
        GameObject fsRow = CreateUIObject("Fullscreen_Row", boxGO.transform);
        RectTransform fsRT = fsRow.GetComponent<RectTransform>();
        fsRT.anchorMin = new Vector2(0, 1);
        fsRT.anchorMax = new Vector2(1, 1);
        fsRT.pivot = new Vector2(0.5f, 1);
        fsRT.sizeDelta = new Vector2(-60, 35);
        fsRT.anchoredPosition = new Vector2(0, currentY);

        GameObject fsLabelGO = CreateUIObject("Label", fsRow.transform);
        RectTransform fslRT = fsLabelGO.GetComponent<RectTransform>();
        fslRT.anchorMin = new Vector2(0, 0.5f);
        fslRT.anchorMax = new Vector2(0, 0.5f);
        fslRT.pivot = new Vector2(0, 0.5f);
        fslRT.sizeDelta = new Vector2(220, 30);
        TextMeshProUGUI fslTMP = fsLabelGO.AddComponent<TextMeshProUGUI>();
        fslTMP.text = "🖥 โหมดหน้าจอเต็มจอ:";
        fslTMP.fontSize = 17;
        fslTMP.color = Color.white;

        GameObject toggleGO = CreateUIObject("Toggle", fsRow.transform);
        RectTransform togRT = toggleGO.GetComponent<RectTransform>();
        togRT.anchorMin = new Vector2(0, 0.5f);
        togRT.anchorMax = new Vector2(0, 0.5f);
        togRT.pivot = new Vector2(0, 0.5f);
        togRT.sizeDelta = new Vector2(30, 30);
        togRT.anchoredPosition = new Vector2(230, 0);
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        Image togBg = toggleGO.AddComponent<Image>();
        togBg.color = new Color(0.2f, 0.25f, 0.32f);

        GameObject checkGO = CreateUIObject("Checkmark", toggleGO.transform);
        RectTransform checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.2f, 0.2f);
        checkRT.anchorMax = new Vector2(0.8f, 0.8f);
        checkRT.sizeDelta = Vector2.zero;
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.8f, 0.4f);
        toggle.graphic = checkImg;
        manager.fullscreenToggle = toggle;

        currentY -= 75;

        // Bottom Action Buttons
        manager.closeSettingsButton = CreateStyledButton(
            "Close_Button", boxGO.transform,
            new Vector2(-110, -500), new Vector2(200, 48),
            "บันทึกและปิด (Save)",
            new Color(0.2f, 0.5f, 0.8f),
            new Color(0.28f, 0.62f, 0.95f)
        );

        manager.resetSettingsButton = CreateStyledButton(
            "Reset_Button", boxGO.transform,
            new Vector2(110, -500), new Vector2(180, 48),
            "รีเซ็ตเริ่มต้น (Reset)",
            new Color(0.38f, 0.42f, 0.48f),
            new Color(0.48f, 0.52f, 0.58f)
        );
    }

    private static void CreateSliderRow(Transform parent, string name, string labelText, float yPos, out Slider slider, out TextMeshProUGUI valueText)
    {
        GameObject rowGO = CreateUIObject(name, parent);
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0, 1);
        rowRT.anchorMax = new Vector2(1, 1);
        rowRT.pivot = new Vector2(0.5f, 1);
        rowRT.sizeDelta = new Vector2(-60, 40);
        rowRT.anchoredPosition = new Vector2(0, yPos);

        // Label
        GameObject labelGO = CreateUIObject("Label", rowGO.transform);
        RectTransform lRT = labelGO.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0, 0.5f);
        lRT.anchorMax = new Vector2(0, 0.5f);
        lRT.pivot = new Vector2(0, 0.5f);
        lRT.sizeDelta = new Vector2(200, 30);
        TextMeshProUGUI lTMP = labelGO.AddComponent<TextMeshProUGUI>();
        lTMP.text = labelText;
        lTMP.fontSize = 16;
        lTMP.color = Color.white;

        // Slider
        GameObject sliderGO = CreateUIObject("Slider", rowGO.transform);
        RectTransform sRT = sliderGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0.5f);
        sRT.anchorMax = new Vector2(1, 0.5f);
        sRT.pivot = new Vector2(0, 0.5f);
        sRT.sizeDelta = new Vector2(-280, 20);
        sRT.anchoredPosition = new Vector2(210, 0);

        slider = sliderGO.AddComponent<Slider>();

        // Slider Background
        GameObject sBgGO = CreateUIObject("Background", sliderGO.transform);
        StretchFull(sBgGO.GetComponent<RectTransform>());
        Image sBgImg = sBgGO.AddComponent<Image>();
        sBgImg.color = new Color(0.18f, 0.22f, 0.28f);

        // Slider Fill Area
        GameObject fillAreaGO = CreateUIObject("Fill Area", sliderGO.transform);
        RectTransform faRT = fillAreaGO.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f);
        faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.sizeDelta = Vector2.zero;

        GameObject fillGO = CreateUIObject("Fill", fillAreaGO.transform);
        RectTransform fRT = fillGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.65f, 0.2f);
        slider.fillRect = fRT;

        // Slider Handle Area
        GameObject handleAreaGO = CreateUIObject("Handle Slide Area", sliderGO.transform);
        StretchFull(handleAreaGO.GetComponent<RectTransform>());

        GameObject handleGO = CreateUIObject("Handle", handleAreaGO.transform);
        RectTransform hRT = handleGO.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(20, 24);
        Image hImg = handleGO.AddComponent<Image>();
        hImg.color = Color.white;
        slider.handleRect = hRT;
        slider.targetGraphic = hImg;

        // Value text
        GameObject valGO = CreateUIObject("ValueText", rowGO.transform);
        RectTransform vRT = valGO.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(1, 0.5f);
        vRT.anchorMax = new Vector2(1, 0.5f);
        vRT.pivot = new Vector2(1, 0.5f);
        vRT.sizeDelta = new Vector2(60, 30);
        valueText = valGO.AddComponent<TextMeshProUGUI>();
        valueText.text = "100%";
        valueText.fontSize = 15;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.color = new Color(0.8f, 0.9f, 1f);
    }

    private static void CreateQuitConfirmModal(Transform parent, MainMenuManager manager)
    {
        GameObject modalGO = CreateUIObject("QuitConfirm_Panel", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());
        manager.quitConfirmPanel = modalGO;

        Image dimImg = modalGO.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.75f);

        GameObject boxGO = CreateUIObject("DialogBox", modalGO.transform);
        RectTransform boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(480, 240);
        Image boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.15f, 0.2f, 0.98f);

        // Header Accent Red
        GameObject lineGO = CreateUIObject("AccentLine", boxGO.transform);
        RectTransform lRT = lineGO.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0, 1);
        lRT.anchorMax = new Vector2(1, 1);
        lRT.pivot = new Vector2(0.5f, 1);
        lRT.sizeDelta = new Vector2(0, 4);
        Image lImg = lineGO.AddComponent<Image>();
        lImg.color = new Color(0.9f, 0.25f, 0.25f);

        // Title
        GameObject titleGO = CreateUIObject("Title", boxGO.transform);
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 1);
        tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.sizeDelta = new Vector2(-40, 40);
        tRT.anchoredPosition = new Vector2(0, -25);
        TextMeshProUGUI tTMP = titleGO.AddComponent<TextMeshProUGUI>();
        tTMP.text = "ยืนยันการออกจากเกม?";
        tTMP.fontSize = 22;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.color = new Color(1f, 0.85f, 0.85f);
        tTMP.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject descGO = CreateUIObject("Desc", boxGO.transform);
        RectTransform dRT = descGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0, 1);
        dRT.anchorMax = new Vector2(1, 1);
        dRT.pivot = new Vector2(0.5f, 1);
        dRT.sizeDelta = new Vector2(-40, 30);
        dRT.anchoredPosition = new Vector2(0, -75);
        TextMeshProUGUI dTMP = descGO.AddComponent<TextMeshProUGUI>();
        dTMP.text = "คุณต้องการออกจากเกม Saharan Hall หรือไม่?";
        dTMP.fontSize = 16;
        dTMP.color = new Color(0.8f, 0.85f, 0.9f);
        dTMP.alignment = TextAlignmentOptions.Center;

        // Buttons
        manager.confirmQuitButton = CreateStyledButton(
            "ConfirmQuit_Btn", boxGO.transform,
            new Vector2(-95, -165), new Vector2(160, 46),
            "ใช่, ออกจากเกม",
            new Color(0.85f, 0.25f, 0.25f),
            new Color(1f, 0.35f, 0.35f)
        );

        manager.cancelQuitButton = CreateStyledButton(
            "CancelQuit_Btn", boxGO.transform,
            new Vector2(95, -165), new Vector2(160, 46),
            "ยกเลิก (กลับ)",
            new Color(0.32f, 0.38f, 0.46f),
            new Color(0.42f, 0.48f, 0.58f)
        );
    }

    private static void CreateLoadingPanel(Transform parent, MainMenuManager manager)
    {
        GameObject loadGO = CreateUIObject("Loading_Panel", parent);
        StretchFull(loadGO.GetComponent<RectTransform>());
        manager.loadingPanel = loadGO;

        Image loadBg = loadGO.AddComponent<Image>();
        loadBg.color = new Color(0.04f, 0.05f, 0.07f, 1f);

        GameObject textGO = CreateUIObject("LoadingText", loadGO.transform);
        RectTransform tRT = textGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 0.5f);
        tRT.anchorMax = new Vector2(0.5f, 0.5f);
        tRT.sizeDelta = new Vector2(400, 40);
        tRT.anchoredPosition = new Vector2(0, 30);
        manager.loadingProgressText = textGO.AddComponent<TextMeshProUGUI>();
        manager.loadingProgressText.text = "กำลังเข้าสู่ศาลาหลอน...";
        manager.loadingProgressText.fontSize = 20;
        manager.loadingProgressText.color = new Color(1f, 0.85f, 0.35f);
        manager.loadingProgressText.alignment = TextAlignmentOptions.Center;

        // Slider Progress Bar
        GameObject sGO = CreateUIObject("LoadingProgressBar", loadGO.transform);
        RectTransform sRT = sGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.5f, 0.5f);
        sRT.anchorMax = new Vector2(0.5f, 0.5f);
        sRT.sizeDelta = new Vector2(460, 16);
        sRT.anchoredPosition = new Vector2(0, -20);
        manager.loadingProgressBar = sGO.AddComponent<Slider>();

        GameObject sBg = CreateUIObject("Background", sGO.transform);
        StretchFull(sBg.GetComponent<RectTransform>());
        Image sBgImg = sBg.AddComponent<Image>();
        sBgImg.color = new Color(0.15f, 0.18f, 0.22f);

        GameObject fArea = CreateUIObject("Fill Area", sGO.transform);
        StretchFull(fArea.GetComponent<RectTransform>());
        GameObject fGO = CreateUIObject("Fill", fArea.transform);
        StretchFull(fGO.GetComponent<RectTransform>());
        Image fImg = fGO.AddComponent<Image>();
        fImg.color = new Color(0.85f, 0.65f, 0.2f);
        manager.loadingProgressBar.fillRect = fGO.GetComponent<RectTransform>();
    }

    private static Button CreateStyledButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string text, Color normalCol, Color highlightCol)
    {
        GameObject btnGO = CreateUIObject(name, parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Image img = btnGO.AddComponent<Image>();
        img.color = normalCol;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalCol;
        colors.highlightedColor = highlightCol;
        colors.pressedColor = normalCol * 0.8f;
        colors.selectedColor = highlightCol;
        btn.colors = colors;

        GameObject textGO = CreateUIObject("Text", btnGO.transform);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
