using UnityEngine;

/// <summary>
/// วางบนโมเดลรถในซีน Forest (ขากลับตำหนัก)
/// เมื่อผู้เล่นกด [E] จะส่งคำสั่งไปยัง CarSceneFader เพื่อเล่นเสียง -> ล็อกผู้เล่น -> เฟดจอดำ -> โหลดซีน GamePlay -> วางผู้เล่นที่จุดเกิดในตำหนัก -> เฟดสว่าง
/// </summary>
public class CarReturnInteractable : InteractableItem
{
    [Header("Car Return Settings (ขากลับตำหนัก)")]
    [Tooltip("ชื่อซีนปลายทางหลัก (เช่น GamePlay)")]
    public string returnSceneName = "GamePlay";

    [Tooltip("ชื่อ ID ของจุดเกิดในซีน GamePlay ที่ต้องการให้ผู้เล่นไปยืน (เช่น 'GamePlayCarSpawn' หรือ 'CarReturnSpawnPoint')")]
    public string returnSpawnPointName = "CarReturnSpawnPoint";

    [Tooltip("เวลาที่ใช้ในการเฟดจอดำ (วินาที)")]
    public float fadeDuration = 0.8f;

    [Tooltip("เสียงสตาร์ทรถ / เสียงขับรถ (Optional)")]
    public AudioClip carSound;

    private void Reset()
    {
        itemName = "รถยนต์";
        canRead = true;
        canCollect = false;
        customReadPromptText = "ขับรถกลับตำหนัก (กด E)";
        readTitle = "";
        readDescription = "";
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(itemName)) itemName = "รถยนต์";
        if (string.IsNullOrEmpty(customReadPromptText)) customReadPromptText = "ขับรถกลับตำหนัก (กด E)";
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        if (CarSceneFader.Instance.IsTransitioning) return;

        if (interactor != null)
        {
            interactor.CloseReading();
        }

        // ส่งคำสั่งให้ CarSceneFader รัน Coroutine ข้ามซีน (เพื่อไม่ให้ถูกทำลายเมื่อ Forest ถูก Unload)
        CarSceneFader.Instance.Travel(returnSceneName, returnSpawnPointName, fadeDuration, carSound, transform.position);
    }
}
