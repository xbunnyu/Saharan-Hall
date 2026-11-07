#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// เมนูอำนวยความสะดวกสำหรับทดสอบการเปลี่ยนแปลงรูปลักษณ์โมเดลโต๊ะหมู่บูชาใน Unity Editor
/// </summary>
public static class AltarVisualEditorTool
{
    private static BuddhistAltarManager GetAltar()
    {
        return BuddhistAltarManager.Instance != null 
            ? BuddhistAltarManager.Instance 
            : Object.FindFirstObjectByType<BuddhistAltarManager>();
    }

    [MenuItem("Tools/Saharan Hall/⛩️ รูปลักษณ์โต๊ะหมู่บูชา/1. แสดงโมเดล Level 1 (Lv.1)", false, 1)]
    public static void SetAltarLevel1()
    {
        var altar = GetAltar();
        if (altar != null)
        {
            Undo.RecordObject(altar, "Set Altar Level 1");
            altar.SetLevel(1);
            Debug.Log("[AltarVisualEditorTool] ⛩️ สลับรูปลักษณ์โต๊ะหมู่บูชาเป็น Level 1 เรียบร้อยแล้ว");
        }
        else
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ BuddhistAltarManager ในฉาก", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/⛩️ รูปลักษณ์โต๊ะหมู่บูชา/2. แสดงโมเดล Level 2 (Lv.2)", false, 2)]
    public static void SetAltarLevel2()
    {
        var altar = GetAltar();
        if (altar != null)
        {
            Undo.RecordObject(altar, "Set Altar Level 2");
            altar.SetLevel(2);
            Debug.Log("[AltarVisualEditorTool] ⛩️ สลับรูปลักษณ์โต๊ะหมู่บูชาเป็น Level 2 เรียบร้อยแล้ว");
        }
        else
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ BuddhistAltarManager ในฉาก", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/⛩️ รูปลักษณ์โต๊ะหมู่บูชา/3. แสดงโมเดล Level 3 (Lv.3)", false, 3)]
    public static void SetAltarLevel3()
    {
        var altar = GetAltar();
        if (altar != null)
        {
            Undo.RecordObject(altar, "Set Altar Level 3");
            altar.SetLevel(3);
            Debug.Log("[AltarVisualEditorTool] ⛩️ สลับรูปลักษณ์โต๊ะหมู่บูชาเป็น Level 3 เรียบร้อยแล้ว");
        }
        else
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ BuddhistAltarManager ในฉาก", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/⛩️ รูปลักษณ์โต๊ะหมู่บูชา/4. แสดงโมเดล Level 4 (Lv.4)", false, 4)]
    public static void SetAltarLevel4()
    {
        var altar = GetAltar();
        if (altar != null)
        {
            Undo.RecordObject(altar, "Set Altar Level 4");
            altar.SetLevel(4);
            Debug.Log("[AltarVisualEditorTool] ⛩️ สลับรูปลักษณ์โต๊ะหมู่บูชาเป็น Level 4 เรียบร้อยแล้ว");
        }
        else
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ BuddhistAltarManager ในฉาก", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/⛩️ รูปลักษณ์โต๊ะหมู่บูชา/5. สั่งอัพเกรดขึ้น 1 ระดับ (Upgrade Next)", false, 5)]
    public static void UpgradeNext()
    {
        var altar = GetAltar();
        if (altar != null)
        {
            if (altar.currentLevel < 4)
            {
                Undo.RecordObject(altar, "Upgrade Altar Level");
                altar.SetLevel(altar.currentLevel + 1);
                Debug.Log($"[AltarVisualEditorTool] ⛩️ อัพเกรดโต๊ะหมู่บูชาเป็น Level {altar.currentLevel} เรียบร้อยแล้ว");
            }
            else
            {
                EditorUtility.DisplayDialog("แจ้งเตือน", "โต๊ะหมู่บูชาอยู่ในระดับสูงสุดแล้ว (Level 4)", "ตกลง");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "ไม่พบ BuddhistAltarManager ในฉาก", "ตกลง");
        }
    }
}
#endif

