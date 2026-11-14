using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// วางจุดเล็ง "FacePoint" ไว้ที่หน้าผีใน prefab Anomaly ทุกตัว (ให้กล้องตอนผีโผล่ซูมไปที่หน้าได้ตรง)
/// เมนู: Tools > Saharan > วางจุดเล็งหน้าผี (FacePoint)
/// - มีกระดูกหัว → วางที่หัว
/// - ไม่มี → วางที่ส่วนบนด้านหน้าของตัวผี
/// - มี FacePoint อยู่แล้ว → ไม่แตะ (กันทับจุดที่ปรับเอง)
/// </summary>
public static class GhostFacePointTool
{
    const string PointName = "FacePoint";

    [MenuItem("Tools/Saharan/วางจุดเล็งหน้าผี (FacePoint)")]
    public static void PlaceAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("วางจุดเล็งหน้าผี", "หยุดเกม (ปุ่ม Play) ก่อน แล้วค่อยกดเมนูนี้", "ตกลง");
            return;
        }

        // prefab ผีจาก GhostCurseManager ในฉาก + ไฟล์ชื่อ Anomaly* ในโปรเจกต์
        var paths = new System.Collections.Generic.HashSet<string>();
        var gcm = Object.FindFirstObjectByType<GhostCurseManager>(FindObjectsInactive.Include);
        if (gcm != null && gcm.ghostPrefabs != null)
        {
            foreach (var p in gcm.ghostPrefabs)
            {
                if (p == null) continue;
                string path = AssetDatabase.GetAssetPath(p);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
        }
        foreach (var guid in AssetDatabase.FindAssets("Anomaly t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("Anomaly")) paths.Add(path);
        }

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("วางจุดเล็งหน้าผี", "ไม่เจอ prefab ผี (Anomaly)", "ตกลง");
            return;
        }

        if (!EditorUtility.DisplayDialog("วางจุดเล็งหน้าผี",
            "จะวาง FacePoint ที่หน้าผีใน prefab เหล่านี้:\n\n" + string.Join("\n", paths.Select(p => "• " + System.IO.Path.GetFileName(p))) +
            "\n\n(ตัวที่มี FacePoint อยู่แล้วจะไม่ถูกแตะ)", "เริ่มเลย", "ยกเลิก")) return;

        var report = new StringBuilder();
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Transform head = FindHeadBone(root);
                var existing = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == PointName);
                if (existing != null)
                {
                    // ของเก่าติดกับตัวผี (ไม่ขยับตามหัว) → ย้ายไปติดกับกระดูกหัว โดยคงตำแหน่งเดิม
                    if (head != null && existing.parent != head)
                    {
                        existing.SetParent(head, true);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        report.AppendLine("✅ " + name + ": ย้าย FacePoint ไปติดกับหัว (" + head.name + ") ให้ขยับตามท่า");
                    }
                    else
                    {
                        report.AppendLine("• " + name + ": มี FacePoint อยู่แล้ว ไม่แตะ");
                    }
                    continue;
                }

                string how;
                Vector3 face = FindFace(root, out how);
                var fp = new GameObject(PointName);
                fp.transform.SetParent(head != null ? head : root.transform, true);
                fp.transform.position = face;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine("✅ " + name + ": วางที่" + how);
            }
            catch (System.Exception e)
            {
                report.AppendLine("❌ " + path + ": " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[GhostFacePointTool]\n" + report);
        EditorUtility.DisplayDialog("วางจุดเล็งหน้าผีเสร็จแล้ว", report +
            "\nถ้าจุดยังไม่ตรงหน้า: ดับเบิลคลิก prefab แล้วลาก FacePoint ไปวางเอง", "ตกลง");
    }

    static Transform FindHeadBone(GameObject root)
    {
        var anim = root.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            var h = anim.GetBoneTransform(HumanBodyBones.Head);
            if (h != null) return h;
        }
        Transform best = null;
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            string n = tr.name.ToLower();
            if (n == "facepoint") continue;
            if (!n.Contains("head") || n.Contains("end") || n.Contains("top") || n.Contains("light")) continue;
            if (n.EndsWith("head")) return tr;
            if (best == null) best = tr;
        }
        return best;
    }

    static Vector3 FindFace(GameObject root, out string how)
    {
        var anim = root.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            var h = anim.GetBoneTransform(HumanBodyBones.Head);
            if (h != null) { how = "กระดูกหัว (" + h.name + ")"; return h.position; }
        }

        Transform best = null;
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            string n = tr.name.ToLower();
            if (!n.Contains("head") || n.Contains("end") || n.Contains("top") || n.Contains("light")) continue;
            if (n.EndsWith("head")) { best = tr; break; }
            if (best == null) best = tr;
        }
        if (best != null) { how = "กระดูกหัว (" + best.name + ")"; return best.position; }

        var rends = root.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rends.Length == 0) { how = "จุดสูงจากเท้า 1.5 ม. (ไม่มีโมเดล)"; return root.transform.position + Vector3.up * 1.5f; }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        how = "ส่วนบนด้านหน้าของตัว (ไม่เจอกระดูกหัว — ควรเช็กด้วยตา)";
        return b.center + Vector3.up * b.extents.y * 0.75f + root.transform.forward * b.extents.z * 0.5f;
    }
}
