using System;
using UnityEngine;

/// <summary>
/// ผู้จัดการระบบมินิเกมส่วนกลาง (Modular Minigame Manager)
/// - ควบคุมการเปิด-ปิดมินิเกม
/// - ควบคุมการหยุดการเคลื่อนที่ของผู้เล่นชั่วคราว
/// - รับส่งผลลัพธ์ (Win/Loss) กลับไปยัง NPC และระบบเควส
/// - รองรับการเชื่อมต่อมินิเกมประเภทอื่นๆ ในอนาคต
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Minigame Runners")]
    public RhythmGameManager rhythmGameManager;

    [Header("Runtime State")]
    public bool isMinigameActive = false;
    public MinigameType currentMinigameType = MinigameType.None;

    private PlayerController playerController;
    private Action<bool> currentCallback;
    private QuestData activeQuestData;

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
        currentMinigameType = quest != null ? quest.minigameType : MinigameType.RhythmChantWASD;

        // ล็อคการควบคุมผู้เล่น และปลดล็อคเมาส์
        FreezePlayer();

        Debug.Log($"[MinigameManager] 🎮 เริ่มมินิเกมประเภท: {currentMinigameType} สำหรับเควส '{quest?.questTitle}'");

        switch (currentMinigameType)
        {
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

            default:
                // เควสทั่วไปที่ไม่มีมินิเกม หรือมินิเกมประเภทอื่น
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

        Debug.Log($"[MinigameManager] 🏁 มินิเกมสิ้นสุดลง ผลลัพธ์: {(isSuccess ? "สำเร็จ (SUCCESS)" : "ล้มเหลว (FAILED)")}");

        // ส่งผลลัพธ์กลับไปยัง NPC / เควส
        currentCallback?.Invoke(isSuccess);
        currentCallback = null;
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
}
