using UnityEngine;

/// <summary>
/// Singleton ติดตามค่า Karma (ค่าความดี) ของผู้เล่น
/// - ค่าความดี >= 50  -> จบดี (Good end)
/// - ค่าความดี < 50   -> จบแย่ (bad end)
/// (ดึงฉากจบหลังกดนอนในวันที่ 2)
/// </summary>
public class KarmaManager : MonoBehaviour
{
    private static KarmaManager _instance;
    public static KarmaManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<KarmaManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("KarmaManager");
                    _instance = go.AddComponent<KarmaManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("1. ค่าความดีเริ่มต้น & เกณฑ์ฉากจบ")]
    [Tooltip("ค่าความดีเริ่มต้น")]
    public int startingKarma = 40;

    [Tooltip("เกณฑ์ค่าความดีสำหรับฉากจบดี (Good end)")]
    public int targetGoodKarma = 50;

    /// <summary>ค่าความดีปัจจุบันของผู้เล่น</summary>
    [Header("2. ค่าความดีปัจจุบัน (Current Karma)")]
    public int karma;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        karma = startingKarma;
        Debug.Log($"[KarmaManager] 🌟 เริ่มต้นค่าความดี = {karma} (เกณฑ์ Good end: >={targetGoodKarma})");
    }

    // ──────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// เพิ่มหรือลดค่าความดี
    /// </summary>
    public void ApplyKarma(int amount, string questTitle = "")
    {
        if (amount == 0) return;

        karma += amount;
        string sign = amount > 0 ? "+" : "";
        Debug.Log($"[KarmaManager] {sign}{amount} จาก '{questTitle}' → ค่าความดีรวมปัจจุบัน: {karma}");
    }

    /// <summary>
    /// ตรวจสอบและเรียกฉากจบ
    /// </summary>
    public void EvaluateKarmaEnding()
    {
        if (GameEndingManager.Instance == null)
        {
            Debug.LogError("[KarmaManager] ⚠️ ไม่พบ GameEndingManager ในฉาก!");
            return;
        }

        GameEndingManager.Instance.EvaluateDay2Ending();
    }

    // ──────────────────────────────────────────────────────────
    // ตัวช่วยทดสอบใน Unity Inspector (คลิกขวาที่คอมโพเนนต์)
    // ──────────────────────────────────────────────────────────
    [ContextMenu("🧪 ทดสอบ: ตั้งค่าความดีเป็น 60 (ฉากจบสายขาว)")]
    public void TestSetKarma60()
    {
        karma = 60;
        EvaluateKarmaEnding();
    }

    [ContextMenu("🧪 ทดสอบ: ตั้งค่าความดีเป็น 30 (ฉากจบสายดำ)")]
    public void TestSetKarma30()
    {
        karma = 30;
        EvaluateKarmaEnding();
    }

    [ContextMenu("🧪 ทดสอบ: เพิ่มความดี +20")]
    public void TestAddKarma20()
    {
        ApplyKarma(20, "ทดสอบเพิ่มความดี");
    }

    [ContextMenu("🧪 ทดสอบ: ลดความดี -20")]
    public void TestSubtractKarma20()
    {
        ApplyKarma(-20, "ทดสอบลดความดี");
    }
}
