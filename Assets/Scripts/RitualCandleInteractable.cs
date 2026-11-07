using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// เทียนพิธีกรรมสำหรับเควสฝังเครื่องราง (Ritual Candle Interactable)
/// ผู้เล่นสามารถเดินเข้ามากด [E] เพื่อจุดเทียน
/// เมื่อจุดครบทั้ง 4 เล่ม จะปลดล็อคการทำพิธีฝังเครื่องรางลงในหลุมดิน
/// </summary>
public class RitualCandleInteractable : InteractableItem
{
    [Header("1. สถานะเทียน (Candle State)")]
    [Tooltip("เทียนเล่มนี้ถูกจุดไฟแล้วหรือไม่")]
    public bool isLit = false;

    [Tooltip("ลำดับของเทียน (เช่น 1, 2, 3, 4 เพื่อความสะดวกในการตรวจสอบ)")]
    public int candleIndex = 1;

    [Header("2. โมเดลและแสงไฟเปลวเทียน (Visuals & Light)")]
    [Tooltip("GameObject เปลวไฟหรือ ParticleSystem ไฟเทียน (จะเปิดเมื่อจุดสำเร็จ)")]
    public GameObject flameVisual;

    [Tooltip("ดวงไฟ Point Light ของเทียน (จะเปิดและกะพริบไหวเมื่อจุดสำเร็จ)")]
    public Light candleLight;

    [Tooltip("ความสว่างสูงสุดของไฟเทียน")]
    public float maxLightIntensity = 1.8f;

    [Tooltip("เปิดเอฟเฟกต์ไฟกะพริบไหวสมจริง (Flicker Effect) หรือไม่")]
    public bool enableFlicker = true;

    [Header("3. เสียงประกอบ (Audio)")]
    [Tooltip("เสียงตอนจุดเทียน (เช่น เสียงไม้ขีด / เสียงพรึ่บ)")]
    public AudioClip lightSound;

    [Header("4. การเชื่อมโยงกับจุดทำพิธีฝัง (Burial Spot Link)")]
    [Tooltip("หลุมพิธีกรรมฝังเครื่องราง (ถ้าเว้นว่าง ระบบจะค้นหา AmuletBurialSpot ให้อัตโนมัติ)")]
    public AmuletBurialSpot burialSpot;

    [Header("Events เพิ่มเติม")]
    public UnityEvent onLit = new UnityEvent();

    private float baseIntensity;
    private float flickerNoiseOffset;

    protected override void Awake()
    {
        base.Awake();

        itemName = "เทียนพิธีกรรม";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        readDescription = ""; // direct action
        isImportantItem = true;
        glowColor = new Color(1f, 0.6f, 0.1f, 1f);

        flickerNoiseOffset = Random.Range(0f, 100f);

        // ค้นหา flameVisual อัตโนมัติหากยังไม่ได้กำหนด
        if (flameVisual == null)
        {
            Transform found = transform.Find("Flame") ?? transform.Find("Fire") ?? transform.Find("flame") ?? transform.Find("Particle");
            if (found != null) flameVisual = found.gameObject;
        }

        // ค้นหาหรือสร้าง candleLight อัตโนมัติหากยังไม่ได้กำหนด
        if (candleLight == null)
        {
            candleLight = GetComponentInChildren<Light>();
            if (candleLight == null)
            {
                GameObject lightObj = new GameObject("CandlePointLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                candleLight = lightObj.AddComponent<Light>();
                candleLight.type = LightType.Point;
                candleLight.color = new Color(1f, 0.65f, 0.25f);
                candleLight.range = 3.5f;
                candleLight.intensity = maxLightIntensity;
                candleLight.shadows = LightShadows.Soft;
            }
        }

        if (candleLight != null)
        {
            baseIntensity = maxLightIntensity;
        }

        // ตรวจสอบและตั้งค่า Collider ให้เป็น IsTrigger เสมอ เพื่อไม่ให้ขวางทางเดินผู้เล่น (ป้องกันกำแพงล่องหน)
        EnsureTriggerColliders();

        // ตั้งสถานะเริ่มต้นของไฟเทียน
        ApplyLitState(isLit, false);
    }

    /// <summary>
    /// ปรับ Collider ทั้งหมดของเทียนให้เป็น IsTrigger เพื่อให้เดินผ่านได้ ไม่ชนเป็นกำแพงล่องหน
    /// </summary>
    public void EnsureTriggerColliders()
    {
        Collider[] allCols = GetComponentsInChildren<Collider>(true);
        if (allCols.Length > 0)
        {
            foreach (var col in allCols)
            {
                if (col is MeshCollider meshCol)
                {
                    meshCol.convex = true;
                }
                col.isTrigger = true;
            }
        }
        else
        {
            CapsuleCollider cap = gameObject.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;

            // ชดเชย scale ของโมเดลเพื่อไม่ให้ collider พองโตผิดขนาด
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
            float scaleXZ = Mathf.Max(0.001f, Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)));
            cap.height = 0.8f / scaleY;
            cap.radius = 0.25f / scaleXZ;
            cap.center = new Vector3(0f, 0.4f / scaleY, 0f);
        }
    }

    private void Start()
    {
        EnsureTriggerColliders();

        if (burialSpot == null)
        {
            burialSpot = FindFirstObjectByType<AmuletBurialSpot>();
        }
        UpdatePromptText();
    }

    private void Update()
    {
        // เอฟเฟกต์ไฟกะพริบไหวอ่อนๆ อย่างเป็นธรรมชาติ
        if (isLit && enableFlicker && candleLight != null && candleLight.enabled)
        {
            float noise = Mathf.PerlinNoise((Time.time * 4f) + flickerNoiseOffset, 0f);
            candleLight.intensity = baseIntensity * Mathf.Lerp(0.82f, 1.18f, noise);
        }
    }

    /// <summary>
    /// อัปเดตข้อความ Prompt แสดงบนหน้าจอ
    /// </summary>
    public void UpdatePromptText()
    {
        if (!isLit)
        {
            customReadPromptText = "จุดเทียนพิธีกรรม (กด E)";
            readTitle = "เทียนพิธีกรรม (ดับอยู่)";
        }
        else
        {
            customReadPromptText = "เทียนถูกจุดแล้ว";
            readTitle = "เทียนพิธีกรรม (สว่างไสว)";
        }
    }

    /// <summary>
    /// เมื่อผู้เล่นกดปุ่ม [E] โต้ตอบกับเทียน
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        if (interactor == null) return;

        if (isLit)
        {
            interactor.ShowNotification("🕯️ <color=#FFAA00>เทียนเล่มนี้ถูกจุดเรียบร้อยแล้ว เปลวไฟกำลังลุกโชติช่วง</color>", 2.5f);
            return;
        }

        // ดำเนินการจุดเทียน
        LightCandle(interactor);
    }

    /// <summary>
    /// ทำการจุดไฟที่เทียนเล่มนี้
    /// </summary>
    public void LightCandle(PlayerInteraction interactor)
    {
        if (isLit) return;

        isLit = true;
        ApplyLitState(true, true);
        UpdatePromptText();

        onLit?.Invoke();

        // แจ้งไปยังศูนย์กลางพิธีฝังเครื่องราง
        if (burialSpot != null)
        {
            burialSpot.OnCandleLit(this, interactor);
        }
        else if (AmuletBurialSpot.Instance != null)
        {
            AmuletBurialSpot.Instance.OnCandleLit(this, interactor);
        }
        else if (interactor != null)
        {
            interactor.ShowNotification("🕯️ จุดเทียนพิธีกรรมสำเร็จ!", 3.0f);
        }

        Debug.Log($"[RitualCandleInteractable] 🕯️ จุดเทียนเล่มที่ [{candleIndex}] สำเร็จ!");
    }

    /// <summary>
    /// ปรับการแสดงผลของเปลวไฟและแสง
    /// </summary>
    public void ApplyLitState(bool lit, bool playSound)
    {
        isLit = lit;

        if (flameVisual != null)
        {
            flameVisual.SetActive(lit);
        }

        if (candleLight != null)
        {
            candleLight.enabled = lit;
        }

        if (lit && playSound && lightSound != null)
        {
            AudioSource.PlayClipAtPoint(lightSound, transform.position);
        }
    }

    /// <summary>
    /// ดับเทียน (สำหรับรีเซ็ตฉาก)
    /// </summary>
    public void Extinguish()
    {
        ApplyLitState(false, false);
        UpdatePromptText();
    }
}
