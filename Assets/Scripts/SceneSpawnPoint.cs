using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// จุดกำหนดตำแหน่งและทิศทางเกิดของผู้เล่น (ใช้ได้กับทุกซีน ทั้งขาไปและขากลับ)
/// นำไปใส่บน Empty GameObject ตรงจุดที่ต้องการให้ผู้เล่นโผล่มา
/// มี Gizmos รูปคนจำลองสีเขียวและลูกศรสีเหลืองชี้ทิศทางใน Scene View
/// </summary>
public class SceneSpawnPoint : MonoBehaviour
{
    private static readonly List<SceneSpawnPoint> allSpawnPoints = new List<SceneSpawnPoint>();

    [Header("Spawn Point Settings")]
    [Tooltip("ชื่อ ID ของจุดเกิด (ให้ตรงกับชื่อจุดเกิดในสคริปต์รถขาไป หรือ ขากลับ)")]
    public string spawnPointId = "SpawnPoint_1";

    protected virtual void OnEnable()
    {
        if (!allSpawnPoints.Contains(this))
        {
            allSpawnPoints.Add(this);
        }
    }

    protected virtual void OnDisable()
    {
        allSpawnPoints.Remove(this);
    }

    /// <summary>
    /// ค้นหาจุดเกิดตาม ID ในฉากปัจจุบัน
    /// </summary>
    public static Transform FindSpawnPoint(string targetId)
    {
        if (string.IsNullOrEmpty(targetId)) return null;

        // 1. ค้นหาจาก SceneSpawnPoint ทั้งหมดในฉาก
        foreach (var sp in allSpawnPoints)
        {
            if (sp != null && string.Equals(sp.spawnPointId, targetId, System.StringComparison.OrdinalIgnoreCase))
            {
                return sp.transform;
            }
        }

        // 2. ค้นหาจากชื่อ GameObject ในฉากโดยตรง
        GameObject foundGO = GameObject.Find(targetId);
        if (foundGO != null)
        {
            return foundGO.transform;
        }

        return null;
    }

    protected virtual void OnDrawGizmos()
    {
        // สีเขียวสำหรับรูปทรงคน
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.85f);

        // หัว
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.25f);
        // ลำตัว
        Gizmos.DrawLine(transform.position + Vector3.up * 1.25f, transform.position + Vector3.up * 0.4f);
        // แขน
        Gizmos.DrawLine(transform.position + Vector3.up * 1.1f - transform.right * 0.35f, transform.position + Vector3.up * 1.1f + transform.right * 0.35f);
        // ขา
        Gizmos.DrawLine(transform.position + Vector3.up * 0.4f, transform.position - transform.right * 0.2f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.4f, transform.position + transform.right * 0.2f);

        // ลูกศรทิศทางด้านหน้า (สีเหลือง)
        Vector3 forwardDir = transform.forward * 1.2f;
        Vector3 arrowPos = transform.position + Vector3.up * 1.0f;
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.95f);
        Gizmos.DrawRay(arrowPos, forwardDir);
        Gizmos.DrawSphere(arrowPos + forwardDir, 0.08f);

#if UNITY_EDITOR
        // แสดงชื่อ ID เหนือจุดเกิดใน Scene View
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.0f, $"📍 {spawnPointId}");
#endif
    }
}
