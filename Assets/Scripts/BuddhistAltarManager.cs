using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ระบบโต๊ะหมู่บูชา — Singleton จัดการระดับและ QTE Assist
/// วิธีใช้: วางบน GameObject ที่เป็นโต๊ะหมู่บูชาในฉาก
/// ผู้เล่นเดินมากด [E] เพื่อดูข้อมูลหรืออัพเกรด
/// </summary>
public class BuddhistAltarManager : InteractableItem
{
    // ──────────────────────────────────────────────────────────
    // Singleton
    // ──────────────────────────────────────────────────────────
    public static BuddhistAltarManager Instance { get; private set; }

    // ──────────────────────────────────────────────────────────
    // ระดับและค่าใช้จ่าย
    // ──────────────────────────────────────────────────────────
    [Header("=== โต๊ะหมู่บูชา (Buddhist Altar) ===")]
    [Tooltip("ระดับปัจจุบันของโต๊ะหมู่บูชา (1–4)")]
    [Range(1, 4)]
    public int currentLevel = 1;

    [Header("ค่าอัพเกรด (Upgrade Cost)")]
    [Tooltip("ค่าอัพเกรดจาก L1 → L2 (บาท)")]
    public int costToLevel2 = 450;
    [Tooltip("ค่าอัพเกรดจาก L2 → L3 (บาท)")]
    public int costToLevel3 = 1200;
    [Tooltip("ค่าอัพเกรดจาก L3 → L4 (บาท)")]
    public int costToLevel4 = 3000;

    [Header("QTE Assist ต่อระดับ (ปรับ Balance ได้)")]

    [Header("  L1 — ยากสุด (ไม่ช่วยเลย)")]
    [Tooltip("หน้าต่าง QTE กว้างแค่ไหน (วินาที) — ยิ่งมากยิ่งง่าย")]
    public float l1_QteWindow   = 0.25f;
    [Tooltip("ความเร็ว Cursor/Bar ของ QTE")]
    public float l1_QteSpeed    = 1.8f;
    [Tooltip("เปิดใช้ Visual Aid (เส้นช่วยอ่านจังหวะ) หรือไม่")]
    public bool  l1_VisualAid   = false;
    [Tooltip("Auto-Catch: ระบบช่วยจับ QTE อัตโนมัติหรือไม่")]
    public bool  l1_AutoCatch   = false;

    [Header("  L2 — ง่ายขึ้นเล็กน้อย")]
    public float l2_QteWindow   = 0.40f;
    public float l2_QteSpeed    = 1.4f;
    public bool  l2_VisualAid   = false;
    public bool  l2_AutoCatch   = false;

    [Header("  L3 — Visual Aid / ช่วยอ่านจังหวะ")]
    public float l3_QteWindow   = 0.55f;
    public float l3_QteSpeed    = 1.0f;
    public bool  l3_VisualAid   = true;
    public bool  l3_AutoCatch   = false;

    [Header("  L4 — ช่วย QTE สูงสุด / Auto-Catch")]
    public float l4_QteWindow   = 0.80f;
    public float l4_QteSpeed    = 0.7f;
    public bool  l4_VisualAid   = true;
    public bool  l4_AutoCatch   = true;

    [Header("Events (Optional)")]
    [Tooltip("ยิง event เมื่ออัพเกรดสำเร็จ (ส่งระดับใหม่เป็น int)")]
    public UnityEvent<int> onUpgraded;

    // ──────────────────────────────────────────────────────────
    // ค่า QTE ที่ใช้งานจริง — QTE system อ่านจากที่นี่
    // ──────────────────────────────────────────────────────────

    /// <summary>หน้าต่าง QTE (วินาที) ของระดับปัจจุบัน</summary>
    public float ActiveQteWindow => currentLevel switch
    {
        1 => l1_QteWindow,
        2 => l2_QteWindow,
        3 => l3_QteWindow,
        4 => l4_QteWindow,
        _ => l1_QteWindow
    };

    /// <summary>ความเร็ว QTE ของระดับปัจจุบัน</summary>
    public float ActiveQteSpeed => currentLevel switch
    {
        1 => l1_QteSpeed,
        2 => l2_QteSpeed,
        3 => l3_QteSpeed,
        4 => l4_QteSpeed,
        _ => l1_QteSpeed
    };

    /// <summary>มี Visual Aid หรือไม่</summary>
    public bool ActiveVisualAid => currentLevel switch
    {
        1 => l1_VisualAid,
        2 => l2_VisualAid,
        3 => l3_VisualAid,
        4 => l4_VisualAid,
        _ => false
    };

    /// <summary>มี Auto-Catch หรือไม่</summary>
    public bool ActiveAutoCatch => currentLevel switch
    {
        1 => l1_AutoCatch,
        2 => l2_AutoCatch,
        3 => l3_AutoCatch,
        4 => l4_AutoCatch,
        _ => false
    };

    // ──────────────────────────────────────────────────────────
    // Internal
    // ──────────────────────────────────────────────────────────
    private PlayerInteraction playerInteraction;

    // ──────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ตั้งค่า InteractableItem base
        itemName             = "โต๊ะหมู่บูชา";
        canRead              = true;
        canCollect           = false;
        interactionKeyText   = "T";
        RefreshPrompt();
    }

    void Start()
    {
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        RefreshPrompt();

        // ตรวจสอบและสร้าง AltarUpgradeUI หากยังไม่มีใน Scene
        EnsureUpgradeUIExists();
    }

    private void EnsureUpgradeUIExists()
    {
        if (AltarUpgradeUI.Instance == null && FindFirstObjectByType<AltarUpgradeUI>() == null)
        {
            GameObject uiObj = new GameObject("AltarUpgradeUI");
            uiObj.AddComponent<AltarUpgradeUI>();
        }
    }

    // ──────────────────────────────────────────────────────────
    // Override OnRead — ผู้เล่นกด [T] หรือ [E] ที่โต๊ะ
    // ──────────────────────────────────────────────────────────
    public override void OnRead(PlayerInteraction interactor)
    {
        playerInteraction = interactor;
        EnsureUpgradeUIExists();

        // เปิด Confirmation Panel ให้เลือกว่าจะอัพเกรดหรือไม่อัพเกรด
        if (AltarUpgradeUI.Instance != null)
        {
            AltarUpgradeUI.Instance.Show(interactor);
        }
        else
        {
            TryUpgrade(interactor);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Logic
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// พยายามอัพเกรดโต๊ะหมู่บูชาหนึ่งระดับ
    /// ตรวจสอบเงินจาก PlayerWalletManager (ถ้ามี) หรือใช้ Fallback
    /// </summary>
    public void TryUpgrade(PlayerInteraction interactor)
    {
        if (currentLevel >= 4)
        {
            interactor?.ShowNotification("⛩️ โต๊ะหมู่บูชาอยู่ในระดับสูงสุดแล้ว!", 2.5f);
            return;
        }

        int cost = GetUpgradeCost();

        // ── ตรวจสอบและหักเงิน ──────────────────────────────
        bool canAfford = false;
        bool deducted  = false;

        var wallet = PlayerWalletManager.Instance;
        if (wallet != null)
        {
            canAfford = wallet.money >= cost;
            if (canAfford)
            {
                wallet.SpendMoney(cost);
                deducted = true;
            }
        }
        else
        {
            // Fallback: ยังไม่มี WalletManager → อัพเกรดฟรี (Dev Mode)
            canAfford = true;
            deducted  = true;
            Debug.LogWarning("[BuddhistAltar] ไม่พบ PlayerWalletManager — อัพเกรดฟรี (Dev Mode)");
        }

        if (!canAfford)
        {
            interactor?.ShowNotification(
                $"💸 เงินไม่พอ! ต้องการ {cost:N0} บาท (มี {wallet.money:N0} บาท)", 3.0f);
            return;
        }

        if (deducted)
        {
            PerformUpgrade(interactor, cost);
        }
    }

    private void PerformUpgrade(PlayerInteraction interactor, int cost)
    {
        int prevLevel = currentLevel;
        currentLevel++;

        RefreshPrompt();
        onUpgraded?.Invoke(currentLevel);

        // ชำระล้างวิญญาณชั่วร้ายด้วยบารมีโต๊ะหมู่บูชา
        if (GhostCurseManager.Instance != null && GhostCurseManager.Instance.currentGhostCount > 0)
        {
            GhostCurseManager.Instance.CleanseGhosts(1);
        }

        string benefitText = GetLevelBenefitText(currentLevel);
        string msg = $"⛩️ อัพเกรดโต๊ะหมู่บูชา Lv.{prevLevel} → <color=#FFD700>Lv.{currentLevel}</color> สำเร็จ!\n" +
                     $"ใช้ {cost:N0} บาท | {benefitText}";
        interactor?.ShowNotification(msg, 4.0f);

        Debug.Log($"[BuddhistAltar] ✨ Lv.{prevLevel}→Lv.{currentLevel} | " +
                  $"QteWindow={ActiveQteWindow:F2}s | Speed={ActiveQteSpeed:F1} | " +
                  $"VisualAid={ActiveVisualAid} | AutoCatch={ActiveAutoCatch}");
    }

    private void ShowStatus(PlayerInteraction interactor)
    {
        string line1 = $"⛩️ โต๊ะหมู่บูชา <color=#FFD700>Lv.{currentLevel}</color>";
        string line2 = GetLevelBenefitText(currentLevel);
        string line3 = currentLevel < 4
            ? $"อัพเกรดครั้งถัดไป: <color=#FFD700>{GetUpgradeCost():N0} บาท</color> → Lv.{currentLevel + 1}"
            : "<color=#4ADE80>★ ระดับสูงสุดแล้ว!</color>";

        interactor?.ShowNotification($"{line1}\n{line2}\n{line3}", 4.0f);
    }

    // ──────────────────────────────────────────────────────────
    // Public Helpers
    // ──────────────────────────────────────────────────────────

    /// <summary>ค่าอัพเกรดสู่ระดับถัดไป</summary>
    public int GetUpgradeCost() => currentLevel switch
    {
        1 => costToLevel2,
        2 => costToLevel3,
        3 => costToLevel4,
        _ => 0
    };

    /// <summary>คำอธิบายสั้นๆ ของแต่ละระดับสำหรับแสดงบน UI</summary>
    public static string GetLevelBenefitText(int level) => level switch
    {
        1 => "QTE ยากสุด — ไม่มีการช่วยเหลือ",
        2 => "Timing/QTE ง่ายขึ้นเล็กน้อย",
        3 => "Visual Aid เปิดใช้งาน — ช่วยอ่านจังหวะ",
        4 => "QTE ช่วยสูงสุด + Auto-Catch",
        _ => ""
    };

    // ──────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────
    private string GetUpgradePrompt()
    {
        if (currentLevel >= 4)
            return $"โต๊ะหมู่บูชา Lv.{currentLevel} (ระดับสูงสุด)";

        int cost = GetUpgradeCost();
        return $"อัพเกรดโต๊ะหมู่บูชา Lv.{currentLevel}→{currentLevel + 1} ({cost:N0} บาท)";
    }

    private void RefreshPrompt()
    {
        customReadPromptText = GetUpgradePrompt();
    }
}
