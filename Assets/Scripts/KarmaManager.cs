using UnityEngine;

/// <summary>
/// Singleton ติดตามค่า Karma (ค่าความดี) ของผู้เล่น
/// - ค่าความดีถึง 80  -> เรียกฉากจบดี (GoodEnding)
/// - ค่าความดีถึง 0   -> เรียกฉากโดนกลืนกิน (BadEnding)
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
    [Tooltip("ค่าความดีเริ่มต้น (แนะนำ 40 เพื่อให้อยู่กึ่งกลางระหว่าง 0 ถึง 80)")]
    public int startingKarma = 40;

    [Tooltip("เกณฑ์ค่าความดีสำหรับฉากจบดี")]
    public int targetGoodKarma = 80;

    [Tooltip("เกณฑ์ค่าความดีสำหรับฉากโดนกลืนกิน")]
    public int targetBadKarma = 0;

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
        Debug.Log($"[KarmaManager] 🌟 เริ่มต้นค่าความดี = {karma} (จบดี: >={targetGoodKarma}, โดนกลืนกิน: <={targetBadKarma})");
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

        // ตรวจสอบเงื่อนไขการจบเกมทันทีที่มีการเปลี่ยนแปลง
        EvaluateKarmaEnding();
    }

    /// <summary>
    /// ตรวจสอบและเรียกฉากจบตามเกณฑ์ค่าความดี
    /// </summary>
    public void EvaluateKarmaEnding()
    {
        if (GameEndingManager.Instance == null)
        {
            Debug.LogError("[KarmaManager] ⚠️ ไม่พบ GameEndingManager ในฉาก!");
            return;
        }

        if (karma >= targetGoodKarma)
        {
            Debug.Log($"[KarmaManager] ✨ ค่าความดีถึง {karma} (>= {targetGoodKarma}) → เรียกฉากจบดี!");
            GameEndingManager.Instance.TriggerEnding(GameEndingType.GoodEnding);
        }
        else if (karma <= targetBadKarma)
        {
            Debug.Log($"[KarmaManager] 💀 ค่าความดีลดลงถึง {karma} (<= {targetBadKarma}) → เรียกฉากโดนกลืนกิน!");
            GameEndingManager.Instance.TriggerEnding(GameEndingType.BadEnding);
        }
        else
        {
            Debug.Log($"[KarmaManager] ค่าความดีปัจจุบัน: {karma} (ยังเล่นต่อได้: เป้าหมายจบดี {targetGoodKarma}, ระวังโดนกลืนกิน {targetBadKarma})");
        }
    }

    // ──────────────────────────────────────────────────────────
    // ตัวช่วยทดสอบใน Unity Inspector (คลิกขวาที่คอมโพเนนต์)
    // ──────────────────────────────────────────────────────────
    [ContextMenu("🧪 ทดสอบ: ตั้งค่าความดีเป็น 80 (เรียกฉากจบดี)")]
    public void TestSetKarma80()
    {
        karma = targetGoodKarma;
        EvaluateKarmaEnding();
    }

    [ContextMenu("🧪 ทดสอบ: ตั้งค่าความดีเป็น 0 (เรียกฉากโดนกลืนกิน)")]
    public void TestSetKarma0()
    {
        karma = targetBadKarma;
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
