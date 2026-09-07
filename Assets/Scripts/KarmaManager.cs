using UnityEngine;

/// <summary>
/// Singleton ติดตามค่า Karma (ดี/ชั่ว) ของผู้เล่น
/// ผู้เล่นไม่สามารถเห็นค่านี้ได้โดยตรง — ใช้ภายในระบบเกมเท่านั้น
/// ค่า karma บวก = ดี | ค่า karma ลบ = ชั่ว
/// </summary>
public class KarmaManager : MonoBehaviour
{
    public static KarmaManager Instance { get; private set; }

    [Header("ค่า Karma เริ่มต้น")]
    public int startingKarma = 0;

    /// <summary>ค่า Karma ปัจจุบัน (ไม่แสดงให้ผู้เล่นเห็น)</summary>
    [HideInInspector]
    public int karma;

    // ──────────────────────────────────────────────────────────
    // Thresholds ระดับ Karma (ปรับ Balance ใน Inspector ไม่ได้
    // เพราะ HideInInspector แต่ Designer แก้ได้ในโค้ดนี้)
    // ──────────────────────────────────────────────────────────
    [Header("Threshold ระดับ Karma (Internal)")]
    [Tooltip("Karma สูงกว่านี้ = ดีมาก")]
    public int thresholdVirtuous  =  50;
    [Tooltip("Karma ต่ำกว่านี้ = ชั่วมาก")]
    public int thresholdSinful    = -50;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        karma = startingKarma;
        Debug.Log($"[Karma] เริ่มต้น karma = {karma}");
    }

    // ──────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────

    /// <summary>เพิ่ม/ลด Karma หลังส่งเควส</summary>
    public void ApplyKarma(int amount, string questTitle = "")
    {
        if (amount == 0) return;

        karma += amount;
        string sign = amount > 0 ? "+" : "";
        Debug.Log($"[Karma] {sign}{amount} จาก '{questTitle}'  → รวม {karma}  ({GetKarmaLabel()})");
    }

    /// <summary>ระดับ Karma ปัจจุบัน (ใช้ภายใน)</summary>
    public KarmaLevel GetKarmaLevel()
    {
        if (karma >= thresholdVirtuous)  return KarmaLevel.Virtuous;
        if (karma <= thresholdSinful)    return KarmaLevel.Sinful;
        return KarmaLevel.Neutral;
    }

    /// <summary>ชื่อระดับ Karma (สำหรับ Debug เท่านั้น)</summary>
    public string GetKarmaLabel() => GetKarmaLevel() switch
    {
        KarmaLevel.Virtuous => "ผู้มีคุณธรรม",
        KarmaLevel.Sinful   => "ผู้มีบาป",
        _                   => "กลาง"
    };
}

public enum KarmaLevel
{
    Virtuous,   // ดี
    Neutral,    // กลาง
    Sinful      // ชั่ว
}
