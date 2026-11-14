using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// เครื่องมือตั้งค่าท่าเดิน NPC ทุกตัวในคลิกเดียว (ทำแบบเดียวกับหนูแดง / อ้ายเมือง)
/// เมนู: Tools > Saharan > ตั้งค่าท่าเดิน NPC ทุกตัว
///
/// สำหรับทุกโฟลเดอร์ใน Models_Animation_Charecter ที่ชื่อยังไม่มี ✅ :
///  1. ปิด Loop Time ของท่าเดิน (เดินรอบเดียวแล้วค้าง) และตัดให้สั้นเท่าหนูแดง (เฟรม 3–56)
///  2. สร้าง/ตั้งค่า Animator Controller: สวิตช์ isWalking, กล่อง idle (ค้างท่า), กล่อง walk, ลูกศรไป-กลับ
///  3. สร้าง prefab ตัวละครพร้อม Animator ไว้ที่ Assets/Prefab/walk (ถ้ามีอยู่แล้วจะแค่ใส่ controller ให้)
///  4. ใส่ ✅ ต่อท้ายชื่อโฟลเดอร์เมื่อเสร็จ
/// </summary>
public static class NPCWalkSetupTool
{
    const string CharacterRoot = "Assets/Models_Animation_Charecter";
    const string PrefabParent = "Assets/Prefab";
    const string PrefabFolder = "Assets/Prefab/walk";
    const string DoneMark = "✅";
    const string WalkParam = "isWalking";
    const float TrimFirstFrame = 3f;  // ช่วงเฟรมเดียวกับที่ตัดไว้ใน nudaeng_walk
    const float TrimLastFrame = 56f;

    [MenuItem("Tools/Saharan/ตั้งค่าท่าเดิน NPC ทุกตัว")]
    public static void SetupAll()
    {
        if (!AssetDatabase.IsValidFolder(CharacterRoot))
        {
            EditorUtility.DisplayDialog("ตั้งค่าท่าเดิน NPC", "ไม่พบโฟลเดอร์ " + CharacterRoot, "ตกลง");
            return;
        }

        var folders = AssetDatabase.GetSubFolders(CharacterRoot)
            .Where(f => !Path.GetFileName(f).Contains(DoneMark))
            .ToList();

        if (folders.Count == 0)
        {
            EditorUtility.DisplayDialog("ตั้งค่าท่าเดิน NPC", "ทุกตัวมี ✅ แล้ว ไม่มีอะไรต้องทำ", "ตกลง");
            return;
        }

        string list = string.Join("\n", folders.Select(f => "• " + Path.GetFileName(f)));
        bool go = EditorUtility.DisplayDialog(
            "ตั้งค่าท่าเดิน NPC",
            "จะตั้งค่าตัวละครเหล่านี้ให้เหมือนหนูแดง:\n\n" + list +
            "\n\n(ท่าเดินเล่นรอบเดียวแล้วค้าง ตัดสั้นเท่าหนูแดง, สวิตช์ isWalking, prefab ใน Prefab/walk, ใส่ ✅ ที่ชื่อโฟลเดอร์)",
            "เริ่มเลย", "ยกเลิก");
        if (!go) return;

        AssetDatabase.SaveAssets();

        var report = new StringBuilder();
        try
        {
            for (int i = 0; i < folders.Count; i++)
            {
                string name = Path.GetFileName(folders[i]);
                EditorUtility.DisplayProgressBar("ตั้งค่าท่าเดิน NPC", name, (float)i / folders.Count);
                try
                {
                    report.AppendLine(SetupCharacter(folders[i]));
                }
                catch (System.Exception e)
                {
                    report.AppendLine("❌ " + name + ": " + e.Message);
                    Debug.LogException(e);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NPCWalkSetup]\n" + report);
        EditorUtility.DisplayDialog("ตั้งค่าท่าเดิน NPC เสร็จแล้ว", report.ToString(), "ตกลง");
    }

    static string SetupCharacter(string folder)
    {
        string name = Path.GetFileName(folder);

        // ── หาไฟล์ท่าเดิน .fbx ในโฟลเดอร์ตัวละคร
        string fbxPath = AssetDatabase.FindAssets("t:Model", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.ToLower().EndsWith(".fbx") && Path.GetFileName(p).ToLower().Contains("walk"));
        if (fbxPath == null)
            return "⚠️ " + name + ": ไม่เจอไฟล์ท่าเดิน (.fbx ที่มีคำว่า walk) — ข้ามไป";

        // ── 1. ปิด Loop Time (เดินรอบเดียวแล้วค้าง)
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
            return "⚠️ " + name + ": อ่านค่าไฟล์ " + Path.GetFileName(fbxPath) + " ไม่ได้ — ข้ามไป";

        importer.importAnimation = true;
        var clipSettings = importer.clipAnimations;
        if (clipSettings == null || clipSettings.Length == 0)
            clipSettings = importer.defaultClipAnimations;
        if (clipSettings == null || clipSettings.Length == 0)
            return "⚠️ " + name + ": ไฟล์ " + Path.GetFileName(fbxPath) + " ไม่มีท่าเดินข้างใน — ข้ามไป";

        foreach (var c in clipSettings)
        {
            c.loopTime = false;
            // ตัดท่าเดินให้สั้นลงเท่าหนูแดง (เฟรม 3–56) เฉพาะคลิปที่ยาวกว่านั้น
            if (c.lastFrame > TrimLastFrame)
            {
                c.firstFrame = TrimFirstFrame;
                c.lastFrame = TrimLastFrame;
            }
        }
        importer.clipAnimations = clipSettings;
        importer.SaveAndReimport();

        var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null)
            return "⚠️ " + name + ": โหลดท่าเดินจาก " + Path.GetFileName(fbxPath) + " ไม่ได้ — ข้ามไป";

        // ── 2. Animator Controller (ใช้ตัวเดิมในโฟลเดอร์ถ้ามี ไม่งั้นสร้างใหม่)
        string ctrlPath = AssetDatabase.FindAssets("t:AnimatorController", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault();
        if (ctrlPath == null)
        {
            string baseName = Path.GetFileNameWithoutExtension(fbxPath).Replace("_walk", "");
            ctrlPath = folder + "/" + baseName + "_controller.controller";
        }

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        BuildController(ctrl, clip);

        // ── 3. Prefab ตัวละคร + Animator
        string prefabNote = MakePrefab(fbxPath, ctrl);

        // ── 4. ใส่ ✅ ที่ชื่อโฟลเดอร์
        string renameError = AssetDatabase.RenameAsset(folder, name + DoneMark);
        string renameNote = string.IsNullOrEmpty(renameError) ? "" : " (เปลี่ยนชื่อโฟลเดอร์ไม่ได้: " + renameError + ")";

        return "✅ " + name + ": " + Path.GetFileName(ctrlPath) + ", " + prefabNote + renameNote;
    }

    /// <summary>สร้างโครงเหมือน nudaeng_controller: isWalking + idle (ค้างท่า) ⇄ walk</summary>
    static void BuildController(AnimatorController ctrl, AnimationClip clip)
    {
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        ctrl.AddParameter(WalkParam, AnimatorControllerParameterType.Bool);

        if (ctrl.layers.Length == 0) ctrl.AddLayer("Base Layer");
        var sm = ctrl.layers[0].stateMachine;

        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var t in sm.entryTransitions.ToArray()) sm.RemoveEntryTransition(t);
        foreach (var sub in sm.stateMachines.ToArray()) sm.RemoveStateMachine(sub.stateMachine);

        var idle = sm.AddState("idle", new Vector3(30, 210, 0));
        idle.writeDefaultValues = false; // ค้างท่าสุดท้าย ไม่เด้งเป็นท่ากางแขน

        var walk = sm.AddState("walk", new Vector3(346, 265, 0));
        walk.motion = clip;

        sm.defaultState = idle;

        var toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.hasFixedDuration = true;
        toWalk.duration = 0.25f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, WalkParam);

        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.hasFixedDuration = true;
        toIdle.duration = 0.25f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, WalkParam);

        EditorUtility.SetDirty(ctrl);
    }

    // ════════════════════════════════════════════════════════════════════
    // เมนูที่ 2: ขนาดเท่ากัน + texture + Animator + วางทุกตัวในฉาก
    // ════════════════════════════════════════════════════════════════════

    const string ReferencePrefab = "Assets/Prefab/walk/nudaeng_walk.prefab"; // ใช้ความสูงหนูแดงเป็นมาตรฐาน
    const string TemplateMaterialName = "nu_daeng_standardSurface2_BaseColor";
    const string SceneGroupName = "ตัวละคร NPC";
    const float FallbackHeight = 1.6f;
    const float Spacing = 1.0f;

    [MenuItem("Tools/Saharan/จัดตัวละครทุกตัว (ขนาด + texture + วางในฉาก)")]
    public static void ArrangeAll()
    {
        var folders = AssetDatabase.GetSubFolders(CharacterRoot).ToList();
        string list = string.Join("\n", folders.Select(f => "• " + Path.GetFileName(f)));
        bool go = EditorUtility.DisplayDialog(
            "จัดตัวละครทุกตัว",
            "จะทำกับตัวละครเหล่านี้:\n\n" + list +
            "\n\n1. ปรับให้สูงเท่าหนูแดงทุกตัว\n2. ใส่ texture ของแต่ละตัว\n3. ใส่ Animator + controller ของตัวเอง\n" +
            "4. วางทุกตัวเรียงกันในฉาก ใต้กลุ่ม \"" + SceneGroupName + "\"\n\n" +
            "ฉากจะยังไม่ถูกเซฟ ถ้าไม่ชอบกด Ctrl+Z ได้",
            "เริ่มเลย", "ยกเลิก");
        if (!go) return;

        AssetDatabase.SaveAssets();

        float targetHeight = MeasureReferenceHeight();
        var template = AssetDatabase.FindAssets(TemplateMaterialName + " t:Material")
            .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(m => m != null);

        var report = new StringBuilder();
        report.AppendLine("ความสูงมาตรฐาน (หนูแดง): " + targetHeight.ToString("F2") + " เมตร\n");
        var prefabs = new System.Collections.Generic.List<GameObject>();

        try
        {
            for (int i = 0; i < folders.Count; i++)
            {
                string name = Path.GetFileName(folders[i]);
                EditorUtility.DisplayProgressBar("จัดตัวละครทุกตัว", name, (float)i / folders.Count);
                try
                {
                    GameObject prefab;
                    report.AppendLine(ArrangeCharacter(folders[i], targetHeight, template, out prefab));
                    if (prefab != null) prefabs.Add(prefab);
                }
                catch (System.Exception e)
                {
                    report.AppendLine("❌ " + name + ": " + e.Message);
                    Debug.LogException(e);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        PlaceInScene(prefabs);
        report.AppendLine("\nวางในฉากแล้ว " + prefabs.Count + " ตัว (อย่าลืมกด Ctrl+S เพื่อเซฟฉาก)");

        Debug.Log("[NPCArrange]\n" + report);
        EditorUtility.DisplayDialog("จัดตัวละครเสร็จแล้ว", report.ToString(), "ตกลง");
    }

    static string ArrangeCharacter(string folder, float targetHeight, Material template, out GameObject prefabAsset)
    {
        prefabAsset = null;
        string name = Path.GetFileName(folder);

        string fbxPath = AssetDatabase.FindAssets("t:Model", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.ToLower().EndsWith(".fbx") && Path.GetFileName(p).ToLower().Contains("walk"));
        if (fbxPath == null) return "⚠️ " + name + ": ไม่เจอไฟล์ .fbx — ข้ามไป";

        var ctrl = AssetDatabase.FindAssets("t:AnimatorController", new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(c => c != null);
        if (ctrl == null) return "⚠️ " + name + ": ยังไม่มี controller (กดเมนู 'ตั้งค่าท่าเดิน NPC ทุกตัว' ก่อน) — ข้ามไป";

        // ── Material: ใช้ของเดิมในโฟลเดอร์ ถ้าไม่มีให้สร้างจากแบบของหนูแดง
        var tex = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(t => t != null);

        // เฉพาะไฟล์ .mat จริง (ไม่เอา material ที่ฝังอยู่ใน .fbx เพราะแก้ไม่ได้)
        var mat = AssetDatabase.FindAssets("t:Material", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.ToLower().EndsWith(".mat"))
            .Select(p => AssetDatabase.LoadAssetAtPath<Material>(p))
            .FirstOrDefault(m => m != null);
        string matNote;
        if (mat == null)
        {
            if (tex == null) return "⚠️ " + name + ": ไม่เจอไฟล์ texture (.png) — ข้ามไป";
            mat = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            string matPath = folder + "/" + tex.name + ".mat";
            AssetDatabase.CreateAsset(mat, matPath);
            matNote = "สร้าง material ใหม่";
        }
        else
        {
            matNote = "ใช้ material เดิม";
        }
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(mat);
        }

        // ── Prefab: Animator + controller, material, ขนาด
        string prefabPath = PrefabFolder + "/" + Path.GetFileNameWithoutExtension(fbxPath) + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) MakePrefab(fbxPath, ctrl);

        float scale;
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }

            Vector3 oldScale = root.transform.localScale;
            root.transform.localScale = Vector3.one;
            float h = MeasureHeight(root);
            if (h > 0.0001f)
            {
                scale = targetHeight / h;
                root.transform.localScale = Vector3.one * scale;
            }
            else
            {
                // วัดความสูงไม่ได้ — คงขนาดเดิมไว้
                root.transform.localScale = oldScale;
                scale = oldScale.y;
            }
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return "✅ " + name + ": scale " + scale.ToString("F2") + ", " + matNote + (tex != null ? " + " + tex.name : "") + ", " + ctrl.name;
    }

    static float MeasureReferenceHeight()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ReferencePrefab) == null) return FallbackHeight;
        var root = PrefabUtility.LoadPrefabContents(ReferencePrefab);
        try
        {
            float h = MeasureHeight(root);
            return h > 0.05f ? h : FallbackHeight;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static float MeasureHeight(GameObject root)
    {
        bool found = false;
        Bounds b = new Bounds();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            if (r.bounds.size.sqrMagnitude < 1e-10f) continue;
            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }
        return found ? b.size.y : 0f;
    }

    static void PlaceInScene(System.Collections.Generic.List<GameObject> prefabs)
    {
        if (prefabs.Count == 0) return;

        // จุดเริ่ม: ตำแหน่งหนูแดงที่วางอยู่ในฉาก ถ้าไม่มีใช้กลางมุมมอง Scene
        Vector3 start = Vector3.zero;
        var existing = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(a => a.gameObject.name == "nudaeng_walk");
        if (existing != null) start = existing.transform.position;
        else if (SceneView.lastActiveSceneView != null) start = SceneView.lastActiveSceneView.pivot;

        // ลบกลุ่มเดิม (ถ้ากดซ้ำ) แล้วสร้างใหม่
        var old = GameObject.Find(SceneGroupName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var group = new GameObject(SceneGroupName);
        Undo.RegisterCreatedObjectUndo(group, "จัดตัวละครทุกตัว");
        group.transform.position = start;

        for (int i = 0; i < prefabs.Count; i++)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], group.transform);
            Undo.RegisterCreatedObjectUndo(inst, "จัดตัวละครทุกตัว");
            inst.transform.localPosition = new Vector3(i * Spacing, 0f, 0f);
            inst.transform.localRotation = Quaternion.identity;
        }

        Selection.activeGameObject = group;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(group.scene);
    }

    // ════════════════════════════════════════════════════════════════════
    // เมนูที่ 3: เดินวนตลอดทาง + นั่งเมื่อถึงที่หมาย
    // ท่าในไฟล์ fbx = เดิน 4 ก้าว (เฟรม 3–56) ต่อด้วยคุกเข่านั่ง (เฟรม 56–จบ)
    // แยกเป็น 2 คลิป: "walk" (วน) และ "sit" (เล่นครั้งเดียวแล้วค้าง)
    // ════════════════════════════════════════════════════════════════════

    const float WalkStartFrame = 7f;   // ช่วงท่าเดินที่วนได้เนียน (ตั้งตามที่ทดลองใน Unity: 7–48)
    const float WalkEndFrame = 48f;
    const float SitStartFrame = 56f;   // ถ้าตัวไหนเริ่มนั่งเร็ว/ช้ากว่านี้ ปรับใน Import Settings ของตัวนั้นได้
    const string DefaultNpcFolder = "Assets/Npc_prefab"; // แก้วตาตัวที่เกมใช้เป็น NPC ปกติ

    [MenuItem("Tools/Saharan/เดินวนตลอดทาง + นั่งเมื่อถึง (ทุกตัว)")]
    public static void WalkLoopSitAll()
    {
        var folders = AssetDatabase.GetSubFolders(CharacterRoot).ToList();
        if (AssetDatabase.IsValidFolder(DefaultNpcFolder)) folders.Add(DefaultNpcFolder);

        string list = string.Join("\n", folders.Select(f => "• " + Path.GetFileName(f)));
        bool go = EditorUtility.DisplayDialog(
            "เดินวน + นั่งเมื่อถึง",
            "จะตั้งค่าตัวละครเหล่านี้:\n\n" + list +
            "\n\n• ท่าเดิน (เฟรม 7–48) ย่ำอยู่กับที่แล้ววนเนียนๆ ตลอดทาง (ไม่เด้งถอยหลัง)\n• ถึงที่หมายแล้วค่อยๆ นั่ง (เฟรม 56–จบ) แล้วค้าง\n• ตอนเดินออก ลุกขึ้นช้าๆ แล้วเดินออก\n• ย้าย ReceptionPoint มาตรงกลางกล้อง",
            "เริ่มเลย", "ยกเลิก");
        if (!go) return;

        AssetDatabase.SaveAssets();
        var report = new StringBuilder();
        try
        {
            for (int i = 0; i < folders.Count; i++)
            {
                string name = Path.GetFileName(folders[i]);
                EditorUtility.DisplayProgressBar("เดินวน + นั่งเมื่อถึง", name, (float)i / folders.Count);
                try { report.AppendLine(SetupWalkLoopSit(folders[i])); }
                catch (System.Exception e)
                {
                    report.AppendLine("❌ " + name + ": " + e.Message);
                    Debug.LogException(e);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        report.AppendLine("\n" + CenterReceptionPoint());
        Debug.Log("[NPCWalkLoopSit]\n" + report);
        EditorUtility.DisplayDialog("เดินวน + นั่งเมื่อถึง เสร็จแล้ว", report.ToString(), "ตกลง");
    }

    static string SetupWalkLoopSit(string folder)
    {
        string name = Path.GetFileName(folder);

        string fbxPath = AssetDatabase.FindAssets("t:Model", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.ToLower().EndsWith(".fbx") && Path.GetFileName(p).ToLower().Contains("walk"));
        if (fbxPath == null) return "⚠️ " + name + ": ไม่เจอไฟล์ .fbx — ข้ามไป";

        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return "⚠️ " + name + ": อ่านค่าไฟล์ fbx ไม่ได้ — ข้ามไป";

        var defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0) return "⚠️ " + name + ": ไม่มีท่าในไฟล์ — ข้ามไป";

        var baseClip = defaults[0];
        float takeEnd = baseClip.lastFrame;
        bool hasSit = takeEnd > SitStartFrame + 2f;

        // ── ให้ท่าเดินย่ำอยู่กับที่: ดึงการเคลื่อนที่ไปข้างหน้าของสะโพกออก (สคริปต์เป็นคนพาเดินเอง)
        //    ถ้าไม่ทำ ตัวละครจะเด้งถอยหลังทุกครั้งที่ท่าเดินวนกลับต้นคลิป
        string hipsPath = FindHipsPath(fbxPath);
        if (hipsPath != null) importer.motionNodeName = hipsPath;

        var walk = defaults[0];
        walk.name = "walk";
        walk.firstFrame = WalkStartFrame;
        walk.lastFrame = Mathf.Min(WalkEndFrame, takeEnd);
        walk.loopTime = true;
        walk.loopPose = true;              // เกลี่ยรอยต่อตอนวนกลับต้นคลิปให้เนียน
        ApplyInPlace(walk);

        ModelImporterClipAnimation[] clipSettings;
        if (hasSit)
        {
            var sit = importer.defaultClipAnimations[0]; // สำเนาใหม่ของค่าเริ่มต้น
            sit.name = "sit";
            sit.firstFrame = SitStartFrame;
            sit.lastFrame = takeEnd;
            sit.loopTime = false;
            ApplyInPlace(sit);
            clipSettings = new[] { walk, sit };
        }
        else
        {
            clipSettings = new[] { walk };
        }

        importer.importAnimation = true;
        importer.clipAnimations = clipSettings;
        importer.SaveAndReimport();

        var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToList();
        var walkClip = clips.FirstOrDefault(c => c.name == "walk");
        var sitClip = clips.FirstOrDefault(c => c.name == "sit");
        if (walkClip == null) return "⚠️ " + name + ": สร้างคลิป walk ไม่สำเร็จ — ข้ามไป";

        string ctrlPath = AssetDatabase.FindAssets("t:AnimatorController", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        if (ctrlPath == null)
            ctrlPath = folder + "/" + Path.GetFileNameWithoutExtension(fbxPath).Replace("_walk", "") + "_controller.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        BuildWalkSitController(ctrl, walkClip, sitClip);

        string driftNote = DescribeDrift(fbxPath, walkClip, hipsPath);

        return "✅ " + name + ": " + (hasSit ? "เดินวน " + WalkStartFrame + "–" + walk.lastFrame + " + นั่ง " + SitStartFrame + "–" + takeEnd : "เดินวนอย่างเดียว (ไฟล์ไม่มีช่วงนั่ง)") + ", " + driftNote;
    }

    /// <summary>ตำแหน่งแนวราบของสะโพกไม่ติดไปกับท่า, แต่ความสูง (ย่อตัว/นั่ง) และการหมุนยังอยู่ในท่า</summary>
    static void ApplyInPlace(ModelImporterClipAnimation c)
    {
        c.lockRootPositionXZ = false;   // Bake Into Pose XZ = ปิด → ดึงการเดินไปข้างหน้าออก
        c.lockRootHeightY = true;       // Bake Into Pose Y = เปิด → ท่านั่งยังลดตัวลงพื้นได้
        c.lockRootRotation = true;      // Bake Into Pose Rotation = เปิด
        c.keepOriginalPositionY = true;
        c.keepOriginalPositionXZ = false;
        c.keepOriginalOrientation = true;
    }

    static string FindHipsPath(string fbxPath)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (model == null) return null;
        var bones = model.GetComponentsInChildren<Transform>(true);
        var hips = bones.FirstOrDefault(t => t.name.ToLower().EndsWith("hips") && !t.name.ToLower().Contains("ctrl"))
                ?? bones.FirstOrDefault(t => t.name.ToLower().Contains("pelvis"));
        return hips == null ? null : AnimationUtility.CalculateTransformPath(hips, model.transform);
    }

    /// <summary>วัดว่าสะโพกยังไหลไปข้างหน้าในท่าเดินหรือไม่ (เทียบกับความสูงตัว)</summary>
    static string DescribeDrift(string fbxPath, AnimationClip clip, string hipsPath)
    {
        if (hipsPath == null) return "⚠️ หากระดูกสะโพกไม่เจอ (ท่าเดินอาจเด้งตอนวน)";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var inst = Object.Instantiate(model);
        inst.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var hips = inst.transform.Find(hipsPath);
            if (hips == null) return "⚠️ ตรวจท่าเดินไม่ได้";
            float height = Mathf.Max(0.0001f, MeasureHeight(inst));
            clip.SampleAnimation(inst, 0f);
            Vector3 a = hips.position;
            clip.SampleAnimation(inst, clip.length);
            Vector3 b = hips.position;
            float drift = new Vector2(b.x - a.x, b.z - a.z).magnitude / height;
            return drift < 0.15f ? "ย่ำอยู่กับที่ ✓" : "⚠️ ท่าเดินยังไหลไปข้างหน้า (" + drift.ToString("F2") + " เท่าความสูง)";
        }
        finally
        {
            Object.DestroyImmediate(inst);
        }
    }

    /// <summary>ย้าย ReceptionPoint ให้อยู่ตรงกลางหน้ากล้อง (ระยะเท่าเดิม ความสูงเท่าเดิม)</summary>
    static string CenterReceptionPoint()
    {
        var rp = GameObject.Find("ReceptionPoint");
        if (rp == null) return "⚠️ ไม่เจอ ReceptionPoint ในฉากที่เปิดอยู่ (เปิดฉาก GamePlay ก่อน)";

        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo != null) cam = camGo.GetComponent<Camera>();
        }
        if (cam == null) return "⚠️ ไม่เจอกล้อง Main Camera";

        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return "⚠️ กล้องก้มตรงลงพื้น จัดกลางไม่ได้";
        fwd.Normalize();

        Vector3 camPos = cam.transform.position;
        Vector3 old = rp.transform.position;
        float dist = new Vector2(old.x - camPos.x, old.z - camPos.z).magnitude;
        Vector3 target = camPos + fwd * dist;

        Undo.RecordObject(rp.transform, "จัด ReceptionPoint กลางกล้อง");
        rp.transform.position = new Vector3(target.x, old.y, target.z);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rp.scene);

        float moved = new Vector2(target.x - old.x, target.z - old.z).magnitude;
        return "ย้าย ReceptionPoint มาตรงกลางกล้อง (ขยับ " + moved.ToString("F2") + " ม.) — กด Ctrl+S เพื่อเซฟฉาก";
    }

    /// <summary>walk (วน, ท่าเริ่มต้น) —isWalking=false→ sit (ค้างท่านั่ง) —isWalking=true→ walk</summary>
    static void BuildWalkSitController(AnimatorController ctrl, AnimationClip walkClip, AnimationClip sitClip)
    {
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        ctrl.AddParameter(WalkParam, AnimatorControllerParameterType.Bool);

        if (ctrl.layers.Length == 0) ctrl.AddLayer("Base Layer");
        var sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var t in sm.entryTransitions.ToArray()) sm.RemoveEntryTransition(t);
        foreach (var sub in sm.stateMachines.ToArray()) sm.RemoveStateMachine(sub.stateMachine);

        var walk = sm.AddState("walk", new Vector3(300, 120, 0));
        walk.motion = walkClip;
        sm.defaultState = walk; // NPC เกิดมาก็เดินทันที

        // ไม่มีช่วงนั่ง → ใช้กล่องว่างค้างท่าแทน
        var rest = sm.AddState(sitClip != null ? "sit" : "idle", new Vector3(300, 260, 0));
        rest.motion = sitClip;
        rest.writeDefaultValues = false;

        var toRest = walk.AddTransition(rest);
        toRest.hasExitTime = false;
        toRest.hasFixedDuration = true;
        toRest.duration = 0.3f;       // หยุดเดินแล้วค่อยๆ เข้าท่านั่ง
        toRest.AddCondition(AnimatorConditionMode.IfNot, 0, WalkParam);

        var toWalk = rest.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.hasFixedDuration = true;
        toWalk.duration = 0.6f;       // ลุกขึ้นช้าๆ (สคริปต์รอให้ลุกก่อนค่อยเดิน)
        toWalk.AddCondition(AnimatorConditionMode.If, 0, WalkParam);

        EditorUtility.SetDirty(ctrl);
    }

    static string MakePrefab(string fbxPath, AnimatorController ctrl)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder(PrefabParent, "walk");

        string prefabPath = PrefabFolder + "/" + Path.GetFileNameWithoutExtension(fbxPath) + ".prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var a = root.GetComponentInChildren<Animator>();
                if (a == null) a = root.AddComponent<Animator>();
                a.runtimeAnimatorController = ctrl;
                a.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "อัปเดต prefab " + Path.GetFileName(prefabPath);
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            var anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(inst);
        }
        return "สร้าง prefab " + Path.GetFileName(prefabPath);
    }
}
