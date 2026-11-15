using UnityEngine;

/// <summary>ไฟเทียนไหววูบวาบ (ใส่ที่ Light) — ใช้กับไฟบนศาลตายายในป่า</summary>
[RequireComponent(typeof(Light))]
public class CandleFlicker : MonoBehaviour
{
    [Range(0f, 0.6f)] public float amount = 0.18f;
    public float speed = 7f;

    Light lightComp;
    float baseIntensity;
    float seed;

    void Awake()
    {
        lightComp = GetComponent<Light>();
        baseIntensity = lightComp.intensity;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
        lightComp.intensity = baseIntensity * (1f + n * 2f * amount);
    }
}
