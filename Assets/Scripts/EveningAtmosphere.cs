using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// บรรยากาศตอนค่ำ: ทำงานครบของวันแล้ว (ปิดตำหนัก + รับแขกครบ) พอลุกจากเก้าอี้ → ฟ้า/แสงข้างนอกค่อยๆ มืดลง
/// เหลือแสงเทียนสลัวๆ ไหววูบวาบ + หมอกมืดจางๆ ให้รู้สึกว่าถึงเวลาเข้านอน
/// ตื่นเช้าวันใหม่ (DayManager.OnDayChanged) → กลับเป็นกลางวันทันที (ระหว่างจอดำตอนนอน)
/// สร้างตัวเองอัตโนมัติในฉาก GamePlay / BedroomScene ไม่ต้องวางในฉาก (ถ้าวางเองจะใช้ตัวที่วางไว้ ปรับค่าได้ใน Inspector)
/// </summary>
public class EveningAtmosphere : MonoBehaviour
{
    static readonly string[] Scenes = { "GamePlay", "BedroomScene" };
    /// <summary>วันที่เข้าช่วงค่ำแล้ว (จำข้ามฉาก เดินไปห้องนอนแล้วยังค่ำอยู่) / -1 = กลางวัน</summary>
    static int eveningDay = -1;

    [Header("การเปลี่ยน")]
    [Tooltip("ใช้เวลากี่วินาทีจากกลางวันเป็นค่ำ")]
    public float transitionSeconds = 5f;

    [Header("แสงอาทิตย์ (Directional Light)")]
    [Range(0f, 1f)] public float sunIntensityScale = 0.1f;
    public Color sunEveningColor = new Color(0.42f, 0.44f, 0.72f);

    [Header("แสงรอบข้าง (Ambient)")]
    [Range(0f, 1f)] public float ambientScale = 0.3f;
    public Color ambientEveningTint = new Color(0.55f, 0.6f, 1f);

    [Header("ท้องฟ้า (Skybox)")]
    [Tooltip("ความสว่างฟ้าตอนค่ำ เทียบกับกลางวัน")]
    [Range(0f, 1f)] public float skyboxBrightness = 0.18f;
    public Color skyboxEveningTint = new Color(0.35f, 0.38f, 0.6f);

    [Header("แสงเทียน (Point / Spot Light)")]
    [Tooltip("แสงเทียนตอนค่ำ เทียบกับกลางวัน (ต่ำกว่า 1 = สลัวลง)")]
    [Range(0.2f, 2f)] public float candleScale = 0.85f;
    [Tooltip("เปลวเทียนไหว (0 = นิ่ง)")]
    [Range(0f, 0.4f)] public float candleFlicker = 0.12f;

    [Header("หมอกมืด")]
    public bool eveningFog = true;
    public Color fogColor = new Color(0.02f, 0.02f, 0.035f);
    [Range(0f, 0.1f)] public float fogDensity = 0.018f;

    struct LightState { public Light light; public float intensity; public Color color; public float seed; }
    readonly List<LightState> suns = new List<LightState>();
    readonly List<LightState> candles = new List<LightState>();

    Color ambSky, ambEquator, ambGround;
    bool fog0; Color fogColor0; float fogDensity0; FogMode fogMode0;
    Material skyOriginal, skyInstance;
    string skyBrightnessProp, skyTintProp;
    float skyBrightness0 = 1f; Color skyTint0 = Color.white;

    float amount;  // 0 = กลางวัน, 1 = ค่ำ
    float target;

    // ── สร้างตัวเองในฉากที่ต้องใช้ ──
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
        if (System.Array.IndexOf(Scenes, s.name) < 0) return;
        if (FindFirstObjectByType<EveningAtmosphere>(FindObjectsInactive.Include) != null) return;
        new GameObject("[EveningAtmosphere]").AddComponent<EveningAtmosphere>();
    }

    /// <summary>ล้างสถานะค่ำ (เริ่มเกมใหม่)</summary>
    public static void ResetToDay() { eveningDay = -1; }

    void Awake()
    {
        Capture();
        HallSeatInteractable.OnPlayerStoodUp += OnStoodUp;
        DayManager.OnDayChanged += OnDayChanged;
    }

    void Start()
    {
        // เดินจากห้องนอนกลับมาตำหนัก (หรือกลับกัน) ในคืนเดียวกัน → ค่ำทันที
        int day = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        if (eveningDay == day) { amount = target = 1f; Apply(1f); }
    }

    void OnDestroy()
    {
        HallSeatInteractable.OnPlayerStoodUp -= OnStoodUp;
        DayManager.OnDayChanged -= OnDayChanged;
        if (skyInstance != null)
        {
            if (RenderSettings.skybox == skyInstance) RenderSettings.skybox = skyOriginal;
            Destroy(skyInstance);
        }
    }

    void Capture()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            string n = l.gameObject.name.ToLowerInvariant();
            if (n.Contains("glow") || n.Contains("highlight")) continue; // แสงไฮไลต์ของไอเทม ไม่ยุ่ง
            var st = new LightState { light = l, intensity = l.intensity, color = l.color, seed = Random.value * 100f };
            if (l.type == LightType.Directional) suns.Add(st); else candles.Add(st);
        }
        ambSky = RenderSettings.ambientSkyColor;
        ambEquator = RenderSettings.ambientEquatorColor;
        ambGround = RenderSettings.ambientGroundColor;
        fog0 = RenderSettings.fog; fogColor0 = RenderSettings.fogColor; fogDensity0 = RenderSettings.fogDensity; fogMode0 = RenderSettings.fogMode;

        skyOriginal = RenderSettings.skybox;
        if (skyOriginal != null)
        {
            foreach (var p in new[] { "_Exposure", "_Brightness", "_Intensity" })
                if (skyOriginal.HasProperty(p)) { skyBrightnessProp = p; skyBrightness0 = skyOriginal.GetFloat(p); break; }
            foreach (var p in new[] { "_Tint", "_SkyTint", "_Color" })
                if (skyOriginal.HasProperty(p)) { skyTintProp = p; skyTint0 = skyOriginal.GetColor(p); break; }
        }
    }

    /// <summary>ทำงานของวันครบแล้วหรือยัง: ปิดตำหนัก + รับแขกครบตามจำนวน</summary>
    static bool DayWorkDone()
    {
        var hall = HallManager.Instance;
        if (hall == null) return true;
        if (hall.isHallOpen || hall.currentActiveNPC != null) return false;
        int need = hall.maxNpcPerSession > 0 ? hall.maxNpcPerSession : 5;
        return hall.npcsServedThisSession >= need;
    }

    void OnStoodUp()
    {
        if (!DayWorkDone()) return;
        eveningDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        target = 1f;
    }

    void OnDayChanged(int day)
    {
        // เช้าวันใหม่ (เกิดตอนจอดำระหว่างนอน) → กลางวันทันที
        eveningDay = -1;
        amount = target = 0f;
        Apply(0f);
    }

    void Update()
    {
        if (!Mathf.Approximately(amount, target))
        {
            amount = Mathf.MoveTowards(amount, target, Time.deltaTime / Mathf.Max(0.1f, transitionSeconds));
            Apply(amount);
        }
        else if (amount > 0f && candleFlicker > 0f)
        {
            ApplyCandles(amount); // เปลวเทียนไหวตลอดตอนค่ำ
        }
    }

    void Apply(float k)
    {
        float e = Mathf.SmoothStep(0f, 1f, k);

        foreach (var s in suns)
        {
            if (s.light == null) continue;
            s.light.intensity = Mathf.Lerp(s.intensity, s.intensity * sunIntensityScale, e);
            s.light.color = Color.Lerp(s.color, sunEveningColor, e);
        }

        Color Dim(Color c) => Color.Lerp(c, c * ambientScale * ambientEveningTint, e);
        RenderSettings.ambientSkyColor = Dim(ambSky);
        RenderSettings.ambientEquatorColor = Dim(ambEquator);
        RenderSettings.ambientGroundColor = Dim(ambGround);

        if (eveningFog)
        {
            if (e > 0.001f)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = fog0 ? fogMode0 : FogMode.ExponentialSquared;
                RenderSettings.fogColor = Color.Lerp(fog0 ? fogColor0 : fogColor, fogColor, e);
                RenderSettings.fogDensity = Mathf.Lerp(fog0 ? fogDensity0 : 0f, fogDensity, e);
            }
            else
            {
                RenderSettings.fog = fog0; RenderSettings.fogMode = fogMode0;
                RenderSettings.fogColor = fogColor0; RenderSettings.fogDensity = fogDensity0;
            }
        }

        if (skyOriginal != null && (skyBrightnessProp != null || skyTintProp != null))
        {
            if (e > 0.001f)
            {
                // แก้สำเนาของวัสดุฟ้า ไม่แก้ไฟล์จริงในโปรเจกต์
                if (skyInstance == null) skyInstance = new Material(skyOriginal) { name = skyOriginal.name + " (Evening)" };
                if (RenderSettings.skybox != skyInstance) RenderSettings.skybox = skyInstance;
                if (skyBrightnessProp != null) skyInstance.SetFloat(skyBrightnessProp, Mathf.Lerp(skyBrightness0, skyBrightness0 * skyboxBrightness, e));
                if (skyTintProp != null) skyInstance.SetColor(skyTintProp, Color.Lerp(skyTint0, skyboxEveningTint, e));
            }
            else if (RenderSettings.skybox != skyOriginal)
            {
                RenderSettings.skybox = skyOriginal;
            }
        }

        ApplyCandles(k);
    }

    void ApplyCandles(float k)
    {
        float e = Mathf.SmoothStep(0f, 1f, k);
        float t = Time.time;
        foreach (var c in candles)
        {
            if (c.light == null) continue;
            float flicker = 1f + (Mathf.PerlinNoise(c.seed, t * 6f) - 0.5f) * 2f * candleFlicker * e;
            c.light.intensity = c.intensity * Mathf.Lerp(1f, candleScale, e) * flicker;
        }
    }
}
