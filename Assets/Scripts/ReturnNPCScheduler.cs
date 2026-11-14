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

    [Header("Single Active Story Policy (เควสติดตามทีละคน)")]
    [Tooltip("เปิดใช้ระบบเควสติดตามทีละคน: ให้มีเควสติดตามดำเนินอยู่ได้เพียง 1 คนเท่านั้น จนกว่าจะจบเนื้อเรื่องทั้งหมด คนอื่นจึงจะมีสิทธิ์เริ่ม")]
    public bool singleActiveStoryAtATime = true;

    [Header("Runtime Story Tracking")]
    [Tooltip("Story NPC คนที่กำลังดำเนินเรื่องราวอยู่ในปัจจุบัน (จะว่างลงเมื่อจบทุก Phase)")]
    public NPCStoryData currentActiveStoryNPC = null;

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
    /// ตรวจหา Story NPC ที่กำลังดำเนินเรื่องราวอยู่ (ยังไม่จบทุก Phase และเริ่มไปแล้ว)
    /// </summary>
    public NPCStoryData GetActiveInProgressStoryNPC()
    {
        if (currentActiveStoryNPC != null)
        {
            if (currentActiveStoryNPC.IsFullyCompleted())
            {
                currentActiveStoryNPC = null;
            }
            else
            {
                return currentActiveStoryNPC;
            }
        }

        foreach (var story in allStoryNPCs)
        {
            if (story != null && !story.IsFullyCompleted() && (story.currentPhaseIndex > 0 || story.hasStarted))
            {
                currentActiveStoryNPC = story;
                return story;
            }
        }

        return null;
    }

    /// <summary>
    /// ดึงรายการ Story NPC ที่พร้อม Spawn ในวันนี้
    /// หากเปิด singleActiveStoryAtATime:
    ///   - ถ้ามี Story NPC คนเดิมที่กำลังดำเนินเรื่องราวอยู่ จะอนุญาตให้เฉพาะคนนี้คนเดียวเท่านั้น
    ///   - ถ้ายังไม่มีใครเริ่ม (หรือคนก่อนหน้าจบแล้ว) จะสุ่มเลือกมาเพียง 1 คนจากคนที่พร้อมสำหรับวันนี้
    /// </summary>
    public List<NPCStoryData> GetReadyStoryNPCs(int currentDay)
    {
        var result = new List<NPCStoryData>();

        if (singleActiveStoryAtATime)
        {
            // 1. ตรวจสอบว่ามี Story NPC คนเดิมที่กำลังดำเนินเรื่องราวค้างอยู่หรือไม่
            NPCStoryData activeStory = GetActiveInProgressStoryNPC();

            if (activeStory != null)
            {
                if (activeStory.isScheduledForToday)
                {
                    Debug.Log($"[ReturnNPCScheduler] ⏰ '{activeStory.npcName}' ถูกปล่อยออกมาแล้วสำหรับวันนี้");
                    return result;
                }

                if (activeStory.IsReadyForDay(currentDay))
                {
                    result.Add(activeStory);
                    Debug.Log($"[ReturnNPCScheduler] 📅 เควสติดตามต่อเนื่อง: '{activeStory.npcName}' Phase {activeStory.currentPhaseIndex} พร้อม spawn วันที่ {currentDay}");
                }
                else
                {
                    StoryPhase phase = activeStory.GetCurrentPhase();
                    string reason = phase != null ? $"(unlockOnDay={phase.unlockOnDay}, requiredRep={phase.requiredReputationLevel})" : "";
                    Debug.Log($"[ReturnNPCScheduler] ⏳ เควสติดตามของ '{activeStory.npcName}' ยังดำเนินอยู่แต่ยังไม่ถึงเงื่อนไขวันนี้ {reason} — ไม่อนุญาตให้เควสติดตามอื่นเข้ามาแทรก");
                }

                return result; // คืนค่าเฉพาะคนนี้ (หรือว่างเปล่าถ้ายังไม่ถึงรอบ) ป้องกันไม่ให้เควสติดตามอื่นแทรกคิว
            }

            // 2. ถ้ายังไม่มีใครเริ่ม หรือคนก่อนหน้าจบสมบูรณ์แล้ว -> สุ่มเลือกเพียง 1 คนจากคนที่พร้อมสำหรับวันนี้
            var candidateList = new List<NPCStoryData>();
            foreach (var story in allStoryNPCs)
            {
                if (story == null) continue;
                if (story.IsFullyCompleted()) continue;
                if (story.isScheduledForToday) continue;

                if (story.IsReadyForDay(currentDay))
                {
                    candidateList.Add(story);
                }
            }

            if (candidateList.Count > 0)
            {
                int randomIndex = Random.Range(0, candidateList.Count);
                NPCStoryData selected = candidateList[randomIndex];
                selected.hasStarted = true;
                currentActiveStoryNPC = selected;
                result.Add(selected);
                Debug.Log($"[ReturnNPCScheduler] 🎲 สุ่มเลือกเควสติดตามคนใหม่: '{selected.npcName}' เริ่ม Phase {selected.currentPhaseIndex} (สุ่มจาก {candidateList.Count} คนที่พร้อม)");
            }

            return result;
        }

        // กรณีปิด singleActiveStoryAtATime (ทำงานแบบเดิม)
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
        storyData.hasStarted = true;
        currentActiveStoryNPC = storyData;
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
            // จบทุก phase แล้ว! ปลดล็อกให้เควสติดตามคนถัดไปสามารถเริ่มได้
            if (currentActiveStoryNPC == storyData)
            {
                currentActiveStoryNPC = null;
            }

            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(
                    $"<color=#FFD700>✨ เรื่องราวของ '{storyData.npcName}' จบสมบูรณ์แล้ว!</color>",
                    4.0f
                );
            }
            Debug.Log($"[ReturnNPCScheduler] 🏁 '{storyData.npcName}' จบทุก Phase แล้ว! เควสติดตามคนถัดไปมีสิทธิ์เริ่มได้");
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

