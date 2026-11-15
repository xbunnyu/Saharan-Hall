using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ตั้งจุดวางจดหมายเชิญไปป่า: เลือกโต๊ะ (หรือของที่อยากให้จดหมายวางทับ) ใน Hierarchy แล้วกดเมนูนี้
/// → สร้าง/ย้ายวัตถุเปล่าชื่อ "LetterSpawnPoint" ไปไว้บนผิวด้านบนของของชิ้นนั้น (กลางชิ้น)
/// ForestInvitationManager จะวางจดหมายตรงจุดนี้เสมอ — ย้ายต่อเองด้วยเครื่องมือ Move ได้ แล้วอย่าลืมเซฟฉาก
/// ถ้าไม่ได้เลือกอะไร จะวางไว้ตรงกลางมุมมอง Scene
/// </summary>
public static class LetterSpawnPointTool
{
    [MenuItem("Tools/Saharan Hall/✉️ ระบบจดหมายเชิญไปป่า/5. ตั้งจุดวางจดหมายบนของที่เลือก", false, 5)]
    static void PlaceOnSelection()
    {
        var point = GameObject.Find("LetterSpawnPoint");
        if (point == null)
        {
            point = new GameObject("LetterSpawnPoint");
            Undo.RegisterCreatedObjectUndo(point, "สร้างจุดวางจดหมาย");
        }
        else Undo.RecordObject(point.transform, "ย้ายจุดวางจดหมาย");

        var sel = Selection.activeGameObject;
        Vector3 pos;
        if (sel != null && sel != point)
        {
            var rends = sel.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                pos = new Vector3(b.center.x, b.max.y + 0.015f, b.center.z);
            }
            else pos = sel.transform.position;
        }
        else
        {
            var sv = SceneView.lastActiveSceneView;
            pos = sv != null ? sv.pivot : Vector3.zero;
        }

        point.transform.position = pos;
        Selection.activeGameObject = point;
        EditorGUIUtility.PingObject(point);
        EditorSceneManager.MarkSceneDirty(point.scene);
        Debug.Log($"[LetterSpawnPoint] วางจุดจดหมายที่ {pos} — ย้ายละเอียดได้ด้วยเครื่องมือ Move แล้วเซฟฉาก (Ctrl+S)");
    }
}
