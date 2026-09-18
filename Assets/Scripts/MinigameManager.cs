using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ผู้จัดการระบบมินิเกมส่วนกลาง (Modular Minigame Manager)
/// - ควบคุมการเปิด-ปิดมินิเกม
/// - ควบคุมการหยุดการเคลื่อนที่ของผู้เล่นชั่วคราว
/// - รับส่งผลลัพธ์ (Win/Loss) กลับไปยัง NPC และระบบเควส
/// - นับจำนวนมินิเกมที่แพ้สะสม (totalFailedMinigames) แสดงผลบน UI "Fail : X" มุมซ้ายบน
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Minigame Runners")]
    public RhythmGameManager rhythmGameManager;
    public QTEController qteController;
    public SequentialSlashController sequentialSlashController;
    public TimingBarController timingBarController;

    [Header("Runtime State")]
    public bool isMinigameActive = false;
    public MinigameType currentMinigameType = MinigameType.None;

    [Header("Fail Tracker System")]
    [Tooltip("จำนวนมินิเกมที่เล่นแพ้สะสมทั้งหมด")]
    public int totalFailedMinigames = 0;

    private PlayerController playerController;
    private Action<bool> currentCallback;
    private QuestData activeQuestData;

    // Fail Tracker UI References
    private GameObject failCanvas;
    private GameObject failPanel;
    private TextMeshProUGUI failCounterText;

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

        if (rhythmGameManager == null)
        {
            rhythmGameManager = GetComponent<RhythmGameManager>();
            if (rhythmGameManager == null)
            {
                rhythmGameManager = gameObject.AddComponent<RhythmGameManager>();
            }
        }

        if (qteController == null)
        {
            qteController = QTEController.Instance;
            if (qteController == null)
            {
                qteController = FindFirstObjectByType<QTEController>();
            }
        }

        if (sequentialSlashController == null)
        {
            sequentialSlashController = SequentialSlashController.Instance;
            if (sequentialSlashController == null)
            {
                sequentialSlashController = FindFirstObjectByType<SequentialSlashController>();
            }
        }

        if (timingBarController == null)
        {
            timingBarController = TimingBarController.Instance;
            if (timingBarController == null)
            {
                timingBarController = FindFirstObjectByType<TimingBarController>();
            }
        }

        EnsureFailUIExists();
        UpdateFailCounterUI();
    }

    /// <summary>
    /// เริ่มต้นเล่นมินิเกมตามที่กำหนดในเควส
    /// </summary>
    public void StartMinigame(QuestData quest, NPCController npc, Action<bool> onComplete)
    {
        if (isMinigameActive)
        {
            Debug.LogWarning("[MinigameManager] ⚠️ มินิเกมกำลังทำงานอยู่แล้ว!");
            return;
        }

        activeQuestData = quest;
        currentCallback = onComplete;
        isMinigameActive = true;
        currentMinigameType = quest != null ? quest.GetCurrentMinigameType() : MinigameType.RhythmChantWASD;

        // ล็อคการควบคุมผู้เล่น และปลดล็อคเมาส์
        FreezePlayer();

        Debug.Log($"[MinigameManager] 🎮 เริ่มมินิเกมประเภท: {currentMinigameType} สำหรับเควส '{quest?.questTitle}'");

        switch (currentMinigameType)
        {
            // 1. มินิเกมท่องคาถา (Rhythm Game W A S D)
            case MinigameType.RhythmChantWASD:
                if (rhythmGameManager != null)
                {
                    rhythmGameManager.StartRhythmGame(quest, OnMinigameFinished);
                }
                else
                {
                    Debug.LogError("[MinigameManager] ❌ ไม่พบ RhythmGameManager!");
                    OnMinigameFinished(true);
                }
                break;

            // 2. มินิเกมเขียนยันต์ / Skill Check QTE (กด Spacebar หยุดเข็มในวงล้อ)
            case MinigameType.DeadByDaylightQTE:
            case MinigameType.TalismanDrawing:
                if (qteController == null)
                {
                    qteController = QTEController.Instance;
                    if (qteController == null)
                    {
                        GameObject qteObj = new GameObject("QTEController");
                        qteController = qteObj.AddComponent<QTEController>();
                    }
                }

                if (qteController != null)
                {
                    int minHits = quest != null && quest.customNoteCount > 0 ? quest.customNoteCount : 3;
                    int maxHits = quest != null && quest.customNoteCount > 0 ? quest.customNoteCount : 5;
                    float baseSpeed = quest != null && quest.customNoteSpeed > 0 ? quest.customNoteSpeed : 180f;
                    string title = "📜 พิธีเขียนยันต์มหาเวทย์ (YANTRA DRAWING) ✨";

                    qteController.StartQTE(
                        minHits,
                        maxHits,
                        baseSpeed,
                        speedInc: 40f,
                        maxFails: 3,
                        onSuccess: () => OnMinigameFinished(true),
                        onFail: () => OnMinigameFinished(false),
                        title: title
                    );
                }
                else
                {
                    Debug.LogError("[MinigameManager] ❌ ไม่พบ QTEController!");
                    OnMinigameFinished(false);
                }
                break;

            case MinigameType.SequentialSlashQTE:
                if (sequentialSlashController == null)
                {
                    sequentialSlashController = SequentialSlashController.Instance;
                    if (sequentialSlashController == null)
                    {
                        GameObject slashObj = new GameObject("SequentialSlashController");
                        sequentialSlashController = slashObj.AddComponent<SequentialSlashController>();
                    }
                }

                if (sequentialSlashController != null)
                {
                    int minHits = quest != null && quest.customNoteCount > 0 ? quest.customNoteCount : 3;
                    int maxHits = quest != null && quest.customNoteCount > 0 ? quest.customNoteCount : 5;

                    sequentialSlashController.StartSlashGame(
                        minHits,
                        maxHits,
                        onSuccess: () => OnMinigameFinished(true),
                        onFail: () => OnMinigameFinished(false)
                    );
                }
                else
                {
                    Debug.LogError("[MinigameManager] ❌ ไม่พบ SequentialSlashController!");
                    OnMinigameFinished(false);
                }
                break;

            case MinigameType.TimingBarQTE:
                if (timingBarController == null)
                {
                    timingBarController = TimingBarController.Instance;
                    if (timingBarController == null)
                    {
                        GameObject barObj = new GameObject("TimingBarController");
                        timingBarController = barObj.AddComponent<TimingBarController>();
                    }
                }

                if (timingBarController != null)
                {
                    int minHits = quest != null && quest.customNoteCount > 0 ? quest.customNoteCount : 4;
                    int maxHits = quest != null && quest.customNoteCount > 0 ? quest.customNoteCount : 6;
                    float baseSpeed = quest != null && quest.customNoteSpeed > 0 ? quest.customNoteSpeed : 1.2f;

                    timingBarController.StartTimingBarGame(
                        minHits,
                        maxHits,
                        baseSpeed,
                        speedInc: 0.35f,
                        maxFails: 3,
                        onSuccess: () => OnMinigameFinished(true),
                        onFail: () => OnMinigameFinished(false),
                        title: "🌾 พิธีโปรยข้าวสารไล่ผี (EXORCISM RICE TOSSING) 👻"
                    );
                }
                else
                {
                    Debug.LogError("[MinigameManager] ❌ ไม่พบ TimingBarController!");
                    OnMinigameFinished(false);
                }
                break;

            default:
                // เควสทั่วไปที่ไม่มีมินิเกม
                Debug.Log("[MinigameManager] ไม่มีมินิเกมสำหรับเควสนี้ ผ่านเควสทันที");
                OnMinigameFinished(true);
                break;
        }
    }

    /// <summary>
    /// Callback เมื่อมินิเกมจบลง (สำเร็จ หรือ ล้มเหลว)
    /// </summary>
    private void OnMinigameFinished(bool isSuccess)
    {
        isMinigameActive = false;
        currentMinigameType = MinigameType.None;

        UnfreezePlayer();

        // เพิ่มตัวนับและทำให้เควสล้มเหลวเมื่อแพ้มินิเกม
        if (!isSuccess)
        {
            RegisterMinigameFailed();

            QuestData qToFail = activeQuestData;
            if (qToFail == null && QuestUIManager.Instance != null && QuestUIManager.Instance.activeQuests.Count > 0)
            {
                qToFail = QuestUIManager.Instance.activeQuests[0];
            }

            if (qToFail != null && QuestUIManager.Instance != null)
            {
                QuestUIManager.Instance.FailQuest(qToFail);
            }
        }

        Debug.Log($"[MinigameManager] 🏁 มินิเกมสิ้นสุดลง ผลลัพธ์: {(isSuccess ? "สำเร็จ (SUCCESS)" : "ล้มเหลว (FAILED)")} | รวมแพ้สะสม: {totalFailedMinigames} ครั้ง");

        // ส่งผลลัพธ์กลับไปยัง NPC / เควส
        currentCallback?.Invoke(isSuccess);
        currentCallback = null;
    }

    /// <summary>
    /// บันทึกการแพ้มินิเกมและเพิ่มตัวนับ Fail +1
    /// </summary>
    public void RegisterMinigameFailed()
    {
        totalFailedMinigames++;
        EnsureFailUIExists();
        UpdateFailCounterUI();
        Debug.Log($"[MinigameManager] ❌ แพ้มินิเกม! รวมแพ้สะสม: {totalFailedMinigames} ครั้ง");
    }

    /// <summary>
    /// อัปเดตข้อความจำนวนครั้งที่แพ้บน UI (แสดงผล Fail : X)
    /// </summary>
    public void UpdateFailCounterUI()
    {
        if (failCounterText != null)
        {
            failCounterText.text = $"<color=#FF3555><b>Fail :</b></color> <color=#FFFFFF>{totalFailedMinigames}</color>";
        }
    }

    /// <summary>
    /// รีเซ็ตตัวนับการแพ้สะสมเป็น 0
    /// </summary>
    public void ResetFailCounter()
    {
        totalFailedMinigames = 0;
        UpdateFailCounterUI();
    }

    /// <summary>
    /// หยุดการเคลื่อนที่และมุมกล้องของผู้เล่นระหว่างเล่นมินิเกม
    /// </summary>
    private void FreezePlayer()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// คืนค่าการควบคุมให้ผู้เล่นเมื่อมินิเกมจบ
    /// </summary>
    private void UnfreezePlayer()
    {
        // ตรวจสอบว่าไม่มี UI หรือ Game Over ขวางอยู่
        if (GhostCurseManager.Instance != null && GhostCurseManager.Instance.isGameOver)
        {
            return;
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsPlaying() => isMinigameActive;

    private Sprite CreateBoxSprite(int width = 64, int height = 64)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    // ==========================================
    // Fail Counter UI Creation (uGUI Badge Top-Left)
    // ==========================================
    private void EnsureFailUIExists()
    {
        if (failPanel != null && failCanvas != null) return;

        failCanvas = GameObject.Find("FailCounter_Canvas");
        if (failCanvas == null)
        {
            failCanvas = new GameObject("FailCounter_Canvas");
            Canvas canvas = failCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 990;

            CanvasScaler scaler = failCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        failPanel = failCanvas.transform.Find("FailPanel")?.gameObject;
        if (failPanel == null)
        {
            Sprite boxSprite = CreateBoxSprite(64, 64);

            failPanel = new GameObject("FailPanel");
            failPanel.transform.SetParent(failCanvas.transform, false);

            RectTransform panelRect = failPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f); // มุมซ้ายบน (Top-Left)
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(100, -45); // พิกัดมุมซ้ายบน
            panelRect.sizeDelta = new Vector2(150, 44);

            // BG Card Panel
            Image bg = failPanel.AddComponent<Image>();
            bg.sprite = boxSprite;
            bg.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);

            // Fail Counter Text
            GameObject textObj = new GameObject("FailCounterText");
            textObj.transform.SetParent(failPanel.transform, false);
            failCounterText = textObj.AddComponent<TextMeshProUGUI>();
            failCounterText.text = $"<color=#FF3555><b>Fail :</b></color> <color=#FFFFFF>{totalFailedMinigames}</color>";
            failCounterText.fontSize = 20;
            failCounterText.fontStyle = FontStyles.Bold;
            failCounterText.alignment = TextAlignmentOptions.Center;
            failCounterText.rectTransform.anchoredPosition = Vector2.zero;
            failCounterText.rectTransform.sizeDelta = new Vector2(150, 44);
        }
    }

    // ==========================================
    // OnGUI Fallback Rendering (การันตีการวาด 100%)
    // ==========================================
    void OnGUI()
    {
        // หากมี uGUI Panel ทำงานอยู่แล้ว ให้ข้ามการวาดด้วย OnGUI
        if (failPanel != null && failPanel.activeSelf) return;

        float boxW = 140f;
        float boxH = 38f;
        float x = 20f;
        float y = 25f;

        GUI.Box(new Rect(x, y, boxW, boxH), "");

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = new Color(1f, 0.25f, 0.25f);

        GUI.Label(new Rect(x, y, boxW, boxH), $"Fail : {totalFailedMinigames}", style);
    }
}
