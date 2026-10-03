using System;
using System.Collections.Generic;
using UnityEngine;

// ══════════════════════════════════════════════════════════════════════
// StoryPhase — เฟสเดียวของเรื่องราว NPC (เช่น Phase 1, Phase 2, ...)
// ══════════════════════════════════════════════════════════════════════
[Serializable]
public class StoryPhase
{
    [Header("เงื่อนไขปลดล็อก")]
    [Tooltip("เฟสนี้จะปรากฏตั้งแต่วันที่เท่าไร (0 = ตั้งแต่วันแรก)")]
    public int unlockOnDay = 1;
    [Tooltip("ต้องการชื่อเสียงกับกลุ่ม npcGroupId อย่างน้อยระดับเท่าไร (0 = ไม่ต้องการ)")]
    public int requiredReputationLevel = 0;
    [Tooltip("ต้องทำ Phase ก่อนหน้าสำเร็จก่อนหรือไม่ (true = ต้องทำตามลำดับ)")]
    public bool requirePreviousPhaseCompleted = true;

    [Header("บทสนทนาเปิดเฟส")]
    [TextArea(2, 4)]
    [Tooltip("NPC จะพูดประโยคนี้เมื่อผู้เล่นกดคุยในเฟสนี้ (ก่อนโชว์ Quest Dialog)")]
    public string introDialogue = "";

    [Header("เควสในเฟสนี้")]
    [Tooltip("ข้อมูลเควสประจำเฟสนี้ (clone จาก QuestData ปกติ)")]
    public QuestData quest;

    [Header("รางวัลชื่อเสียงเมื่อส่งเสร็จ")]
    [Tooltip("คะแนนชื่อเสียงที่ได้เพิ่มเมื่อส่งเควสเฟสนี้สำเร็จ")]
    public int reputationReward = 10;

    // สถานะ runtime (ไม่ต้องตั้งใน Inspector)
    [HideInInspector] public bool isUnlocked  = false;
    [HideInInspector] public bool isCompleted = false;
    [HideInInspector] public bool isFailed    = false;
}

// ══════════════════════════════════════════════════════════════════════
/// <summary>
/// NPCStoryData — ScriptableObject เก็บเรื่องราวข้ามวันของ NPC 1 ตัว
/// ─────────────────────────────────────────────────────────────────────
/// สร้างได้จาก Assets > Create > Saharan Hall > NPC Story Data
///
/// วิธีใช้งาน:
///   1. สร้าง ScriptableObject ใน Project Window
///   2. กรอก npcName, npcGroupId, และ phases ตามต้องการ
///   3. ลาก NPCStoryData ไปใส่ใน HallManager.storyNPCList[]
///      — HallManager จะจัดการ spawn และ track phase ให้อัตโนมัติ
/// </summary>
// ══════════════════════════════════════════════════════════════════════
[CreateAssetMenu(menuName = "Saharan Hall/NPC Story Data", fileName = "NPCStory_NewNPC")]
public class NPCStoryData : ScriptableObject
{
    [Header("ข้อมูล NPC")]
    public string npcName     = "ป้าสมจิต";
    [Tooltip("ID กลุ่มชื่อเสียง (ตรงกับ ReputationManager) เช่น 'somjit_family'")]
    public string npcGroupId  = "somjit_family";
    public Sprite npcPortrait;

    [Header("Prefab (ถ้าเว้นว่างจะใช้ defaultNpcPrefab ของ HallManager)")]
    public GameObject npcPrefab;

    [Header("เรื่องราวทั้งหมด (เฟสตามลำดับ)")]
    public List<StoryPhase> phases = new List<StoryPhase>();

    // ─── Runtime tracking ───────────────────────────────────────────
    [System.NonSerialized] public int     currentPhaseIndex   = 0;
    [System.NonSerialized] public bool    isScheduledForToday = false;
    [System.NonSerialized] public int     scheduledDay        = -1;

    private void OnEnable()
    {
        ResetRuntimeState();
    }

    /// <summary>
    /// รีเซ็ตค่า Runtime ทั้งหมดให้พร้อมสำหรับการเล่นใหม่
    /// </summary>
    public void ResetRuntimeState()
    {
        currentPhaseIndex = 0;
        isScheduledForToday = false;
        scheduledDay = -1;
        if (phases != null)
        {
            foreach (var p in phases)
            {
                if (p != null)
                {
                    p.isUnlocked = false;
                    p.isCompleted = false;
                    p.isFailed = false;
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Helper API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>ดึง Phase ปัจจุบัน (null ถ้าจบทุก Phase แล้ว)</summary>
    public StoryPhase GetCurrentPhase()
    {
        if (phases == null || currentPhaseIndex >= phases.Count) return null;
        return phases[currentPhaseIndex];
    }

    /// <summary>ตรวจว่า Phase ปัจจุบันพร้อม spawn ในวันนี้หรือไม่</summary>
    public bool IsReadyForDay(int day)
    {
        StoryPhase phase = GetCurrentPhase();
        if (phase == null) return false;                    // จบทุก phase แล้ว

        // ตรวจวัน
        if (day < phase.unlockOnDay) return false;

        // ตรวจชื่อเสียง
        if (phase.requiredReputationLevel > 0 && ReputationManager.Instance != null)
        {
            if (!ReputationManager.Instance.HasReputationLevel(npcGroupId, phase.requiredReputationLevel))
                return false;
        }

        // ตรวจว่า Phase ก่อนหน้าเสร็จแล้วหรือไม่
        if (phase.requirePreviousPhaseCompleted && currentPhaseIndex > 0)
        {
            StoryPhase prev = phases[currentPhaseIndex - 1];
            if (!prev.isCompleted) return false;
        }

        return true;
    }

    /// <summary>
    /// Mark phase ปัจจุบันว่าเสร็จแล้ว และขยับไปยัง phase ถัดไป
    /// Returns true ถ้ายังมี phase ถัดไป
    /// </summary>
    public bool AdvancePhase()
    {
        StoryPhase phase = GetCurrentPhase();
        if (phase == null) return false;

        phase.isCompleted = true;
        currentPhaseIndex++;
        Debug.Log($"[NPCStory] '{npcName}' ผ่าน Phase {currentPhaseIndex - 1} → เข้าสู่ Phase {currentPhaseIndex}");
        return currentPhaseIndex < phases.Count;
    }

    /// <summary>Mark phase ปัจจุบันว่าล้มเหลว</summary>
    public void FailCurrentPhase()
    {
        StoryPhase phase = GetCurrentPhase();
        if (phase != null) phase.isFailed = true;
    }

    /// <summary>ตรวจว่าจบทุก Phase เรียบร้อยแล้วหรือยัง</summary>
    public bool IsFullyCompleted()
    {
        if (phases == null || phases.Count == 0) return true;
        return currentPhaseIndex >= phases.Count && phases[phases.Count - 1].isCompleted;
    }
}

