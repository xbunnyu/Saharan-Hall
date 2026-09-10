using UnityEngine;
using UnityEngine.Events;

public class QTEInteractable : InteractableItem
{
    [Header("QTE Minigame Configuration")]
    [Tooltip("จำนวนครั้งขั้นต่ำที่ต้องกดสำเร็จ (สุ่ม 3 ถึง 5)")]
    [Range(1, 10)]
    public int minHits = 3;

    [Tooltip("จำนวนครั้งสูงสุดที่ต้องกดสำเร็จ (สุ่ม 3 ถึง 5)")]
    [Range(1, 10)]
    public int maxHits = 5;

    [Tooltip("ความเร็วเริ่มต้นของเข็มหมุน (องศา/วินาที)")]
    public float baseSpeed = 180f;

    [Tooltip("ความเร็วที่เพิ่มขึ้นทุกครั้งที่กดสำเร็จ (องศา/วินาที)")]
    public float speedIncrement = 40f;

    [Tooltip("จำนวนครั้งสูงสุดที่อนุญาตให้ล้มเหลว (พลาดได้ไม่เกิน 3 ครั้ง)")]
    public int maxAllowedFailures = 3;

    [Tooltip("เมื่อทำสำเร็จแล้ว ให้ปิดการโต้ตอบกับวัตถุนี้หรือไม่")]
    public bool disableAfterSuccess = false;

    [Header("QTE Events")]
    public UnityEvent onQTESuccess = new UnityEvent();
    public UnityEvent onQTEFailed = new UnityEvent();

    private bool isCompletedSuccessfully = false;

    void Reset()
    {
        itemName = "ผ้ายันต์ / แท่นเขียนยันต์";
        canRead = true;
        canCollect = false;
        customReadPromptText = "เขียนยันต์";
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
                interactor.ShowNotification($"[{itemName}] ทำพิธีเขียนยันต์เสร็จสิ้นแล้ว!");
            }
            return;
        }

        QuestData activeQuest = GetActiveQuestForMinigame();
        if (activeQuest == null)
        {
            if (interactor != null)
            {
                interactor.ShowNotification("🔒 พิธีเขียนยันต์ไม่ได้อยู่ในขั้นตอนของเควสปัจจุบัน!", 3.0f);
            }
            return;
        }

        // หากยังไม่มี QTEController ในฉาก ให้สร้างให้อัตโนมัติ
        if (QTEController.Instance == null)
        {
            GameObject qteObj = new GameObject("QTEController");
            qteObj.AddComponent<QTEController>();
        }

        if (QTEController.Instance != null)
        {
            if (QTEController.Instance.IsQTEActive()) return;

            // เรียก Event OnRead เดิมก่อน (ถ้ามี)
            base.OnRead(interactor);

            // เริ่ม Minigame QTE เขียนยันต์
            QTEController.Instance.StartQTE(
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
                if (q != null && q.IsMinigameRequired(MinigameType.TalismanDrawing))
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
            bool allDone = q.MarkMinigameCompleted(MinigameType.TalismanDrawing);
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
                    player.ShowNotification($"✨ <color=#00FF7F>[เขียนยันต์สำเร็จ {progressText}]</color> ขั้นต่อไปทำ: <color=#FFD700>{nextThai}</color>", 4.0f);
                }
            }
        }
        else
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null)
            {
                player.ShowNotification($"ทำพิธี [{itemName}] สำเร็จเรียบร้อย!");
            }
        }

        onQTESuccess?.Invoke();
        Debug.Log($"[QTEInteractable] 🎉 พิธีเขียนยันต์สำเร็จบนวัตถุ '{itemName}'");
    }

    private void HandleFail()
    {
        onQTEFailed?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"ทำพิธีเขียนยันต์ [{itemName}] พลาด! (ล้มเหลวเกิน 2 ครั้ง)");
        }

        Debug.Log($"[QTEInteractable] ❌ พิธีเขียนยันต์พลาดบนวัตถุ '{itemName}'");
    }
}
