using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// วัตถุโต้ตอบในฉากสำหรับเริ่มมินิเกมตวัดดาบฟันยันต์สะบั้นมาร (Sequential Slash QTE)
/// </summary>
public class SlashInteractable : InteractableItem
{
    [Header("Slash Minigame Configuration")]
    [Tooltip("จำนวนครั้งขั้นต่ำที่ต้องฟันสำเร็จ (สุ่ม 3 ถึง 5)")]
    [Range(3, 5)]
    public int minHits = 3;

    [Tooltip("จำนวนครั้งสูงสุดที่ต้องฟันสำเร็จ (สุ่ม 3 ถึง 5)")]
    [Range(3, 5)]
    public int maxHits = 5;

    [Tooltip("เมื่อทำสำเร็จแล้ว ให้ปิดการโต้ตอบกับวัตถุนี้หรือไม่")]
    public bool disableAfterSuccess = false;

    [Header("Slash Events")]
    public UnityEvent onSlashSuccess = new UnityEvent();
    public UnityEvent onSlashFailed = new UnityEvent();

    private bool isCompletedSuccessfully = false;

    void Reset()
    {
        itemName = "ดาบปราบผี / แท่นฟันยันต์สะบั้นมาร";
        canRead = true;
        canCollect = false;
        customReadPromptText = "ตวัดดาบฟันยันต์";
        readTitle = "";
        readDescription = "";
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        if (disableAfterSuccess && isCompletedSuccessfully)
        {
            if (interactor != null)
            {
                interactor.ShowNotification($"[{itemName}] ทำพิธีตวัดดาบเสร็จสิ้นแล้ว!");
            }
            return;
        }

        QuestData activeQuest = GetActiveQuestForMinigame();
        if (activeQuest == null)
        {
            if (interactor != null)
            {
                interactor.ShowNotification("🔒 พิธีตวัดดาบไม่ได้อยู่ในขั้นตอนของเควสปัจจุบัน!", 3.0f);
            }
            return;
        }

        if (SequentialSlashController.Instance == null)
        {
            GameObject slashObj = new GameObject("SequentialSlashController");
            slashObj.AddComponent<SequentialSlashController>();
        }

        if (SequentialSlashController.Instance != null)
        {
            if (SequentialSlashController.Instance.IsSlashActive()) return;

            base.OnRead(interactor);

            SequentialSlashController.Instance.StartSlashGame(
                minHits,
                maxHits,
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
                if (q != null && q.IsMinigameRequired(MinigameType.SequentialSlashQTE))
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
            bool allDone = q.MarkMinigameCompleted(MinigameType.SequentialSlashQTE);
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
                    player.ShowNotification($"✨ <color=#00FF7F>[ตวัดดาบสำเร็จ {progressText}]</color> ขั้นต่อไปทำ: <color=#FFD700>{nextThai}</color>", 4.0f);
                }
            }
        }

        onSlashSuccess?.Invoke();
        Debug.Log($"[SlashInteractable] ⚔️ พิธีตวัดดาบสำเร็จบนวัตถุ '{itemName}'");
    }

    private void HandleFail()
    {
        onSlashFailed?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"ทำพิธีตวัดดาบ [{itemName}] พลาด!");
        }
    }
}
