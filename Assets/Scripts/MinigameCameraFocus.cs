using System.Collections;
using UnityEngine;

/// <summary>
/// ล็อคกล้องให้หันเข้าหาวัตถุทำพิธีระหว่างเล่นมินิเกม
/// มินิเกมทุกตัวปิด PlayerController ตอนเริ่ม และเปิดคืนตอนจบ — ใช้ตรงนั้นเป็นสัญญาณเริ่ม/จบ
/// </summary>
public class MinigameCameraFocus : MonoBehaviour
{
    public float turnSpeed = 6f;
    [Tooltip("รอให้มินิเกมเริ่มไม่เกินกี่วินาที (ถ้าไม่เริ่ม = ไม่ใช่มินิเกม ไม่ล็อค)")]
    public float startTimeout = 1.0f;

    private PlayerController pc;
    private Transform cam;
    private Coroutine routine;

    private NPCController backNpc;

    /// <param name="turnBackNpc">NPC ที่ต้องหันหลังให้ระหว่างเล่น (ตวัดดาบ/ท่องคาถา) — null = ไม่ต้อง</param>
    public static void Watch(GameObject player, Transform target, NPCController turnBackNpc = null)
    {
        if (player == null || target == null) return;
        var focus = player.GetComponent<MinigameCameraFocus>();
        if (focus == null) focus = player.AddComponent<MinigameCameraFocus>();
        if (focus.backNpc != null) focus.backNpc.showBackToPlayer = false;
        focus.backNpc = turnBackNpc;
        focus.Begin(target);
    }

    private void Begin(Transform target)
    {
        if (pc == null) pc = GetComponent<PlayerController>() ?? GetComponentInParent<PlayerController>();
        if (pc == null) return;
        cam = pc.playerCamera != null ? pc.playerCamera : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FocusRoutine(target));
    }

    private IEnumerator FocusRoutine(Transform target)
    {
        // 1. รอให้มินิเกมเริ่ม (PlayerController ถูกปิด)
        float t = 0f;
        while (pc.enabled && t < startTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (pc.enabled) { routine = null; backNpc = null; yield break; }

        if (backNpc != null) backNpc.showBackToPlayer = true;

        Vector3 point = GetFocusPoint(target);
        Transform body = pc.transform;

        // 2. ระหว่างเล่น: ค่อยๆ หันตัว (ซ้าย-ขวา) และกล้อง (ก้ม-เงย) เข้าหาวัตถุ แล้วค้างไว้
        while (!pc.enabled && target != null)
        {
            float k = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);

            Vector3 flat = point - body.position; flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
                body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(flat), k);

            Vector3 local = body.InverseTransformPoint(point) - body.InverseTransformPoint(cam.position);
            float pitch = -Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, pc.minVerticalAngle, pc.maxVerticalAngle);
            float current = cam.localEulerAngles.x; if (current > 180f) current -= 360f;
            cam.localRotation = Quaternion.Euler(Mathf.Lerp(current, pitch, k), 0f, 0f);

            yield return null;
        }

        // 3. จบมินิเกม: NPC หันกลับมา + ให้ PlayerController ใช้มุมกล้องปัจจุบันต่อ (ไม่เด้งกลับ)
        if (backNpc != null) { backNpc.showBackToPlayer = false; backNpc = null; }
        pc.SyncPitchFromCamera();
        routine = null;
    }

    private static Vector3 GetFocusPoint(Transform target)
    {
        var rends = target.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return target.position;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b.center;
    }
}
