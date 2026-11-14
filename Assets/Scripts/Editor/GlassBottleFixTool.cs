using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// แก้ขวดแก้ว (kuad.fbx) ทุกใบในฉากที่เปิดอยู่ ให้ใสถูกต้องและไม่พังตอน Bake แสง
/// 1. วาด 2 รอบ: ผิวด้านหลังก่อน (kuad_Glass_BackFaces) แล้วค่อยผิวด้านหน้า (AlbedoTransparency)
///    → เห็นน้ำแดงด้านในแบบใน Maya โดยไม่ซ้อนกันมั่ว (วาดสองด้านพร้อมกันใน mesh เดียว ลำดับจะสุ่ม)
/// 2. ไม่ทอดเงา (แก้วใสแต่ตอน Bake จะได้เงาทึบดำ)
/// 3. ไม่ร่วม Bake แสง (เอา Contribute GI ออก) แล้วรับแสงจาก Light Probe แทน
///    (วัตถุใสที่ถูก Bake ลง lightmap มักเป็นปื้นดำ / ทำให้ฉากรอบๆ มีเงาทึบ)
/// กด Ctrl+Z ย้อนได้
/// </summary>
public static class GlassBottleFixTool
{
    const string Folder = "Assets/Models/ขวด/";
    const string FbxPath = Folder + "kuad.fbx";
    const string FrontMat = Folder + "Materials/kuad_openPBR_shader1_AlbedoTransparency.mat";
    const string BackMat = Folder + "Materials/kuad_Glass_BackFaces.mat";

    [MenuItem("Tools/Saharan/แก้ขวดแก้ว (kuad) ในฉากนี้")]
    public static void FixBottlesInOpenScenes()
    {
        var front = AssetDatabase.LoadAssetAtPath<Material>(FrontMat);
        var back = AssetDatabase.LoadAssetAtPath<Material>(BackMat);
        if (front == null || back == null)
        {
            EditorUtility.DisplayDialog("แก้ขวดแก้ว", "หาไฟล์ Material ของขวดไม่เจอ:\n" + FrontMat + "\n" + BackMat, "ตกลง");
            return;
        }

        var bottleMeshes = new HashSet<Mesh>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxPath)) if (o is Mesh m) bottleMeshes.Add(m);

        int fixedCount = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || !bottleMeshes.Contains(mf.sharedMesh)) continue;
            var r = mf.GetComponent<MeshRenderer>();
            if (r == null) continue;

            Undo.RecordObject(r, "แก้ขวดแก้ว");
            Undo.RecordObject(r.gameObject, "แก้ขวดแก้ว");

            // mesh มีวัสดุช่องเดียว → ใส่ 2 อัน Unity จะวาด mesh ซ้ำด้วยอันที่สอง (หลังก่อน แล้วหน้า)
            r.sharedMaterials = new[] { back, front };
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveGI = ReceiveGI.LightProbes;
            r.lightProbeUsage = LightProbeUsage.BlendProbes;
            r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags & ~StaticEditorFlags.ContributeGI);

            EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
            fixedCount++;
        }

        bool hasProbes = Object.FindObjectsByType<LightProbeGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
        string msg = $"แก้ขวดแล้ว {fixedCount} ใบ";
        if (fixedCount == 0) msg = "ไม่เจอขวด kuad ในฉากที่เปิดอยู่";
        else if (!hasProbes)
            msg += "\n\nฉากนี้ยังไม่มี Light Probe Group → ขวดจะได้แค่แสงรอบข้าง อาจมืดกว่าของที่ Bake\n" +
                   "เพิ่มได้ที่ GameObject > Light > Light Probe Group วางรอบๆ ขวด แล้วกด Bake ใหม่";
        else
            msg += "\n\nกด Generate Lighting (Bake) ใหม่อีกรอบ แล้วเซฟฉาก";
        Debug.Log("[GlassBottleFixTool] " + msg.Replace("\n", " "));
        EditorUtility.DisplayDialog("แก้ขวดแก้ว", msg, "ตกลง");
    }
}
