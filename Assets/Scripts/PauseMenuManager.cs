using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบ Pause Menu — ลาก UI Panels / Buttons มาเชื่อมใน Inspector
/// กด Escape เพื่อเปิด/ปิด Pause Menu
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("State (Read Only)")]
    public bool isPaused = false;

    [Header("UI Panels — ลากมาจาก Hierarchy")]
    [Tooltip("Panel หลักของ Pause Menu")]
    public GameObject pausePanel;
    [Tooltip("Panel การตั้งค่า (ถ้ามี)")]
    public GameObject settingsPanel;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // ซ่อน Panel ทั้งสองตั้งแต่เริ่มต้น
        if (pausePanel != null)   pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // ถ้ามีระบบอื่นเปิดอยู่ ไม่ให้ Pause
        if (IsAnySystemActive()) return;

        TogglePause();
    }

    // ══════════════════════════════════════════════════════════
    // Public API — เรียกจากปุ่ม UI ใน Inspector ได้โดยตรง
    // ══════════════════════════════════════════════════════════

    public void TogglePause()
    {
        SetPauseState(!isPaused);
    }

    public void SetPauseState(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)   pausePanel.SetActive(paused);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;
        }
        else
        {
            if (!IsAnyOtherSystemActive())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;

                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) pc.enabled = true;
            }
        }
    }

    /// <summary>เล่นต่อ — ผูกกับปุ่ม Resume ใน Inspector</summary>
    public void Resume()
    {
        SetPauseState(false);
    }

    /// <summary>เปิดหน้าตั้งค่า — ผูกกับปุ่ม Settings ใน Inspector</summary>
    public void OpenSettings()
    {
        if (pausePanel != null)   pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>กลับจากหน้าตั้งค่า — ผูกกับปุ่ม Back ใน Settings Panel</summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null)   pausePanel.SetActive(true);
    }

    /// <summary>รีสตาร์ทเกม — ผูกกับปุ่ม Restart ใน Inspector</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>ไปหน้าหลัก — ผูกกับปุ่ม Main Menu ใน Inspector</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════

    private bool IsAnySystemActive()
    {
        if (MinigameManager.Instance != null && MinigameManager.Instance.isMinigameActive) return true;
        if (QuestUIManager.Instance  != null && QuestUIManager.Instance.IsDialogActive())  return true;
        if (ShopController.Instance  != null && ShopController.Instance.isShopOpen)        return true;
        if (KumanThongUIController.Instance != null && KumanThongUIController.Instance.isUIOpen) return true;
        if (GhostCurseManager.Instance != null && GhostCurseManager.Instance.isGameOver)   return true;
        return false;
    }

    private bool IsAnyOtherSystemActive()
    {
        if (MinigameManager.Instance != null && MinigameManager.Instance.isMinigameActive) return true;
        if (QuestUIManager.Instance  != null && QuestUIManager.Instance.IsDialogActive())  return true;
        if (ShopController.Instance  != null && ShopController.Instance.isShopOpen)        return true;
        if (KumanThongUIController.Instance != null && KumanThongUIController.Instance.isUIOpen) return true;
        return false;
    }
}
