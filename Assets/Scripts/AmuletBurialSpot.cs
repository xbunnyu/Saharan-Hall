using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// จุดทำพิธีขุดหลุมฝังเครื่องรางในป่า (Amulet Burial Spot)
/// เควสสำหรับซีนป่า (Forest 2):
/// 1. ผู้เล่นต้องจุดเทียนพิธีกรรมให้ครบทั้ง 4 จุดรอบบริเวณก่อน
/// 2. หากยังจุดเทียนไม่ครบ หรือยังไม่มีเครื่องราง จะยังไม่สามารถทำพิธีได้
/// 3. เมื่อจุดเทียนครบ 4 จุด และมีไอเทม "เครื่องราง" ในตัว กด [E] เพื่อทำพิธีฝังเครื่องรางลงดิน
/// 4. หลังจากฝังเครื่องรางสำเร็จ 5 วินาที ผีร้ายในป่าจะตื่นขึ้นมาและวิ่งไล่ล่าผู้เล่นทันที!
/// </summary>
public class AmuletBurialSpot : InteractableItem
{
    public static AmuletBurialSpot Instance { get; private set; }

    [Header("1. การเชื่อมโยงเทียนพิธีกรรม (Ritual Candles)")]
    [Tooltip("รายการเทียนพิธีกรรมทั้ง 4 เล่ม (หากเว้นว่าง ระบบจะค้นหาเทียนในฉากให้อัตโนมัติ)")]
    public List<RitualCandleInteractable> candles = new List<RitualCandleInteractable>();

    [Tooltip("จำนวนเทียนที่ต้องจุดให้ครบก่อนทำพิธีฝังได้ (Default: 4 เล่ม)")]
    public int totalCandlesRequired = 4;

    [Header("2. โมเดลและชิ้นส่วนของหลุมพิธีกรรม (Burial Visuals)")]
    [Tooltip("โมเดลหลุมดินก่อนฝัง (เช่น หลุมเปิด หรือเนินดินก่อนทำพิธี)")]
    public GameObject unburiedVisual;

    [Tooltip("โมเดลหลุมดินหลังฝังเสร็จ (เช่น เนินดินที่กลบแล้ว / ปิดผนึกแล้ว)")]
    public GameObject buriedVisual;

    [Tooltip("โมเดลเครื่องรางที่ปรากฏอยู่ในหลุม (จะแสดงขึ้นมาเมื่อทำพิธีฝัง)")]
    public GameObject amuletVisualInPit;

    [Header("3. เงื่อนไขไอเทมเครื่องราง (Amulet Requirements)")]
    [Tooltip("ชื่อไอเทมเครื่องรางในกระเป๋าผู้เล่น (Default: เครื่องราง)")]
    public string amuletItemName = "เครื่องราง";

    [Tooltip("หักไอเทมเครื่องรางออกจากตัวเมื่อทำพิธีฝังสำเร็จหรือไม่")]
    public bool consumeAmuletOnBurial = true;

    [Tooltip("มอบเครื่องรางให้ผู้เล่น 1 ชิ้นอัตโนมัติตอนเริ่มเกม หากในตัวยังไม่มี (สะดวกสำหรับทดสอบ)")]
    public bool autoGiveAmuletOnStart = true;

    [Header("4. การสปอนผีไล่ล่าหลังทำพิธี (Ghost Chase Trigger)")]
    [Tooltip("เวลาหน่วงหลังฝังเสร็จ ก่อนที่ผีจะสปอนและวิ่งไล่ตาม (วินาที ตามที่กำหนดคือ 5 วินาที)")]
    public float ghostChaseDelay = 5.0f;

    [Tooltip("สคริปต์ควบคุมผี ForestGhostController (ถ้าเว้นว่างจะค้นหา Instance ให้อัตโนมัติ)")]
    public ForestGhostController ghostController;

    [Header("5. เสียงและเอฟเฟกต์ (Audio & Visual FX)")]
    [Tooltip("เสียงเมื่อจุดเทียนครบทั้ง 4 เล่มสำเร็จ")]
    public AudioClip candleCompleteSound;

    [Tooltip("เสียงขุดดิน / กลบดินตอนฝังเครื่องราง")]
    public AudioClip burialDigSound;

    [Tooltip("เสียงสะท้อนมนต์ขลังเมื่อพิธีฝังเสร็จสมบูรณ์")]
    public AudioClip ritualCompleteSound;

    [Tooltip("ละอองเอฟเฟกต์ควันดิน/แสงมนตราตอนฝังเครื่องราง")]
    public ParticleSystem burialParticle;

    [Header("6. สถานะเควส (Quest State)")]
    [Tooltip("ทำพิธีฝังเครื่องรางเสร็จสิ้นแล้วหรือไม่")]
    public bool isBuried = false;

    [Header("Events เพิ่มเติม")]
    public UnityEvent onAllCandlesLit = new UnityEvent();
    public UnityEvent onBurialCompleted = new UnityEvent();
    public UnityEvent onGhostChaseStarted = new UnityEvent();

    protected override void Awake()
    {
        base.Awake();

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // หาก Instance เดิมเป็นเพียง dummy object ไม่มี visual แต่ตัวนี้มี visual ให้แทนที่ด้วยตัวจริงนี้
            if (Instance.unburiedVisual == null && Instance.buriedVisual == null && (unburiedVisual != null || buriedVisual != null))
            {
                Debug.LogWarning($"[AmuletBurialSpot] ⚠️ พบ Instance ซ้ำซ้อนที่ไม่มี visual [{Instance.name}] — แทนที่ด้วย [{name}] ตัวจริง!");
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"[AmuletBurialSpot] ⚠️ มี AmuletBurialSpot มากกว่า 1 ตัวในฉาก! ทำลายตัวซ้ำบน [{gameObject.name}]");
                Destroy(this);
                return;
            }
        }

        itemName = "หลุมพิธีฝังเครื่องราง";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        readDescription = ""; // direct action
        isImportantItem = true;
        glowColor = new Color(0.9f, 0.4f, 0.9f, 1f); // แสงเรืองสีม่วงมนตรา

        // ตั้งค่าโมเดลเริ่มต้น
        if (buriedVisual != null)
        {
            buriedVisual.SetActive(isBuried);
        }

        if (unburiedVisual != null)
        {
            unburiedVisual.SetActive(!isBuried);
        }

        if (amuletVisualInPit != null)
        {
            amuletVisualInPit.SetActive(isBuried);
        }

        // ตรวจสอบและตั้งค่า Collider ให้เป็น IsTrigger เสมอ เพื่อไม่ให้ขวางทางเดินผู้เล่น (ป้องกันกำแพงล่องหน)
        EnsureTriggerColliders();

        UpdatePromptText();
    }

    /// <summary>
    /// ปรับและรับประกันว่าหลุมพิธีมี Trigger Collider เสมอ เพื่อให้ผู้เล่นเดินผ่านได้ และสามารถมองเห็น/เล็งกดโต้ตอบได้ 100%
    /// </summary>
    public void EnsureTriggerColliders()
    {
        // 1. รับประกันว่าตัว Root GameObject มี BoxCollider ประจำตัวเสมอ
        BoxCollider rootBox = GetComponent<BoxCollider>();
        if (rootBox == null)
        {
            rootBox = gameObject.AddComponent<BoxCollider>();
        }
        rootBox.isTrigger = true;
        rootBox.enabled = true;

        float scaleX = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
        float scaleZ = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.z));

        // ขนาด 2.8m x 1.4m x 2.8m กว้างพอให้เล็งง่ายเมื่อยืนอยู่รอบๆ หลุมดิน
        rootBox.size = new Vector3(2.8f / scaleX, 1.4f / scaleY, 2.8f / scaleZ);
        rootBox.center = new Vector3(0f, 0.45f / scaleY, 0f);

        // 2. ปรับ Collider ในลูกๆ ทั้งหมดให้เป็น Trigger เพื่อไม่ให้กลายเป็นกำแพงล่องหน
        Collider[] allCols = GetComponentsInChildren<Collider>(true);
        foreach (var col in allCols)
        {
            if (col == rootBox) continue;
            if (col is MeshCollider meshCol)
            {
                meshCol.convex = true;
            }
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        EnsureTriggerColliders();

        // 1. ค้นหาเทียนพิธีกรรมอัตโนมัติหากยังไม่ได้ลากใส่ใน Inspector
        if (candles == null || candles.Count == 0)
        {
            candles = new List<RitualCandleInteractable>(FindObjectsByType<RitualCandleInteractable>(FindObjectsSortMode.None));
            // จัดเรียงตาม Index
            candles.Sort((a, b) => a.candleIndex.CompareTo(b.candleIndex));
        }

        // เชื่อมโยงเทียนกลับมาที่จุดฝังนี้
        foreach (var c in candles)
        {
            if (c != null && c.burialSpot == null)
            {
                c.burialSpot = this;
            }
        }

        // 2. ค้นหา ForestGhostController อัตโนมัติ
        if (ghostController == null)
        {
            ghostController = ForestGhostController.Instance ?? FindFirstObjectByType<ForestGhostController>();
        }

        // 3. มอบเครื่องรางให้ผู้เล่น 1 ชิ้นอัตโนมัติตอนเริ่มเกม (หากเปิดใช้งาน)
        if (autoGiveAmuletOnStart)
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null && !player.HasItem(amuletItemName))
            {
                GiveAmuletToPlayer();
            }
        }

        UpdatePromptText();
    }

    /// <summary>
    /// อัปเดตข้อความ Prompt บนหน้าจอตามความคืบหน้าของเควส
    /// </summary>
    public void UpdatePromptText()
    {
        if (isBuried)
        {
            customReadPromptText = "ทำพิธีเสร็จสิ้นแล้ว";
            readTitle = "หลุมฝังเครื่องราง (ฝังเรียบร้อยแล้ว)";
            return;
        }

        int litCount = GetLitCandleCount();
        if (litCount < totalCandlesRequired)
        {
            customReadPromptText = $"หลุมพิธี (ต้องจุดเทียน {litCount}/{totalCandlesRequired})";
            readTitle = $"หลุมพิธี (เทียนยังไม่ครบ: {litCount}/{totalCandlesRequired})";
        }
        else
        {
            customReadPromptText = $"ทำพิธีฝัง{amuletItemName} (กด E)";
            readTitle = "หลุมพิธี (พร้อมทำพิธีฝัง)";
        }
    }

    /// <summary>
    /// นับจำนวนเทียนที่จุดไฟแล้ว
    /// </summary>
    public int GetLitCandleCount()
    {
        int count = 0;
        foreach (var c in candles)
        {
            if (c != null && c.isLit) count++;
        }
        return count;
    }

    /// <summary>
    /// ตรวจสอบว่าเทียนถูกจุดครบทุกเล่มแล้วหรือไม่
    /// </summary>
    public bool AreAllCandlesLit()
    {
        return GetLitCandleCount() >= totalCandlesRequired;
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นจุดเทียนเล่มใดเล่มหนึ่งสำเร็จ
    /// </summary>
    public void OnCandleLit(RitualCandleInteractable candle, PlayerInteraction interactor)
    {
        int lit = GetLitCandleCount();
        UpdatePromptText();

        if (lit < totalCandlesRequired)
        {
            if (interactor != null)
            {
                interactor.ShowNotification($"🕯️ จุดเทียนสำเร็จ! (<color=#FFA500><b>{lit}/{totalCandlesRequired}</b></color> จุด)", 3.0f);
            }
        }
        else
        {
            // จุดเทียนครบทั้ง 4 เล่มแล้ว!
            if (candleCompleteSound != null)
            {
                AudioSource.PlayClipAtPoint(candleCompleteSound, transform.position);
            }

            onAllCandlesLit?.Invoke();

            if (interactor != null)
            {
                interactor.ShowNotification(
                    "🔥 <color=#FFA500><b>จุดเทียนครบทั้ง 4 ทิศแล้ว! เปลวเพลิงศักดิ์สิทธิ์ลุกโชติช่วง!</b></color>\n" +
                    $"<color=#00FF7F>ปลดล็อคพิธีกรรมแล้ว! นำ [{amuletItemName}] ไปทำพิธีฝังที่หลุมดินได้เลย!</color>",
                    5.5f
                );
            }

            Debug.Log("[AmuletBurialSpot] 🔥 จุดเทียนครบทั้ง 4 เล่มแล้ว! ปลดล็อคพิธีฝังเครื่องราง");
        }
    }

    /// <summary>
    /// เมื่อผู้เล่นกดปุ่ม [E] โต้ตอบกับหลุมฝัง
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        if (interactor == null) return;

        // 1. ตรวจสอบว่าฝังไปแล้วหรือไม่
        if (isBuried)
        {
            interactor.ShowNotification("⚱️ พิธีฝังเครื่องรางเสร็จสิ้นสมบูรณ์แล้ว พลังอำนาจถูกผนึกไว้ใต้ดิน", 3.0f);
            return;
        }

        // 2. ตรวจสอบว่าจุดเทียนครบ 4 เล่มหรือยัง
        int lit = GetLitCandleCount();
        if (lit < totalCandlesRequired)
        {
            interactor.ShowNotification(
                $"⚠️ <color=#FF8800><b>ยังไม่สามารถทำพิธีได้!</b></color>\n" +
                $"ต้องจุดเทียนพิธีกรรมให้ครบทั้ง {totalCandlesRequired} จุดรอบบริเวณก่อน! (ปัจจุบัน: <color=#FFA500>{lit}/{totalCandlesRequired}</color>)",
                4.5f
            );
            return;
        }

        // 3. ตรวจสอบว่ามีไอเทมเครื่องรางในตัวหรือไม่
        bool hasAmulet = interactor.HasItem(amuletItemName);
        if (!hasAmulet)
        {
            if (autoGiveAmuletOnStart)
            {
                interactor.AddAmuletItem();
                hasAmulet = true;
                Debug.Log($"[AmuletBurialSpot] 🎁 แอดไอเทม [{amuletItemName}] ให้ผู้เล่นอัตโนมัติเนื่องจากกำลังเริ่มพิธีฝัง");
            }
            else
            {
                interactor.ShowNotification(
                    $"⚠️ <color=#FFAA00>จุดเทียนครบแล้ว แต่คุณยังไม่มี <b>[{amuletItemName}]</b> สำหรับนำมาทำพิธีฝัง!</color>",
                    4.5f
                );
                return;
            }
        }

        // 4. เงื่อนไขครบทั้งหมด -> ทำพิธีฝังเครื่องรางทันที!
        PerformBurial(interactor);
    }

    /// <summary>
    /// ดำเนินการทำพิธีฝังเครื่องราง
    /// </summary>
    public void PerformBurial(PlayerInteraction interactor)
    {
        isBuried = true;

        // 1. หักไอเทมเครื่องรางออกจากตัวผู้เล่น
        if (consumeAmuletOnBurial && interactor != null)
        {
            interactor.RemoveItem(amuletItemName);
        }

        // 2. สลับโมเดลหลุมดิน
        if (unburiedVisual != null) unburiedVisual.SetActive(false);
        if (buriedVisual != null) buriedVisual.SetActive(true);
        if (amuletVisualInPit != null) amuletVisualInPit.SetActive(true);

        // 3. เล่นเสียงประกอบ
        if (burialDigSound != null)
        {
            AudioSource.PlayClipAtPoint(burialDigSound, transform.position);
        }

        if (ritualCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(ritualCompleteSound, transform.position);
        }

        if (burialParticle != null)
        {
            burialParticle.Play();
        }

        UpdatePromptText();
        onBurialCompleted?.Invoke();

        // 4. แจ้งเตือนผู้เล่นอย่างชัดเจน
        if (interactor != null)
        {
            interactor.ShowNotification(
                $"⚱️ <color=#FFD700><b>ทำพิธีฝัง [{amuletItemName}] ลงในผืนป่าสำเร็จแล้ว!</b></color>\n" +
                $"<color=#FF4444>⚠️ สัมผัสได้ถึงไอวิญญาณรุนแรง... มีบางสิ่งกำลังจะตื่นขึ้นใน {ghostChaseDelay:F0} วินาที!</color>",
                5.0f
            );
        }

        Debug.Log($"[AmuletBurialSpot] ⚱️ ฝัง [{amuletItemName}] สำเร็จ! กำลังนับถอยหลัง {ghostChaseDelay} วินาทีก่อนผีจะวิ่งไล่ตาม...");

        // 5. นับถอยหลัง 5 วินาที แล้วสั่งให้ผีวิ่งไล่ล่าผู้เล่น
        StartCoroutine(SpawnGhostAfterDelayRoutine(ghostChaseDelay));
    }

    /// <summary>
    /// นับถอยหลังแล้วปล่อยผีมาวิ่งไล่ล่าผู้เล่น
    /// </summary>
    private IEnumerator SpawnGhostAfterDelayRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ghostController == null)
        {
            ghostController = ForestGhostController.Instance ?? FindFirstObjectByType<ForestGhostController>();
        }

        if (ghostController != null)
        {
            ghostController.SpawnAndChasePlayer();
            onGhostChaseStarted?.Invoke();

            PlayerInteraction interactor = FindFirstObjectByType<PlayerInteraction>();
            if (interactor != null)
            {
                interactor.ShowNotification(
                    "👻 <color=#FF2222><b>วิญญาณร้ายในป่าถูกปลุกขึ้นมาแล้ว! มันกำลังพุ่งตรงมาที่คุณ! รีบวิ่งหนีเร็ว!</b></color>",
                    5.0f
                );
            }

            Debug.Log("[AmuletBurialSpot] 👻 ครบ 5 วินาทีแล้ว! ผีสปอนออกมาและเริ่มวิ่งไล่ล่าผู้เล่นทันที!");
        }
        else
        {
            Debug.LogWarning("[AmuletBurialSpot] ⚠️ ไม่พบ ForestGhostController ในฉาก! ไม่สามารถสั่งให้ผีวิ่งไล่ได้");
        }
    }

    /// <summary>
    /// มอบเครื่องรางให้ผู้เล่น 1 ชิ้น
    /// </summary>
    [ContextMenu("🎁 แอดเครื่องรางเข้าตัวผู้เล่น (Give Amulet)")]
    public void GiveAmuletToPlayer()
    {
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null)
        {
            player.AddAmuletItem();
        }
    }

    /// <summary>
    /// รีเซ็ตเควส (สำหรับเล่นรอบใหม่)
    /// </summary>
    public void ResetQuest()
    {
        isBuried = false;

        if (unburiedVisual != null) unburiedVisual.SetActive(true);
        if (buriedVisual != null) buriedVisual.SetActive(false);
        if (amuletVisualInPit != null) amuletVisualInPit.SetActive(false);

        foreach (var c in candles)
        {
            if (c != null) c.Extinguish();
        }

        UpdatePromptText();
    }
}
