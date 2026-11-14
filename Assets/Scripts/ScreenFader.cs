using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// จอดำแล้วค่อยๆ สว่างกลับ (ใช้ตอนนั่ง/ลุกจากเก้าอี้) + ซ่อน/คืน Anomaly ระหว่างจอดำ
/// สร้าง Canvas ของตัวเองอัตโนมัติ ไม่ต้องตั้งค่าในฉาก
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;
    private Image overlay;
    private Coroutine routine;

    // ซ่อนผี/Anomaly ด้วยการปิด Renderer (ไม่ SetActive) — ระบบนับคำสาปของ GhostCurseManager จะไม่เพี้ยน
    private static bool anomaliesHidden = false;
    public static bool AnomaliesHidden => anomaliesHidden;
    private static readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private float rescanTimer = 0f;

    private void LateUpdate()
    {
        // ระหว่างที่ซ่อนอยู่ ถ้ามีผีโผล่ใหม่ (ระบบผีเปิดกลับเอง) ให้ซ่อนตามทันที
        if (!anomaliesHidden) return;
        rescanTimer -= Time.unscaledDeltaTime;
        if (rescanTimer > 0f) return;
        rescanTimer = 0.2f;
        HideNow();
    }

    private static void HideNow()
    {
        var targets = new List<GameObject>();
        if (GhostCurseManager.Instance != null) targets.AddRange(GhostCurseManager.Instance.GetShownGhostObjects());
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t != null && t.name.Contains("Anomaly") && (t.parent == null || !t.parent.name.Contains("Anomaly")))
                targets.Add(t.gameObject);
        }
        foreach (var go in targets)
        {
            if (go == null) continue;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    hiddenRenderers.Add(r);
                }
            }
        }
    }

    private static ScreenFader Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("[ScreenFader]");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var imgGo = new GameObject("Overlay");
        imgGo.transform.SetParent(go.transform, false);
        var img = imgGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        instance = go.AddComponent<ScreenFader>();
        instance.overlay = img;
        return instance;
    }

    /// <summary>จอดำทันที ค้างไว้ hold วินาที แล้วค่อยๆ สว่างกลับใน fadeIn วินาที</summary>
    public static void Dip(float hold = 0.2f, float fadeIn = 0.6f)
    {
        var f = Get();
        if (f.routine != null) f.StopCoroutine(f.routine);
        f.routine = f.StartCoroutine(f.DipRoutine(hold, fadeIn));
    }

    private IEnumerator DipRoutine(float hold, float fadeIn)
    {
        overlay.color = new Color(0f, 0f, 0f, 1f);
        yield return new WaitForSecondsRealtime(hold);
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            overlay.color = new Color(0f, 0f, 0f, 1f - Mathf.SmoothStep(0f, 1f, t / fadeIn));
            yield return null;
        }
        overlay.color = new Color(0f, 0f, 0f, 0f);
        routine = null;
    }

    /// <summary>ซ่อนผีคำสาปและ Anomaly ทั้งหมด (และตัวที่โผล่ใหม่ระหว่างนี้) จนกว่าจะเรียก RestoreAnomalies</summary>
    public static void HideAnomalies()
    {
        Get(); // ให้มีตัว ScreenFader คอยซ่อนตัวที่โผล่ใหม่
        anomaliesHidden = true;
        HideNow();
    }

    /// <summary>คืนการแสดงผลผี/Anomaly ที่เคยซ่อนไว้</summary>
    public static void RestoreAnomalies()
    {
        anomaliesHidden = false;
        foreach (var r in hiddenRenderers)
        {
            if (r != null) r.enabled = true;
        }
        hiddenRenderers.Clear();
    }
}
