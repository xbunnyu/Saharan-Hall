using System.Collections;
using UnityEngine;

/// <summary>
/// ตอนผี/Anomaly โผล่: บังคับหันกล้องไปที่หน้าผี แล้วซูมเข้า ค้างไว้ครู่หนึ่ง แล้วคืนการควบคุม
/// - เล็งที่กระดูกหัวทุกเฟรม (ผีขยับได้ก็ยังเล็งตรง)
/// - หมุนกล้องใน LateUpdate ช่วงท้ายสุดของเฟรม เพื่อไม่ให้สคริปต์กล้องอื่นเขียนทับ
/// - ถ้าผู้เล่นกำลังคุย/เล่นมินิเกมอยู่ จะรอให้เสร็จก่อนค่อยหัน
/// </summary>
[DefaultExecutionOrder(10000)]
public class GhostRevealCamera : MonoBehaviour
{
    [Header("เปิด/ปิด")]
    [Tooltip("ปิดไว้ = ผีโผล่แล้วกล้องไม่หันไปหา")]
    public bool enableReveal = true;

    [Header("จังหวะเวลา (วินาที)")]
    [Tooltip("เวลาที่ใช้หันกล้องไปหาผี + ซูมเข้า")]
    public float turnTime = 0.6f;
    [Tooltip("ค้างจ้องหน้าผีนานเท่าไหร่")]
    public float holdTime = 1.6f;
    [Tooltip("เวลาซูมออกคืนมุมเดิม")]
    public float releaseTime = 0.5f;
    [Tooltip("ถ้าตอนผีโผล่ผู้เล่นกำลังคุย/เล่นมินิเกม รอได้นานสุดกี่วินาที")]
    public float waitLimit = 20f;

    [Header("การซูม")]
    [Tooltip("ซูมเข้าเล็กน้อยกี่ % (0 = ไม่ซูมเลย หันหน้าหาอย่างเดียว)")]
    [Range(0f, 50f)]
    public float slightZoomPercent = 12f;
    [Tooltip("ซูมให้เห็นความสูงกี่เมตรเต็มจอ (น้อย = ซูมใกล้หน้ามาก)")]
    public float zoomFrameHeight = 2.2f;   // เห็นทั้งตัวผีคร่าวๆ ไม่ซูมจนเห็นแต่หน้า
    [Tooltip("ซูมได้ใกล้สุด (Field of View ต่ำสุด) — ค่ามาก = ซูมได้น้อย")]
    public float minFov = 30f;

    [Header("การเล็ง")]
    [Tooltip("ความไวในการหันตาม (มาก = หันเร็ว/ติดแน่น)")]
    public float aimSharpness = 10f;
    [Tooltip("ขยับจุดเล็งขึ้น/ลงจากหัวผี (เมตร) เช่น -0.1 = เล็งต่ำลงมาที่หน้า")]
    public float aimHeightOffset = 0.1f;   // จุดหมุนกระดูกหัวอยู่ที่โคนคอ → ยกขึ้นมาที่หน้า
    [Tooltip("ถ้ามีของบังกลางทางระหว่างกล้องกับหน้าผี (คาน/ผนัง) จะซูมแค่นิดเดียว ไม่ซูมเข้าไปเห็นแต่ของที่บัง")]
    public bool noZoomWhenBlocked = true;
    [Tooltip("ตอนมีของบัง ซูมได้แค่เห็นความสูงกี่เมตร (มาก = ซูมน้อย)")]
    public float blockedFrameHeight = 3f;
    [Tooltip("ไม่นับของที่อยู่ใกล้กล้องไม่เกินกี่เมตรว่าเป็นของบัง (โต๊ะ/เก้าอี้ตรงหน้า)")]
    public float ignoreNearCamera = 1.2f;
    [Tooltip("ไม่นับของที่อยู่ใกล้หน้าผีไม่เกินกี่เมตรว่าเป็นของบัง (ขอบประตู/ผนังข้างตัวผี)")]
    public float ignoreNearGhost = 0.8f;

    [Header("จุดเล็งเอง (แนะนำ)")]
    [Tooltip("ชื่อวัตถุลูกในตัวผีที่ใช้เป็นจุดเล็ง — สร้าง Empty ชื่อนี้ไว้ในผีแต่ละตัว แล้ววางตรงหน้าผี")]
    public string facePointName = "FacePoint";

    private static GhostRevealCamera runner;
    private bool busy = false;

    // สถานะการเล็ง (ใช้ใน LateUpdate)
    private bool aiming = false;
    private PlayerController pc;
    private Transform aimHead;      // กระดูกหัว (ถ้ามี)
    private GameObject aimGhost;    // ใช้คำนวณจุดหน้าจากกรอบโมเดลถ้าไม่มีกระดูกหัว

    public static void Reveal(GameObject ghost)
    {
        if (ghost == null) return;
        // ใช้ตัวที่วางไว้ในฉาก (ตั้งค่าได้ใน Inspector) ถ้าไม่มีค่อยสร้างใหม่ด้วยค่าเริ่มต้น
        if (runner == null) runner = FindFirstObjectByType<GhostRevealCamera>();
        if (runner == null)
        {
            var go = new GameObject("[GhostRevealCamera]");
            runner = go.AddComponent<GhostRevealCamera>();
        }
        if (runner.busy || !runner.enableReveal) return;
        runner.StartCoroutine(runner.RevealRoutine(ghost));
    }

    private IEnumerator RevealRoutine(GameObject ghost)
    {
        busy = true;
        pc = FindFirstObjectByType<PlayerController>();
        if (pc == null || pc.playerCamera == null) { busy = false; yield break; }
        Camera cam = pc.playerCamera.GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) { busy = false; yield break; }

        // รอให้ผู้เล่นว่าง (ไม่ได้คุย/เล่นมินิเกม)
        float waited = 0f;
        while (!pc.enabled && waited < waitLimit) { waited += Time.deltaTime; yield return null; }
        if (!pc.enabled || ghost == null || !ghost.activeInHierarchy) { busy = false; yield break; }

        aimGhost = ghost;
        // FacePoint ที่ติดกับกระดูก (ขยับตามหัว) ใช้ก่อน / FacePoint ที่ติดกับตัวผีเฉยๆ จะไม่ขยับตามท่า → ใช้กระดูกหัวแทน
        Transform fp = FindFacePoint(ghost);
        Transform headBone = FindHead(ghost);
        bool fpFollowsBone = fp != null && fp.parent != null && fp.parent != ghost.transform;
        aimHead = (fp != null && (fpFollowsBone || headBone == null)) ? fp : (headBone ?? fp);

        float startFov = cam.fieldOfView;
        string blocker = "";
        bool blocked = noZoomWhenBlocked && IsViewBlocked(cam.transform.position, GetFacePoint(), ghost, ignoreNearCamera, ignoreNearGhost, out blocker);
        if (blocked) Debug.Log($"[GhostRevealCamera] '{blocker}' บังระหว่างกล้องกับ '{ghost.name}' → ซูมแค่นิดเดียว");
        float frameHeight = blocked ? Mathf.Max(zoomFrameHeight, blockedFrameHeight) : zoomFrameHeight;
        pc.enabled = false;
        aiming = true;

        float t = 0f;
        while (t < turnTime + holdTime && ghost != null)
        {
            t += Time.deltaTime;
            Vector3 face = GetFacePoint();
            float dist = Mathf.Max(0.5f, Vector3.Distance(cam.transform.position, face));
            // หันหน้าหาผีเป็นหลัก ซูมแค่นิดเดียว (ลดมุมกล้องลง slightZoomPercent %)
            float targetFov = startFov * (1f - Mathf.Clamp01(slightZoomPercent / 100f));
            float zoom = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / turnTime));
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, zoom);
            yield return null;
        }

        float fovAtRelease = cam.fieldOfView;
        t = 0f;
        while (t < releaseTime)
        {
            t += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(fovAtRelease, startFov, Mathf.SmoothStep(0f, 1f, t / releaseTime));
            yield return null;
        }
        cam.fieldOfView = startFov;

        aiming = false;
        aimHead = null;
        aimGhost = null;
        pc.SyncPitchFromCamera();
        pc.enabled = true;
        busy = false;
    }

    private void LateUpdate()
    {
        if (!aiming || pc == null || pc.playerCamera == null) return;
        if (aimGhost == null) { aiming = false; return; }
        Aim(GetFacePoint(), 1f - Mathf.Exp(-aimSharpness * Time.deltaTime));
    }

    private Vector3 GetFacePoint()
    {
        if (aimHead != null) return aimHead.position + Vector3.up * aimHeightOffset;
        var rends = aimGhost.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return aimGhost.transform.position + Vector3.up * 1.5f;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b.center + Vector3.up * b.extents.y * 0.6f;
    }

    /// <summary>จุดเล็งที่ตั้งเองในตัวผี (Empty ชื่อ FacePoint)</summary>
    private Transform FindFacePoint(GameObject ghost)
    {
        if (string.IsNullOrEmpty(facePointName)) return null;
        foreach (var tr in ghost.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(tr.name, facePointName, System.StringComparison.OrdinalIgnoreCase)) return tr;
        }
        return null;
    }

    /// <summary>มีวัตถุอื่น (ไม่ใช่ตัวผี) ขวางเส้นจากกล้องไปหน้าผีหรือไม่</summary>
    private static bool IsViewBlocked(Vector3 eye, Vector3 face, GameObject ghost, float nearCam, float nearGhost, out string blocker)
    {
        blocker = "";
        Vector3 dir = face - eye;
        float dist = dir.magnitude;
        if (dist < 0.1f) return false;
        foreach (var hit in Physics.RaycastAll(eye, dir / dist, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == null) continue;
            if (hit.distance < nearCam) continue;                 // โต๊ะ/เก้าอี้ตรงหน้าผู้เล่น
            if (dist - hit.distance < nearGhost) continue;        // ขอบประตู/ผนังติดตัวผี
            if (hit.transform.IsChildOf(ghost.transform)) continue;
            if (hit.transform.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.transform.GetComponentInParent<NPCController>() != null) continue;
            blocker = hit.transform.name;
            return true;
        }
        return false;
    }

    /// <summary>หากระดูกหัว: Humanoid ใช้ Avatar, ไม่งั้นค้นจากชื่อ (เลือกชื่อที่ลงท้ายด้วย head ก่อน)</summary>
    private static Transform FindHead(GameObject ghost)
    {
        var anim = ghost.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            var h = anim.GetBoneTransform(HumanBodyBones.Head);
            if (h != null) return h;
        }
        Transform best = null;
        foreach (var tr in ghost.GetComponentsInChildren<Transform>())
        {
            string n = tr.name.ToLower();
            if (n == "facepoint") continue;
            if (!n.Contains("head") || n.Contains("end") || n.Contains("top") || n.Contains("light")) continue;
            if (n.EndsWith("head")) return tr;
            if (best == null) best = tr;
        }
        return best;
    }

    /// <summary>
    /// หมุนตัว (ซ้าย-ขวา) และกล้อง (ก้ม-เงย) ให้ "กล้องจริง" มองตรงไปที่จุด
    /// วัดจากทิศที่กล้องจริงกำลังมองแล้วปรับส่วนต่าง → ตรงแม้กล้องจะเยื้องจากแกนตัวผู้เล่น
    /// </summary>
    private void Aim(Vector3 point, float k)
    {
        Transform body = pc.transform;
        Transform pivot = pc.playerCamera;
        Camera cam = pivot.GetComponentInChildren<Camera>();
        Transform eyeT = cam != null ? cam.transform : pivot;

        Vector3 want = point - eyeT.position;
        if (want.sqrMagnitude < 0.0001f) return;
        Vector3 have = eyeT.forward;

        // ซ้าย-ขวา: หมุนตัวตามส่วนต่างมุมแนวราบ
        Vector3 wantFlat = new Vector3(want.x, 0f, want.z);
        Vector3 haveFlat = new Vector3(have.x, 0f, have.z);
        if (wantFlat.sqrMagnitude > 0.0001f && haveFlat.sqrMagnitude > 0.0001f)
        {
            float yawDelta = Vector3.SignedAngle(haveFlat, wantFlat, Vector3.up);
            body.Rotate(0f, yawDelta * k, 0f, Space.World);
        }

        // ก้ม-เงย: ปรับกล้องตามส่วนต่างมุมแนวดิ่ง
        float wantPitch = -Mathf.Atan2(want.y, new Vector2(want.x, want.z).magnitude) * Mathf.Rad2Deg;
        float havePitch = -Mathf.Asin(Mathf.Clamp(have.y, -1f, 1f)) * Mathf.Rad2Deg;
        float current = pivot.localEulerAngles.x; if (current > 180f) current -= 360f;
        float next = Mathf.Clamp(current + (wantPitch - havePitch) * k, pc.minVerticalAngle, pc.maxVerticalAngle);
        pivot.localRotation = Quaternion.Euler(next, 0f, 0f);
    }
}
