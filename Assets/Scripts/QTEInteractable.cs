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

    [Tooltip("จำนวนครั้งสูงสุดที่อนุญาตให้ล้มเหลว (พลาดได้ไม่เกิน 2 ครั้ง)")]
    public int maxAllowedFailures = 2;

    [Tooltip("เมื่อทำสำเร็จแล้ว ให้ปิดการโต้ตอบกับวัตถุนี้หรือไม่")]
    public bool disableAfterSuccess = false;

    [Header("QTE Events")]
    public UnityEvent onQTESuccess = new UnityEvent();
    public UnityEvent onQTEFailed = new UnityEvent();

    private bool isCompletedSuccessfully = false;

    void Reset()
    {
        itemName = "กลไก / เครื่องปั่นไฟ";
        canRead = true;
        canCollect = false;
        customReadPromptText = "เริ่ม Minigame QTE";
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
                interactor.ShowNotification($"[{itemName}] ทำงานเสร็จสิ้นแล้ว!");
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

            // เริ่ม Minigame QTE
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

    private void HandleSuccess()
    {
        isCompletedSuccessfully = true;

        if (disableAfterSuccess)
        {
            canRead = false;
        }

        // หากมีเควสที่รับมาและเป็นเควสมินิเกม DeadByDaylightQTE ให้เปลี่ยนสถานะเควสเป็นทำภารกิจสำเร็จ
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.activeQuests != null)
        {
            foreach (var q in QuestUIManager.Instance.activeQuests)
            {
                if (q != null && q.minigameType == MinigameType.DeadByDaylightQTE && !q.isTaskCompleted)
                {
                    q.isTaskCompleted = true;
                    Debug.Log($"[QTEInteractable] 📜 อัปเดตเควส '{q.questTitle}' -> ภารกิจสำเร็จแล้ว!");
                }
            }
        }

        onQTESuccess?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"ทำ Minigame [{itemName}] สำเร็จเรียบร้อย!");
        }

        Debug.Log($"[QTEInteractable] 🎉 Minigame สำเร็จบนวัตถุ '{itemName}'");
    }

    private void HandleFail()
    {
        onQTEFailed?.Invoke();

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.ShowNotification($"ทำ Minigame [{itemName}] พลาด! (ล้มเหลวเกิน 2 ครั้ง)");
        }

        Debug.Log($"[QTEInteractable] ❌ Minigame พลาดบนวัตถุ '{itemName}'");
    }
}
