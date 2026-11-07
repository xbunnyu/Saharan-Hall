using UnityEngine;

/// <summary>
/// วางบนโมเดลรถในซีน GamePlay (ขาไปป่า)
/// - ตรวจสอบเงื่อนไข: ต้องได้รับและตอบรับจดหมายเชิญ (ForestInvitationManager) ในวันที่ 2 ก่อนจึงจะสามารถเดินทางได้
/// - เมื่อผู้เล่นกด [E] จะส่งคำสั่งไปยัง CarSceneFader เพื่อเล่นเสียง -> ล็อกผู้เล่น -> เฟดจอดำ -> โหลดซีน Forest -> ย้ายผู้เล่นไปยังจุดเกิดในป่า -> เฟดสว่าง
/// </summary>
public class CarToForestInteractable : InteractableItem
{
    [Header("Car Travel Settings (ขาไปป่า)")]
    [Tooltip("ชื่อซีนปลายทาง (เช่น Forest)")]
    public string targetSceneName = "Forest";

    [Tooltip("ชื่อ ID ของจุดเกิดในซีน Forest ที่ต้องการให้ผู้เล่นไปยืน (เช่น 'ForestCarSpawn')")]
    public string targetSpawnPointName = "ForestCarSpawn";

    [Tooltip("เวลาที่ใช้ในการเฟดจอดำ (วินาที)")]
    public float fadeDuration = 0.8f;

    [Tooltip("เสียงสตาร์ทรถ / เสียงขับรถ (Optional)")]
    public AudioClip carSound;

    [Header("Invitation Requirement (เงื่อนไขจดหมายเชิญ)")]
    [Tooltip("ต้องได้รับและกดยอมรับจดหมายเชิญในวันที่ 2 ก่อนจึงจะสามารถขับรถไปป่าได้")]
    public bool requireInvitationAccepted = true;

    private void Reset()
    {
        itemName = "รถยนต์";
        canRead = true;
        canCollect = false;
        customReadPromptText = "ขับรถไปป่า (กด E)";
        readTitle = "";
        readDescription = "";
        requireInvitationAccepted = true;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(itemName)) itemName = "รถยนต์";
        UpdatePromptState();
    }

    private void Update()
    {
        UpdatePromptState();
    }

    public void UpdatePromptState()
    {
        if (requireInvitationAccepted && ForestInvitationManager.Instance != null && !ForestInvitationManager.Instance.IsInvitationAccepted)
        {
            customReadPromptText = "🔒 รถยนต์ (ต้องตอบรับจดหมายเชิญก่อน)";
        }
        else
        {
            customReadPromptText = "🚗 ขับรถไปป่า (กด E)";
        }
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        if (CarSceneFader.Instance.IsTransitioning) return;

        // ตรวจสอบเงื่อนไขการตอบรับจดหมายเชิญ
        if (requireInvitationAccepted && ForestInvitationManager.Instance != null && !ForestInvitationManager.Instance.IsInvitationAccepted)
        {
            string lockReason = ForestInvitationManager.Instance.GetCarLockReason();
            if (string.IsNullOrEmpty(lockReason))
            {
                lockReason = "<color=#FF4500>⚠️ ยังไม่สามารถออกเดินทางได้!</color>\nต้องได้รับและตอบรับจดหมายเชิญไปป่าก่อน";
            }

            if (interactor != null)
            {
                interactor.ShowNotification(lockReason, 4.0f);
            }
            else if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(lockReason, 4.0f);
            }
            return;
        }

        if (interactor != null)
        {
            interactor.CloseReading();
        }

        // ส่งคำสั่งให้ CarSceneFader รัน Coroutine ข้ามซีน (เพื่อไม่ให้ถูกทำลายเมื่อ GamePlay ถูก Unload)
        CarSceneFader.Instance.Travel(targetSceneName, targetSpawnPointName, fadeDuration, carSound, transform.position);
    }
}
