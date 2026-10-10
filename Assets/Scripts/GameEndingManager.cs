using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public enum GameEndingType
{
    GhostGameOver,    // โดนผีเกาะครบ 3 ตัว
    GoodEnding,       // Good end (karma >= 50)
    BadEnding,        // bad end (karma < 50)
    KumanThongEnding  // kumanthong ending (dependencyLevel > 20)
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
    [Tooltip("UI Canvas หลักสำหรับฉากจบ (Good End / Bad End / KumanThong)")]
    public GameObject endingCanvas;

    [Tooltip("UI Canvas หรือ Panel สำหรับ Game Over (โดนผีเกาะครบ 3 ตัว)")]
    public GameObject gameOverCanvas;

    [Tooltip("UI Panel สำหรับฉากจบดี (ถ้าต้องการแยก GameObject ใน Inspector)")]
    public GameObject goodEndingPanel;

    [Tooltip("UI Panel สำหรับฉากโดนกลืนกิน / ฉากจบสายดำ (ถ้าต้องการแยก GameObject ใน Inspector)")]
    public GameObject badEndingPanel;

    [Tooltip("UI Panel สำหรับฉากจบกุมารทอง (ถ้าต้องการแยก GameObject ใน Inspector)")]
    public GameObject kumanThongEndingPanel;

    [Header("UI Components (ค้นหาอัตโนมัติจาก Canvas ถ้าเว้นว่าง)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button restartButton;
    public Button mainMenuButton;
    public Image bgImage;

    [Header("State")]
    public bool isGameEnding = false;
    public GameEndingType currentEndingType = GameEndingType.BadEnding;

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
        EnsureEndingCanvas();
        BindUIComponents();

        // ซ่อน Canvas และ Panel ฉากจบ/Game Over ไว้ก่อนเริ่มเกม
        if (endingCanvas != null) endingCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (goodEndingPanel != null) goodEndingPanel.SetActive(false);
        if (badEndingPanel != null) badEndingPanel.SetActive(false);
        if (kumanThongEndingPanel != null) kumanThongEndingPanel.SetActive(false);
    }

    /// <summary>
    /// ประเมินและดึงฉากจบหลังจากกดนอนในวันที่ 2
    /// - dependency > 20 -> ฉากจบกุมารทอง (KumanThongEnding)
    /// - karma >= 50     -> ฉากจบสายขาว (GoodEnding)
    /// - karma < 50      -> ฉากจบสายดำ (BadEnding)
    /// </summary>
    public void EvaluateDay2Ending()
    {
        int dependency = 0;
        if (KumanThongUIController.Instance != null)
        {
            dependency = KumanThongUIController.Instance.dependencyLevel;
        }

        int karma = 40;
        if (KarmaManager.Instance != null)
        {
            karma = KarmaManager.Instance.karma;
        }

        Debug.Log($"[GameEndingManager] 📊 ประเมินผลฉากจบวันที่ 2 | Dependency: {dependency}, Karma: {karma}");

        if (dependency > 20)
        {
            TriggerEnding(GameEndingType.KumanThongEnding);
        }
        else if (karma >= 50)
        {
            TriggerEnding(GameEndingType.GoodEnding);
        }
        else
        {
            TriggerEnding(GameEndingType.BadEnding);
        }
    }

    /// <summary>
    /// ค้นหา UI Canvas และ Panels สำหรับฉากจบ และ Game Over
    /// </summary>
    private void EnsureEndingCanvas()
    {
        // 1. ค้นหา endingCanvas
        if (endingCanvas == null)
        {
            GameObject foundCanvas = GameObject.Find("EndingCanvas");
            if (foundCanvas == null) foundCanvas = GameObject.Find("GameEndingCanvas");
            if (foundCanvas == null)
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    if (c != null && c.gameObject.name.ToLower().Contains("ending") && !c.gameObject.name.ToLower().Contains("gameover"))
                    {
                        foundCanvas = c.gameObject;
                        break;
                    }
                }
            }

            if (foundCanvas != null)
            {
                endingCanvas = foundCanvas;
            }
        }

        // 2. ค้นหา gameOverCanvas / gameOverPanel สำหรับ Game Over
        if (gameOverCanvas == null)
        {
            GameObject foundGOCanvas = GameObject.Find("GameOverCanvas");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("GameOverPanel");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("GameOver");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("Game Over Canvas");
            if (foundGOCanvas == null) foundGOCanvas = GameObject.Find("Game Over Panel");
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
                if (goChild == null) goChild = endingCanvas.transform.Find("Game Over");
                if (goChild != null) foundGOCanvas = goChild.gameObject;
            }

            if (foundGOCanvas != null)
            {
                gameOverCanvas = foundGOCanvas;
            }
        }

        // 3. ค้นหา Panels ย่อย หากยังไม่ได้ลากวางใน Inspector
        if (badEndingPanel == null && endingCanvas != null)
        {
            Transform t = endingCanvas.transform.Find("BadEndingPanel");
            if (t == null) t = endingCanvas.transform.Find("BadEnding");
            if (t == null) t = endingCanvas.transform.Find("Bad Ending Panel");
            if (t != null) badEndingPanel = t.gameObject;
        }

        if (goodEndingPanel == null && endingCanvas != null)
        {
            Transform t = endingCanvas.transform.Find("GoodEndingPanel");
            if (t == null) t = endingCanvas.transform.Find("GoodEnding");
            if (t == null) t = endingCanvas.transform.Find("Good Ending Panel");
            if (t != null) goodEndingPanel = t.gameObject;
        }

        if (kumanThongEndingPanel == null && endingCanvas != null)
        {
            Transform t = endingCanvas.transform.Find("KumanThongEndingPanel");
            if (t == null) t = endingCanvas.transform.Find("KumanThongEnding");
            if (t != null) kumanThongEndingPanel = t.gameObject;
        }
    }

    /// <summary>
    /// เชื่อมต่อคอมโพเนนต์บน UI Canvas โดยอัตโนมัติหากยังไม่ได้ลากใส่ใน Inspector
    /// </summary>
    private void BindUIComponents()
    {
        GameObject targetCanvas = (currentEndingType == GameEndingType.GhostGameOver && gameOverCanvas != null) 
            ? gameOverCanvas 
            : endingCanvas;

        if (targetCanvas == null) targetCanvas = endingCanvas;
        if (targetCanvas == null) return;

        // 1. ค้นหา Title Text (มองหา TextMeshProUGUI ที่ไม่ใช่ข้อความบนปุ่ม)
        if (titleText == null)
        {
            var tmps = targetCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
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

        // 3. ค้นหาภาพพื้นหลัง Panel
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
    /// สั่งจบเกมและเรียก UI ตามประเภทฉากจบ
    /// </summary>
    public void TriggerEnding(GameEndingType endingType)
    {
        if (isGameEnding) return;
        isGameEnding = true;
        currentEndingType = endingType;

        Debug.Log($"[GameEndingManager] 🎬 เรียก {(endingType == GameEndingType.GhostGameOver ? "Game Over" : "ฉากจบ")}: {endingType}");

        // หยุดเวลาและหยุดการควบคุมผู้เล่น
        Time.timeScale = 0f;
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ปิดจอมืด Fade ทุกตัวในฉาก
        ClearAllFadeOverlays();

        // ตรวจสอบและเตรียม UI Canvas ในฉาก
        EnsureEndingCanvas();

        // ปรับแต่งข้อความและแผงควบคุมตามประเภท
        SetupEndingUI(endingType);

        // เปิด Canvas ตามประเภท
        if (endingType == GameEndingType.GhostGameOver && gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }
        else if (endingCanvas != null)
        {
            endingCanvas.SetActive(true);
        }

        BindUIComponents();
    }

    private void ClearAllFadeOverlays()
    {
        var fadeObjs = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in fadeObjs)
        {
            if (c.gameObject != null && c.gameObject != endingCanvas && c.gameObject != gameOverCanvas && c.gameObject.name.ToLower().Contains("fade"))
            {
                c.gameObject.SetActive(false);
            }
        }
    }

    private void SetupEndingUI(GameEndingType endingType)
    {
        bool isGhostGameOver = (endingType == GameEndingType.GhostGameOver);

        // 1. ปิด badEndingPanel แน่นอนถ้าเป็น Game Over (ผีครบ 3 ตัว) และเปิดเมื่อเป็น BadEnding เท่านั้น
        if (badEndingPanel != null)
        {
            badEndingPanel.SetActive(endingType == GameEndingType.BadEnding);
        }

        if (goodEndingPanel != null)
        {
            goodEndingPanel.SetActive(endingType == GameEndingType.GoodEnding);
        }

        if (kumanThongEndingPanel != null)
        {
            kumanThongEndingPanel.SetActive(endingType == GameEndingType.KumanThongEnding);
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(isGhostGameOver);

            // หาก gameOverCanvas เป็น Canvas แยก ให้ปิด endingCanvas ไม่ให้ขึ้นซ้อนทับกัน
            if (isGhostGameOver && endingCanvas != null && gameOverCanvas != endingCanvas && gameOverCanvas.transform.parent != endingCanvas.transform)
            {
                endingCanvas.SetActive(false);
            }
        }

        // 2. กำหนดข้อความชื่อฉากจบเฉพาะที่ titleText (หากมีการตั้งค่าไว้)
        string endingTitle = "";
        switch (endingType)
        {
            case GameEndingType.GhostGameOver:
                endingTitle = "Game Over";
                break;
            case GameEndingType.GoodEnding:
                endingTitle = "ฉากจบสายขาว";
                break;
            case GameEndingType.BadEnding:
                endingTitle = "ฉากจบสายดำ";
                break;
            case GameEndingType.KumanThongEnding:
                endingTitle = "ฉากจบกุมารทอง";
                break;
        }

        if (titleText != null)
        {
            titleText.text = endingTitle;
        }
        if (descriptionText != null)
        {
            descriptionText.text = "";
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
    // ContextMenu สำหรับทดสอบฉากจบได้ทันทีใน Unity Editor
    // ==========================================
    [ContextMenu("🧪 ทดสอบ: ประเมินผลฉากจบวันที่ 2")]
    public void TestEvaluateDay2Ending() => EvaluateDay2Ending();

    [ContextMenu("🧪 ทดสอบ: Game Over (GhostGameOver)")]
    public void TestTriggerGhostGameOver() => TriggerEnding(GameEndingType.GhostGameOver);

    [ContextMenu("🧪 ทดสอบ: ฉากจบสายขาว (GoodEnding)")]
    public void TestTriggerGoodEnding() => TriggerEnding(GameEndingType.GoodEnding);

    [ContextMenu("🧪 ทดสอบ: ฉากจบสายดำ (BadEnding)")]
    public void TestTriggerBadEnding() => TriggerEnding(GameEndingType.BadEnding);

    [ContextMenu("🧪 ทดสอบ: ฉากจบกุมารทอง (KumanThongEnding)")]
    public void TestTriggerKumanThongEnding() => TriggerEnding(GameEndingType.KumanThongEnding);
}
