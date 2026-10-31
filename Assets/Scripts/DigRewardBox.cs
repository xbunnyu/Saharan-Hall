using UnityEngine;

/// <summary>
/// กล่องไอเทมหรือของรางวัลที่ขุดพบในป่า (Dig Reward Box)
/// ติดตั้งไว้ที่โมเดลกล่อง เมื่อผู้เล่นกด [E] จะเก็บกล่องเข้ากระเป๋า Inventory ทันที และกล่องจะหายไป
/// </summary>
[RequireComponent(typeof(Collider))]
public class DigRewardBox : InteractableItem
{
    [Header("ข้อมูลไอเทมรางวัล (Reward Item Info)")]
    [Tooltip("ชื่อของไอเทมที่จะเพิ่มเข้ากระเป๋าเมื่อเก็บ")]
    public string rewardName = "กล่องโบราณ";

    [Tooltip("รูปไอคอนของไอเทมในกระเป๋า (Optional)")]
    public Sprite rewardIcon;

    [TextArea(2, 5)]
    [Tooltip("คำอธิบายไอเทมเมื่อเปิดดูกระเป๋า")]
    public string rewardDescription = "กล่องไม้ลึกลับที่ถูกฝังไว้ใต้ดินในป่าลึก...";

    [Tooltip("เป็นไอเทมสำคัญ/เควสหรือไม่ (จะแสดงสัญลักษณ์พิเศษในกระเป๋า)")]
    public bool isQuestItem = true;

    [Tooltip("Prefab โมเดล 3D ตอนนำออกมาถือในมือ (Optional)")]
    public GameObject holdModelPrefab;

    [Header("เสียงและการแจ้งเตือน (Audio & Prompt)")]
    [Tooltip("ข้อความที่แสดงตอนเล็งมองกล่อง")]
    public string pickupPrompt = "เก็บกล่อง (กด E)";

    [Tooltip("เสียงตอนเก็บกล่องเข้าตัว (Optional)")]
    public AudioClip pickupSound;

    [Tooltip("ทำลาย GameObject ทิ้งเมื่อเก็บแล้ว (หรือซ่อนไว้ถ้าติ๊กออก)")]
    public bool destroyOnPickup = true;

    private bool isCollected = false;

    private void Awake()
    {
        isCollected = false;
    }

    private void OnEnable()
    {
        isCollected = false;
    }

    private void Reset()
    {
        itemName = "กล่องโบราณ";
        canRead = true;
        canCollect = true;
        interactionKeyText = "E";
        customReadPromptText = "เก็บกล่อง (กด E)";
        readTitle = "";
        readDescription = "";
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(itemName)) itemName = rewardName;
        if (string.IsNullOrEmpty(customReadPromptText)) customReadPromptText = pickupPrompt;

        canRead = true;
        canCollect = true;
        readDescription = "";
        isImportantItem = isQuestItem;
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นกด [E] เพื่อเก็บกล่อง
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        CollectReward(interactor);
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นกด [F] เพื่อเก็บกล่อง (รองรับทั้งสองปุ่ม)
    /// </summary>
    public override void OnCollect(PlayerInteraction interactor)
    {
        CollectReward(interactor);
    }

    private void CollectReward(PlayerInteraction interactor)
    {
        if (isCollected) return;
        isCollected = true;

        Debug.Log($"[DigRewardBox] 📦 ผู้เล่นเก็บกล่อง [{rewardName}] เข้ากระเป๋า!");

        // 1. เล่นเสียงเก็บของ
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // 2. สร้างข้อมูลไอเทมและเพิ่มเข้ากระเป๋าของผู้เล่น
        if (interactor != null)
        {
            InventoryItem item = new InventoryItem(
                rewardName,
                rewardIcon,
                rewardDescription,
                true,
                useSound,
                onUse,
                holdModelPrefab != null ? holdModelPrefab : worldPrefab,
                holdOffset,
                holdRotation,
                holdScale,
                isQuestItem
            );

            interactor.AddItem(item);
            interactor.ShowNotification($"📦 เก็บ [{rewardName}] เรียบร้อยแล้ว! (กด Tab เพื่อดูกระเป๋า)", 3.5f);
            interactor.CloseReading();
        }

        // 3. กล่องหายไป
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // 4. สั่งให้ผีในป่าสปอนออกมาและเริ่มไล่ล่าผู้เล่นทันที
        ForestGhostController ghost = ForestGhostController.Instance != null 
            ? ForestGhostController.Instance 
            : FindFirstObjectByType<ForestGhostController>();

        if (ghost != null)
        {
            ghost.SpawnAndChasePlayer();
        }
        else
        {
            Debug.Log("[DigRewardBox] 👻 ไม่พบ ForestGhostController ในฉาก กำลังสร้างตัวควบคุมผีให้อัตโนมัติ...");
            GameObject ghostMgr = new GameObject("ForestGhostManager");
            ghost = ghostMgr.AddComponent<ForestGhostController>();
            ghost.SpawnAndChasePlayer();
        }
    }
}
