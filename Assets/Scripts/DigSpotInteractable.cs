using UnityEngine;

/// <summary>
/// กองดินสำหรับขุดหาของในป่า (Dig Spot Interactable)
/// วางไว้ที่กองดินแต่ละกอง เมื่อผู้เล่นเดินเข้ามากด [E] กองดินจะหายไป
/// หากกองนี้มีของรางวัลซ่อนอยู่ กล่องไอเทมจะโผล่ขึ้นมาแทนที่เพื่อให้กดเก็บต่อ
/// </summary>
[RequireComponent(typeof(Collider))]
public class DigSpotInteractable : InteractableItem
{
    [Header("โมเดลและอ็อบเจ็กต์ของกองดิน (Dig Visuals)")]
    [Tooltip("โมเดลเนินดินที่จะหายไปเมื่อขุด (ลากโมเดลดินของกองนี้มาใส่)")]
    public GameObject dirtModel;

    [Tooltip("โมเดลหลุมดิน/ร่องรอยการขุด (Optional: ถ้ามีจะแสดงขึ้นมาแทนที่เนินดิน หรือเว้นว่างไว้ก็ได้)")]
    public GameObject dugModel;

    [Tooltip("กล่องไอเทมหรือของรางวัลที่ซ่อนอยู่ข้างในกองดินนี้ (จะถูกเปิดใช้งานเมื่อขุดเจอกล่อง)")]
    public GameObject rewardBox;

    [Header("สถานะรางวัล (Reward State)")]
    [Tooltip("กองนี้มีของรางวัลซ่อนอยู่หรือไม่ (ถูกกำหนดอัตโนมัติโดย DigSpotManager)")]
    public bool hasReward = false;

    [Tooltip("ถูกขุดไปแล้วหรือไม่")]
    public bool isDug = false;

    [Header("เสียงและเอฟเฟกต์ (Audio & FX)")]
    [Tooltip("เสียงตอนขุดดิน (Optional)")]
    public AudioClip digSound;

    [Tooltip("ละอองอนุภาคดินกระจายตอนขุด (Optional)")]
    public ParticleSystem digEffect;

    [Header("ข้อความแจ้งเตือน (Notifications)")]
    public string digPromptText = "ขุดดิน (กด E)";
    public string emptyMessage = "ขุดแล้ว... ไม่พบอะไรเลย";
    public string foundMessage = "✨ ขุดพบกล่องปริศนา!";

    private Collider myCollider;

    private void Reset()
    {
        itemName = "กองดิน";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "ขุดดิน (กด E)";
        readTitle = "";
        readDescription = "";
    }

    private void Awake()
    {
        isDug = false;

        // 1. ค้นหา dirtModel อัตโนมัติหากยังไม่ได้ลากใส่ Inspector
        if (dirtModel == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("dirt") || child.name.ToLower().Contains("soil"))
                {
                    dirtModel = child.gameObject;
                    break;
                }
            }
            if (dirtModel == null && transform.childCount > 0)
            {
                dirtModel = transform.GetChild(0).gameObject;
            }
        }

        // 2. ค้นหา rewardBox อัตโนมัติหากยังไม่ได้ลากใส่ Inspector
        if (rewardBox == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("box") || child.name.ToLower().Contains("chest") || child.name.ToLower().Contains("reward"))
                {
                    rewardBox = child.gameObject;
                    break;
                }
            }
            if (rewardBox == null && transform.childCount > 1)
            {
                rewardBox = transform.GetChild(1).gameObject;
            }
        }

        // 3. ตรวจสอบให้แน่ใจว่า rewardBox มีคอมโพเนนต์ DigRewardBox สำหรับให้ผู้เล่นกด [E] เก็บได้
        if (rewardBox != null && rewardBox.GetComponent<DigRewardBox>() == null)
        {
            rewardBox.AddComponent<DigRewardBox>();
        }
    }

    private void Start()
    {
        myCollider = GetComponent<Collider>();

        if (string.IsNullOrEmpty(itemName)) itemName = "กองดิน";
        if (string.IsNullOrEmpty(customReadPromptText)) customReadPromptText = digPromptText;

        isDug = false;
        canRead = true;
        canCollect = false;
        readDescription = "";

        // ซ่อนกล่องรางวัลไว้ก่อนเสมอตอนเริ่มต้น
        if (rewardBox != null)
        {
            rewardBox.SetActive(false);
        }

        // ซ่อนโมเดลหลุมขุดไว้ก่อน
        if (dugModel != null)
        {
            dugModel.SetActive(false);
        }

        // แสดงโมเดลเนินดินเสมอตอนเริ่มเกม
        if (dirtModel != null)
        {
            dirtModel.SetActive(true);
        }

        // เปิด Collider กองดินให้พร้อมรับการขุด
        if (myCollider != null)
        {
            myCollider.enabled = true;
        }
    }

    /// <summary>
    /// กำหนดว่ากองดินนี้จะมีของรางวัลหรือไม่ (เรียกจาก DigSpotManager)
    /// </summary>
    public void SetHasReward(bool reward)
    {
        hasReward = reward;
    }

    /// <summary>
    /// เมื่อผู้เล่นกด [E] ขุดดิน
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        // ป้องกันการขุดอัตโนมัติหากเพิ่งโหลดฉากไม่ถึง 0.5 วินาที
        if (Time.timeSinceLevelLoad < 0.5f) return;

        if (isDug) return;

        isDug = true;
        canRead = false;

        Debug.Log($"[DigSpotInteractable] ⛏️ ผู้เล่นขุดกองดิน [{gameObject.name}] สำเร็จ! (hasReward: {hasReward}, เวลา: {Time.time:F2}s)");

        // 1. เล่นเสียงขุดดิน
        if (digSound != null)
        {
            AudioSource.PlayClipAtPoint(digSound, transform.position);
        }

        // 2. เล่น Particle ละอองดิน
        if (digEffect != null)
        {
            digEffect.Play();
        }

        // 3. ปิดโมเดลเนินดินให้หายไป
        if (dirtModel != null)
        {
            dirtModel.SetActive(false);
        }

        // 4. แสดงโมเดลร่องรอยการขุด (ถ้ามี)
        if (dugModel != null)
        {
            dugModel.SetActive(true);
        }

        // 5. ปิด Collider ของกองดิน เพื่อให้เป้าเล็งไม่ล็อกกองดินเดิมซ้ำ
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        // 6. ตรวจสอบว่ามีของรางวัลซ่อนอยู่หรือไม่
        if (hasReward)
        {
            if (rewardBox != null)
            {
                rewardBox.SetActive(true);
            }

            if (interactor != null)
            {
                interactor.ShowNotification(foundMessage, 3.0f);
            }
        }
        else
        {
            if (interactor != null)
            {
                interactor.ShowNotification(emptyMessage, 2.5f);
            }
        }

        // ปิดหน้าต่างอ่าน (ถ้าเปิดค้างไว้)
        if (interactor != null)
        {
            interactor.CloseReading();
        }
    }
}
