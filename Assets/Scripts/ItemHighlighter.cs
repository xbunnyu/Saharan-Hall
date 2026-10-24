using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// เงื่อนไขการเรืองแสง
/// </summary>
public enum HighlightTriggerMode
{
    AlwaysGlow,                    // เรืองแสงตลอดเวลา (สังเกตเห็นได้จากระยะไกล)
    AlwaysSubtleAndBrightOnHover,  // เรืองแสงอ่อนๆ ตลอดเวลา และสว่างขึ้นเด่นชัดเมื่อเล็งมอง (แนะนำที่สุด)
    ProximityOnly,                 // เรืองแสงเฉพาะเมื่อผู้เล่นเดินเข้ามาใกล้
    HoverOnly                      // เรืองแสงเฉพาะเมื่อผู้เล่นเล็งเป้าใส่ไอเทม
}

/// <summary>
/// คอมโพเนนต์สำหรับสร้างระบบไฮไลท์และเรืองแสง (Highlight & Glow Effect) ให้กับวัตถุหรือไอเทมสำคัญ
/// รองรับทั้งการกระพริบเรืองแสงของโมเดล (Emission Pulse), แสงไฟอ่อนๆ ส่องกระทบพื้น (Point Light),
/// และละอองประกายแสงวิบวับ (Sparkles)
/// </summary>
[DisallowMultipleComponent]
public class ItemHighlighter : MonoBehaviour
{
    [Header("1. การตั้งค่าการเรืองแสง (Highlight Settings)")]
    [Tooltip("เปิด/ปิดระบบไฮไลท์เรืองแสง")]
    public bool isHighlightActive = true;

    [Tooltip("เงื่อนไขการทำงานของแสง")]
    public HighlightTriggerMode triggerMode = HighlightTriggerMode.AlwaysSubtleAndBrightOnHover;

    [ColorUsage(true, true)]
    [Tooltip("สีของแสงไฮไลท์ / แสงเรือง (คลิกเลือกสีและปรับแต่งเองได้อย่างอิสระ)")]
    public Color glowColor = new Color(1f, 0.85f, 0.3f, 1f);

    [Header("2. การกระพริบของโมเดล (Emission Breathing)")]
    [Tooltip("เปิดการเรืองแสงบนพื้นผิวโมเดล 3D")]
    public bool enableMeshEmission = true;

    [Range(0f, 3f)]
    [Tooltip("ความสว่างต่ำสุดตอนหายใจ")]
    public float minEmissionIntensity = 0.4f;

    [Range(0.5f, 8f)]
    [Tooltip("ความสว่างสูงสุดตอนหายใจ")]
    public float maxEmissionIntensity = 1.8f;

    [Range(0.2f, 5f)]
    [Tooltip("ความเร็วในการกระพริบหายใจ (รอบต่อวินาที)")]
    public float pulseSpeed = 1.2f;

    [Range(1f, 3f)]
    [Tooltip("ตัวคูณความสว่างเมื่อผู้เล่นหันเป้าเล็งใส่ไอเทม")]
    public float hoverBrightnessMultiplier = 1.6f;

    [Header("3. แสงไฟส่องพื้น (Point Light Aura)")]
    [Tooltip("สร้างดวงไฟอ่อนๆ ส่องกระทบพื้นหรือโต๊ะรอบไอเทม")]
    public bool enablePointLight = true;

    [Range(0.5f, 5f)]
    [Tooltip("ระยะของแสงไฟ (เมตร)")]
    public float pointLightRange = 2.0f;

    [Range(0f, 4f)]
    [Tooltip("ความสว่างต่ำสุดของแสงไฟ")]
    public float minLightIntensity = 0.5f;

    [Range(0.5f, 6f)]
    [Tooltip("ความสว่างสูงสุดของแสงไฟ")]
    public float maxLightIntensity = 1.8f;

    [Header("4. ละอองประกายแสง (Sparkles / Glint Effect)")]
    [Tooltip("เปิดประกายวิบวับลอยเบาๆ รอบไอเทม")]
    public bool enableSparkles = true;

    [Range(2, 15)]
    [Tooltip("จำนวนละอองแสงต่อวินาที")]
    public int sparkleRate = 5;

    [Header("5. ระยะการแสดงผล (Optimization / Distance)")]
    [Tooltip("ระยะสูงสุดที่จะแสดงผลการเรืองแสง (ถ้าผู้เล่นอยู่ไกลกว่านี้จะหยุดเพื่อประหยัดสเปค)")]
    public float maxEffectDistance = 18f;

    [Tooltip("ระยะที่เริ่มเรืองแสง (ใช้สำหรับโหมด ProximityOnly)")]
    public float proximityDistance = 7f;

    [Header("6. เอฟเฟกต์ลอย/หมุนเบาๆ (Optional Floating)")]
    [Tooltip("ให้ไอเทมลอยขึ้นลงเบาๆ (เหมาะกับไอเทมเวทมนตร์หรือของลอย)")]
    public bool enableGentleFloating = false;
    public float floatAmplitude = 0.05f;
    public float floatFrequency = 1.5f;

    // สถานะภายใน
    private bool isHovered = false;
    private float currentHoverFactor = 0f;
    private float currentProximityFactor = 1f;
    private Vector3 initialPosition;

    // แคช Renderers & Materials
    private List<Renderer> targetRenderers = new List<Renderer>();
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    // ออบเจ็กต์ลูกที่สร้างขึ้นมาเพื่อเอฟเฟกต์
    private Light glowLight;
    private ParticleSystem glowParticles;
    private Camera playerCam;
    private static Material sharedParticleMaterial;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        initialPosition = transform.position;
        CacheRenderers();
    }

    private void Start()
    {
        FindPlayerCamera();
        SetupPointLight();
        SetupSparkles();
    }

    private void OnEnable()
    {
        if (targetRenderers == null || targetRenderers.Count == 0)
        {
            CacheRenderers();
        }

        EnableEmissionKeywordOnMaterials();

        if (glowLight != null) glowLight.gameObject.SetActive(enablePointLight && isHighlightActive);
        if (glowParticles != null) glowParticles.gameObject.SetActive(enableSparkles && isHighlightActive);
    }

    private void OnDisable()
    {
        ResetEmission();
        if (glowLight != null) glowLight.gameObject.SetActive(false);
        if (glowParticles != null) glowParticles.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (glowLight != null) Destroy(glowLight.gameObject);
        if (glowParticles != null) Destroy(glowParticles.gameObject);
    }

    /// <summary>
    /// ค้นหา Renderers ทั้งหมดในวัตถุและลูกๆ
    /// </summary>
    public void CacheRenderers()
    {
        targetRenderers.Clear();
        var allRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var rend in allRenderers)
        {
            // ข้าม ParticleSystem และ UI
            if (rend is ParticleSystemRenderer || rend is TrailRenderer) continue;
            targetRenderers.Add(rend);
        }

        EnableEmissionKeywordOnMaterials();
    }

    /// <summary>
    /// เปิดการใช้งานคีย์เวิร์ด _EMISSION บน Material เพื่อให้ URP Lit แสดงผลแสงเรืองได้อย่างถูกต้อง
    /// </summary>
    private void EnableEmissionKeywordOnMaterials()
    {
        foreach (var rend in targetRenderers)
        {
            if (rend == null) continue;
            foreach (var mat in rend.materials)
            {
                if (mat == null) continue;
                if (!mat.IsKeywordEnabled("_EMISSION"))
                {
                    mat.EnableKeyword("_EMISSION");
                }
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
    }

    private void FindPlayerCamera()
    {
        if (playerCam == null)
        {
            playerCam = Camera.main;
            if (playerCam == null)
            {
                var pc = FindFirstObjectByType<PlayerInteraction>();
                if (pc != null && pc.playerCamera != null) playerCam = pc.playerCamera;
            }
        }
    }

    private void Update()
    {
        if (!isHighlightActive)
        {
            if (glowLight != null && glowLight.gameObject.activeSelf) glowLight.gameObject.SetActive(false);
            if (glowParticles != null && glowParticles.gameObject.activeSelf) glowParticles.gameObject.SetActive(false);
            return;
        }

        if (playerCam == null) FindPlayerCamera();

        // 1. ตรวจสอบระยะห่างจากกล้องผู้เล่น
        float distToPlayer = playerCam != null ? Vector3.Distance(transform.position, playerCam.transform.position) : 0f;
        if (distToPlayer > maxEffectDistance && playerCam != null)
        {
            // อยู่นอกระยะการแสดงผล ให้พักเอฟเฟกต์
            if (glowLight != null && glowLight.gameObject.activeSelf) glowLight.gameObject.SetActive(false);
            if (glowParticles != null && glowParticles.gameObject.activeSelf) glowParticles.gameObject.SetActive(false);
            return;
        }

        // 2. คำนวณ Proximity Factor ตาม Trigger Mode
        CalculateProximityAndHover(distToPlayer);

        // 3. ถ้าไม่มีการแสดงผลในขณะนี้ (เช่น Proximity / Hover Only แต่ยังไม่ถึงเงื่อนไข)
        if (currentProximityFactor <= 0.001f && currentHoverFactor <= 0.001f && triggerMode != HighlightTriggerMode.AlwaysGlow)
        {
            ResetEmission();
            if (glowLight != null && glowLight.gameObject.activeSelf) glowLight.gameObject.SetActive(false);
            if (glowParticles != null && glowParticles.gameObject.activeSelf) glowParticles.gameObject.SetActive(false);
            return;
        }

        // 4. คำนวณจังหวะการหายใจ (Breathing Sine Curve)
        float sinePulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f; // ค่า 0 ถึง 1

        Color activeColor = GetActiveColor();
        float hoverMultiplier = Mathf.Lerp(1.0f, hoverBrightnessMultiplier, currentHoverFactor);

        // 5. อัปเดต Emission บนโมเดล
        if (enableMeshEmission)
        {
            float targetEmissionIntensity = Mathf.Lerp(minEmissionIntensity, maxEmissionIntensity, sinePulse) * hoverMultiplier * currentProximityFactor;
            Color finalEmissionColor = activeColor * targetEmissionIntensity;

            for (int i = 0; i < targetRenderers.Count; i++)
            {
                var rend = targetRenderers[i];
                if (rend == null) continue;

                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, finalEmissionColor);
                rend.SetPropertyBlock(propBlock);
            }
        }

        // 6. อัปเดต Point Light
        if (enablePointLight && glowLight != null)
        {
            if (!glowLight.gameObject.activeSelf) glowLight.gameObject.SetActive(true);

            float targetLightIntensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, sinePulse) * hoverMultiplier * currentProximityFactor;
            glowLight.color = activeColor;
            glowLight.intensity = targetLightIntensity;
            glowLight.range = pointLightRange;
        }

        // 7. อัปเดต Particles
        if (enableSparkles && glowParticles != null)
        {
            if (!glowParticles.gameObject.activeSelf) glowParticles.gameObject.SetActive(true);

            var mainModule = glowParticles.main;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(activeColor, Color.white);
        }

        // 8. เอฟเฟกต์ลอยเบาๆ (ถ้าเปิดใช้งาน)
        if (enableGentleFloating)
        {
            float newY = initialPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
        }
    }

    private void CalculateProximityAndHover(float dist)
    {
        // อัปเดตค่า Hover Transition ให้นุ่มนวล
        float targetHover = isHovered ? 1f : 0f;
        currentHoverFactor = Mathf.MoveTowards(currentHoverFactor, targetHover, Time.deltaTime * 6f);

        switch (triggerMode)
        {
            case HighlightTriggerMode.AlwaysGlow:
                currentProximityFactor = 1f;
                break;

            case HighlightTriggerMode.AlwaysSubtleAndBrightOnHover:
                // ปกติ 0.6f เมื่อมองตรงเป้าเล็งจะกลายเป็น 1f
                currentProximityFactor = Mathf.Lerp(0.65f, 1.0f, currentHoverFactor);
                break;

            case HighlightTriggerMode.ProximityOnly:
                if (dist <= proximityDistance)
                {
                    currentProximityFactor = Mathf.Clamp01(1f - (dist / proximityDistance) * 0.4f);
                }
                else
                {
                    currentProximityFactor = 0f;
                }
                break;

            case HighlightTriggerMode.HoverOnly:
                currentProximityFactor = currentHoverFactor;
                break;
        }
    }

    /// <summary>
    /// ได้รับสีของแสงไฮไลท์
    /// </summary>
    public Color GetActiveColor()
    {
        return glowColor;
    }

    /// <summary>
    /// รีเซ็ต Emission ของทุก Renderer กลับเป็นปกติ
    /// </summary>
    private void ResetEmission()
    {
        if (targetRenderers == null) return;
        for (int i = 0; i < targetRenderers.Count; i++)
        {
            var rend = targetRenderers[i];
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorID, Color.black);
            rend.SetPropertyBlock(propBlock);
        }
    }

    /// <summary>
    /// สร้างดวงไฟ Point Light อัตโนมัติที่กึ่งกลางของวัตถุ
    /// </summary>
    private void SetupPointLight()
    {
        if (!enablePointLight) return;

        var existingLightObj = transform.Find("Glow_PointLight");
        GameObject lightObj;
        if (existingLightObj != null)
        {
            lightObj = existingLightObj.gameObject;
        }
        else
        {
            lightObj = new GameObject("Glow_PointLight");
            lightObj.transform.SetParent(transform, false);

            // วางตำแหน่งที่กึ่งกลาง Bounding Box ของวัตถุ
            Vector3 centerOffset = GetRenderersCenterLocal();
            lightObj.transform.localPosition = centerOffset;
        }

        glowLight = lightObj.GetComponent<Light>();
        if (glowLight == null)
        {
            glowLight = lightObj.AddComponent<Light>();
        }

        glowLight.type = LightType.Point;
        glowLight.color = GetActiveColor();
        glowLight.range = pointLightRange;
        glowLight.intensity = minLightIntensity;
        glowLight.shadows = LightShadows.None; // ไม่มีเงา เพื่อประสิทธิภาพสูงสุด
    }

    /// <summary>
    /// สร้างระบบละอองประกายแสง (Sparkles) อัตโนมัติ
    /// </summary>
    private void SetupSparkles()
    {
        if (!enableSparkles) return;

        var existingSparklesObj = transform.Find("Glow_Sparkles");
        GameObject pObj;
        if (existingSparklesObj != null)
        {
            pObj = existingSparklesObj.gameObject;
        }
        else
        {
            pObj = new GameObject("Glow_Sparkles");
            pObj.transform.SetParent(transform, false);
            pObj.transform.localPosition = GetRenderersCenterLocal();
        }

        glowParticles = pObj.GetComponent<ParticleSystem>();
        if (glowParticles == null)
        {
            glowParticles = pObj.AddComponent<ParticleSystem>();
        }

        var main = glowParticles.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.15f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;

        var emission = glowParticles.emission;
        emission.rateOverTime = sparkleRate;

        var shape = glowParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.2f, GetRendererBoundsSize().magnitude * 0.35f);

        var colorOverLifetime = glowParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(GetActiveColor(), 0.5f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0.9f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = glowParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.5f, 1.0f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ตั้งค่า Renderer ของ Particle ให้แสดงผลอย่างสวยงาม
        var pRenderer = pObj.GetComponent<ParticleSystemRenderer>();
        if (pRenderer != null)
        {
            pRenderer.material = GetOrCreateParticleMaterial();
        }
    }

    /// <summary>
    /// คำนวณจุดกึ่งกลางของ Renderers ในพิกัด Local
    /// </summary>
    private Vector3 GetRenderersCenterLocal()
    {
        if (targetRenderers == null || targetRenderers.Count == 0) return Vector3.up * 0.2f;

        Bounds b = targetRenderers[0].bounds;
        for (int i = 1; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != null) b.Encapsulate(targetRenderers[i].bounds);
        }

        return transform.InverseTransformPoint(b.center);
    }

    private Vector3 GetRendererBoundsSize()
    {
        if (targetRenderers == null || targetRenderers.Count == 0) return Vector3.one * 0.4f;

        Bounds b = targetRenderers[0].bounds;
        for (int i = 1; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != null) b.Encapsulate(targetRenderers[i].bounds);
        }

        return b.size;
    }

    /// <summary>
    /// สร้างหรือเรียกใช้ Material ของ Particle ที่มีความเข้ากันได้สูงสุดกับ URP
    /// </summary>
    private static Material GetOrCreateParticleMaterial()
    {
        if (sharedParticleMaterial != null) return sharedParticleMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        sharedParticleMaterial = new Material(shader);
        sharedParticleMaterial.name = "ItemGlow_Sparkle_Mat";

        // สร้าง Texture ประกายดาว/วงกลมฟุ้งๆ นุ่มนวลแบบ Runtime
        Texture2D softCircleTex = CreateSoftGlowTexture(32);
        if (sharedParticleMaterial.HasProperty("_BaseMap"))
        {
            sharedParticleMaterial.SetTexture("_BaseMap", softCircleTex);
        }
        else if (sharedParticleMaterial.HasProperty("_MainTex"))
        {
            sharedParticleMaterial.SetTexture("_MainTex", softCircleTex);
        }

        return sharedParticleMaterial;
    }

    private static Texture2D CreateSoftGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float t = Mathf.Clamp01(dist / maxDist);
                // ขอบฟุ้งเรียบเนียน (Smooth falloff)
                float alpha = Mathf.Pow(1.0f - t, 2.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    // ==========================================
    // Public API สำหรับเรียกใช้จากสคริปต์อื่น
    // ==========================================

    /// <summary>
    /// กำหนดว่าผู้เล่นกำลังเล็งเป้าใส่ไอเทมนี้หรือไม่
    /// </summary>
    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
    }

    /// <summary>
    /// เปิดหรือปิดเอฟเฟกต์ไฮไลท์
    /// </summary>
    public void SetHighlightEnabled(bool enabled)
    {
        isHighlightActive = enabled;
        if (!enabled)
        {
            ResetEmission();
            if (glowLight != null) glowLight.gameObject.SetActive(false);
            if (glowParticles != null) glowParticles.gameObject.SetActive(false);
        }
        else
        {
            if (glowLight != null) glowLight.gameObject.SetActive(enablePointLight);
            if (glowParticles != null) glowParticles.gameObject.SetActive(enableSparkles);
        }
    }

    /// <summary>
    /// ปรับตั้งค่าอัตโนมัติตาม InteractableItem
    /// </summary>
    public void ApplySettingsFromItem(InteractableItem item)
    {
        if (item == null) return;

        isHighlightActive = item.isImportantItem;
        triggerMode = item.highlightTriggerMode;
        glowColor = item.glowColor;
        enablePointLight = item.enablePointLightGlow;
        enableSparkles = item.enableSparkles;

        if (glowLight != null)
        {
            glowLight.color = glowColor;
            glowLight.gameObject.SetActive(enablePointLight && isHighlightActive);
        }

        if (glowParticles != null)
        {
            glowParticles.gameObject.SetActive(enableSparkles && isHighlightActive);
        }
    }

    /// <summary>
    /// เปลี่ยนสีไฮไลท์ตามต้องการแบบไดนามิก
    /// </summary>
    public void SetColor(Color color)
    {
        glowColor = color;
        if (glowLight != null) glowLight.color = color;
    }
}
