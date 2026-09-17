using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ตัวควบคุมระบบกุมารทอง (Kuman Thong Shrine Controller)
/// - กดถวายเครื่องเซ่น เพื่อลดค่า Fail สะสมลง 1 หน่วย
/// - เพิ่มค่าพึ่งพากุมารทอง (dependencyLevel) ขึ้น +10 หน่วยเป็นข้อมูลเบื้องหลัง (ไม่แสดงผลบน UI)
/// </summary>
public class KumanThongUIController : MonoBehaviour
{
    public static KumanThongUIController Instance { get; private set; }

    [Header("Runtime State")]
    public bool isUIOpen = false;

    [Header("Hidden Background State (ข้อมูลเบื้องหลัง ซ่อนไม่แสดงผลบน UI)")]
    [Tooltip("ค่าการพึ่งพากุมารทอง สะสมขึ้นเรื่อยๆ เมื่อกดถวายของเซ่น")]
    public int dependencyLevel = 0;

    private PlayerInteraction activePlayer;
    private PlayerController playerController;
    private AudioSource audioSource;

    // uGUI Canvas References
    private GameObject kumanCanvas;
    private GameObject kumanPanel;
    private TextMeshProUGUI failStatusText;
    private Button offerButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (!isUIOpen) return;

        // กด Esc หรือ E หรือ Tab เพื่อปิดหน้าต่าง
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab))
        {
            CloseUI();
        }
    }

    /// <summary>
    /// เปิดหน้าต่างศาลกุมารทอง
    /// </summary>
    public void OpenUI(PlayerInteraction player)
    {
        if (isUIOpen) return;

        activePlayer = player != null ? player : FindFirstObjectByType<PlayerInteraction>();
        isUIOpen = true;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HideReadingDialog();
        }

        EnsureUIExists();
        LockPlayerControls(true);

        if (kumanPanel != null) kumanPanel.SetActive(true);
        if (kumanCanvas != null) kumanCanvas.SetActive(true);

        RefreshUI();
        Debug.Log("[KumanThongUIController] 👶 เปิดหน้าต่างศาลกุมารทองแล้ว");
    }

    /// <summary>
    /// ปิดหน้าต่างศาลกุมารทอง
    /// </summary>
    public void CloseUI()
    {
        if (!isUIOpen) return;

        isUIOpen = false;

        if (kumanPanel != null) kumanPanel.SetActive(false);
        if (kumanCanvas != null) kumanCanvas.SetActive(false);

        LockPlayerControls(false);
        Debug.Log("[KumanThongUIController] 🚪 ปิดหน้าต่างศาลกุมารทองเรียบร้อย");
    }

    /// <summary>
    /// กดถวายเครื่องเซ่นกุมารทอง
    /// - ลดค่า Fail สะสมลง 1 หน่วย (หากมากกว่า 0)
    /// - เพิ่มค่าพึ่งพา (dependencyLevel) ขึ้น +10 หน่วย (ซ่อนหลังบ้าน)
    /// </summary>
    public void MakeOffering()
    {
        if (!isUIOpen) return;

        // 1. เพิ่มค่าพึ่งพา (dependency) ขึ้น +10 หน่วย (ข้อมูลเบื้องหลัง)
        dependencyLevel += 10;

        // 2. ลดค่า Fail ลง 1 หน่วย
        bool hadFail = false;
        if (MinigameManager.Instance != null)
        {
            if (MinigameManager.Instance.totalFailedMinigames > 0)
            {
                MinigameManager.Instance.totalFailedMinigames--;
                MinigameManager.Instance.UpdateFailCounterUI();
                hadFail = true;
            }
        }

        // 3. เล่นเสียงสังเคราะห์และแสดงผลแจ้งเตือน
        PlaySynthChime();

        if (InteractionUIManager.Instance != null)
        {
            if (hadFail)
            {
                int currentFail = MinigameManager.Instance != null ? MinigameManager.Instance.totalFailedMinigames : 0;
                InteractionUIManager.Instance.ShowNotification($"<color=#00FF7F>✨ ถวายเครื่องเซ่นกุมารทองแล้ว!</color> (ลด Fail ลง 1 ➔ เหลือ {currentFail})", 3.5f);
            }
            else
            {
                InteractionUIManager.Instance.ShowNotification($"<color=#FFD700>✨ ถวายเครื่องเซ่นกุมารทองแล้ว!</color> (กุมารทองคอยปกป้องคุณอยู่)", 3.5f);
            }
        }

        Debug.Log($"[KumanThong] 👶 ถวายเครื่องเซ่นเรียบร้อย | ค่าพึ่งพาเบื้องหลัง (Dependency) = {dependencyLevel}");

        RefreshUI();
    }

    private void RefreshUI()
    {
        int currentFail = MinigameManager.Instance != null ? MinigameManager.Instance.totalFailedMinigames : 0;

        if (failStatusText != null)
        {
            failStatusText.text = $"⚠️ ค่า Fail สะสมปัจจุบัน: <color=#FF4500><b>{currentFail} ครั้ง</b></color>";
        }
    }

    private void LockPlayerControls(bool lockState)
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

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

    private void PlaySynthChime()
    {
        if (audioSource == null) return;
        int sampleRate = 44100;
        float duration = 0.4f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = (Mathf.Sin(2 * Mathf.PI * 1046.5f * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 1318.5f * t) * 0.4f) * envelope * 0.3f;
        }
        AudioClip clip = AudioClip.Create("KumanChimeSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }

    private Sprite CreateRoundedRectSprite(int width = 64, int height = 64)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    // ==========================================
    // Dynamic Canvas UI Creation (uGUI Panel)
    // ==========================================
    private void EnsureUIExists()
    {
        if (kumanPanel != null && kumanCanvas != null) return;

        kumanCanvas = GameObject.Find("KumanThong_Canvas");
        if (kumanCanvas == null)
        {
            kumanCanvas = new GameObject("KumanThong_Canvas");
            Canvas canvas = kumanCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 997;

            CanvasScaler scaler = kumanCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            kumanCanvas.AddComponent<GraphicRaycaster>();
        }

        kumanPanel = kumanCanvas.transform.Find("KumanPanel")?.gameObject;
        if (kumanPanel == null)
        {
            Sprite boxSprite = CreateRoundedRectSprite(64, 64);

            kumanPanel = new GameObject("KumanPanel");
            kumanPanel.transform.SetParent(kumanCanvas.transform, false);

            RectTransform panelRect = kumanPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620, 380);

            // BG Panel
            Image bg = kumanPanel.AddComponent<Image>();
            bg.sprite = boxSprite;
            bg.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            // Close Button (Top Right X)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(kumanPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-22, -22);
            closeRect.sizeDelta = new Vector2(34, 34);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = boxSprite;
            closeImg.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseUI);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI closeTMP = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 18;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.rectTransform.anchoredPosition = Vector2.zero;
            closeTMP.rectTransform.sizeDelta = new Vector2(34, 34);

            // Header Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(kumanPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "<color=#FFD700>👶 ศาลกุมารทอง (KUMAN THONG)</color>";
            titleTMP.fontSize = 26;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.rectTransform.anchoredPosition = new Vector2(0, 140);
            titleTMP.rectTransform.sizeDelta = new Vector2(580, 45);

            // Fail Status Display
            GameObject failObj = new GameObject("FailStatusText");
            failObj.transform.SetParent(kumanPanel.transform, false);
            failStatusText = failObj.AddComponent<TextMeshProUGUI>();
            failStatusText.fontSize = 20;
            failStatusText.alignment = TextAlignmentOptions.Center;
            failStatusText.rectTransform.anchoredPosition = new Vector2(0, 85);
            failStatusText.rectTransform.sizeDelta = new Vector2(580, 35);

            // Info Guidance Text
            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(kumanPanel.transform, false);
            TextMeshProUGUI infoTMP = infoObj.AddComponent<TextMeshProUGUI>();
            infoTMP.text = "กุมารทองจะช่วยรับเคราะห์และชำระล้างค่า Fail ให้ 1 หน่วยทุกครั้งที่กราบไหว้ถวายเครื่องเซ่น";
            infoTMP.fontSize = 16;
            infoTMP.alignment = TextAlignmentOptions.Center;
            infoTMP.color = new Color(0.85f, 0.88f, 0.95f);
            infoTMP.rectTransform.anchoredPosition = new Vector2(0, 30);
            infoTMP.rectTransform.sizeDelta = new Vector2(540, 50);

            // Offer Button
            GameObject offerBtnObj = new GameObject("OfferButton");
            offerBtnObj.transform.SetParent(kumanPanel.transform, false);
            RectTransform offerRect = offerBtnObj.AddComponent<RectTransform>();
            offerRect.anchoredPosition = new Vector2(0, -45);
            offerRect.sizeDelta = new Vector2(320, 50);

            Image offerImg = offerBtnObj.AddComponent<Image>();
            offerImg.sprite = boxSprite;
            offerImg.color = new Color(0.9f, 0.65f, 0.15f, 1f);

            offerButton = offerBtnObj.AddComponent<Button>();
            offerButton.onClick.AddListener(MakeOffering);

            GameObject offerTxtObj = new GameObject("Text");
            offerTxtObj.transform.SetParent(offerBtnObj.transform, false);
            TextMeshProUGUI offerTMP = offerTxtObj.AddComponent<TextMeshProUGUI>();
            offerTMP.text = "🙏 ถวายเครื่องเซ่น  (ลด Fail -1)";
            offerTMP.fontSize = 20;
            offerTMP.fontStyle = FontStyles.Bold;
            offerTMP.color = new Color(0.1f, 0.08f, 0.05f);
            offerTMP.alignment = TextAlignmentOptions.Center;
            offerTMP.rectTransform.anchoredPosition = Vector2.zero;
            offerTMP.rectTransform.sizeDelta = new Vector2(320, 50);

            // Footer Guide
            GameObject guideObj = new GameObject("GuideText");
            guideObj.transform.SetParent(kumanPanel.transform, false);
            TextMeshProUGUI guideTMP = guideObj.AddComponent<TextMeshProUGUI>();
            guideTMP.text = "กดปุ่มเพื่อถวายเครื่องเซ่น  |  กด <color=#FF6347>[E / Esc]</color> เพื่อปิด";
            guideTMP.fontSize = 15;
            guideTMP.alignment = TextAlignmentOptions.Center;
            guideTMP.rectTransform.anchoredPosition = new Vector2(0, -140);
            guideTMP.rectTransform.sizeDelta = new Vector2(580, 30);
        }
    }

    // ==========================================
    // OnGUI Fallback Rendering (การันตีการวาด 100%)
    // ==========================================
    void OnGUI()
    {
        if (!isUIOpen) return;

        // หากมี uGUI Panel ให้ข้ามการวาด OnGUI เพื่อไม่ให้ UI ซ้อนทับกัน
        if (kumanPanel != null && kumanPanel.activeSelf) return;

        float w = 560f;
        float h = 340f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Box(new Rect(x + 5, y + 5, w - 10, h - 10), "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(x, y + 15, w, 32), "👶 ศาลกุมารทอง (KUMAN THONG)", titleStyle);

        int curFail = MinigameManager.Instance != null ? MinigameManager.Instance.totalFailedMinigames : 0;
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        statusStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y + 55, w, 28), $"⚠️ ค่า Fail สะสมปัจจุบัน: {curFail} ครั้ง", statusStyle);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        infoStyle.normal.textColor = new Color(0.85f, 0.88f, 0.95f);
        GUI.Label(new Rect(x + 20, y + 90, w - 40, 45), "กุมารทองจะช่วยรับเคราะห์และชำระล้างค่า Fail ให้ 1 หน่วย\nทุกครั้งที่กราบไหว้ถวายเครื่องเซ่น", infoStyle);

        if (GUI.Button(new Rect(x + (w - 280f) / 2f, y + 150, 280f, 48f), "🙏 ถวายเครื่องเซ่น (ลด Fail -1)"))
        {
            MakeOffering();
        }

        GUIStyle footerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        footerStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x, y + h - 35, w, 25), "กดปุ่มเพื่อถวายเครื่องเซ่น  |  กด [E / Esc] เพื่อปิด", footerStyle);
    }
}
