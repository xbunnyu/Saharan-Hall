#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AudioManagerEditorHelper
{
    [MenuItem("Tools/Audio/Find or Create AudioManager", false, 1)]
    [MenuItem("GameObject/Audio/Audio Manager (Global)", false, 10)]
    public static void CreateAudioManagerInScene()
    {
        AudioManager existing = Object.FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            Debug.Log($"[AudioManager] พบ AudioManager ใน Hierarchy แล้ว: '{existing.gameObject.name}'");
            return;
        }

        GameObject go = new GameObject("AudioManager");
        AudioManager mgr = go.AddComponent<AudioManager>();
        mgr.AutoAssignClips();
        Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[AudioManager] ✅ สร้าง 'AudioManager' ใน Scene พร้อมตั้งค่าเสียงเริ่มต้นให้เรียบร้อยแล้ว!");
    }

    [MenuItem("Tools/Audio/Save AudioManager as Prefab", false, 2)]
    public static void SaveAsPrefab()
    {
        AudioManager mgr = Object.FindFirstObjectByType<AudioManager>();
        if (mgr == null)
        {
            CreateAudioManagerInScene();
            mgr = Object.FindFirstObjectByType<AudioManager>();
        }
        if (mgr != null)
        {
            if (!System.IO.Directory.Exists("Assets/Prefab"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefab");
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(mgr.gameObject, "Assets/Prefab/AudioManager.prefab", InteractionMode.UserAction);
            AssetDatabase.Refresh();
            Debug.Log("[AudioManager] ✅ บันทึกเป็น Prefab ที่ Assets/Prefab/AudioManager.prefab สำเร็จ!");
        }
    }
}
#endif

