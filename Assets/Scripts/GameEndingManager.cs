using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public enum GameEndingType
{
    GhostGameOver, // โดนผีเกาะครบ 3 ตัว (แพ้)
    GoodEnding,    // คาม่า 80-100 (จบดี)
    BadEnding      // คาม่า 0-20 (จบแย่)
}

/// <summary>
/// ผู้จัดการฉากจบและ Game Over ศูนย์กลาง
/// </summary>
public class GameEndingManager : MonoBehaviour
{
    public static GameEndingManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("UI Canvas สำหรับฉากจบ (จะสร้างให้ชั่วคราวถ้าไม่ได้ใส่มา)")]
    public GameObject endingCanvas;
    
    private GameObject endingPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private Button restartButton;
    private Button mainMenuButton;
    private Image bgImage;

    [Header("State")]
    public bool isGameEnding = false;

    private PlayerController playerController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        EnsureEndingUIExists();
    }

    /// <summary>
    /// เรียกฉากจบตามประเภทที่ส่งเข้ามา
    /// </summary>
    public void TriggerEnding(GameEndingType endingType)
    {
        if (isGameEnding) return;
        isGameEnding = true;

        Debug.Log($"[GameEndingManager] 🎬 เรียกฉากจบ: {endingType}");

        // หยุดเวลาและบล็อกการควบคุมผู้เล่น
        Time.timeScale = 0f;
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ปิด UI อื่นๆ หรือซ่อนเกม (ถ้ามี)
        if (endingCanvas != null)
        {
            endingCanvas.SetActive(true);
        }

        // ตั้งค่า UI ตามฉากจบ
        SetupEndingUI(endingType);
    }

    private void SetupEndingUI(GameEndingType endingType)
    {
        if (titleText == null || descriptionText == null) return;

        switch (endingType)
        {
            case GameEndingType.GhostGameOver:
                bgImage.color = new Color(0.1f, 0f, 0f, 0.95f); // พื้นหลังแดงเข้ม
                titleText.text = "<color=#FF2222>💀 วิญญาณแตกดับ 💀</color>";
                descriptionText.text = "คุณทำพลาดจนถูกภูตผีปีศาจเข้าครอบงำครบ 3 ตน\nวิญญาณของคุณถูกกลืนกินและต้องวนเวียนอยู่ที่นี่ตลอดไป...";
                break;

            case GameEndingType.GoodEnding:
                bgImage.color = new Color(1f, 0.95f, 0.8f, 0.95f); // พื้นหลังขาวทอง
                titleText.text = "<color=#DAA520>✨ หลุดพ้นสังสารวัฏ ✨</color>";
                descriptionText.text = "คุณได้สะสมบุญบารมีอย่างเต็มเปี่ยม\nแสงสว่างนำทางวิญญาณของคุณไปสู่สุคติ\nขอขอบคุณที่ร่วมเดินทาง...";
                break;

            case GameEndingType.BadEnding:
                bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); // พื้นหลังดำมืด
                titleText.text = "<color=#888888>🌑 จมดิ่งสู่ความมืดมิด 🌑</color>";
                descriptionText.text = "ผลแห่งกรรมชั่วและความเห็นแก่ตัว\nดึงรั้งคุณไว้ในห้วงแห่งความทุกข์ระทม\nไม่อาจหลุดพ้นจากวังวนแห่งนี้ได้...";
                break;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        // เปลี่ยนเป็นชื่อซีน MainMenu ของคุณ
        SceneManager.LoadScene("MainMenu");
    }

    // ==========================================
    // UI Creation (สร้าง Canvas สวยๆ ถ้าย้อนกลับไปตั้งค่าใน Inspector ไม่ทัน)
    // ==========================================
    private void EnsureEndingUIExists()
    {
        if (endingCanvas != null) return;

        // 1. สร้าง Canvas
        endingCanvas = new GameObject("EndingCanvas");
        Canvas canvas = endingCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // ให้อยู่บนสุดเสมอ

        CanvasScaler scaler = endingCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        endingCanvas.AddComponent<GraphicRaycaster>();

        // 2. สร้าง Background Panel
        endingPanel = new GameObject("EndingPanel");
        endingPanel.transform.SetParent(endingCanvas.transform, false);
        RectTransform panelRect = endingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero; // Fix offset

        bgImage = endingPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.95f);

        // 3. สร้าง Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(endingPanel.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(1200, 200);
        titleRect.anchoredPosition = Vector2.zero;
        
        titleText.fontSize = 80;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // 4. สร้าง Description Text
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(endingPanel.transform, false);
        descriptionText = descObj.AddComponent<TextMeshProUGUI>();
        RectTransform descRect = descriptionText.rectTransform;
        descRect.anchorMin = new Vector2(0.5f, 0.5f);
        descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.sizeDelta = new Vector2(1400, 300);
        descRect.anchoredPosition = Vector2.zero;

        descriptionText.fontSize = 36;
        descriptionText.alignment = TextAlignmentOptions.Top;

        // 5. สร้างปุ่ม Restart
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(endingPanel.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.3f);
        btnRect.anchorMax = new Vector2(0.5f, 0.3f);
        btnRect.sizeDelta = new Vector2(300, 80);
        btnRect.anchoredPosition = new Vector2(-170, 0); // เลื่อนไปทางซ้าย

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        restartButton = btnObj.AddComponent<Button>();
        restartButton.onClick.AddListener(RestartGame);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "เริ่มเล่นใหม่";
        btnText.fontSize = 32;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.rectTransform.anchorMin = Vector2.zero;
        btnText.rectTransform.anchorMax = Vector2.one;
        btnText.rectTransform.sizeDelta = Vector2.zero;
        btnText.rectTransform.anchoredPosition = Vector2.zero;

        // 6. สร้างปุ่ม กลับหน้าหลัก (Main Menu)
        GameObject menuBtnObj = new GameObject("MainMenuButton");
        menuBtnObj.transform.SetParent(endingPanel.transform, false);
        RectTransform menuBtnRect = menuBtnObj.AddComponent<RectTransform>();
        menuBtnRect.anchorMin = new Vector2(0.5f, 0.3f);
        menuBtnRect.anchorMax = new Vector2(0.5f, 0.3f);
        menuBtnRect.sizeDelta = new Vector2(300, 80);
        menuBtnRect.anchoredPosition = new Vector2(170, 0); // เลื่อนไปทางขวา

        Image menuBtnImg = menuBtnObj.AddComponent<Image>();
        menuBtnImg.color = new Color(0.2f, 0.3f, 0.5f, 1f); // สีออกน้ำเงินเพื่อให้ต่างจาก Restart
        mainMenuButton = menuBtnObj.AddComponent<Button>();
        
        // เพิ่ม method GoToMainMenu ที่เตรียมไว้ในสคริปต์
        mainMenuButton.onClick.AddListener(GoToMainMenu);

        GameObject menuBtnTextObj = new GameObject("Text");
        menuBtnTextObj.transform.SetParent(menuBtnObj.transform, false);
        TextMeshProUGUI menuBtnText = menuBtnTextObj.AddComponent<TextMeshProUGUI>();
        menuBtnText.text = "กลับหน้าหลัก";
        menuBtnText.fontSize = 32;
        menuBtnText.alignment = TextAlignmentOptions.Center;
        menuBtnText.color = Color.white;
        menuBtnText.rectTransform.anchorMin = Vector2.zero;
        menuBtnText.rectTransform.anchorMax = Vector2.one;
        menuBtnText.rectTransform.sizeDelta = Vector2.zero;
        menuBtnText.rectTransform.anchoredPosition = Vector2.zero;

        endingCanvas.SetActive(false); // ปิดไว้ก่อน
    }
}
