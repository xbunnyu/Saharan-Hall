using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Panel ยืนยันการอัพเกรดโต๊ะหมู่บูชา
/// รองรับทั้ง Canvas UI และ Built-in OnGUI Modal ในตัว
/// ผู้เล่นกด [T] ที่โต๊ะหมู่บูชาเพื่อเปิดหน้าต่างนี้ แล้วเลือกอัพเกรดหรือไม่อัพเกรด
/// </summary>
public class AltarUpgradeUI : MonoBehaviour
{
    public static AltarUpgradeUI Instance { get; private set; }

    [Header("Panel หลัก (Optional Canvas)")]
    public GameObject panel;                      // Panel ทั้งหมด (เปิด/ปิด)

    [Header("ข้อมูล")]
    public TextMeshProUGUI titleText;             // "อัพเกรดโต๊ะหมู่บูชา"
    public TextMeshProUGUI levelInfoText;         // "Lv.1 → Lv.2"
    public TextMeshProUGUI benefitText;           // คำอธิบายระดับใหม่
    public TextMeshProUGUI costText;              // "ค่าใช้จ่าย: 450 บาท"
    public TextMeshProUGUI walletText;            // "เงินของคุณ: 500 บาท"

    [Header("ปุ่ม")]
    public Button confirmButton;                  // [ใช่] อัพเกรด
    public Button cancelButton;                   // [ไม่] ยกเลิก
    public TextMeshProUGUI confirmButtonText;
    public TextMeshProUGUI cancelButtonText;

    // Internal
    private PlayerInteraction playerInteraction;
    private PlayerController  playerController;
    private bool isOpen = false;
    private float canAcceptInputTime = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        playerController  = FindFirstObjectByType<PlayerController>();

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(OnCancel);

        if (confirmButtonText != null) confirmButtonText.text = "อัพเกรด  [T]";
        if (cancelButtonText  != null) cancelButtonText.text  = "ไม่อัพเกรด  [Esc]";
    }

    void Update()
    {
        if (!isOpen) return;
        if (Time.time < canAcceptInputTime) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var altar = BuddhistAltarManager.Instance;
        bool isMaxLevel = altar != null && altar.currentLevel >= 4;

        // [T] หรือ [Enter] หรือ [E] หรือ [Y] = ยืนยันอัพเกรด (หรือปิดถ้าเต็มแล้ว)
        if (keyboard.tKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame 
            || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame 
            || keyboard.yKey.wasPressedThisFrame)
        {
            if (isMaxLevel)
            {
                OnCancel();
            }
            else
            {
                OnConfirm();
            }
        }

        // [Esc] หรือ [Q] หรือ [N] = ยกเลิก / ปิดหน้าต่าง
        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame 
            || keyboard.nKey.wasPressedThisFrame)
        {
            OnCancel();
        }
    }

    // ──────────────────────────────────────────────────────────
    // เปิด Panel
    // ──────────────────────────────────────────────────────────
    public void Show(PlayerInteraction interactor)
    {
        var altar = BuddhistAltarManager.Instance;
        if (altar == null) return;

        playerInteraction = interactor;
        isOpen = true;
        canAcceptInputTime = Time.time + 0.2f;

        int nextLevel = altar.currentLevel + 1;
        int cost      = altar.GetUpgradeCost();
        int wallet    = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;
        bool canAfford = wallet >= cost;
        bool isMaxLevel = altar.currentLevel >= 4;

        // ── อัพเดต Canvas Text (ถ้ามี) ──
        if (titleText != null) titleText.text = "⛩  อัพเกรดโต๊ะหมู่บูชา";
        if (levelInfoText != null)
        {
            levelInfoText.text = isMaxLevel ? $"Lv.{altar.currentLevel} (ระดับสูงสุด)" : $"Lv.{altar.currentLevel}  →  Lv.{nextLevel}";
        }
        if (benefitText != null)
        {
            benefitText.text = isMaxLevel ? BuddhistAltarManager.GetLevelBenefitText(altar.currentLevel) : BuddhistAltarManager.GetLevelBenefitText(nextLevel);
        }
        if (costText != null)
        {
            costText.text = isMaxLevel ? "ระดับสูงสุดแล้ว" : $"ค่าอัพเกรด:  <color=#FFD700>{cost:N0} บาท</color>";
        }

        if (walletText != null)
        {
            string walletColor = canAfford ? "#4ADE80" : "#F87171";
            walletText.text = $"เงินของคุณ:  <color={walletColor}>{wallet:N0} บาท</color>";
        }

        // ── ปุ่มยืนยัน ──
        if (confirmButton != null)
        {
            confirmButton.interactable = canAfford && !isMaxLevel;
            if (confirmButtonText != null)
            {
                if (isMaxLevel) confirmButtonText.text = "สูงสุดแล้ว";
                else confirmButtonText.text = canAfford ? "อัพเกรด  [T]" : "เงินไม่พอ";
            }
        }

        // ── แสดง Canvas Panel และ Freeze Player ──
        if (panel != null) panel.SetActive(true);
        FreezePlayer(true);
    }

    // ──────────────────────────────────────────────────────────
    // ปุ่ม อัพเกรด
    // ──────────────────────────────────────────────────────────
    public void OnConfirm()
    {
        if (!isOpen) return;

        var altar = BuddhistAltarManager.Instance;
        if (altar == null) { Close(); return; }

        int cost = altar.GetUpgradeCost();
        int wallet = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;

        if (wallet < cost && PlayerWalletManager.Instance != null)
        {
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification($"💸 เงินไม่พอ! ต้องการ {cost:N0} บาท (คุณมี {wallet:N0} บาท)", 2.5f);
            }
            return;
        }

        Close();
        altar.TryUpgrade(playerInteraction);
    }

    // ──────────────────────────────────────────────────────────
    // ปุ่ม ไม่อัพเกรด / ปิด
    // ──────────────────────────────────────────────────────────
    public void OnCancel()
    {
        if (!isOpen) return;
        Close();

        if (InteractionUIManager.Instance != null)
            InteractionUIManager.Instance.ShowNotification("ยกเลิกการอัพเกรดโต๊ะหมู่บูชา", 1.5f);
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        FreezePlayer(false);
    }

    private void FreezePlayer(bool freeze)
    {
        Cursor.lockState = freeze ? CursorLockMode.None  : CursorLockMode.Locked;
        Cursor.visible   = freeze;
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null) playerController.enabled = !freeze;
    }

    public bool IsOpen() => isOpen;

    // ──────────────────────────────────────────────────────────
    // OnGUI Fallback Modal — แสดงผลหน้าต่างเลือกอัพเกรดแบบสวยงาม
    // ──────────────────────────────────────────────────────────
    void OnGUI()
    {
        if (!isOpen) return;
        if (panel != null && panel.activeInHierarchy) return;

        var altar = BuddhistAltarManager.Instance;
        if (altar == null) return;

        int currentLevel = altar.currentLevel;
        int nextLevel = currentLevel + 1;
        int cost = altar.GetUpgradeCost();
        int wallet = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;
        bool canAfford = wallet >= cost || PlayerWalletManager.Instance == null;
        bool isMaxLevel = currentLevel >= 4;

        float boxWidth = Mathf.Min(560f, Screen.width * 0.9f);
        float boxHeight = Mathf.Min(400f, Screen.height * 0.8f);
        float posX = (Screen.width - boxWidth) / 2f;
        float posY = (Screen.height - boxHeight) / 2f;

        // พื้นหลังโปร่งแสงดำลึก
        GUI.color = new Color(0.08f, 0.09f, 0.13f, 0.95f);
        GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), GUIContent.none);
        GUI.color = Color.white;

        // เส้นขอบทองตกแต่งบนหัว
        GUI.color = new Color(1f, 0.84f, 0.2f, 0.8f);
        GUI.Box(new Rect(posX + 10, posY + 10, boxWidth - 20, 3f), GUIContent.none);
        GUI.color = Color.white;

        // หัวข้อ Modal
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(posX + 20, posY + 20, boxWidth - 40, 30), "⛩️ โต๊ะหมู่บูชา (Buddhist Altar)", headerStyle);

        // แถบแสดงระดับ
        GUIStyle levelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        levelStyle.normal.textColor = Color.white;

        string levelStr = isMaxLevel
            ? $"ระดับปัจจุบัน: <color=#FFD700>Lv.{currentLevel}</color> (ระดับสูงสุดแล้ว ★)"
            : $"ระดับปัจจุบัน: <color=#A0E6FF>Lv.{currentLevel}</color>   ➔   ระดับถัดไป: <color=#FFD700>Lv.{nextLevel}</color>";
        GUI.Label(new Rect(posX + 20, posY + 60, boxWidth - 40, 26), levelStr, levelStyle);

        // กล่องรายละเอียด Perk / ความสามารถ
        float infoY = posY + 95;
        float infoH = 135;
        GUI.color = new Color(0.13f, 0.15f, 0.22f, 0.8f);
        GUI.Box(new Rect(posX + 25, infoY, boxWidth - 50, infoH), GUIContent.none);
        GUI.color = Color.white;

        GUIStyle detailHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        detailHeaderStyle.normal.textColor = new Color(1f, 0.8f, 0.3f);
        GUI.Label(new Rect(posX + 40, infoY + 10, boxWidth - 80, 20), "✨ คุณสมบัติและความช่วยเหลือ:", detailHeaderStyle);

        GUIStyle detailBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true
        };
        detailBodyStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        string currentPerk = BuddhistAltarManager.GetLevelBenefitText(currentLevel);
        string nextPerk = !isMaxLevel ? BuddhistAltarManager.GetLevelBenefitText(nextLevel) : "ระดับสูงสุด";

        string perkText = $"• ปัจจุบัน (Lv.{currentLevel}): {currentPerk}\n";
        if (!isMaxLevel)
        {
            perkText += $"• อัพเกรด (Lv.{nextLevel}): <color=#00FF7F>{nextPerk}</color>\n";
            perkText += "• บารมีช่วยชำระล้างดวงวิญญาณและลดคำสาปในตำหนัก";
        }
        else
        {
            perkText += "<color=#00FF7F>★ เปิดใช้งานการช่วยเหลือ QTE สูงสุด + Auto-Catch แล้ว</color>";
        }

        GUI.Label(new Rect(posX + 40, infoY + 32, boxWidth - 80, 95), perkText, detailBodyStyle);

        // ส่วนแสดงราคาและยอดเงิน
        float priceY = infoY + infoH + 12;
        GUIStyle priceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        if (!isMaxLevel)
        {
            priceStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(posX + 30, priceY, (boxWidth - 60) / 2f, 24), $"💰 ค่าอัพเกรด: {cost:N0} บาท", priceStyle);

            string walletColorTag = canAfford ? "#00FF7F" : "#FF6347";
            priceStyle.normal.textColor = canAfford ? new Color(0f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(posX + 30 + (boxWidth - 60) / 2f, priceY, (boxWidth - 60) / 2f, 24), $"💵 เงินในกระเป๋า: {wallet:N0} บาท", priceStyle);
        }
        else
        {
            priceStyle.normal.textColor = new Color(0f, 1f, 0.5f);
            GUI.Label(new Rect(posX + 30, priceY, boxWidth - 60, 24), $"💵 เงินในกระเป๋า: {wallet:N0} บาท (โต๊ะหมู่บูชาเต็มระดับแล้ว)", priceStyle);
        }

        // ปุ่ม Action
        float btnY = posY + boxHeight - 65;
        float btnHeight = 44;

        if (!isMaxLevel)
        {
            float btnWidth = (boxWidth - 70) / 2f;

            // ปุ่ม ยืนยันอัพเกรด [T]
            if (canAfford)
            {
                GUI.backgroundColor = new Color(0.15f, 0.75f, 0.35f);
                if (GUI.Button(new Rect(posX + 25, btnY, btnWidth, btnHeight), "✅ อัพเกรด  [T / Enter]"))
                {
                    OnConfirm();
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                GUI.Button(new Rect(posX + 25, btnY, btnWidth, btnHeight), $"💸 เงินไม่พอ ({cost:N0} บ.)");
            }

            // ปุ่ม ไม่อัพเกรด / ยกเลิก [Esc]
            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUI.Button(new Rect(posX + 35 + btnWidth, btnY, btnWidth, btnHeight), "❌ ไม่อัพเกรด  [Esc / Q]"))
            {
                OnCancel();
            }
        }
        else
        {
            // ปุ่ม ปิดหน้าต่าง (ระดับสูงสุดแล้ว)
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
            if (GUI.Button(new Rect(posX + 30, btnY, boxWidth - 60, btnHeight), "🌟 โต๊ะหมู่บูชาเต็มระดับแล้ว (กด Esc เพื่อปิด)"))
            {
                OnCancel();
            }
        }

        GUI.backgroundColor = Color.white;
    }
}
