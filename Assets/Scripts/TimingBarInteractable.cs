using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// วัตถุโต้ตอบในฉากสำหรับเริ่มมินิเกมแถบจังหวะโปรยข้าวสารไล่ผี (Horizontal Timing Bar QTE)
/// </summary>
public class TimingBarInteractable : InteractableItem
{
    [Header("Timing Bar Minigame Configuration")]
    [Tooltip("จำนวนครั้งขั้นต่ำที่ต้องกดสำเร็จ (สุ่ม 4 ถึง 6)")]
    [Range(1, 10)]
    public int minHits = 4;

    [Tooltip("จำนวนครั้งสูงสุดที่ต้องกดสำเร็จ (สุ่ม 4 ถึง 6)")]
    [Range(1, 10)]
    public int maxHits = 6;

    [Tooltip("ความเร็วเริ่มต้นของการสลับตัวชี้ (รอบ/วินาที)")]
    public float baseSpeed = 1.2f;

    [Tooltip("ความเร็วที่เพิ่มขึ้นทุกครั้งที่กดสำเร็จ (รอบ/วินาที)")]
    public float speedIncrement = 0.35f;

    [Tooltip("จำนวนครั้งสูงสุดที่อนุญาตให้ล้มเหลว (พลาดได้ไม่เกิน 3 ครั้ง)")]
    public int maxAllowedFailures = 3;

    [Tooltip("เมื่อทำสำเร็จแล้ว ให้ปิดการโต้ตอบกับวัตถุนี้หรือไม่")]
    public bool disableAfterSuccess = false;

    [Header("Timing Bar Events")]
    public UnityEvent onTimingBarSuccess = new UnityEvent();
    public UnityEvent onTimingBarFailed = new UnityEvent();

    private bool isCompletedSuccessfully = false;

    void Reset()
    {
        itemName = "ขันข้าวสารไล่ผี / แท่นพิธีโปรยข้าวสาร";
        canRead = true;
        canCollect = false;
        customReadPromptText = "โปรยข้าวสาร";
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
                interactor.ShowNotification($"[{itemName}] ทำพิธีโปรยข้าวสารเสร็จสิ้นแล้ว!");
            }
            return;
        }

        QuestData activeQuest = GetActiveQuestForMinigame();
        if (activeQuest == null)
        {
            if (interactor != null)
            {
                interactor.ShowNotification("🔒 พิธีโปรยข้าวสารไม่ได้อยู่ในขั้นตอนของเควสปัจจุบัน!", 3.0f);
            }
            return;
        }

        if (TimingBarController.Instance == null)
        {
            GameObject barObj = new GameObject("TimingBarController");
            barObj.AddComponent<TimingBarController>();
        }

        if (TimingBarController.Instance != null)
        {
            if (TimingBarController.Instance.IsActive()) return;

            base.OnRead(interactor);

            TimingBarController.Instance.StartTimingBarGame(
                minHits,
                maxHits,
                baseSpeed,
                speedIncrement,
                maxAllowedFailures,
                HandleSuccess,
                HandleFail
            );
        }
    }

    private QuestData GetActiveQuestForMinigame()
    {
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.activeQuests != null)
        {
            foreach (var q in QuestUIManager.Instance.activeQuests)
            {
                if (q != null && q.IsMinigameRequired(MinigameType.TimingBarQTE))
                {
                    return q;
                }
            }
        }
        return null;
    }

    private void HandleSuccess()
    {
        isCompletedSuccessfully = true;

        if (disableAfterSuccess)
        {
            canRead = false;
        }

        QuestData q = GetActiveQuestForMinigame();
        if (q != null)
        {
            bool allDone = q.MarkMinigameCompleted(MinigameType.TimingBarQTE);
            string progressText = $"({q.completedMinigameSequence.Count}/{q.requiredMinigameSequence.Count})";

            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null)
            {
                if (allDone)
                {
                    player.ShowNotification($"🎉 <color=#00FF7F>[ทำพิธีครบทุกขั้นตอนแล้ว {progressText}]</color> กลับไปรายงาน {q.npcName} ได้เลย!", 4.0f);
                }
                else
                {
                    MinigameType nextGame = q.requiredMinigameSequence[q.completedMinigameSequence.Count];
                    string nextThai = QuestData.GetMinigameNameThai(nextGame);
                    player.ShowNotification($"✨ <color=#00FF7F>[โปรยข้าวสารสำเร็จ {progressText}]</color> ขั้นต่อไปทำ: <color=#FFD700>{nextThai}</color>", 4.0f);
                }
            }
        }

        onTimingBarSuccess?.Invoke();
        Debug.Log($"[TimingBarInteractable] 🎉 พิธีโปรยข้าวสารสำเร็จบนวัตถุ '{itemName}'");
    }

    private void HandleFail()
    {
        onTimingBarFailed?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"พิธี [{itemName}] หลุดจังหวะ ล้มเหลว! ลองใหม่อีกครั้ง");
        }

        Debug.Log($"[TimingBarInteractable] ❌ พิธีโปรยข้าวสารล้มเหลวบนวัตถุ '{itemName}'");
    }
}
