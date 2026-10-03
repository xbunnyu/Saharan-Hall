using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ReturnNPCScheduler — จัดตาราง Story NPC ที่จะกลับมาในวันถัดไป
/// ─────────────────────────────────────────────────────────────────────
/// ทำงานร่วมกับ:
///   • NPCStoryData  — เก็บข้อมูลเรื่องราวแต่ละ NPC
///   • HallManager   — ดึงรายการ Story NPC ผ่าน GetReadyStoryNPCs()
///   • DayManager    — ส่งสัญญาณขึ้นวันใหม่ผ่าน OnNewDay()
///   • ReputationManager — ตรวจเงื่อนไขชื่อเสียง
///
/// วิธีใช้ใน HallManager.OpenHall():
///   var storyNPCs = ReturnNPCScheduler.Instance.GetReadyStoryNPCs(currentDay);
///   if (storyNPCs.Count > 0) ← spawn storyNPC ก่อน NPC ปกติ
/// </summary>
public class ReturnNPCScheduler : MonoBehaviour
{
    public static ReturnNPCScheduler Instance { get; private set; }

    // ─── รายการ NPCStoryData ทั้งหมดในเกม ────────────────────────────
    // ลาก ScriptableObject ใส่จาก Inspector หรือผ่าน RegisterStoryNPC()
    [Header("Story NPC ทั้งหมดในเกม (ลาก NPCStoryData มาใส่)")]
    public List<NPCStoryData> allStoryNPCs = new List<NPCStoryData>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // ══════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// เพิ่ม NPCStoryData เข้า Scheduler แบบ Runtime
    /// (ใช้เมื่อต้องการสร้าง Story NPC แบบ Procedural ผ่านโค้ด)
    /// </summary>
    public void RegisterStoryNPC(NPCStoryData storyData)
    {
        if (storyData == null || allStoryNPCs.Contains(storyData)) return;
        storyData.ResetRuntimeState();
        allStoryNPCs.Add(storyData);
        Debug.Log($"[ReturnNPCScheduler] ✅ ลงทะเบียน Story NPC: '{storyData.npcName}' ({storyData.phases.Count} phases)");
    }

    /// <summary>
    /// ดึงรายการ Story NPC ที่พร้อม Spawn ในวันนี้
    /// (ผ่านเงื่อนไขวัน + ชื่อเสียง + phase ก่อนหน้าเสร็จ)
    /// ── เรียงตาม phase index เพื่อให้ NPC ที่ค้างไว้นานกว่ามาก่อน ──
    /// </summary>
    public List<NPCStoryData> GetReadyStoryNPCs(int currentDay)
    {
        var result = new List<NPCStoryData>();

        foreach (var story in allStoryNPCs)
        {
            if (story == null) continue;
            if (story.IsFullyCompleted())
            {
                Debug.Log($"[ReturnNPCScheduler] 🏁 '{story.npcName}' จบทุก Phase แล้ว");
                continue;
            }
            if (story.isScheduledForToday)
            {
                Debug.Log($"[ReturnNPCScheduler] ⏰ '{story.npcName}' ถูกปล่อยออกมาแล้วสำหรับวันนี้");
                continue;
            }

            if (story.IsReadyForDay(currentDay))
            {
                result.Add(story);
                Debug.Log($"[ReturnNPCScheduler] 📅 '{story.npcName}' Phase {story.currentPhaseIndex} พร้อม spawn วันที่ {currentDay}");
            }
            else
            {
                StoryPhase phase = story.GetCurrentPhase();
                if (phase != null)
                {
                    Debug.Log($"[ReturnNPCScheduler] ⏳ '{story.npcName}' Phase {story.currentPhaseIndex} ยังไม่พร้อมสำหรับวันที่ {currentDay} (เงื่อนไข: unlockOnDay={phase.unlockOnDay}, requiredRep={phase.requiredReputationLevel})");
                }
            }
        }

        // เรียงตาม phase index (NPC ที่ค้างมานานกว่า / phase สูงกว่ามาก่อน)
        result.Sort((a, b) => b.currentPhaseIndex.CompareTo(a.currentPhaseIndex));
        return result;
    }

    /// <summary>
    /// Mark Story NPC ว่า Spawn วันนี้แล้ว (ป้องกัน spawn ซ้ำในวันเดียวกัน)
    /// </summary>
    public void MarkSpawnedToday(NPCStoryData storyData, int day)
    {
        if (storyData == null) return;
        storyData.isScheduledForToday = true;
        storyData.scheduledDay = day;
    }

    /// <summary>
    /// เรียกทุกครั้งที่ขึ้นวันใหม่ (จาก DayManager.AdvanceDay หรือ HallManager.OnNewDay)
    /// Reset isScheduledForToday เพื่อให้ NPC กลับมา Spawn ได้ในวันถัดไป
    /// </summary>
    public void OnNewDay()
    {
        foreach (var story in allStoryNPCs)
        {
            if (story == null) continue;
            story.isScheduledForToday = false;
        }
        Debug.Log("[ReturnNPCScheduler] 🌅 Reset การ Spawn Story NPC สำหรับวันใหม่เรียบร้อย");
    }

    /// <summary>
    /// เรียกเมื่อ Phase NPC สำเร็จ:
    /// - เพิ่มชื่อเสียง
    /// - ขยับไป Phase ถัดไป
    /// </summary>
    public void OnStoryPhaseCompleted(NPCStoryData storyData)
    {
        if (storyData == null) return;

        StoryPhase completedPhase = storyData.GetCurrentPhase();
        string groupId = storyData.npcGroupId;

        // ให้รางวัลชื่อเสียง
        if (completedPhase != null && completedPhase.reputationReward > 0)
        {
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.AddReputation(
                    groupId,
                    completedPhase.reputationReward,
                    $"ส่ง Phase {storyData.currentPhaseIndex} '{storyData.npcName}' สำเร็จ"
                );
            }
        }

        // ขยับ Phase ถัดไป
        bool hasMore = storyData.AdvancePhase();

        if (hasMore)
        {
            StoryPhase next = storyData.GetCurrentPhase();
            string hintDay = next != null ? $"วันที่ {next.unlockOnDay}" : "เร็วๆ นี้";
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(
                    $"<color=#A0E6FF>📖 เรื่องราวของ '{storyData.npcName}' ยังไม่จบ...</color>\n" +
                    $"<size=85%>พวกเขาอาจกลับมาในวันถัดไป ({hintDay})</size>",
                    4.0f
                );
            }
        }
        else
        {
            // จบทุก phase แล้ว!
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(
                    $"<color=#FFD700>✨ เรื่องราวของ '{storyData.npcName}' จบสมบูรณ์แล้ว!</color>",
                    4.0f
                );
            }
            Debug.Log($"[ReturnNPCScheduler] 🏁 '{storyData.npcName}' จบทุก Phase แล้ว!");
        }
    }

    /// <summary>
    /// เรียกเมื่อ Phase NPC ล้มเหลว
    /// </summary>
    public void OnStoryPhaseFailed(NPCStoryData storyData)
    {
        if (storyData == null) return;
        storyData.FailCurrentPhase();

        string groupId = storyData.npcGroupId;
        // เสียชื่อเสียงเล็กน้อยเมื่อล้มเหลว
        if (ReputationManager.Instance != null)
        {
            ReputationManager.Instance.AddReputation(groupId, -5, $"ล้มเหลว Phase {storyData.currentPhaseIndex}");
        }

        Debug.Log($"[ReturnNPCScheduler] ❌ '{storyData.npcName}' Phase {storyData.currentPhaseIndex} ล้มเหลว");
    }

    // ══════════════════════════════════════════════════════════════════
    // Debug
    // ══════════════════════════════════════════════════════════════════

    [ContextMenu("Debug: แสดงสถานะ Story NPC ทั้งหมด")]
    public void DebugPrintAll()
    {
        int day = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        Debug.Log($"[ReturnNPCScheduler] ── วันที่ {day} Story NPC ทั้งหมด ──");
        foreach (var story in allStoryNPCs)
        {
            if (story == null) continue;
            string status = story.IsFullyCompleted() ? "✅ จบแล้ว" : $"Phase {story.currentPhaseIndex}/{story.phases.Count}";
            string ready  = story.IsReadyForDay(day) ? "🟢 พร้อม" : "🔴 ยังไม่ถึง";
            Debug.Log($"  • '{story.npcName}' ({story.npcGroupId}) | {status} | {ready}");
        }
    }
}

