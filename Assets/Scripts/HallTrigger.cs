using UnityEngine;

public class HallTrigger : InteractableItem
{
    [Header("Hall Trigger Settings")]
    [Tooltip("ข้อความเมื่อตำหนักยังปิดอยู่")]
    public string openPromptText = "กดเพื่อเปิดตำหนัก (เริ่มต้อนรับผู้มาเยือน)";
    [Tooltip("ข้อความเมื่อตำหนักเปิดอยู่")]
    public string closePromptText = "กดเพื่อปิดตำหนัก (หยุดรับผู้มาเยือน)";

    public AudioClip bellSound;

    void Start()
    {
        canRead = true;
        canCollect = false;
        UpdateTriggerInfo();
    }

    void Update()
    {
        UpdateTriggerInfo();
    }

    private void UpdateTriggerInfo()
    {
        if (HallManager.Instance != null)
        {
            int target = HallManager.Instance.maxNpcPerSession > 0 ? HallManager.Instance.maxNpcPerSession : 3;
            int current = HallManager.Instance.npcsServedThisSession;

            if (HallManager.Instance.isHallOpen)
            {
                bool canClose = current >= target;
                itemName = $"แท่นควบคุมตำหนัก [เปิดอยู่: {current}/{target} คน]";
                customReadPromptText = canClose ? "กดเพื่อปิดตำหนัก" : $"ต้องรับแขกให้ครบ ({current}/{target} คน)";
                readTitle = $"สถานะตำหนัก: เปิดทำการ ({current}/{target} คน)";
                readDescription = canClose ? closePromptText : $"ยังปิดไม่ได้! ต้องให้บริการผู้มาเยือนให้ครบ {target} คนก่อน (ขณะนี้ {current}/{target})";
            }
            else
            {
                itemName = "แท่นควบคุมตำหนัก [ปิดอยู่]";
                customReadPromptText = "กดเพื่อเปิดตำหนัก";
                readTitle = "สถานะตำหนัก: ปิดทำการ";
                readDescription = openPromptText;
            }
        }
        else
        {
            itemName = "แท่นควบคุมตำหนัก";
            customReadPromptText = "เปิด/ปิดตำหนัก";
        }
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        // เมื่อกด E ที่แท่น ให้สลับสถานะเปิด/ปิดตำหนัก
        if (bellSound != null)
        {
            AudioSource.PlayClipAtPoint(bellSound, transform.position);
        }

        if (HallManager.Instance != null)
        {
            HallManager.Instance.ToggleHall();
        }
        else
        {
            Debug.LogWarning("[HallTrigger] ⚠️ ไม่พบ HallManager ในฉาก!");
        }

        interactor.CloseReading();
    }
}
