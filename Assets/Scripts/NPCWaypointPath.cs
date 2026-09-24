using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สคริปต์กำหนดเส้นทางเดิน NPC (Waypoint Path System)
/// - วางไว้บน GameObject เพื่อจัดเก็บจุด Waypoints เรียงตามลำดับ (เช่น Point A ➔ B ➔ C ➔ โต๊ะรับแขก)
/// - แสดง Gizmos เส้นเชื่อมต่อทางเดินเป็นสีเด่นชัดในหน้าต่าง Scene View ช่วยให้มองเห็นทางเดิน 3D ได้ทันที
/// </summary>
public class NPCWaypointPath : MonoBehaviour
{
    [Header("1. รายการจุดทางเดิน (Waypoints in Order)")]
    [Tooltip("ลาก Transform จุดทางเดินเรียงตามลำดับจากจุดเริ่มต้น ➔ จุดปลายทาง")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("2. การแสดงผลใน Scene Editor (Gizmos Settings)")]
    public Color pathColor = new Color(0.2f, 0.9f, 0.4f, 0.9f); // สีเขียวเด่นชัด
    public Color sphereColor = new Color(1.0f, 0.85f, 0.2f, 0.9f); // สีเหลืองทอง
    public float sphereRadius = 0.35f;
    public bool showNumbersInScene = true;

    /// <summary>
    /// ดึงพิกัดตาม Index (พร้อมความปลอดภัย)
    /// </summary>
    public Vector3 GetPointPosition(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Count) return transform.position;
        return waypoints[index] != null ? waypoints[index].position : transform.position;
    }

    /// <summary>
    /// จำนวนจุดทั้งหมดในเส้นทาง
    /// </summary>
    public int PointCount => waypoints != null ? waypoints.Count : 0;

    // ==========================================
    // วาดเส้นทางเดินและมาร์เกอร์ใน Scene View อัตโนมัติ
    // ==========================================
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            if (current == null) continue;

            // วาดทรงกลมมาร์เกอร์ตรงแต่ละจุด
            Gizmos.color = sphereColor;
            Gizmos.DrawSphere(current.position, sphereRadius);
            Gizmos.DrawWireSphere(current.position, sphereRadius + 0.08f);

            // วาดเส้นเชื่อมต่อระหว่างจุดปัจจุบัน ➔ จุดถัดไป
            if (i < waypoints.Count - 1)
            {
                Transform next = waypoints[i + 1];
                if (next != null)
                {
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(current.position, next.position);

                    // วาดลูกศรชี้ทิศทางตรงกลางเส้น
                    Vector3 midPoint = (current.position + next.position) * 0.5f;
                    Vector3 dir = (next.position - current.position).normalized;
                    if (dir != Vector3.zero)
                    {
                        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 200, 0) * Vector3.forward * 0.5f;
                        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 160, 0) * Vector3.forward * 0.5f;
                        Gizmos.DrawRay(midPoint, right);
                        Gizmos.DrawRay(midPoint, left);
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // เมื่อคลิกเลือกอ็อบเจกต์ ให้เน้นสีเส้นทางให้สว่างยิ่งขึ้น
        OnDrawGizmos();
    }
}
