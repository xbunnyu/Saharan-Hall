#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// เมนูอำนวยความสะดวกสำหรับทดสอบระบบ Cutscene สำหรับ 3 ฉากจบใน Unity Editor:
/// 1. แพ้เกม (Game Over)
/// 2. จบแบบดี (Good Ending)
/// 3. จบแบบชั่ว (Bad Ending)
/// </summary>
public static class GameEndingSetupTool
{
    [MenuItem("Tools/Saharan Hall/🎬 ทดสอบ Cutscene ฉากจบ/👻 1. แพ้เกม (Ghost Game Over)", false, 10)]
    public static void TestGhostGameOverCutscene()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบ Cutscene แบบเต็มระบบพร้อมภาพเคลื่อนไหวและเสียง", "ตกลง");
            return;
        }

        if (GameEndingManager.Instance != null)
        {
            GameEndingManager.Instance.TestTriggerGhostGameOver();
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ GameEndingManager ในฉากปัจจุบัน", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/🎬 ทดสอบ Cutscene ฉากจบ/✨ 2. จบแบบดี (Good Ending)", false, 11)]
    public static void TestGoodEndingCutscene()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบ Cutscene แบบเต็มระบบพร้อมภาพเคลื่อนไหวและเสียง", "ตกลง");
            return;
        }

        if (GameEndingManager.Instance != null)
        {
            GameEndingManager.Instance.TestTriggerGoodEnding();
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ GameEndingManager ในฉากปัจจุบัน", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/🎬 ทดสอบ Cutscene ฉากจบ/💀 3. จบแบบชั่ว (Bad Ending)", false, 12)]
    public static void TestBadEndingCutscene()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบ Cutscene แบบเต็มระบบพร้อมภาพเคลื่อนไหวและเสียง", "ตกลง");
            return;
        }

        if (GameEndingManager.Instance != null)
        {
            GameEndingManager.Instance.TestTriggerBadEnding();
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ GameEndingManager ในฉากปัจจุบัน", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/🎬 ทดสอบ Cutscene ฉากจบ/💤 ประเมินผลฉากจบวันที่ 3 (EvaluateDay3Ending)", false, 25)]
    public static void TestEvaluateDay3Ending()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบการประเมินผลฉากจบตามสถานะผู้เล่นจริง", "ตกลง");
            return;
        }

        if (GameEndingManager.Instance != null)
        {
            GameEndingManager.Instance.TestEvaluateDay3Ending();
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ GameEndingManager ในฉากปัจจุบัน", "ตกลง");
        }
    }
}
#endif
