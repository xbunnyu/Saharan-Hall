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
            if (HallManager.Instance.isHallOpen)
            {
                itemName = "แท่นควบคุมตำหนัก [เปิดอยู่]";
                customReadPromptText = "กดเพื่อปิดตำหนัก";
                readTitle = "สถานะตำหนัก: เปิดทำการ";
                readDescription = closePromptText;
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
