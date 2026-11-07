#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// เมนูอำนวยความสะดวกสำหรับตรวจสอบและทดสอบระบบป้องกันเควสออกซ้ำใน Unity Editor
/// </summary>
public static class QuestNoRepeatEditorTool
{
    [MenuItem("Tools/Saharan Hall/📜 ระบบเควส NPC/1. ตรวจสอบสถานะคลังเควส (Check Quest Pool)", false, 1)]
    public static void CheckQuestPool()
    {
        if (HallManager.Instance == null)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ HallManager ในฉากขณะนี้", "ตกลง");
            return;
        }

        int totalQuests = HallManager.Instance.questList != null ? HallManager.Instance.questList.Count : 0;
        int maxNpc = HallManager.Instance.maxNpcPerSession;
        int served = HallManager.Instance.npcsServedThisSession;

        string msg = $"• จำนวนเควสทั้งหมดในคลัง: {totalQuests} เควส\n" +
                     $"• จำนวน NPC สูงสุดต่อวัน: {maxNpc} คน\n" +
                     $"• วันนี้ให้บริการไปแล้ว: {served} คน\n" +
                     $"• ป้องกันเควสซ้ำ: {(HallManager.Instance.preventDuplicateQuests ? "เปิดใช้งาน ✅" : "ปิด ❌")}\n" +
                     $"• ห้ามซ้ำติดกัน: {(HallManager.Instance.avoidConsecutiveRepeat ? "เปิดใช้งาน ✅" : "ปิด ❌")}\n" +
                     $"• เติมเควสเสริมอัตโนมัติ: {(HallManager.Instance.autoSupplementDefaultQuests ? "เปิดใช้งาน ✅" : "ปิด ❌")}";

        EditorUtility.DisplayDialog("สถานะคลังเควส (Quest Pool Status)", msg, "ตกลง");
    }

    [MenuItem("Tools/Saharan Hall/📜 ระบบเควส NPC/2. ล้างประวัติเควสที่เคยออก (Reset Used Quests)", false, 2)]
    public static void ResetUsedQuests()
    {
        if (HallManager.Instance != null)
        {
            HallManager.Instance.ResetUsedQuests();
            EditorUtility.DisplayDialog("สำเร็จ", "ล้างประวัติเควสที่เคยออกในวันนี้เรียบร้อยแล้ว", "ตกลง");
        }
        else
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ HallManager ในฉากขณะนี้", "ตกลง");
        }
    }
}
#endif

