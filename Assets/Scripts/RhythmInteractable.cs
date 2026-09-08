using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// วัตถุโต้ตอบในฉากหรือไอเทมสำหรับเริ่มมินิเกมท่องคาถา (Rhythm Chant WASD)
/// ทำงานเหมือนกับ QTEInteractable (พิธีเขียนยันต์)
/// </summary>
public class RhythmInteractable : InteractableItem
{
    [Header("Rhythm Minigame Configuration")]
    [Tooltip("ระดับความยากของมินิเกมท่องคาถา (Easy, Medium, Hard, VeryHard)")]
    public QuestDifficulty difficulty = QuestDifficulty.Easy;

    [Tooltip("จำนวนโน้ตทั้งหมด (0 = ใช้ค่าเริ่มต้นตามระดับความยาก)")]
    public int customNoteCount = 0;

    [Tooltip("ความเร็วในการเลื่อนของโน้ต (0 = ใช้ค่าเริ่มต้นตามระดับความยาก)")]
    public float customNoteSpeed = 0f;

    [Tooltip("เปอร์เซ็นต์คะแนนขั้นต่ำในการผ่าน (ค่าเริ่มต้น 60%)")]
    [Range(30, 100)]
    public int passPercentage = 60;

    [Tooltip("เมื่อทำสำเร็จแล้ว ให้ปิดการโต้ตอบกับวัตถุนี้หรือไม่")]
    public bool disableAfterSuccess = false;

    [Header("Rhythm Events")]
    public UnityEvent onRhythmSuccess = new UnityEvent();
    public UnityEvent onRhythmFailed = new UnityEvent();

    private bool isCompletedSuccessfully = false;

    void Reset()
    {
        itemName = "ตำราคาถา / แท่นสวดมนต์";
        canRead = true;
        canCollect = false;
        customReadPromptText = "ท่องคาถา";
        readTitle = "";
        readDescription = "";
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นเดินมาเล็งแล้วกดปุ่มโต้ตอบ [E]
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        if (disableAfterSuccess && isCompletedSuccessfully)
        {
            if (interactor != null)
            {
                interactor.ShowNotification($"[{itemName}] ทำพิธีท่องคาถาเสร็จสิ้นแล้ว!");
            }
            return;
        }

        // ค้นหาหรือสร้าง RhythmGameManager
        if (RhythmGameManager.Instance == null)
        {
            GameObject rhythmObj = new GameObject("RhythmGameManager");
            rhythmObj.AddComponent<RhythmGameManager>();
        }

        if (RhythmGameManager.Instance != null)
        {
            if (RhythmGameManager.Instance.isPlaying) return;

            // เรียก Event OnRead เดิมก่อน (ถ้ามี)
            base.OnRead(interactor);

            // สร้าง QuestData จำลองเพื่อส่งให้ RhythmGameManager
            QuestData dummyQuest = new QuestData
            {
                questTitle = itemName,
                difficulty = difficulty,
                customNoteCount = customNoteCount,
                customNoteSpeed = customNoteSpeed,
                passPercentage = passPercentage,
                minigameType = MinigameType.RhythmChantWASD
            };

            // เริ่ม Minigame ท่องคาถา Rhythm
            RhythmGameManager.Instance.StartRhythmGame(dummyQuest, (isPassed) =>
            {
                if (isPassed) HandleSuccess();
                else HandleFail();
            });
        }
    }

    private void HandleSuccess()
    {
        isCompletedSuccessfully = true;

        if (disableAfterSuccess)
        {
            canRead = false;
        }

        // หากมีเควสที่รับมาและเป็นเควสมินิเกม RhythmChantWASD ให้เปลี่ยนสถานะเควสเป็นทำภารกิจสำเร็จ
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.activeQuests != null)
        {
            foreach (var q in QuestUIManager.Instance.activeQuests)
            {
                if (q != null && q.minigameType == MinigameType.RhythmChantWASD && !q.isTaskCompleted)
                {
                    q.isTaskCompleted = true;
                    Debug.Log($"[RhythmInteractable] 📜 อัปเดตเควส '{q.questTitle}' -> ทำพิธีท่องคาถาสำเร็จแล้ว!");
                }
            }
        }

        onRhythmSuccess?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"ทำพิธีท่องคาถา [{itemName}] สำเร็จเรียบร้อย!");
        }

        Debug.Log($"[RhythmInteractable] 🎉 พิธีท่องคาถาสำเร็จบนวัตถุ '{itemName}'");
    }

    private void HandleFail()
    {
        onRhythmFailed?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"ทำพิธีท่องคาถา [{itemName}] พลาด!");
        }

        Debug.Log($"[RhythmInteractable] ❌ พิธีท่องคาถาพลาดบนวัตถุ '{itemName}'");
    }
}
