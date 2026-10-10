using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public enum GameEndingType
{
    GhostGameOver, // โดนผีเกาะครบ 3 ตัว (โดนกลืนกิน)
    GoodEnding,    // ค่าความดีถึง 80 (จบดี)
    BadEnding      // ค่าความดีถึง 0 (โดนกลืนกิน)
}

/// <summary>
/// ผู้จัดการฉากจบและ Game Over ศูนย์กลาง
/// รองรับทั้ง Canvas UI ในฉาก และ OnGUI Fallback เพื่อให้แสดงผลได้แน่นอน 100%
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

    [Header("UI References (Inspector)")]
    [Tooltip("UI Canvas หลักสำหรับฉากจบ")]
    public GameObject endingCanvas;

    [Tooltip("UI Panel สำหรับฉากจบดี (ถ้าต้องการแยก GameObject ใน Inspector)")]
    public GameObject goodEndingPanel;

    [Tooltip("UI Panel สำหรับฉากโดนกลืนกิน (ถ้าต้องการแยก GameObject ใน Inspector)")]
    public GameObject badEndingPanel;

    [Header("UI Components (ค้นหาอัตโนมัติจาก Canvas ถ้าเว้นว่าง)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button restartButton;
    public Button mainMenuButton;
    public Image bgImage;

    [Header("State")]
    public bool isGameEnding = false;
    public GameEndingType currentEndingType = GameEndingType.GhostGameOver;

    private PlayerController playerController;

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
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        BindUIComponents();

        // ซ่อน Canvas และ Panel ฉากจบไว้ก่อนเริ่มเกม
        if (endingCanvas != null) endingCanvas.SetActive(false);
        if (goodEndingPanel != null) goodEndingPanel.SetActive(false);
        if (badEndingPanel != null) badEndingPanel.SetActive(false);
    }

    /// <summary>
    /// เชื่อมต่อคอมโพเนนต์บน UI Canvas โดยอัตโนมัติหากยังไม่ได้ลากใส่ใน Inspector
    /// </summary>
    private void BindUIComponents()
    {
        if (endingCanvas == null) return;

        // 1. ค้นหา Title Text (มองหา TextMeshProUGUI ที่ไม่ใช่ข้อความบนปุ่ม)
        if (titleText == null)
        {
            var tmps = endingCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.transform.parent != null && t.transform.parent.GetComponent<Button>() != null)
                    continue;

                titleText = t;
                break;
            }
        }

        // 2. ค้นหา Buttons (Restart และ Main Menu)
        if (restartButton == null || mainMenuButton == null)
        {
            var buttons = endingCanvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string bName = b.gameObject.name.ToLower();
                if (restartButton == null && (bName.Contains("restart") || bName.Contains("retry")))
                {
                    restartButton = b;
                }
                else if (mainMenuButton == null && (bName.Contains("menu") || bName.Contains("return") || bName.Contains("main")))
                {
                    mainMenuButton = b;
                }
            }

            // ถ้าหาตามชื่อไม่เจอ ให้ใช้ลำดับ 0 และ 1
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

        // 3. ค้นหาภาพพื้นหลัง Panel
        if (bgImage == null)
        {
            var images = endingCanvas.GetComponentsInChildren<Image>(true);
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
    /// สั่งจบเกมและเรียก UI ตามประเภทฉากจบ
    /// </summary>
    public void TriggerEnding(GameEndingType endingType)
    {
        if (isGameEnding) return;
        isGameEnding = true;
        currentEndingType = endingType;

        Debug.Log($"[GameEndingManager] 🎬 เรียกฉากจบ: {endingType}");

        // หยุดเวลาและหยุดการควบคุมผู้เล่น
        Time.timeScale = 0f;
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // เชื่อมต่อ UI หากยังไม่ได้เชื่อม
        BindUIComponents();

        // เปิด Canvas ฉากจบ
        if (endingCanvas != null)
        {
            endingCanvas.SetActive(true);
        }

        // ปรับแต่งข้อความและแผงควบคุมตามฉากจบ
        SetupEndingUI(endingType);
    }

    private void SetupEndingUI(GameEndingType endingType)
    {
        // 1. จัดการการเปิด/ปิด Panel แยก (หากผู้พัฒนาสร้าง Panel แยกไว้ใน Inspector)
        if (goodEndingPanel != null || badEndingPanel != null)
        {
            if (endingType == GameEndingType.GoodEnding)
            {
                if (goodEndingPanel != null) goodEndingPanel.SetActive(true);
                if (badEndingPanel != null) badEndingPanel.SetActive(false);
            }
            else
            {
                if (badEndingPanel != null) badEndingPanel.SetActive(true);
                if (goodEndingPanel != null) goodEndingPanel.SetActive(false);
            }
        }

        // 2. ปรับข้อความและสีบน UI หลัก (ใช้ได้ทั้งแบบมี Panel แยกหรือใช้ Panel ร่วม)
        switch (endingType)
        {
            case GameEndingType.GoodEnding:
                if (bgImage != null)
                {
                    bgImage.color = new Color(0.08f, 0.18f, 0.12f, 0.95f); // พื้นหลังเขียวมงคลอมทอง
                }
                if (titleText != null)
                {
                    titleText.text = "<color=#00FF7F>✨ จบแบบดี ✨</color>\n<size=60%><color=#FFD700>(ค่าความดีถึง 80)</color></size>";
                }
                if (descriptionText != null)
                {
                    descriptionText.text = "คุณได้สะสมคุณงามความดีและบุญบารมีอย่างเต็มเปี่ยม\nแสงสว่างนำทางวิญญาณของคุณหลุดพ้นจากอาถรรพณ์ตำหนักแห่งนี้...";
                }
                break;

            case GameEndingType.BadEnding:
                if (bgImage != null)
                {
                    bgImage.color = new Color(0.18f, 0.02f, 0.02f, 0.96f); // พื้นหลังแดงเข้มดำ
                }
                if (titleText != null)
                {
                    titleText.text = "<color=#FF2222>💀 โดนกลืนกิน 💀</color>\n<size=60%><color=#FFAAAA>(ค่าความดีลดลงถึง 0)</color></size>";
                }
                if (descriptionText != null)
                {
                    descriptionText.text = "จิตใจของคุณตกต่ำจนไร้ซึ่งคุณงามความดี\nความมืดมิดครอบงำและกลืนกินวิญญาณของคุณไปตลอดกาล...";
                }
                break;

            case GameEndingType.GhostGameOver:
                if (bgImage != null)
                {
                    bgImage.color = new Color(0.18f, 0.02f, 0.02f, 0.96f); // พื้นหลังแดงเลือดหมู
                }
                if (titleText != null)
                {
                    titleText.text = "<color=#FF2222>💀 โดนกลืนกิน 💀</color>\n<size=60%><color=#FFAAAA>(ผีร้ายเข้าครอบงำครบ 3 ตน)</color></size>";
                }
                if (descriptionText != null)
                {
                    descriptionText.text = "คุณทำพลาดจนถูกภูตผีปีศาจเข้าครอบงำครบ 3 ตน\nวิญญาณของคุณถูกกลืนกินและต้องวนเวียนอยู่ที่นี่ตลอดไป...";
                }
                break;
        }
    }

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
    // OnGUI Fallback: กรณีในฉากไม่มี Canvas หรือ Canvas ไม่ทำงาน
    // ==========================================
    void OnGUI()
    {
        if (!isGameEnding) return;

        // หากมี endingCanvas แสดงผลอยู่ใน Hierarchy แล้ว ไม่ต้องวาด OnGUI ซ้ำ
        if (endingCanvas != null && endingCanvas.activeInHierarchy) return;

        DrawEndingModalGUI();
    }

    private void DrawEndingModalGUI()
    {
        // 1. ม่านดำโปร่งใสทั้งหน้าจอ
        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float boxW = Mathf.Min(600f, Screen.width * 0.9f);
        float boxH = 360f;
        float boxX = (Screen.width - boxW) / 2f;
        float boxY = (Screen.height - boxH) / 2f;

        bool isGood = currentEndingType == GameEndingType.GoodEnding;

        // 2. กรอบข้อความตรงกลาง
        GUI.color = isGood 
            ? new Color(0.06f, 0.16f, 0.10f, 0.96f) 
            : new Color(0.18f, 0.02f, 0.02f, 0.96f);
        GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none);
        GUI.color = Color.white;

        // 3. หัวข้อ
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = isGood ? new Color(0.1f, 1f, 0.5f) : new Color(1f, 0.2f, 0.2f);

        string titleStr = isGood ? "✨ จบแบบดี (ค่าความดีถึง 80) ✨" : "💀 โดนกลืนกิน (ค่าความดีถึง 0) 💀";
        GUI.Label(new Rect(boxX, boxY + 25, boxW, 50), titleStr, titleStyle);

        // 4. คำอธิบาย
        GUIStyle descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        descStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        string descStr = isGood
            ? "คุณได้สะสมคุณงามความดีและบุญบารมีอย่างเต็มเปี่ยม\nแสงสว่างนำทางวิญญาณของคุณหลุดพ้นจากอาถรรพณ์ตำหนักแห่งนี้..."
            : "จิตใจของคุณตกต่ำจนไร้ซึ่งคุณงามความดี\nความมืดมิดครอบงำและกลืนกินวิญญาณของคุณไปตลอดกาล...";

        GUI.Label(new Rect(boxX + 30, boxY + 90, boxW - 60, 130), descStr, descStyle);

        // 5. ปุ่มกด Restart และ Main Menu
        float btnW = 180f;
        float btnH = 48f;
        float gap = 20f;
        float totalBtnsW = (btnW * 2) + gap;
        float btnStartX = boxX + (boxW - totalBtnsW) / 2f;
        float btnY = boxY + boxH - 75f;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        // ปุ่มเริ่มใหม่
        GUI.backgroundColor = isGood ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.85f, 0.2f, 0.2f);
        if (GUI.Button(new Rect(btnStartX, btnY, btnW, btnH), "🔄 เริ่มเล่นใหม่", btnStyle))
        {
            RestartGame();
        }

        // ปุ่มกลับหน้าหลัก
        GUI.backgroundColor = new Color(0.2f, 0.4f, 0.7f);
        if (GUI.Button(new Rect(btnStartX + btnW + gap, btnY, btnW, btnH), "🏠 กลับหน้าหลัก", btnStyle))
        {
            GoToMainMenu();
        }

        GUI.backgroundColor = Color.white;
    }

    // ==========================================
    // ContextMenu สำหรับทดสอบฉากจบได้ทันทีใน Unity Editor
    // ==========================================
    [ContextMenu("🧪 ทดสอบ: เรียกฉากจบดี (Good Ending)")]
    public void TestTriggerGoodEnding() => TriggerEnding(GameEndingType.GoodEnding);

    [ContextMenu("🧪 ทดสอบ: เรียกฉากโดนกลืนกิน (Devoured Ending)")]
    public void TestTriggerDevouredEnding() => TriggerEnding(GameEndingType.BadEnding);
}
