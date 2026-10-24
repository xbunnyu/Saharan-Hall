#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// เครื่องมือและ Custom Inspector สำหรับระบบ ItemHighlighter ใน Unity Editor
/// เพิ่มความสะดวกในการเลือก Preset สี และกดเพิ่มระบบเรืองแสงให้วัตถุได้ในคลิกเดียว
/// </summary>
[CustomEditor(typeof(ItemHighlighter))]
public class ItemHighlightEditorTool : Editor
{
    public override void OnInspectorGUI()
    {
        ItemHighlighter hl = (ItemHighlighter)target;

        // วาดส่วนหัว
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("✨ ระบบไฮไลท์และเรืองแสง (Saharan Item Glow)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ระบบนี้จะช่วยให้ผู้เล่นสังเกตเห็นไอเทมหรือวัตถุสำคัญได้ชัดเจนจากระยะไกล ผ่านการกระพริบเรืองแสง (Emission), แสงไฟอ่อนๆ ส่องกระทบพื้น (Point Light), และประกายวิบวับ (Sparkles)", MessageType.Info);
        EditorGUILayout.Space(5);

        // วาด Inspector เริ่มต้น
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🔧 ควบคุมการทดสอบ:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("รีเฟรช Renderers"))
        {
            hl.CacheRenderers();
            Debug.Log($"[ItemHighlighter] รีเฟรช Renderers บนวัตถุ '{hl.gameObject.name}' เรียบร้อยแล้ว");
        }

        if (GUILayout.Button(hl.isHighlightActive ? "ปิดการเรืองแสง" : "เปิดการเรืองแสง"))
        {
            Undo.RecordObject(hl, "Toggle Glow Active");
            hl.SetHighlightEnabled(!hl.isHighlightActive);
            EditorUtility.SetDirty(hl);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// เพิ่มคำสั่งคลิกขวาที่วัตถุใน Hierarchy เพื่อใส่ ItemHighlighter ได้ทันที
    /// </summary>
    [MenuItem("GameObject/Saharan Hall/✨ เพิ่มการเรืองแสงให้ไอเทมนี้ (Add Item Glow)", false, 10)]
    public static void AddItemHighlighterToSelection(MenuCommand menuCommand)
    {
        GameObject target = menuCommand.context as GameObject;
        if (target == null && Selection.activeGameObject != null)
        {
            target = Selection.activeGameObject;
        }

        if (target == null)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณาเลือกวัตถุในฉากก่อนเพิ่มระบบเรืองแสง", "ตกลง");
            return;
        }

        Undo.RecordObject(target, "Add Item Glow");

        // ตรวจสอบ InteractableItem
        var interactable = target.GetComponent<InteractableItem>();
        if (interactable != null)
        {
            interactable.isImportantItem = true;
            interactable.InitHighlighter();
            EditorUtility.SetDirty(interactable);
        }
        else
        {
            var hl = target.GetComponent<ItemHighlighter>();
            if (hl == null)
            {
                hl = Undo.AddComponent<ItemHighlighter>(target);
            }
            hl.SetHighlightEnabled(true);
            EditorUtility.SetDirty(hl);
        }

        Debug.Log($"[Saharan Hall] ✅ เพิ่มระบบเรืองแสงให้กับ '{target.name}' เรียบร้อยแล้ว!");
    }

    /// <summary>
    /// เพิ่มคำสั่งคลิกขวาเพื่อแปลงวัตถุเป็นหนังสือพิมพ์/กระดานข่าวสารประจำวัน (DailyNewsBulletin)
    /// </summary>
    [MenuItem("GameObject/Saharan Hall/📰 ตั้งเป็นหนังสือพิมพ์หรือกระดานข่าวสาร (Make Daily News Item)", false, 11)]
    public static void AddDailyNewsToSelection(MenuCommand menuCommand)
    {
        GameObject target = menuCommand.context as GameObject;
        if (target == null && Selection.activeGameObject != null)
        {
            target = Selection.activeGameObject;
        }

        if (target == null)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณาเลือกวัตถุในฉากก่อน", "ตกลง");
            return;
        }

        Undo.RecordObject(target, "Add Daily News Bulletin");

        var news = target.GetComponent<DailyNewsBulletin>();
        if (news == null)
        {
            news = Undo.AddComponent<DailyNewsBulletin>(target);
        }

        news.itemName = "หนังสือพิมพ์รายวัน";
        news.customReadPromptText = "อ่านข่าวสาร";
        news.glowWhenHasUnreadNews = true;
        news.isImportantItem = true;
        news.InitHighlighter();

        EditorUtility.SetDirty(target);
        Debug.Log($"[Saharan Hall] 📰 ติดตั้งระบบข่าวสารประจำวันให้กับ '{target.name}' เรียบร้อยแล้ว!");
    }
}
#endif
