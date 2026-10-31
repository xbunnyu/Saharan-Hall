using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ระบบศาลตายาย (Forest Shrine Interactable)
/// ผู้เล่นสามารถเดินเข้ามากด [E] เพื่อถวายไอเทม (เช่น ขนมไทย) แด่ศาลตายาย:
/// 1. ตอนยังไม่ถวาย: โมเดลขนมไทยบนศาลจะถูกซ่อนไว้ (SetActive = false)
/// 2. เมื่อกด E ถวาย:
///    - ตรวจสอบว่ามี "ขนมไทย" ในกระเป๋าหรือไม่
///    - หักไอเทมออกจากตัว และเปิดโมเดลขนมไทยบนศาลให้ปรากฏขึ้นมา
///    - เล่นเสียงบูชา / เอฟเฟกต์มงคล และมอบ "บารมีคุ้มครอง" ให้กับผู้เล่น
/// 3. กลไกการคุ้มครอง:
///    - หากผู้เล่นโดนผีจับตัว และกด Smash Key (Spacebar) ไม่ทัน
///    - บารมีศาลตายายจะทำงาน: ขับไล่ผีให้สลายหายไปทันที และผู้เล่นรอดชีวิต ไม่โดนส่งกลับตำหนัก
///    - จากนั้น ผีจะฟื้นคืนชีพและกลับมาไล่ล่าใหม่หลังจากผ่านไป 5 วินาที
/// </summary>
public class ForestShrineInteractable : InteractableItem
{
    public static ForestShrineInteractable Instance { get; private set; }

    [Header("1. วัตถุโมเดลของถวายบนศาล (Offering Visual)")]
    [Tooltip("โมเดลขนมไทยที่วางรออยู่บนแท่นศาลตายาย (ตอนเริ่มฉากระบบจะซ่อนให้อัตโนมัติ และจะเปิดขึ้นมาเมื่อถวายสำเร็จ)")]
    public GameObject dessertVisualOnShrine;

    [Tooltip("จุดสำหรับวางของถวายบนศาล (Optional กรณีต้องการใช้วิธี Instantiate จาก Prefab)")]
    public Transform offeringSocket;

    [Header("2. เงื่อนไขของถวาย (Offering Requirements)")]
    [Tooltip("ชื่อไอเทมของถวายที่ผู้เล่นต้องมีในกระเป๋า (Default: ขนมไทย)")]
    public string offeringItemName = "ขนมไทย";

    [Tooltip("หักไอเทมออกจากตัวผู้เล่นเมื่อถวายสำเร็จหรือไม่")]
    public bool consumeItemOnOffer = true;

    [Tooltip("มอบขนมไทยให้ผู้เล่น 1 ชิ้นอัตโนมัติตอนเริ่มเกม หากในตัวยังไม่มี")]
    public bool autoGiveDessertToPlayerOnStart = true;

    [Header("3. ระบบคุ้มครองผู้เล่นจากผีร้าย (Shrine Blessing)")]
    [Tooltip("คุ้มครองเพียง 1 ครั้งต่อการถวาย (เมื่อช่วยชีวิตสำเร็จ บัฟจะหมดลงและต้องถวายใหม่) หรือคุ้มครองตลอดไป")]
    public bool oneTimeProtection = true;

    [Tooltip("เวลาหน่วงก่อนผีจะฟื้นคืนชีพกลับมาไล่ล่าใหม่ (วินาที ตามที่กำหนดคือ 5 วินาที)")]
    public float ghostRespawnDelay = 5.0f;

    [Header("4. แสง สี เสียง และเอฟเฟกต์ (Audio & Visual FX)")]
    [Tooltip("เสียงเมื่อกดถวายของสำเร็จ (เช่น เสียงสวดมนต์, เสียงระฆัง/กระดิ่ง, เสียงฉิ่ง)")]
    public AudioClip offeringSuccessSound;

    [Tooltip("เสียงเมื่อบารมีศาลตายายทำงานช่วยชีวิตและขับไล่ผีร้าย")]
    public AudioClip blessingTriggerSound;

    [Tooltip("ไฟส่องสว่าง/แสงเรืองที่ศาล (จะเปิดสว่างขึ้นเมื่อถวายของแล้ว)")]
    public Light shrineGlowLight;

    [Tooltip("ละอองแสง/ควันธูปตอนถวายของสำเร็จ")]
    public ParticleSystem offeringParticle;

    [Tooltip("เอฟเฟกต์แสงสลายผีร้ายเมื่อบารมีทำงาน")]
    public ParticleSystem blessingDispelParticle;

    [Header("5. สถานะปัจจุบัน (Current State)")]
    [Tooltip("ได้รับการถวายของแล้วหรือไม่")]
    public bool hasOffered = false;

    [Tooltip("สถานะบัฟบารมีคุ้มครองที่พร้อมทำงาน")]
    public bool hasActiveProtection = false;

    [Header("Events เพิ่มเติม")]
    public UnityEvent onOfferingSuccess = new UnityEvent();
    public UnityEvent onBlessingTriggered = new UnityEvent();

    protected override void Awake()
    {
        base.Awake();

        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        // กำหนดค่าเริ่มต้นสำหรับ InteractableItem
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        readDescription = ""; // ตั้งเป็นค่าว่างเพื่อให้ PlayerInteraction จัดการเป็น Direct Action ทันที
        isImportantItem = true;

        // 1. ตอนยังไม่ถวาย: ให้ซ่อนโมเดลขนมไทยบนศาลไว้ก่อนเสมอ
        if (dessertVisualOnShrine != null && !hasOffered)
        {
            dessertVisualOnShrine.SetActive(false);
        }

        if (shrineGlowLight != null && !hasOffered)
        {
            shrineGlowLight.enabled = false;
        }

        // ตรวจสอบ Collider สำหรับ Raycast เล็งเป้า
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1.5f, 2f, 1.5f);
            box.center = new Vector3(0f, 1f, 0f);
        }

        UpdatePromptState();
    }

    private void Start()
    {
        UpdatePromptState();

        if (autoGiveDessertToPlayerOnStart)
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null && !player.HasItem(offeringItemName))
            {
                player.AddThaiDessertItem();
            }
        }
    }

    [ContextMenu("🎁 แอดขนมไทยเข้าตัวผู้เล่นทันที (Give Dessert To Player)")]
    public void GiveDessertToPlayer()
    {
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.AddThaiDessertItem();
        }
    }

    /// <summary>
    /// อัปเดตข้อความ Prompt บนหน้าจอตามสถานะการถวาย
    /// </summary>
    public void UpdatePromptState()
    {
        if (!hasOffered)
        {
            customReadPromptText = $"ถวาย{offeringItemName}";
            readTitle = "ศาลตายาย";
        }
        else if (hasActiveProtection)
        {
            customReadPromptText = "ได้รับการคุ้มครองแล้ว (บารมีศาลตายาย)";
            readTitle = "ศาลตายาย (มีของถวายแล้ว)";
        }
        else
        {
            customReadPromptText = $"ถวาย{offeringItemName}เพิ่มเติม";
            readTitle = "ศาลตายาย (บารมีหมดลงแล้ว)";
        }
    }

    /// <summary>
    /// ตรวจสอบว่ามีบัฟบารมีคุ้มครองพร้อมช่วยชีวิตหรือไม่
    /// </summary>
    public bool HasActiveProtection()
    {
        return hasActiveProtection;
    }

    /// <summary>
    /// เมื่อผู้เล่นกดปุ่ม [E] โต้ตอบกับศาลตายาย
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        if (interactor == null) return;

        // 1. หากยังคงมีบัฟคุ้มครองที่ยังไม่ได้ใช้งาน
        if (hasOffered && hasActiveProtection)
        {
            interactor.ShowNotification("✨ <color=#FFD700><b>ศาลตายายได้รับการถวายของแล้ว และบารมีกำลังคุ้มครองคุณอยู่!</b></color>", 3.5f);
            return;
        }

        // 2. ตรวจสอบว่าผู้เล่นมีไอเทมของถวายในกระเป๋าหรือไม่
        bool hasItem = interactor.HasItem(offeringItemName);

        if (!hasItem)
        {
            // ยังไม่มีของถวาย
            interactor.ShowNotification($"⚠️ <color=#FFAA00>ต้องการ <b>[{offeringItemName}]</b> เพื่อนำมาถวายแด่ศาลตายาย!</color>", 4.0f);
            return;
        }

        // 3. ดำเนินการถวายของ
        PerformOffering(interactor);
    }

    /// <summary>
    /// ประมวลผลการถวายของสำเร็จ
    /// </summary>
    public void PerformOffering(PlayerInteraction interactor)
    {
        // 1. หักไอเทมออกจากกระเป๋าผู้เล่น
        if (consumeItemOnOffer && interactor != null)
        {
            interactor.RemoveItem(offeringItemName);
        }

        // 2. แสดงโมเดลขนมไทยบนศาลตายาย
        if (dessertVisualOnShrine != null)
        {
            dessertVisualOnShrine.SetActive(true);
        }

        // 3. เปิดไฟเรืองแสงที่ศาล
        if (shrineGlowLight != null)
        {
            shrineGlowLight.enabled = true;
        }

        // 4. เล่นเสียงและละอองเอฟเฟกต์
        if (offeringSuccessSound != null)
        {
            AudioSource.PlayClipAtPoint(offeringSuccessSound, transform.position);
        }

        if (offeringParticle != null)
        {
            offeringParticle.Play();
        }

        // 5. อัปเดตสถานะบารมีคุ้มครอง
        hasOffered = true;
        hasActiveProtection = true;
        UpdatePromptState();

        onOfferingSuccess?.Invoke();

        // 6. แจ้งเตือนผู้เล่นบนหน้าจอ
        if (interactor != null)
        {
            interactor.ShowNotification(
                $"✨ <color=#FFD700><b>ถวาย [{offeringItemName}] แด่ศาลตายายเรียบร้อยแล้ว!</b></color>\n" +
                "<color=#00FF7F>คุณได้รับบารมีคุ้มครอง! หากพลาดท่าให้แก่ผีร้าย บารมีจะช่วยสลายผีให้ชั่วคราว!</color>",
                5.0f
            );
        }

        Debug.Log($"[ForestShrineInteractable] 🙏 ถวาย [{offeringItemName}] สำเร็จ! ผู้เล่นได้รับบารมีคุ้มครอง");
    }

    /// <summary>
    /// บารมีศาลตายายทำงานเมื่อผู้เล่นกด Smash Key พลาด
    /// ขับไล่ผีร้ายให้สลายไป ปลดล็อคผู้เล่น และสั่งให้ผีเกิดใหม่ในอีก 5 วินาที
    /// </summary>
    public void TriggerProtection(ForestGhostController ghost)
    {
        if (!hasActiveProtection) return;

        // 1. หักบัฟหากตั้งค่าให้คุ้มครองครั้งเดียว
        if (oneTimeProtection)
        {
            hasActiveProtection = false;
        }
        UpdatePromptState();

        // 2. เล่นเสียงและเอฟเฟกต์ขับไล่ผี
        Vector3 effectPos = ghost != null ? ghost.transform.position : transform.position;

        if (blessingTriggerSound != null)
        {
            AudioSource.PlayClipAtPoint(blessingTriggerSound, effectPos);
        }

        if (blessingDispelParticle != null)
        {
            blessingDispelParticle.transform.position = effectPos;
            blessingDispelParticle.Play();
        }

        // 3. แจ้งเตือนผู้เล่นบนหน้าจอด้วยข้อความมงคลที่เด่นชัด
        PlayerInteraction interactor = FindFirstObjectByType<PlayerInteraction>();
        if (interactor != null)
        {
            interactor.ShowNotification(
                "✨ <color=#FFD700><b>บารมีศาลตายายช่วยคุ้มครองคุณไว้! ผีร้ายสลายไปชั่วคราว!</b></color>\n" +
                $"<color=#FF5555>⚠️ ระวัง! ผีร้ายจะฟื้นคืนชีพใน {ghostRespawnDelay:F0} วินาที! รีบวิ่งไปที่รถเร็ว!</color>",
                5.5f
            );
        }

        onBlessingTriggered?.Invoke();

        // 4. สั่งให้ผีสลายตัว และนับถอยหลังเกิดใหม่ตามที่กำหนด (5 วินาที)
        if (ghost != null)
        {
            ghost.DispelAndRespawnAfter(ghostRespawnDelay);
        }

        Debug.Log("[ForestShrineInteractable] 🛡️ บารมีศาลตายายช่วยชีวิตผู้เล่นสำเร็จ! ผีสลายตัวและจะเกิดใหม่ในอีก 5 วินาที");
    }

    /// <summary>
    /// รีเซ็ตสถานะศาลตายาย (กรณีเริ่มรอบใหม่ในป่า)
    /// </summary>
    public void ResetShrine()
    {
        hasOffered = false;
        hasActiveProtection = false;

        if (dessertVisualOnShrine != null)
        {
            dessertVisualOnShrine.SetActive(false);
        }

        if (shrineGlowLight != null)
        {
            shrineGlowLight.enabled = false;
        }

        UpdatePromptState();
    }
}
