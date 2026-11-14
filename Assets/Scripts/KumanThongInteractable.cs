using UnityEngine;

/// <summary>
/// วัตถุศาลกุมารทองในฉาก (สืบทอดมาจาก InteractableItem)
/// ผู้เล่นสามารถเดินเข้ามากด [E] เพื่อเปิดหน้าต่าง UI ถวายของเซ่น (น้ำแดง)
/// เมื่อถวายสำเร็จ:
/// - โมเดลน้ำแดงบนศาลกุมารทองจะปรากฏขึ้นมา (เหมือนศาลตายาย)
/// - หักไอเทม [น้ำแดง] ออกจากกระเป๋าผู้เล่น
/// - ช่วยชำระล้างวิญญาณผีร้าย / ลดค่า Fail
/// </summary>
public class KumanThongInteractable : InteractableItem
{
    public static KumanThongInteractable Instance { get; private set; }

    [Header("1. วัตถุโมเดลของถวายบนศาล (Offering Visual)")]
    [Tooltip("โมเดลน้ำแดงที่วางรออยู่บนแท่นศาลกุมารทอง (ตอนเริ่มฉากระบบจะซ่อนให้อัตโนมัติ และจะเปิดขึ้นมาเมื่อถวายสำเร็จ เหมือนศาลตายาย)")]
    public GameObject redWaterVisualOnShrine;

    [Tooltip("จุดสำหรับวางของถวายบนศาล (Optional กรณีต้องการใช้วิธี Instantiate จาก Prefab)")]
    public Transform offeringSocket;

    [Tooltip("Prefab ของน้ำแดง (Optional กรณีต้องการใช้วิธี Instantiate ลงบน Socket)")]
    public GameObject redWaterPrefab;

    [Header("2. เงื่อนไขของถวาย (Offering Requirements)")]
    [Tooltip("ชื่อไอเทมของถวายที่ผู้เล่นต้องมีในกระเป๋า (Default: น้ำแดง)")]
    public string offeringItemName = "น้ำแดง";

    [Tooltip("หักไอเทมน้ำแดงออกจากตัวผู้เล่นเมื่อถวายสำเร็จหรือไม่")]
    public bool consumeItemOnOffer = true;

    [Tooltip("จำเป็นต้องมีไอเทมในกระเป๋าถึงจะถวายได้หรือไม่ (true = ต้องมีน้ำแดงในตัว)")]
    public bool requireItemToOffer = true;

    [Tooltip("รีเซ็ตโมเดลของถวายเมื่อขึ้นวันใหม่หรือไม่ (เพื่อให้ถวายใหม่ในวันถัดไปได้)")]
    public bool resetOfferingOnNewDay = true;

    [Header("3. แสง สี เสียง และเอฟเฟกต์ (Audio & Visual FX)")]
    [Tooltip("เสียงเมื่อถวายของสำเร็จ (เช่น เสียงกระดิ่ง, เสียงบูชา)")]
    public AudioClip offeringSuccessSound;

    [Tooltip("ไฟส่องสว่าง/แสงเรืองที่ศาลกุมารทอง (จะเปิดสว่างขึ้นเมื่อถวายของแล้ว)")]
    public Light shrineGlowLight;

    [Tooltip("ละอองแสง/เอฟเฟกต์ตอนถวายของสำเร็จ")]
    public ParticleSystem offeringParticle;

    [Header("4. สถานะปัจจุบัน (Current State)")]
    [Tooltip("ได้รับการถวายน้ำแดงแล้วหรือไม่")]
    public bool hasOffered = false;

    private GameObject spawnedVisualInstance;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        itemName = "ศาลกุมารทอง";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "กราบไหว้/ถวายของ";
        readDescription = "";
        readTitle = "";
        isImportantItem = true;

        // ซ่อนโมเดลน้ำแดงบนศาลกุมารทองไว้ก่อนตอนเริ่มเกมเสมอ (เหมือนศาลตายาย)
        if (redWaterVisualOnShrine != null && !hasOffered)
        {
            redWaterVisualOnShrine.SetActive(false);
        }

        if (shrineGlowLight != null && !hasOffered)
        {
            shrineGlowLight.enabled = false;
        }
    }

    private void OnEnable()
    {
        DayManager.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        DayManager.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int newDay)
    {
        if (resetOfferingOnNewDay)
        {
            ResetOfferingVisual();
        }
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        KumanThongUIController ui = KumanThongUIController.Instance;
        if (ui == null)
        {
            ui = FindFirstObjectByType<KumanThongUIController>();
            if (ui == null)
            {
                GameObject uiObj = new GameObject("KumanThongUIController");
                ui = uiObj.AddComponent<KumanThongUIController>();
            }
        }

        ui.OpenUI(interactor);
    }

    /// <summary>
    /// แสดงโมเดลน้ำแดงบนศาลกุมารทองเมื่อถวายสำเร็จ
    /// </summary>
    public void PerformOfferingVisual()
    {
        hasOffered = true;

        if (redWaterVisualOnShrine != null)
        {
            redWaterVisualOnShrine.SetActive(true);
        }
        else if (offeringSocket != null && redWaterPrefab != null)
        {
            if (spawnedVisualInstance == null)
            {
                spawnedVisualInstance = Instantiate(redWaterPrefab, offeringSocket.position, offeringSocket.rotation, offeringSocket);
            }
            else
            {
                spawnedVisualInstance.SetActive(true);
            }
        }

        if (shrineGlowLight != null)
        {
            shrineGlowLight.enabled = true;
        }

        if (offeringParticle != null)
        {
            offeringParticle.Play();
        }

        if (offeringSuccessSound != null)
        {
            AudioSource.PlayClipAtPoint(offeringSuccessSound, transform.position);
        }
    }

    /// <summary>
    /// ซ่อนโมเดลน้ำแดง (เมื่อขึ้นวันใหม่ เพื่อให้ถวายใหม่ได้)
    /// </summary>
    public void ResetOfferingVisual()
    {
        hasOffered = false;

        if (redWaterVisualOnShrine != null)
        {
            redWaterVisualOnShrine.SetActive(false);
        }

        if (spawnedVisualInstance != null)
        {
            spawnedVisualInstance.SetActive(false);
        }

        if (shrineGlowLight != null)
        {
            shrineGlowLight.enabled = false;
        }
    }
}
