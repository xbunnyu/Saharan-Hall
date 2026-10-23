using UnityEngine;

/// <summary>
/// วางบนโมเดลรถในซีน GamePlay (ขาไปป่า)
/// เมื่อผู้เล่นกด [E] จะส่งคำสั่งไปยัง CarSceneFader เพื่อเล่นเสียง -> ล็อกผู้เล่น -> เฟดจอดำ -> โหลดซีน Forest -> ย้ายผู้เล่นไปยังจุดเกิดในป่า -> เฟดสว่าง
/// </summary>
public class CarToForestInteractable : InteractableItem
{
    [Header("Car Travel Settings (ขาไปป่า)")]
    [Tooltip("ชื่อซีนปลายทาง (เช่น Forest)")]
    public string targetSceneName = "Forest";

    [Tooltip("ชื่อ ID ของจุดเกิดในซีน Forest ที่ต้องการให้ผู้เล่นไปยืน (เช่น 'ForestCarSpawn')")]
    public string targetSpawnPointName = "ForestCarSpawn";

    [Tooltip("เวลาที่ใช้ในการเฟดจอดำ (วินาที)")]
    public float fadeDuration = 0.8f;

    [Tooltip("เสียงสตาร์ทรถ / เสียงขับรถ (Optional)")]
    public AudioClip carSound;

    private void Reset()
    {
        itemName = "รถยนต์";
        canRead = true;
        canCollect = false;
        customReadPromptText = "ขับรถไปป่า (กด E)";
        readTitle = "";
        readDescription = "";
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(itemName)) itemName = "รถยนต์";
        if (string.IsNullOrEmpty(customReadPromptText)) customReadPromptText = "ขับรถไปป่า (กด E)";
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        if (CarSceneFader.Instance.IsTransitioning) return;

        if (interactor != null)
        {
            interactor.CloseReading();
        }

        // ส่งคำสั่งให้ CarSceneFader รัน Coroutine ข้ามซีน (เพื่อไม่ให้ถูกทำลายเมื่อ GamePlay ถูก Unload)
        CarSceneFader.Instance.Travel(targetSceneName, targetSpawnPointName, fadeDuration, carSound, transform.position);
    }
}
