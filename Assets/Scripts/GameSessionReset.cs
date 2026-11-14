using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ล้างข้อมูลรอบเล่นเก่า (วัน / ชื่อเสียง / กรรม / จดหมายเชิญ / NPC ที่กลับมา)
/// ตัวจัดการพวกนี้อยู่ข้ามฉาก (DontDestroyOnLoad) → กลับเมนูหรือเริ่มใหม่แล้วค่าเก่าค้าง เช่น เข้าเกมใหม่แต่ยังเป็น "วันที่ ๓"
/// - กลับมาหน้า MainMenu เมื่อไร (จากหน้าแพ้ / เมนูหยุดเกม / ทางไหนก็ได้) ล้างให้อัตโนมัติ
/// - ปุ่ม "เริ่มวันแรกใหม่" ในหน้าแพ้ เรียก ResetRun() ก่อนโหลดฉาก GamePlay
/// ตัวจัดการในฉาก GamePlay (DayManager, ReputationManager) และ HallManager จะสร้างตัวใหม่ให้เองตอนโหลดฉาก
/// </summary>
public static class GameSessionReset
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") ResetRun();
    }

    public static void ResetRun()
    {
        // วัน: ทำลายตัวเก่า → ฉาก GamePlay สร้างตัวใหม่ที่วันที่ 1 (พร้อมป้ายวันที่ของฉากเอง)
        if (DayManager.Instance != null) Object.Destroy(DayManager.Instance.gameObject);
        var dayCanvas = GameObject.Find("DayManager_Canvas");
        if (dayCanvas != null) Object.Destroy(dayCanvas);

        // ชื่อเสียง / NPC ที่กลับมาตามเนื้อเรื่อง: HallManager สร้างตัวใหม่ให้ถ้าไม่มี
        if (ReputationManager.Instance != null) Object.Destroy(ReputationManager.Instance.gameObject);
        if (ReturnNPCScheduler.Instance != null) Object.Destroy(ReturnNPCScheduler.Instance.gameObject);

        // กรรม: ตั้งกลับค่าเริ่มต้น (ไม่สร้างตัวใหม่ถ้ายังไม่มี)
        var karma = Object.FindFirstObjectByType<KarmaManager>();
        if (karma != null) karma.karma = karma.startingKarma;

        // จดหมายเชิญไปป่า: ตัวนี้สร้างครั้งเดียวตอนเปิดเกม → ห้ามทำลาย ตั้งสถานะกลับแทน
        if (ForestInvitationManager.Instance != null)
        {
            ForestInvitationManager.Instance.isLetterDelivered = false;
            ForestInvitationManager.Instance.isInvitationAccepted = false;
        }

        EveningAtmosphere.ResetToDay();

        Time.timeScale = 1f;
        Debug.Log("[GameSessionReset] ล้างข้อมูลรอบเก่าแล้ว → เริ่มวันที่ 1 ใหม่");
    }
}
