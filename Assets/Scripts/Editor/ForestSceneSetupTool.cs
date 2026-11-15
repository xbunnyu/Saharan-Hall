using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// จัดฉากป่า (ใช้กับฉากป่าที่เปิดอยู่: Forest หรือ Forest 2) — Tools > Saharan Hall > ป่า > จัดฉากป่า
/// 1. ศาลตายาย: ฐานศาล (Sal) + ตัวศาล (Salcal) + ธูป (toop) + เทียนเล็ก 2 เล่ม (candle small) วางบนศาล
///    ตอนถวาย: ข้าวเหนียวหมู (Khao niew moo) โผล่ + เปลวเทียน (fire) + ไฟส้มอุ่นติด  (แทนลูกกลมเดิม "snack")
/// 2. กองดิน 5 จุด: โมเดลดินใหม่ (Din_Ver_2) / ขุดแล้ว = Din_Small / กล่องรางวัลเป็นกล่องไม้
///    กระจายเป็นกลุ่มไม่เกิน ~16 ม. จากจุดลงรถ (เดิมเรียงแถวตรง)
/// 3. จุดฝังเครื่องราง + เทียนพิธี (ถ้ามีในฉาก) ย้ายมาใกล้ผู้เล่น
/// 4. ต้นไม้ + พุ่มไม้รอบๆ ให้ดูเป็นป่า (เว้นทางเดินกับรอบๆ จุดเควส) — กดซ้ำได้ จะลบของตกแต่งชุดเดิมก่อน
/// ทุกโมเดลใส่ Material (สร้างให้จาก Texture ถ้ายังไม่มี) / วางบนพื้น Terrain / ปรับขนาดให้สมจริงอัตโนมัติ
/// กด Ctrl+Z ย้อนได้ แล้วอย่าลืมเซฟฉาก
/// </summary>
public static class ForestSceneSetupTool
{
    const string MenuRoot = "Tools/Saharan Hall/ป่า/";
    const string DecorRootName = "[ForestDecor]";
    const string ShrineVisualName = "[ShrineVisual]";

    static Vector3 A, fwd, right;
    static Terrain terrain;

    [MenuItem(MenuRoot + "จัดฉากป่า (ศาล ดิน ของตกแต่ง)")]
    static void SetupForest()
    {
        if (!EditorUtility.DisplayDialog("จัดฉากป่า",
            "จะจัดฉากที่เปิดอยู่ (" + EditorSceneManager.GetActiveScene().name + "):\n" +
            "- ศาลตายาย + ธูปเทียน + ข้าวเหนียวหมูตอนถวาย\n- กองดินใหม่ 5 จุด\n- ย้ายจุดเควสมาใกล้ผู้เล่น\n- ต้นไม้/พุ่มไม้รอบๆ\n\nกด Ctrl+Z ย้อนได้",
            "จัดเลย", "ยกเลิก")) return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("จัดฉากป่า");

        terrain = Terrain.activeTerrain;
        // ลบต้นไม้ชุดเก่าก่อน (ไม่ให้ไปขวางการหาพื้น) แล้วอัปเดตตำแหน่งกล่องชนให้ตรงก่อนยิงหาพื้น
        var oldDecor = GameObject.Find(DecorRootName);
        if (oldDecor != null) Undo.DestroyObjectImmediate(oldDecor);
        Physics.SyncTransforms();
        FindAnchor();

        var report = new List<string>();
        var keepClear = new List<Vector3> { A };

        var car = Object.FindFirstObjectByType<CarReturnInteractable>();
        if (car != null) keepClear.Add(car.transform.position);

        var shrine = Object.FindFirstObjectByType<ForestShrineInteractable>(FindObjectsInactive.Include);
        if (shrine != null) { BuildShrine(shrine, report); keepClear.Add(shrine.transform.position); }
        else report.Add("ไม่พบศาลตายาย (ForestShrineInteractable) ในฉาก");

        foreach (var p in ArrangeDigSpots(report)) keepClear.Add(p);
        foreach (var p in ArrangeAmulet(report)) keepClear.Add(p);

        BuildDecor(keepClear, report);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        if (shrine != null) Selection.activeGameObject = shrine.gameObject;

        string msg = string.Join("\n", report);
        Debug.Log("[ForestSceneSetupTool]\n" + msg);
        EditorUtility.DisplayDialog("จัดฉากป่าเสร็จ", msg + "\n\nอย่าลืมเซฟฉาก (Ctrl+S)", "ตกลง");
    }

    // ══════════ จุดอ้างอิง: จุดลงรถ / ทิศเดินเข้าป่า ══════════
    static void FindAnchor()
    {
        var spawns = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var sp = spawns.FirstOrDefault(s => s.name.Contains("CarSpawn")) ?? spawns.FirstOrDefault();
        var player = Object.FindFirstObjectByType<PlayerController>();
        A = sp != null ? sp.transform.position : (player != null ? player.transform.position : Vector3.zero);

        var car = Object.FindFirstObjectByType<CarReturnInteractable>();
        Vector3 f = car != null ? A - car.transform.position : Vector3.forward;
        f.y = 0f;
        fwd = f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
        right = Vector3.Cross(Vector3.up, fwd).normalized;
        A = Ground(A);
    }

    static Vector3 At(float ahead, float side) => Ground(A + fwd * ahead + right * side);

    /// <summary>
    /// พื้นที่ผู้เล่นเหยียบจริง: ยิงลงหาพื้นที่มีกล่องชน (ชั้นพื้นในฉากอยู่สูงกว่า Terrain ~2 ม. — เดิมวางตาม Terrain ศาลเลยจมดิน)
    /// ข้ามของที่ไม่ใช่พื้น (ไอเทมกด E / ผู้เล่น / ต้นไม้ตกแต่ง) — ไม่เจอพื้นค่อยใช้ความสูง Terrain
    /// </summary>
    static Vector3 Ground(Vector3 p)
    {
        var hits = Physics.RaycastAll(new Vector3(p.x, p.y + 40f, p.z), Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var c = h.collider;
            if (c.GetComponentInParent<InteractableItem>() != null) continue;
            if (c.GetComponentInParent<PlayerController>() != null) continue;
            if (c.GetComponentInParent<CharacterController>() != null) continue;
            var root = c.transform.root;
            if (root.name == DecorRootName) continue;
            if (h.normal.y < 0.5f) continue; // ผนัง/ลำต้น ไม่ใช่พื้น
            return h.point;
        }
        if (terrain != null)
            return new Vector3(p.x, terrain.SampleHeight(p) + terrain.transform.position.y, p.z);
        return p;
    }

    // ══════════ ศาลตายาย ══════════
    static void BuildShrine(ForestShrineInteractable shrine, List<string> report)
    {
        var st = shrine.transform;
        Undo.RecordObject(st, "ย้ายศาล");
        st.position = At(2f, -3f); // ข้างๆ ผู้เล่นเลย (หน้า 2 ม. ซ้าย 3 ม. จากจุดลงรถ)
        Vector3 look = A - st.position; look.y = 0f;
        if (look.sqrMagnitude > 0.01f) st.rotation = Quaternion.LookRotation(look.normalized, Vector3.up); // หันหน้าศาลเข้าหาผู้เล่น
        st.localScale = Vector3.one;

        // กล่องเดิม (placeholder) ซ่อนหน้าตา เหลือแต่กล่องชนไว้กด E
        var mr = shrine.GetComponent<MeshRenderer>();
        if (mr != null) { Undo.RecordObject(mr, "ซ่อนกล่องศาลเดิม"); mr.enabled = false; }

        var old = st.Find(ShrineVisualName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var root = NewChild(ShrineVisualName, st);

        // ตัวศาล (โมเดล "ศาล" / Salcal อย่างเดียว ไม่ใช้ฐานศาล) ตั้งบนพื้น สูง ShrineHeight ม.
        float groundY = st.position.y;
        var house = PlaceModel("Salcal", root, Vector3.zero, height: ShrineHeight);
        SetBottomAt(house, groundY, st.position);
        Bounds hb = WorldBounds(house);
        float w = Mathf.Max(0.3f, Mathf.Min(hb.size.x, hb.size.z));

        // หา "ผิวแท่นหน้าศาล" จากรูปทรงโมเดลจริง (ใส่กล่องชนชั่วคราวแล้วยิงหา) → วางธูปเทียนบนนั้นพอดี
        var temp = new List<MeshCollider>();
        foreach (var mf in house.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); }
        Physics.SyncTransforms();

        float shelfY = 0f, frontD = 0f;
        bool hasShelf = false;
        for (float d = hb.extents.magnitude; d >= 0f; d -= 0.02f)   // จากหน้าสุดเข้ามา หาจุดหน้าสุดที่มีแท่นให้วาง
        {
            if (FindShelf(st.position + st.forward * d, hb, temp, out float y)) { shelfY = y; frontD = d; hasShelf = true; break; }
        }
        Vector3 front;
        float topL, topR, topC;
        if (hasShelf)
        {
            front = st.position + st.forward * Mathf.Max(0f, frontD - 0.06f);
            topC = shelfY;
            topL = FindShelf(front - st.right * w * 0.25f, hb, temp, out float yl) ? yl : shelfY;
            topR = FindShelf(front + st.right * w * 0.25f, hb, temp, out float yr) ? yr : shelfY;
        }
        else
        {
            // ไม่เจอแท่น → วางของถวายบนพื้นหน้าศาล
            front = st.position + st.forward * (hb.extents.z + 0.25f);
            topC = topL = topR = groundY;
        }
        foreach (var mc in temp) if (mc != null) Object.DestroyImmediate(mc);
        Physics.SyncTransforms();

        // ธูป + เทียนเล็ก 2 เล่ม หน้าศาล (ไม่มีไฟจนกว่าจะถวาย)
        var incense = PlaceModel("toop", root, Vector3.zero, height: 0.16f);
        SetBottomAt(incense, topC, front);
        var candleL = PlaceModel("candle small", root, Vector3.zero, height: 0.11f);
        SetBottomAt(candleL, topL, front - st.right * w * 0.25f);
        var candleR = PlaceModel("candle small", root, Vector3.zero, height: 0.11f);
        SetBottomAt(candleR, topR, front + st.right * w * 0.25f);
        float top = topC;

        // ของที่โผล่ตอนถวาย: ข้าวเหนียวหมู + เปลวเทียน + ไฟ (ข้างหลังธูปนิดหน่อย)
        var offering = NewChild("[OfferingOnShrine]", root.transform);
        var rice = PlaceModel("Khao niew moo", offering.transform, Vector3.zero, maxWidth: Mathf.Min(0.18f, w * 0.3f));
        SetBottomAt(rice, top, front - st.forward * 0.12f + st.right * 0.08f);

        Light mainLight = null;
        foreach (var c in new[] { candleL, candleR })
        {
            if (c == null) continue;
            Bounds cb = WorldBounds(c);
            var flame = PlaceModel("fire", offering.transform, Vector3.zero, height: 0.05f, folderHint: "Models/fire");
            if (flame != null) SetBottomAt(flame, cb.max.y - 0.005f, cb.center);
            var l = AddLight(offering.transform, new Vector3(cb.center.x, cb.max.y + 0.06f, cb.center.z), 0.8f, 1.6f, "CandleLight");
            l.gameObject.AddComponent<CandleFlicker>();
        }
        mainLight = AddLight(offering.transform, front + st.forward * 0.3f + Vector3.up * (top - front.y + 0.45f), 2.2f, 5f, "ShrineGlow");
        mainLight.gameObject.AddComponent<CandleFlicker>();

        offering.SetActive(false);

        // ผูกกับระบบศาล
        Undo.RecordObject(shrine, "ผูกของถวาย");
        if (shrine.dessertVisualOnShrine != null && shrine.dessertVisualOnShrine != offering)
        {
            Undo.RecordObject(shrine.dessertVisualOnShrine, "ซ่อนของถวายเดิม");
            shrine.dessertVisualOnShrine.SetActive(false); // ลูกกลม "snack" เดิม
        }
        shrine.dessertVisualOnShrine = offering;
        shrine.shrineGlowLight = mainLight;
        EditorUtility.SetDirty(shrine);

        // กล่องชนครอบทั้งศาล
        var box = shrine.GetComponent<BoxCollider>();
        if (box == null) box = Undo.AddComponent<BoxCollider>(shrine.gameObject);
        Undo.RecordObject(box, "ขยายกล่องชนศาล");
        Bounds all = WorldBounds(root);
        box.center = st.InverseTransformPoint(all.center);
        box.size = new Vector3(all.size.x + 0.2f, all.size.y + 0.1f, all.size.z + 0.2f);

        report.Add("ศาลตายาย: ฐาน + ตัวศาล + ธูป + เทียน 2 เล่ม / ตอนถวาย: ข้าวเหนียวหมู + เปลวเทียน + ไฟ");
        report.Add($"   ตำแหน่งศาล {st.position:F1} (ห่างจุดลงรถ {Flat(st.position - A):F1} ม.) " +
                   $"สูง {hb.size.y:F2} ม. / กว้าง {Mathf.Max(hb.size.x, hb.size.z):F2} ม. / " +
                   (hasShelf ? $"วางธูปเทียนบนแท่นสูง {topC - groundY:F2} ม." : "หาแท่นไม่เจอ → วางธูปเทียนบนพื้นหน้าศาล") +
                   (house == null ? " / ไม่พบโมเดลศาล Salcal" : ""));
        report.Add("   หาศาลไม่เจอ: คลิก ForestShrine ใน Hierarchy แล้วกด F ในหน้า Scene");
    }

    /// <summary>ความสูงศาลทั้งหลัง (เมตร) — ศาลตายายจริงราว 1.6–2 ม. รวมเสา</summary>
    const float ShrineHeight = 1.8f;

    /// <summary>
    /// หาผิวแนวนอนบนโมเดลศาลที่จุด xz นี้ ที่วางของได้: หันขึ้น, มีที่ว่างเหนือหัว ≥ 15 ซม., ไม่สูงเกิน 80% ของศาล (ไม่เอาหลังคา)
    /// ไล่จากบนลงล่างทีละ 2 ซม. → ได้แท่นที่สูงที่สุดที่วางของได้จริง
    /// </summary>
    static bool FindShelf(Vector3 xz, Bounds hb, List<MeshCollider> cols, out float y)
    {
        y = 0f;
        float maxY = hb.min.y + hb.size.y * 0.8f;
        for (float sy = maxY; sy > hb.min.y + 0.15f; sy -= 0.02f)
        {
            var o = new Vector3(xz.x, sy, xz.z);
            if (!Physics.Raycast(o, Vector3.down, out var hit, 0.025f, ~0, QueryTriggerInteraction.Collide)) continue;
            if (!cols.Contains(hit.collider as MeshCollider)) continue;
            if (hit.normal.y < 0.75f) continue;
            // ที่ว่างเหนือผิว (ไม่ชนหลังคา/ผนังภายใน 15 ซม.)
            if (Physics.Raycast(hit.point + Vector3.up * 0.01f, Vector3.up, out var up, 0.15f, ~0, QueryTriggerInteraction.Collide)
                && cols.Contains(up.collider as MeshCollider)) continue;
            y = hit.point.y;
            return true;
        }
        return false;
    }

    // ══════════ กองดิน ══════════
    static IEnumerable<Vector3> ArrangeDigSpots(List<string> report)
    {
        var spots = Object.FindObjectsByType<DigSpotInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                          .OrderBy(s => s.name).ToArray();
        if (spots.Length == 0) yield break;

        // จุดกระจายเป็นกลุ่มด้านขวาหน้าผู้เล่น ใกล้ๆ (ระยะ 3–8 ม.) — ห่างกันอย่างน้อย ~2 ม.
        var slots = new List<Vector2> { new Vector2(3.5f, 2.5f), new Vector2(5f, 5f), new Vector2(6.5f, 2f), new Vector2(4f, 7.5f), new Vector2(7.5f, 4.5f),
                                        new Vector2(8f, 1f), new Vector2(6.5f, 7.5f), new Vector2(2.5f, 5f) };
        var dirtMat = GetLitMaterial("Din_Ver_2", 0.15f);
        var boxMat = GetColorMaterial("ForestRewardBox_Wood", new Color(0.33f, 0.21f, 0.12f), 0.25f);
        int i = 0;
        foreach (var spot in spots)
        {
            Vector2 s = slots[i % slots.Count];
            i++;
            var t = spot.transform;
            Undo.RecordObject(t, "ย้ายกองดิน");
            t.position = At(s.x, s.y);
            t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            t.localScale = Vector3.one;

            // ลบของที่เครื่องมือนี้สร้างไว้รอบก่อน
            foreach (var n in new[] { "dirt_mound", "dug_hole" })
            {
                var old = t.Find(n);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }

            Undo.RecordObject(spot, "ตั้งโมเดลดิน");
            // โมเดลดินเดิม (กล่อง) ซ่อนไว้
            foreach (Transform c in t)
                if (c.name.ToLower().Contains("dirt") && c.name != "dirt_mound") { Undo.RecordObject(c.gameObject, "ซ่อนดินเดิม"); c.gameObject.SetActive(false); }

            var mound = PlaceModel("Din_Ver_2", t, Vector3.zero, maxWidth: 1.1f, overrideMat: dirtMat);
            if (mound != null) { mound.name = "dirt_mound"; SetBottomAt(mound, t.position.y - 0.05f, t.position); spot.dirtModel = mound; }
            var dug = PlaceModel("Din_Small", t, Vector3.zero, maxWidth: 1.0f);
            if (dug != null) { dug.name = "dug_hole"; SetBottomAt(dug, t.position.y - 0.03f, t.position); dug.SetActive(false); spot.dugModel = dug; }

            // กล่องรางวัล: กล่องไม้เล็ก
            var reward = spot.rewardBox;
            if (reward == null) { var rb = spot.GetComponentInChildren<DigRewardBox>(true); if (rb != null) reward = rb.gameObject; }
            if (reward != null)
            {
                Undo.RecordObject(reward.transform, "จัดกล่องรางวัล");
                reward.transform.position = t.position + Vector3.up * 0.2f;
                reward.transform.localScale = new Vector3(0.45f, 0.3f, 0.32f);
                var mf = reward.GetComponent<MeshFilter>();
                if (mf != null) { Undo.RecordObject(mf, "กล่อง"); mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); }
                var rr = reward.GetComponent<MeshRenderer>();
                if (rr != null) { Undo.RecordObject(rr, "สีกล่อง"); rr.sharedMaterial = boxMat; }
                spot.rewardBox = reward;
            }

            var col = spot.GetComponent<BoxCollider>();
            if (col != null) { Undo.RecordObject(col, "กล่องชนกองดิน"); col.center = new Vector3(0f, 0.35f, 0f); col.size = new Vector3(1.2f, 0.8f, 1.2f); }

            // เรืองแสงสีทองตลอด + ไฟส่องพื้น มองเห็นจากไกล (ขุดแล้วดับ — DigSpotGlowSync)
            var hl = spot.GetComponent<ItemHighlighter>();
            if (hl == null) hl = Undo.AddComponent<ItemHighlighter>(spot.gameObject);
            Undo.RecordObject(hl, "ดินเรืองแสง");
            hl.isHighlightActive = true;
            hl.triggerMode = HighlightTriggerMode.AlwaysGlow;
            hl.glowColor = new Color(1f, 0.78f, 0.32f, 1f);
            hl.enableMeshEmission = true;
            hl.minEmissionIntensity = 0.5f;
            hl.maxEmissionIntensity = 1.6f;
            hl.pulseSpeed = 0.6f;
            hl.enablePointLight = true;
            hl.pointLightRange = 3.5f;
            hl.minLightIntensity = 0.8f;
            hl.maxLightIntensity = 2.2f;
            hl.enableSparkles = false;
            hl.enableGentleFloating = false;
            hl.maxEffectDistance = 80f;
            if (mound != null) hl.SetTargetRenderers(mound.GetComponentsInChildren<Renderer>(true));
            if (spot.GetComponent<DigSpotGlowSync>() == null) Undo.AddComponent<DigSpotGlowSync>(spot.gameObject);
            // ระบบดินเดิมจะตั้งค่าไฮไลต์ทับ → ให้ตัวไอเทมใช้ค่าเดียวกัน
            spot.isImportantItem = true;
            spot.highlightTriggerMode = HighlightTriggerMode.AlwaysGlow;
            spot.glowColor = hl.glowColor;
            spot.enablePointLightGlow = true;
            spot.enableSparkles = false;
            EditorUtility.SetDirty(hl);
            EditorUtility.SetDirty(spot);
            yield return t.position;
        }
        report.Add($"กองดิน {spots.Length} จุด: โมเดลดินใหม่ (Din_Ver_2) เรืองแสงทอง + ไฟส่อง (ขุดแล้วดับ) / ขุดแล้ว = Din_Small / กล่องรางวัลไม้ — ห่างผู้เล่น 3–8 ม.");
    }

    // ══════════ จุดฝังเครื่องราง + เทียนพิธี ══════════
    static IEnumerable<Vector3> ArrangeAmulet(List<string> report)
    {
        var amulet = Object.FindFirstObjectByType<AmuletBurialSpot>(FindObjectsInactive.Include);
        if (amulet == null) yield break;
        Vector3 oldPos = amulet.transform.position;
        Vector3 newPos = At(12f, -9f);
        Undo.RecordObject(amulet.transform, "ย้ายจุดฝังเครื่องราง");
        amulet.transform.position = newPos;
        int moved = 0;
        foreach (var c in Object.FindObjectsByType<RitualCandleInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.transform.IsChildOf(amulet.transform)) continue; // ลูกย้ายตามไปเอง
            Undo.RecordObject(c.transform, "ย้ายเทียนพิธี");
            c.transform.position = Ground(newPos + (c.transform.position - oldPos));
            moved++;
        }
        report.Add($"จุดฝังเครื่องราง: ย้ายมาห่างผู้เล่น ~15 ม." + (moved > 0 ? $" (เทียนพิธี {moved} เล่มย้ายตาม)" : ""));
        yield return newPos;
    }

    // ══════════ ต้นไม้ / พุ่มไม้ ══════════
    static void BuildDecor(List<Vector3> keepClear, List<string> report)
    {
        var old = GameObject.Find(DecorRootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(DecorRootName);
        Undo.RegisterCreatedObjectUndo(root, "ของตกแต่งป่า");

        var trees = new[] { "Assets/Prefab/Tree (1).prefab", "Assets/Prefab/tree2.prefab", "Assets/Prefab/TreeRex.prefab" }
                    .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(p => p != null).ToArray();
        var bushes = Enumerable.Range(1, 5).Select(i => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/YughuesFreeBushes2018/Prefabs/P_Bush0{i}.prefab"))
                    .Where(p => p != null).ToArray();

        var rng = new System.Random(20261115);
        int t = Scatter(trees, 30, 8f, 34f, 4f, 3.5f, keepClear, root.transform, rng, 0.85f, 1.35f);
        int b = Scatter(bushes, 55, 3f, 24f, 2.2f, 1.8f, keepClear, root.transform, rng, 0.7f, 1.3f);
        report.Add($"ของตกแต่ง: ต้นไม้ {t} ต้น + พุ่มไม้ {b} พุ่ม (อยู่ใน {DecorRootName} — ลบทั้งกลุ่มได้ถ้าไม่ชอบ)");
    }

    static int Scatter(GameObject[] prefabs, int count, float minR, float maxR, float clearR, float pathR,
                       List<Vector3> keepClear, Transform parent, System.Random rng, float sMin, float sMax)
    {
        if (prefabs.Length == 0) return 0;
        int placed = 0, tries = 0;
        Vector3 pathEnd = A + fwd * 17f;
        var placedPts = new List<Vector3>();
        while (placed < count && tries++ < count * 30)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Lerp(minR, maxR, Mathf.Sqrt((float)rng.NextDouble()));
            Vector3 p = A + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;
            if (keepClear.Any(k => Flat(p - k) < clearR)) continue;
            if (DistToSegment(p, A, pathEnd) < pathR) continue;          // เว้นทางเดินเข้าป่า
            if (placedPts.Any(q => Flat(p - q) < clearR * 0.6f)) continue; // ไม่ซ้อนกัน
            p = Ground(p);
            var prefab = prefabs[rng.Next(prefabs.Length)];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "ของตกแต่ง");
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            go.transform.localScale = prefab.transform.localScale * Mathf.Lerp(sMin, sMax, (float)rng.NextDouble());
            placedPts.Add(p);
            placed++;
        }
        return placed;
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        p.y = a.y = b.y = 0f;
        Vector3 ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
        return Vector3.Distance(p, a + ab * t);
    }

    // ══════════ ช่วยวางโมเดล ══════════
    static GameObject NewChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject PlaceModel(string modelName, GameObject parent, Vector3 worldPos, float height = 0f, float maxWidth = 0f, Material overrideMat = null, string folderHint = null)
        => PlaceModel(modelName, parent.transform, worldPos, height, maxWidth, overrideMat, folderHint);

    /// <summary>หาโมเดล (.fbx) ตามชื่อไฟล์ วางเป็นลูก ปรับขนาดตามความสูง/ความกว้างที่ต้องการ และใส่ Material</summary>
    static GameObject PlaceModel(string modelName, Transform parent, Vector3 worldPos, float height = 0f, float maxWidth = 0f, Material overrideMat = null, string folderHint = null)
    {
        var path = FindModelPath(modelName, folderHint);
        if (path == null) { Debug.LogWarning($"[ForestSceneSetupTool] หาโมเดล '{modelName}' ไม่เจอ"); return null; }
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        Undo.RegisterCreatedObjectUndo(go, modelName);
        go.transform.position = worldPos;

        var mat = overrideMat != null ? overrideMat : GetLitMaterial(modelName, modelName == "fire" ? -1f : 0.2f, System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        if (mat != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();

        Bounds b = WorldBounds(go);
        float k = 1f;
        if (height > 0f && b.size.y > 1e-4f) k = height / b.size.y;
        else if (maxWidth > 0f && Mathf.Max(b.size.x, b.size.z) > 1e-4f) k = maxWidth / Mathf.Max(b.size.x, b.size.z);
        go.transform.localScale *= k;
        return go;
    }

    static void SetBottomAt(GameObject go, float bottomY, Vector3 centerXZ)
    {
        if (go == null) return;
        Bounds b = WorldBounds(go);
        go.transform.position += new Vector3(centerXZ.x - b.center.x, bottomY - b.min.y, centerXZ.z - b.center.z);
    }

    /// <summary>ขอบเขตของโมเดล — นับเฉพาะชิ้นที่แสดงอยู่ (ชิ้นที่ซ่อนไว้ Unity ให้ขอบเขตอยู่ที่ 0,0,0 → กล่องชนเคยยาว 174 ม.)</summary>
    static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled).ToArray();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static string FindModelPath(string modelName, string folderHint)
    {
        var guids = AssetDatabase.FindAssets(modelName + " t:Model");
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
                         .Where(p => System.IO.Path.GetFileNameWithoutExtension(p) == modelName).ToList();
        if (folderHint != null) { var hinted = paths.FirstOrDefault(p => p.Contains(folderHint)); if (hinted != null) return hinted; }
        return paths.FirstOrDefault();
    }

    static Light AddLight(Transform parent, Vector3 pos, float intensity, float range, string name)
    {
        var go = NewChild(name, parent);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.62f, 0.3f);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    // ══════════ Material ══════════
    /// <summary>
    /// ใช้ Material ที่มีอยู่ในโฟลเดอร์โมเดล ถ้าไม่มีสร้างใหม่ (URP Lit) จาก Texture Albedo/BaseColor + Normal
    /// smoothness &lt; 0 = ใช้ของเดิมไม่แตะ (เช่น เปลวไฟที่เรืองแสงอยู่แล้ว)
    /// </summary>
    static Material GetLitMaterial(string baseName, float smoothness, string folder = null)
    {
        if (folder == null)
        {
            var mp = FindModelPath(baseName, null);
            if (mp == null) return null;
            folder = System.IO.Path.GetDirectoryName(mp).Replace('\\', '/');
        }
        var mats = AssetDatabase.FindAssets("t:Material", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                   .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).StartsWith(baseName)).ToList();
        string matPath = mats.FirstOrDefault(p => p.Contains("Albedo") || p.Contains("BaseColor") || p.EndsWith("_Lit.mat")) ?? mats.FirstOrDefault();
        Material mat = matPath != null ? AssetDatabase.LoadAssetAtPath<Material>(matPath) : null;
        if (smoothness < 0f && mat != null) return mat;

        var texs = AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                   .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).StartsWith(baseName)).ToList();
        string albedo = texs.FirstOrDefault(p => p.Contains("Albedo") || p.Contains("BaseColor"));
        string normal = texs.FirstOrDefault(p => p.Contains("Normal"));

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (mat == null)
        {
            if (lit == null || albedo == null) return null;
            string dir = folder + "/Materials";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(folder, "Materials");
            mat = new Material(lit) { name = baseName + "_Lit" };
            AssetDatabase.CreateAsset(mat, dir + "/" + baseName + "_Lit.mat");
        }
        if (lit != null && mat.shader != lit) mat.shader = lit;

        if (albedo != null && mat.GetTexture("_BaseMap") == null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedo);
            mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        if (normal != null && mat.GetTexture("_BumpMap") == null)
        {
            var ti = AssetImporter.GetAtPath(normal) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            mat.EnableKeyword("_NORMALMAP");
        }
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material GetColorMaterial(string name, Color color, float smoothness)
    {
        string path = "Assets/Materiasl/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return null;
            if (!AssetDatabase.IsValidFolder("Assets/Materiasl")) AssetDatabase.CreateFolder("Assets", "Materiasl");
            mat = new Material(lit) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
