using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ให้รูป 2D ในฉาก (ผ้ายันต์ / กระดาษยันต์ / ต้นไม้ / พรม ที่ใช้ Sprite-Unlit) มืด-สว่างตามแสงจริง
/// เดิม Sprite-Unlit ไม่สนแสงเลย → ห้องมืดแต่ยันต์สว่างโร่
/// วิธี: คำนวณแสงที่ตกถึงรูปแต่ละใบ (แสงรอบข้าง + แสงอาทิตย์ที่ไม่ถูกบัง + เทียน/ไฟจุดใกล้ๆ ตามสูตร URP)
///       แล้วย้อมสีรูปตามนั้น ไม่ต้องเปลี่ยน shader หรือแก้ฉาก — ตอนค่ำ (EveningAtmosphere) ก็มืดตามเอง
/// สร้างตัวเองอัตโนมัติทุกฉาก ยกเว้น MainMenu
/// </summary>
public class SpriteLighting : MonoBehaviour
{
    [Tooltip("คำนวณแสงใหม่ทุกกี่วินาที (เทียนไหวก็ไหวตาม)")]
    public float updateInterval = 0.08f;
    [Tooltip("คูณความสว่างรวม (ถ้ารูปมืด/สว่างกว่าของรอบข้าง ปรับตรงนี้)")]
    [Range(0.2f, 3f)] public float brightness = 1f;
    [Tooltip("สว่างสุดได้เท่าไร (1 = สีเดิมของรูป)")]
    [Range(0.2f, 1.5f)] public float maxBrightness = 1f;
    [Tooltip("เช็กว่าแสงอาทิตย์ถูกผนัง/หลังคาบังไหม (รูปในบ้านจะไม่โดนแดด)")]
    public bool sunShadowCheck = true;

    [Header("เรืองแสงตอนเล็ง (รูป 2D ใช้ Emission แบบของ 3D ไม่ได้ → เติมสีแทน)")]
    public bool hoverGlow = true;
    public Color hoverGlowColor = new Color(1f, 0.62f, 0.25f, 1f); // สีเทียน เหมือนของ 3D ชิ้นอื่น
    [Range(0f, 2f)] public float hoverGlowMin = 0.45f;
    [Range(0f, 2f)] public float hoverGlowMax = 1f;
    public float hoverPulseSpeed = 0.8f;
    [Tooltip("ความแรงของชั้นแสงที่บวกทับรูป (ยิ่งมากยิ่งสว่าง เกินสีจริงของรูปได้)")]
    [Range(0f, 4f)] public float glowLayerIntensity = 1.8f;

    struct Entry { public SpriteRenderer sr; public Color baseColor; public bool sunBlocked; public float hover; public SpriteRenderer glow; }
    static Material glowMaterial;
    InteractableItem hoveredItem;
    Bounds hoveredBounds;
    bool anyHover;
    readonly List<Entry> sprites = new List<Entry>();
    Light[] lights = new Light[0];
    float nextUpdate, nextLightScan, nextShadowCheck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure(SceneManager.GetActiveScene());
    }
    static void OnSceneLoaded(Scene s, LoadSceneMode m) { Ensure(s); }
    static void Ensure(Scene s)
    {
        if (s.name == "MainMenu") return;
        if (FindFirstObjectByType<SpriteLighting>(FindObjectsInactive.Include) != null) return;
        new GameObject("[SpriteLighting]").AddComponent<SpriteLighting>();
    }

    void Start()
    {
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mat = sr.sharedMaterial;
            if (mat == null || mat.shader == null) continue;
            // เฉพาะรูปที่ไม่สนแสง (Unlit) — ถ้ามีรูปที่ใช้ shader รับแสงอยู่แล้วจะไม่ยุ่ง
            if (!mat.shader.name.Contains("Unlit") && !mat.name.Contains("Unlit")) continue;
            if (sr.GetComponentInParent<Canvas>() != null) continue;
            sprites.Add(new Entry { sr = sr, baseColor = sr.color });
        }
        ScanLights();
        CheckSunShadows();
        Apply();
    }

    void OnDestroy()
    {
        foreach (var e in sprites) if (e.sr != null) e.sr.color = e.baseColor;
    }

    void ScanLights() { lights = FindObjectsByType<Light>(FindObjectsSortMode.None); }

    void CheckSunShadows()
    {
        if (!sunShadowCheck) return;
        Light sun = null;
        foreach (var l in lights) if (l != null && l.type == LightType.Directional && l.isActiveAndEnabled && (sun == null || l.intensity > sun.intensity)) sun = l;
        for (int i = 0; i < sprites.Count; i++)
        {
            var e = sprites[i];
            if (e.sr == null || sun == null) continue;
            Vector3 p = e.sr.bounds.center;
            e.sunBlocked = Physics.Raycast(p - sun.transform.forward * 0.05f, -sun.transform.forward, 80f, ~0, QueryTriggerInteraction.Ignore);
            sprites[i] = e;
        }
    }

    void Update()
    {
        if (Time.time >= nextLightScan) { nextLightScan = Time.time + 2f; ScanLights(); }
        if (Time.time >= nextShadowCheck) { nextShadowCheck = Time.time + 1f; CheckSunShadows(); }
        UpdateHovered();
        // ตอนมีรูปกำลังเรืองแสง อัปเดตทุกเฟรมให้การหายใจลื่น
        if (Time.time < nextUpdate && !anyHover) return;
        nextUpdate = Time.time + updateInterval;
        Apply();
    }

    /// <summary>ของที่ผู้เล่นเล็งอยู่ (ป้าย [E] ขึ้น) → รูป 2D ที่เป็นของชิ้นนั้น หรือซ้อนอยู่ในกล่องชนของมัน จะเรืองแสง</summary>
    void UpdateHovered()
    {
        InteractableItem item = null;
        var ui = InteractionUIManager.Instance;
        if (hoverGlow && ui != null && ui.promptPanel != null && ui.promptPanel.activeInHierarchy) item = ui.CurrentPromptItem;
        if (item != hoveredItem)
        {
            hoveredItem = item;
            if (item != null)
            {
                var cols = item.GetComponentsInChildren<Collider>();
                hoveredBounds = cols.Length > 0 ? cols[0].bounds : new Bounds(item.transform.position, Vector3.one * 0.3f);
                for (int i = 1; i < cols.Length; i++) hoveredBounds.Encapsulate(cols[i].bounds);
            }
        }
        anyHover = false;
        float dt = Time.deltaTime;
        for (int i = 0; i < sprites.Count; i++)
        {
            var e = sprites[i];
            if (e.sr == null) continue;
            bool on = item != null &&
                      (e.sr.GetComponentInParent<InteractableItem>() == item ||
                       (hoveredBounds.Intersects(e.sr.bounds) && e.sr.bounds.size.magnitude < 3f));
            e.hover = Mathf.MoveTowards(e.hover, on ? 1f : 0f, dt * 6f);
            if (e.hover > 0f) anyHover = true;
            sprites[i] = e;
        }
    }

    /// <summary>
    /// ชั้นแสงเรือง: สำเนารูปเดียวกันวางทับ ใช้ shader แบบบวกแสง (Saharan/SpriteAdditiveGlow)
    /// → สว่างเกินสีจริงของรูปได้ (สีของรูป 2D ปกติสว่างได้ไม่เกินรูปต้นฉบับ)
    /// </summary>
    void UpdateGlowLayer(ref Entry e, float k)
    {
        if (k <= 0.001f)
        {
            if (e.glow != null && e.glow.enabled) e.glow.enabled = false;
            return;
        }
        if (glowMaterial == null)
        {
            var sh = Resources.Load<Shader>("Shaders/SpriteAdditiveGlow");
            if (sh == null) return; // ไม่มี shader → ใช้แค่การย้อมสีอย่างเดียว
            glowMaterial = new Material(sh) { name = "SpriteHoverGlow" };
        }
        if (e.glow == null)
        {
            var go = new GameObject("HoverGlow");
            go.transform.SetParent(e.sr.transform, false);
            e.glow = go.AddComponent<SpriteRenderer>();
            e.glow.sharedMaterial = glowMaterial;
        }
        var g = e.glow;
        g.sprite = e.sr.sprite;
        g.flipX = e.sr.flipX; g.flipY = e.sr.flipY;
        g.drawMode = e.sr.drawMode;
        if (e.sr.drawMode != SpriteDrawMode.Simple) g.size = e.sr.size;
        g.sortingLayerID = e.sr.sortingLayerID;
        g.sortingOrder = e.sr.sortingOrder + 1;
        glowMaterial.SetFloat("_Intensity", glowLayerIntensity);
        g.color = new Color(hoverGlowColor.r, hoverGlowColor.g, hoverGlowColor.b, Mathf.Clamp01(k));
        g.enabled = true;
    }

    Color Ambient()
    {
        switch (RenderSettings.ambientMode)
        {
            case UnityEngine.Rendering.AmbientMode.Trilight: return RenderSettings.ambientEquatorColor.linear;
            case UnityEngine.Rendering.AmbientMode.Flat: return RenderSettings.ambientLight.linear;
            default: return RenderSettings.ambientSkyColor.linear * RenderSettings.ambientIntensity;
        }
    }

    void Apply()
    {
        Color amb = Ambient();
        for (int i = 0; i < sprites.Count; i++)
        {
            var e = sprites[i];
            if (e.sr == null || !e.sr.enabled) continue;
            Vector3 p = e.sr.bounds.center;
            Vector3 n = e.sr.transform.forward; // รูปมองได้สองด้าน → ใช้ค่าสัมบูรณ์

            Color sum = amb;
            foreach (var l in lights)
            {
                if (l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
                Color lc = l.color.linear * l.intensity;
                if (l.type == LightType.Directional)
                {
                    if (e.sunBlocked) continue;
                    sum += lc * Mathf.Abs(Vector3.Dot(n, l.transform.forward));
                }
                else if (l.type == LightType.Point || l.type == LightType.Spot)
                {
                    Vector3 toLight = l.transform.position - p;
                    float d2 = toLight.sqrMagnitude;
                    float r2 = l.range * l.range;
                    if (d2 >= r2) continue;
                    // สูตรจางตามระยะของ URP: 1/d² * (1 - (d²/r²)²)²
                    float f = 1f - (d2 / r2) * (d2 / r2);
                    float atten = (f * f) / Mathf.Max(d2, 0.0001f);
                    Vector3 dir = toLight / Mathf.Sqrt(Mathf.Max(d2, 1e-6f));
                    if (l.type == LightType.Spot)
                    {
                        float cos = Vector3.Dot(-dir, l.transform.forward);
                        float outer = Mathf.Cos(l.spotAngle * 0.5f * Mathf.Deg2Rad);
                        float inner = Mathf.Cos(l.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
                        atten *= Mathf.Clamp01((cos - outer) / Mathf.Max(inner - outer, 1e-4f));
                    }
                    sum += lc * atten * Mathf.Max(0.35f, Mathf.Abs(Vector3.Dot(n, dir)));
                }
            }

            sum *= brightness;
            float m = Mathf.Max(sum.r, Mathf.Max(sum.g, sum.b));
            if (m > maxBrightness) sum *= maxBrightness / m;

            // เรืองแสงตอนเล็ง: หายใจเข้าออกแบบเดียวกับของ 3D (ItemHighlighter)
            float glowK = 0f;
            if (e.hover > 0f)
            {
                float pulse = (Mathf.Sin(Time.time * hoverPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                glowK = Mathf.Lerp(hoverGlowMin, hoverGlowMax, pulse) * e.hover;
                sum += hoverGlowColor.linear * glowK;
                float mm = Mathf.Max(sum.r, Mathf.Max(sum.g, sum.b));
                if (mm > 1f) sum *= 1f / mm;
            }
            UpdateGlowLayer(ref e, glowK);
            sprites[i] = e;
            Color tint = new Color(sum.r, sum.g, sum.b, 1f).gamma;
            e.sr.color = new Color(e.baseColor.r * tint.r, e.baseColor.g * tint.g, e.baseColor.b * tint.b, e.baseColor.a);
        }
    }
}
