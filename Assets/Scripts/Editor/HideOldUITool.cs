using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ซ่อน UI แบบเก่าออกจากหน้าฉาก (ตอนไม่ได้กด Play) ให้หน้าจอไม่รก
/// ไม่ลบทิ้ง — ระบบเกมยังใช้ข้อความในกล่องพวกนี้ (เงิน/วัน/บทพูด) ตอนเล่น
/// - กล่องบทสนทนา / ป้าย [E] / ข้อความแจ้งเตือน / กล่องภารกิจเก่า → ปิดไว้ (ระบบเปิดเองตอนต้องใช้ แล้ว HorrorUIStyle จัดหน้าตาใหม่)
/// - ป้ายวันที่ / เงิน / ชื่อเสียงเก่า → มองไม่เห็น (ยังทำงานอยู่ เพราะ UI ใหม่อ่านตัวเลขจากมัน)
/// เมนู: Tools > Saharan > ซ่อน UI เก่าในหน้าฉาก   (Ctrl+Z ย้อนได้ / Ctrl+S เพื่อเซฟ)
/// </summary>
public static class HideOldUITool
{
    [MenuItem("Tools/Saharan/ซ่อน UI เก่าในหน้าฉาก")]
    public static void HideOld()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ซ่อน UI เก่า", "หยุดเกม (ปุ่ม Play) ก่อน แล้วค่อยกดเมนูนี้", "ตกลง");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("ซ่อน UI เก่า");
        var report = new List<string>();

        var q = Object.FindFirstObjectByType<QuestUIManager>(FindObjectsInactive.Include);
        var ui = Object.FindFirstObjectByType<InteractionUIManager>(FindObjectsInactive.Include);

        void Deactivate(GameObject go, string label)
        {
            if (go == null || !go.activeSelf) return;
            Undo.RecordObject(go, "ซ่อน UI เก่า");
            go.SetActive(false);
            report.Add("ปิด: " + label + " (" + go.name + ")");
        }

        if (q != null)
        {
            Deactivate(q.questDialogPanel, "กล่องบทสนทนาเก่า");
            Deactivate(q.questTrackerPanel, "กล่องภารกิจเก่า");
            Deactivate(q.responsePanel, "กล่องคำตอบเก่า");
        }
        if (ui != null)
        {
            Deactivate(ui.promptPanel, "ป้าย [E] เก่า");
            Deactivate(ui.notificationPanel, "ข้อความแจ้งเตือนเก่า");
        }

        // ป้ายวันที่ / เงิน / ชื่อเสียง: ยังต้องทำงาน → ทำให้มองไม่เห็นด้วย CanvasGroup
        var hudTexts = new List<TMP_Text>();
        var dm = Object.FindFirstObjectByType<DayManager>(FindObjectsInactive.Include);
        if (dm != null && dm.dayHUDText != null) hudTexts.Add(dm.dayHUDText);
        foreach (var m in Object.FindObjectsByType<MoneyDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { var t = m.GetComponentInChildren<TMP_Text>(true); if (t != null) hudTexts.Add(t); }
        foreach (var r in Object.FindObjectsByType<ReputationDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { var t = r.GetComponentInChildren<TMP_Text>(true); if (t != null) hudTexts.Add(t); }

        var done = new HashSet<Transform>();
        foreach (var t in hudTexts)
        {
            Transform root = t.transform;
            var canvas = t.GetComponentInParent<Canvas>(true);
            if (canvas != null) canvas = canvas.rootCanvas;
            while (root.parent != null && canvas != null && root.parent != canvas.transform) root = root.parent;
            if (!done.Add(root)) continue;

            var g = root.GetComponent<CanvasGroup>();
            if (g == null) g = Undo.AddComponent<CanvasGroup>(root.gameObject);
            else Undo.RecordObject(g, "ซ่อน UI เก่า");
            g.alpha = 0f;
            g.blocksRaycasts = false;
            report.Add("มองไม่เห็น: ป้าย HUD เก่า (" + root.name + ")");
        }

        Undo.CollapseUndoOperations(group);
        if (q != null) EditorSceneManager.MarkSceneDirty(q.gameObject.scene);

        EditorUtility.DisplayDialog("ซ่อน UI เก่าแล้ว",
            (report.Count > 0 ? string.Join("\n", report) : "ไม่มีอะไรต้องซ่อน (ซ่อนไว้แล้ว)") +
            "\n\nกด Ctrl+S เพื่อเซฟฉาก (ไม่ชอบกด Ctrl+Z)", "ตกลง");
    }
}
