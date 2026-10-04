using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ระบบชื่อเสียงแบบง่าย (Global Reputation System)
/// ─────────────────────────────────────────────────────────────────────
/// เก็บค่าชื่อเสียงรวมของผู้เล่น/ตำหนัก เป็นตัวเลขและระดับง่ายๆ
/// ระดับชื่อเสียง:
///   ระดับ 0 (0-9 แต้ม)   : เริ่มต้น (ยังไม่รู้จัก)
///   ระดับ 1 (10-29 แต้ม) : พอมีชื่อเสียง (NPC เริ่มเปิดเผยข้อมูลสอบถาม)
///   ระดับ 2 (30-59 แต้ม) : มีชื่อเสียง (ปลดล็อกเควสและเรื่องราวขั้นสูง)
///   ระดับ 3 (60+ แต้ม)   : เลื่องลือ
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance { get; private set; }

    [Header("1. ค่าชื่อเสียงปัจจุบัน")]
    [Tooltip("คะแนนชื่อเสียงปัจจุบันของตำหนัก")]
    public int reputationScore = 0;

    [Header("2. เกณฑ์ระดับชื่อเสียง")]
    public int thresholdLevel1 = 10;
    public int thresholdLevel2 = 30;
    public int thresholdLevel3 = 60;

    [Header("3. HUD แสดงผลบนหน้าจอ")]
    [Tooltip("แสดงตัวเลขชื่อเสียงมุมจอด้านล่างแบบง่ายอัตโนมัติ")]
    public bool showHUD = true;

    [Header("Event")]
    public UnityEvent<int, int> onReputationChanged; // (คะแนนปัจจุบัน, ระดับปัจจุบัน)

    private static readonly string[] LevelNames =
    {
        "เริ่มต้น",       // ระดับ 0
        "พอมีชื่อเสียง",   // ระดับ 1
        "มีชื่อเสียง",     // ระดับ 2
        "เลื่องลือ"       // ระดับ 3
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>ดึงคะแนนชื่อเสียงรวม</summary>
    public int GetScore(string groupId = "") => reputationScore;

    /// <summary>ดึงระดับชื่อเสียง (0-3)</summary>
    public int GetLevel(string groupId = "")
    {
        if (reputationScore >= thresholdLevel3) return 3;
        if (reputationScore >= thresholdLevel2) return 2;
        if (reputationScore >= thresholdLevel1) return 1;
        return 0;
    }

    /// <summary>ดึงชื่อระดับภาษาไทย</summary>
    public string GetLevelName(string groupId = "")
    {
        return LevelNames[GetLevel()];
    }

    /// <summary>เพิ่มหรือลดคะแนนชื่อเสียง</summary>
    public void AddReputation(int amount, string reason = "")
    {
        if (amount == 0) return;

        int oldLevel = GetLevel();
        reputationScore = Mathf.Max(0, reputationScore + amount);
        int newLevel = GetLevel();

        string sign = amount > 0 ? $"+{amount}" : $"{amount}";
        Debug.Log($"[Reputation] {sign} แต้ม ({reason}) → รวม {reputationScore} แต้ม (ระดับ {newLevel}: {GetLevelName()})");

        onReputationChanged?.Invoke(reputationScore, newLevel);

        // แจ้งเตือนเมื่อระดับชื่อเสียงเลื่อนขั้น
        if (newLevel > oldLevel)
        {
            string msg = $"<color=#FFD700>⭐ ชื่อเสียงตำหนักเลื่อนระดับ!</color>\n" +
                         $"ระดับใหม่: <color=#00FF7F>ระดับ {newLevel} ({GetLevelName()})</color>";
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(msg, 3.5f);
            }
        }
    }

    /// <summary>โอเวอร์โหลดสำหรับโค้ดเดิมที่ยังส่ง groupId เข้ามา (รวมแต้มเข้าด้วยกัน)</summary>
    public void AddReputation(string groupId, int amount, string reason = "")
    {
        AddReputation(amount, reason);
    }

    /// <summary>ตรวจว่ามีชื่อเสียงถึงระดับที่กำหนดหรือไม่</summary>
    public bool HasReputationLevel(int requiredLevel) => GetLevel() >= requiredLevel;
    public bool HasReputationLevel(string groupId, int requiredLevel) => GetLevel() >= requiredLevel;

    // ══════════════════════════════════════════════════════════════════
    // Simple Corner HUD (แสดงตัวเลขชื่อเสียงมุมซ้ายล่างแบบเรียบง่าย)
    // ══════════════════════════════════════════════════════════════════
    void OnGUI()
    {
        if (!showHUD) return;

        // วาดแถบข้อความเรียบง่ายที่มุมซ้ายล่างของจอ
        GUIStyle hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        hudStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

        string text = $"⭐ ชื่อเสียง: {reputationScore} (ระดับ {GetLevel()}: {GetLevelName()})";
        GUI.Label(new Rect(30, Screen.height - 35, 300, 25), text, hudStyle);
    }
}
