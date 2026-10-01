using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบชื่อเสียงของผู้เล่น (Reputation System)
/// ─────────────────────────────────────────────────────────────────────
/// ต่างจาก KarmaManager ตรงที่:
///   • ชื่อเสียงเป็นค่าที่ผู้เล่น "รู้สึกได้" (แสดงบน UI ได้)
///   • แต่ละ npcGroupId มีค่าชื่อเสียงแยกกัน
///
/// ระดับชื่อเสียง (0–3):
///   0 = ยังไม่รู้จัก   → NPC พูดน้อย ไม่เปิดเผย
///   1 = รู้จักกัน      → NPC เล่าเรื่องเพิ่มเติมได้
///   2 = ไว้วางใจ      → NPC เปิดเผย Phase 2 / ความลับ
///   3 = สนิทชิดเชื้อ  → NPC เปิด Quest พิเศษ / Ending สมบูรณ์
/// ─────────────────────────────────────────────────────────────────────
/// วิธีใช้:
///   ReputationManager.Instance.AddReputation("somjit_family", 10, "ส่งเควสสำเร็จ");
///   bool ok = ReputationManager.Instance.HasReputationLevel("somjit_family", 2);
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance { get; private set; }

    // ── ค่าชื่อเสียงแยกตาม npcGroupId ──────────────────────────────────
    // Key   = npcGroupId เช่น "somjit_family", "village_monk", "merchant_guild"
    // Value = คะแนนสะสม
    private Dictionary<string, int> reputationScores = new Dictionary<string, int>();

    // ─── Thresholds (ปรับ Balance ใน Inspector) ─────────────────────────
    [Header("Threshold ระดับชื่อเสียง")]
    [Tooltip("คะแนนขั้นต่ำระดับ 1 'รู้จักกัน'")]
    public int thresholdLevel1 = 10;
    [Tooltip("คะแนนขั้นต่ำระดับ 2 'ไว้วางใจ'")]
    public int thresholdLevel2 = 30;
    [Tooltip("คะแนนขั้นต่ำระดับ 3 'สนิทชิดเชื้อ'")]
    public int thresholdLevel3 = 60;

    private static readonly string[] LevelNames =
    {
        "ยังไม่รู้จัก",    // 0
        "รู้จักกัน",       // 1
        "ไว้วางใจ",        // 2
        "สนิทชิดเชื้อ"    // 3
    };

    // ══════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // ══════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>ดึงคะแนนชื่อเสียงดิบ (0 ขึ้นไป)</summary>
    public int GetScore(string npcGroupId)
    {
        if (string.IsNullOrEmpty(npcGroupId)) return 0;
        return reputationScores.TryGetValue(npcGroupId, out int val) ? val : 0;
    }

    /// <summary>ดึงระดับชื่อเสียง (0–3)</summary>
    public int GetLevel(string npcGroupId)
    {
        int score = GetScore(npcGroupId);
        if (score >= thresholdLevel3) return 3;
        if (score >= thresholdLevel2) return 2;
        if (score >= thresholdLevel1) return 1;
        return 0;
    }

    /// <summary>ดึงชื่อระดับภาษาไทย</summary>
    public string GetLevelName(string npcGroupId)
    {
        return LevelNames[Mathf.Clamp(GetLevel(npcGroupId), 0, 3)];
    }

    /// <summary>
    /// เพิ่มหรือลดคะแนนชื่อเสียงของกลุ่ม NPC
    /// amount > 0 = ได้ชื่อเสียง | amount < 0 = เสียชื่อเสียง
    /// </summary>
    public void AddReputation(string npcGroupId, int amount, string reason = "")
    {
        if (string.IsNullOrEmpty(npcGroupId) || amount == 0) return;

        if (!reputationScores.ContainsKey(npcGroupId))
            reputationScores[npcGroupId] = 0;

        int before = reputationScores[npcGroupId];
        reputationScores[npcGroupId] = Mathf.Max(0, before + amount);

        string sign = amount > 0 ? $"+{amount}" : $"{amount}";
        Debug.Log($"[Reputation] '{npcGroupId}' {sign} ({reason}) → {reputationScores[npcGroupId]} คะแนน ({GetLevelName(npcGroupId)})");

        // แจ้งเตือนเมื่อระดับขึ้น
        int levelBefore = ScoreToLevel(before);
        int levelAfter  = GetLevel(npcGroupId);
        if (levelAfter > levelBefore)
        {
            string msg = $"<color=#FFD700>⭐ ความสัมพันธ์กับกลุ่ม '{npcGroupId}' เพิ่มขึ้น!</color>\n" +
                         $"ระดับใหม่: <color=#00FF7F>{LevelNames[levelAfter]}</color>";
            if (InteractionUIManager.Instance != null)
                InteractionUIManager.Instance.ShowNotification(msg, 3.5f);
        }
    }

    /// <summary>ตรวจว่าผู้เล่นมีชื่อเสียงถึงระดับที่กำหนดหรือไม่</summary>
    public bool HasReputationLevel(string npcGroupId, int requiredLevel)
    {
        return GetLevel(npcGroupId) >= requiredLevel;
    }

    // ══════════════════════════════════════════════════════════════════
    // Helper
    // ══════════════════════════════════════════════════════════════════
    private int ScoreToLevel(int score)
    {
        if (score >= thresholdLevel3) return 3;
        if (score >= thresholdLevel2) return 2;
        if (score >= thresholdLevel1) return 1;
        return 0;
    }

    [ContextMenu("Debug: แสดงชื่อเสียงทั้งหมด")]
    public void DebugPrintAll()
    {
        if (reputationScores.Count == 0)
        {
            Debug.Log("[Reputation] ยังไม่มีข้อมูลชื่อเสียง");
            return;
        }
        foreach (var kv in reputationScores)
            Debug.Log($"[Reputation] '{kv.Key}': {kv.Value} คะแนน → ระดับ {GetLevel(kv.Key)} ({GetLevelName(kv.Key)})");
    }
}

