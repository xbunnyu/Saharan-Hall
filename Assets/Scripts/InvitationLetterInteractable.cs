using UnityEngine;

/// <summary>
/// วัตถุจดหมายเชิญลึกลับในฉาก (Invitation Letter Interactable)
/// - ปรากฏขึ้นบนโต๊ะในวันที่ 2 หลังจากเคลียร์เควส NPC ทั้งหมดเรียบร้อยแล้ว
/// - มีเอฟเฟกต์เรืองแสงสีทองและละอองประกาย (ItemHighlighter)
/// - เมื่อผู้เล่นกด [E] จะเปิดหน้าต่างอีเวนต์พิเศษ (Event Modal Dialog) ให้กดยอมรับคำเชิญ
/// </summary>
public class InvitationLetterInteractable : InteractableItem
{
    protected override void Awake()
    {
        base.Awake();
        InitLetterProperties();
    }

    private void Reset()
    {
        InitLetterProperties();
    }

    private void Start()
    {
        InitLetterProperties();
    }

    private void Update()
    {
        UpdatePromptState();
    }

    private void InitLetterProperties()
    {
        if (string.IsNullOrEmpty(itemName) || itemName == "ไอเทม")
        {
            itemName = "จดหมายเชิญลึกลับ";
        }

        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        readTitle = "";
        readDescription = "";

        isImportantItem = true;
        glowColor = new Color(1f, 0.85f, 0.25f, 1f);
        enablePointLightGlow = true;
        enableSparkles = true;
        showImportantBadgeInUI = true;

        UpdatePromptState();
        RefreshHighlighter();
    }

    public void UpdatePromptState()
    {
        if (ForestInvitationManager.Instance != null && ForestInvitationManager.Instance.IsInvitationAccepted)
        {
            customReadPromptText = "จดหมายเชิญ (ตอบรับแล้ว) [E]";
        }
        else
        {
            customReadPromptText = "เปิดอ่านจดหมายเชิญ [E]";
        }
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        if (interactor != null)
        {
            interactor.CloseReading();
        }

        if (ForestInvitationManager.Instance != null)
        {
            ForestInvitationManager.Instance.OpenInvitationEventUI();
        }
    }
}

