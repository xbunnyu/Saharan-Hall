using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ตั้งค่าของ 2 ชิ้นในฉาก GamePlay ด้วยการเลือกของใน Hierarchy แล้วกดเมนู:
/// 1) หนังสือพิมพ์รายวัน: ย้ายระบบข่าว (DailyNewsBulletin) จากกล่อง "News" มาใส่โมเดลหนังสือพิมพ์ที่เลือก
///    (คัดลอกค่าทั้งหมด รวมรายการข่าวแต่ละวัน) + กล่องชน + เรืองแสงตอนเล็ง แล้วซ่อนกล่อง News เดิม
/// 2) กุญแจรถไปป่า: ย้ายระบบขับรถไปป่า (CarToForestInteractable) จากกล่อง "Cube" มาใส่โมเดลกุญแจที่เลือก
///    ถ้าไม่ได้เลือกอะไร (หรือเลือกกล่อง Cube เอง) → แค่เปลี่ยนชื่อกล่องเป็น "กุญแจรถไปป่า" + เรืองแสงตอนเล็ง
/// ป้ายลอยบอกชื่อของตอนเล็ง (HorrorUIStyle) ขึ้นเองอยู่แล้วสำหรับของที่กด E ได้
/// กด Ctrl+Z ย้อนได้ / อย่าลืมเซฟฉาก
/// </summary>
public static class NewsAndCarKeySetupTool
{
    const string Menu = "Tools/Saharan Hall/ตั้งค่าของในตำหนัก/";

    [MenuItem(Menu + "1. ใส่ระบบหนังสือพิมพ์รายวันให้ของที่เลือก")]
    static void SetupNewspaper()
    {
        var target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("หนังสือพิมพ์รายวัน", "เลือกโมเดลหนังสือพิมพ์ใน Hierarchy ก่อน แล้วกดเมนูนี้อีกครั้ง", "ตกลง");
            return;
        }

        var source = FindOther<DailyNewsBulletin>(target);
        var news = target.GetComponent<DailyNewsBulletin>();
        if (news == null) news = Undo.AddComponent<DailyNewsBulletin>(target);
        Undo.RecordObject(news, "ตั้งค่าหนังสือพิมพ์");
        if (source != null) EditorUtility.CopySerialized(source, news); // ข่าวทุกวันเหมือนเดิม

        news.itemName = "หนังสือพิมพ์รายวัน";
        news.customReadPromptText = "อ่านหนังสือพิมพ์";
        news.canRead = true;
        news.canCollect = false;
        // เรืองแสงเฉพาะตอนเล็ง: ปิดการเรืองแสงค้าง + ไฟ + ประกาย ตอนมีข่าวใหม่ที่ยังไม่อ่าน
        news.glowWhenHasUnreadNews = false;
        news.isImportantItem = false;
        news.highlightTriggerMode = HighlightTriggerMode.HoverOnly;
        news.enablePointLightGlow = false;
        news.enableSparkles = false;

        EnsureCollider(target, new Vector3(0.6f, 0.25f, 0.6f)); // กระดาษบางมาก → ขยายพื้นที่เล็งให้ใหญ่ขึ้น
        EnsureHoverGlow(target);
        HideOld(source, target, "หนังสือพิมพ์");
        Done(target, "ตั้งค่าหนังสือพิมพ์รายวันแล้ว" + (source != null ? "\nคัดลอกข่าวจากกล่อง '" + source.name + "' มาครบ และซ่อนกล่องเดิมไว้" : ""));
    }

    [MenuItem(Menu + "2. ตั้งเป็นกุญแจรถไปป่า (เลือกโมเดลกุญแจ หรือไม่เลือกก็ได้)")]
    static void SetupCarKey()
    {
        var target = Selection.activeGameObject;
        var existing = Object.FindFirstObjectByType<CarToForestInteractable>(FindObjectsInactive.Include);

        // ไม่ได้เลือก หรือเลือกกล่อง Cube เดิม → ใช้กล่องเดิม แค่เปลี่ยนชื่อ
        if (target == null || (existing != null && target == existing.gameObject))
        {
            if (existing == null)
            {
                EditorUtility.DisplayDialog("กุญแจรถไปป่า", "ไม่เจอของที่มี CarToForestInteractable ในฉาก", "ตกลง");
                return;
            }
            target = existing.gameObject;
        }

        var car = target.GetComponent<CarToForestInteractable>();
        CarToForestInteractable source = null;
        if (car == null)
        {
            source = existing;
            car = Undo.AddComponent<CarToForestInteractable>(target);
            if (source != null) EditorUtility.CopySerialized(source, car);
        }
        Undo.RecordObject(car, "ตั้งค่ากุญแจรถ");
        car.itemName = "กุญแจรถไปป่า";
        car.canRead = true;
        car.canCollect = false;
        car.isImportantItem = false;
        car.highlightTriggerMode = HighlightTriggerMode.HoverOnly;
        car.enablePointLightGlow = false;
        car.enableSparkles = false;

        EnsureCollider(target, new Vector3(0.35f, 0.25f, 0.35f)); // กุญแจชิ้นเล็ก → ขยายพื้นที่เล็ง
        EnsureHoverGlow(target);
        HideOld(source, target, "กุญแจรถ");
        Done(target, "ตั้งเป็น 'กุญแจรถไปป่า' แล้ว" + (source != null ? "\nย้ายระบบขับรถไปป่ามาจาก '" + source.name + "' และซ่อนกล่องเดิมไว้" : ""));
    }

    static T FindOther<T>(GameObject exclude) where T : Component
    {
        foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.gameObject != exclude) return c;
        return null;
    }

    /// <summary>
    /// กล่องชนให้เป้าเล็งโดน: ครอบโมเดลทั้งชิ้น + ขยายอย่างน้อยเท่า minWorldSize (เมตร) ของชิ้นเล็ก/บาง เล็งง่ายขึ้น
    /// ถ้ามี BoxCollider บนตัวมันอยู่แล้วจะขยายอันเดิม / ถ้ามี Collider แบบอื่นอยู่ ใส่ BoxCollider เพิ่มอีกอัน
    /// </summary>
    static void EnsureCollider(GameObject go, Vector3 minWorldSize)
    {
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = Undo.AddComponent<BoxCollider>(go);
        else Undo.RecordObject(box, "ขยายพื้นที่เล็ง");

        var rends = go.GetComponentsInChildren<Renderer>();
        Bounds b;
        if (rends.Length > 0)
        {
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        }
        else b = new Bounds(go.transform.position, Vector3.zero);

        Vector3 size = Vector3.Max(b.size + Vector3.one * 0.06f, minWorldSize); // เผื่อขอบ 3 ซม. รอบด้าน
        Vector3 s = go.transform.lossyScale;
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size = new Vector3(size.x / Mathf.Max(1e-4f, Mathf.Abs(s.x)), size.y / Mathf.Max(1e-4f, Mathf.Abs(s.y)), size.z / Mathf.Max(1e-4f, Mathf.Abs(s.z)));
        box.isTrigger = false;
    }

    /// <summary>เรืองแสงสีเทียนเฉพาะตอนเล็ง (แบบเดียวกับของชิ้นอื่นในตำหนัก)</summary>
    static void EnsureHoverGlow(GameObject go)
    {
        var hl = go.GetComponent<ItemHighlighter>();
        if (hl == null) hl = Undo.AddComponent<ItemHighlighter>(go);
        Undo.RecordObject(hl, "เรืองแสงตอนเล็ง");
        hl.triggerMode = HighlightTriggerMode.HoverOnly;
        hl.enablePointLight = false;
        hl.enableSparkles = false;
        hl.enableGentleFloating = false;
        hl.glowColor = new Color(1f, 0.62f, 0.25f, 1f);
        hl.minEmissionIntensity = 0.25f;
        hl.maxEmissionIntensity = 0.9f;
        hl.pulseSpeed = 0.8f;
        hl.CacheRenderers();
    }

    static void HideOld(Component source, GameObject target, string what)
    {
        if (source == null || source.gameObject == target) return;
        Undo.RecordObject(source.gameObject, "ซ่อนของเดิม");
        source.gameObject.SetActive(false); // ซ่อน ไม่ลบ — เปิดกลับได้ถ้าต้องการ
    }

    static void Done(GameObject target, string msg)
    {
        // เรืองแสงใช้ค่า Emission ของวัสดุ — วัสดุแบบ Unlit / รูป 2D (Sprite) ไม่มีค่านี้ เลยเรืองแสงไม่ได้
        foreach (var r in target.GetComponentsInChildren<Renderer>())
        {
            if (r is SpriteRenderer)
            {
                msg += $"\n\n'{r.name}' เป็นรูป 2D (Sprite) → เรืองแสงตอนเล็งผ่าน SpriteLighting (ทำงานเองตอนกด Play)";
                break;
            }
            foreach (var m in r.sharedMaterials)
                if (m != null && !m.HasProperty("_EmissionColor"))
                {
                    msg += $"\n\nคำเตือน: วัสดุ '{m.name}' (shader {m.shader.name}) ไม่รองรับการเรืองแสง → เปลี่ยน Shader เป็น Universal Render Pipeline/Lit";
                    goto checkedMats;
                }
        }
        checkedMats:
        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = target;
        Debug.Log("[NewsAndCarKeySetupTool] " + msg.Replace("\n", " "));
        EditorUtility.DisplayDialog("ตั้งค่าเสร็จ", msg + "\n\nอย่าลืมเซฟฉาก (Ctrl+S)", "ตกลง");
    }
}
