using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class QuestUIManager : MonoBehaviour
{
    public static QuestUIManager Instance { get; private set; }

    [Header("1. UI Panel & References (หน้าต่างเสนอเควส)")]
    [Tooltip("Panel หน้าต่างเควส")]
    public GameObject questDialogPanel;

    [Header("NPC Info Display")]
    public Image npcPortraitImage;
    public TextMeshProUGUI npcNameText;

    [Header("Quest Content")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questRewardText;
    public TextMeshProUGUI questRequirementText;

    [Header("Buttons")]
    public Button acceptButton;
    public Button declineButton;
    [Tooltip("ปุ่ม 'สอบถามเพิ่มเติม' — ต้องการชื่อเสียงกับ NPC ก่อนจึงจะได้คำตอบที่ลึกขึ้น (Optional)")]
    public Button inquiryButton;
    public TextMeshProUGUI acceptButtonText;
    public TextMeshProUGUI declineButtonText;
    public TextMeshProUGUI inquiryButtonText;

    [Header("Response Dialogue Panel (เมื่อกดรับ/ปฏิเสธ)")]
    public GameObject responsePanel;
    public TextMeshProUGUI responseDialogueText;

    [Header("Decline & Waiting Dialogue (กล่องข้อความปฏิเสธ และ ข้อความตอนรอ)")]
    [Tooltip("กล่องข้อความเมื่อปฏิเสธเควส (Decline Dialogue Text) ในหน้าต่างเควส")]
    public TextMeshProUGUI declineDialogueText;
    [Tooltip("กล่องข้อความตอนรอเมื่อรับเควสแล้ว (Waiting Dialogue Text) ในหน้าต่างเควส")]
    public TextMeshProUGUI waitingDialogueText;

    [Header("2. Quest Tracker HUD (แถบแสดงเควสฝั่งซ้ายจอ)")]
    [Tooltip("Panel HUD แสดงรายการเควสฝั่งซ้ายจอ (Optional Canvas)")]
    public GameObject questTrackerPanel;
    [Tooltip("ข้อความหัวข้อ Quest Tracker")]
    public TextMeshProUGUI trackerHeaderText;
    [Tooltip("ข้อความรายละเอียดเควสที่กำลังทำ")]
    public TextMeshProUGUI trackerContentText;

    [Header("3. Settings")]
    public bool enableBuiltInFallbackUI = true;
    public bool showTrackerOnHUD = true;

    // Active Quest Tracking
    public List<QuestData> activeQuests = new List<QuestData>();

    private NPCController currentNPC;
    private QuestData currentQuest;
    private bool isDialogActive = false;
    private bool isShowingResponse = false;
    private bool isPendingDecline = false;
    private bool isPendingAccept = false;
    private string responseMessage = "";
    private float canAcceptInputTime = 0f;
    private PlayerController playerController;
    private PlayerInteraction playerInteraction;

    // Dialogue Pagination & Multi-step Dialogue
    private int currentDialoguePageIndex = 0;
    private List<string> currentDialoguePages = new List<string>();
    private bool isChoicesPage = false;
    private bool isSimpleDialogue = false;
    private Action onSimpleDialogueFinished = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (questDialogPanel != null) questDialogPanel.SetActive(false);
        if (responsePanel != null) responsePanel.SetActive(false);

        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }

        if (declineButton != null)
        {
            declineButton.onClick.AddListener(OnDeclineClicked);
        }

        if (inquiryButton != null)
        {
            inquiryButton.onClick.AddListener(OnInquiryClicked);
            inquiryButton.gameObject.SetActive(false); // ซ่อนไว้ก่อน
        }

        EnsureProperTextSettings();
        UpdateQuestTrackerCanvas();
    }

    /// <summary>
    /// ปรับ RectTransform ของข้อความปฏิเสธให้ตรงกับกล่องข้อความหลัก (ไม่ปรับฟอนต์ เพื่อให้ผู้ใช้กำหนดฟอนต์และขนาดได้เองใน Inspector 100%)
    /// </summary>
    private void EnsureProperTextSettings()
    {
        // จัดตำแหน่งและ Scale ของข้อความปฏิเสธให้เท่ากับ questDescriptionText โดยไม่ยุ่งกับค่าฟอนต์
        if (declineDialogueText != null && questDescriptionText != null)
        {
            RectTransform decRT = declineDialogueText.GetComponent<RectTransform>();
            RectTransform descRT = questDescriptionText.GetComponent<RectTransform>();
            if (decRT != null && descRT != null)
            {
                decRT.localScale = Vector3.one;
                decRT.anchorMin = descRT.anchorMin;
                decRT.anchorMax = descRT.anchorMax;
                decRT.pivot = descRT.pivot;
                decRT.anchoredPosition = descRT.anchoredPosition;
                decRT.sizeDelta = descRT.sizeDelta;
            }
        }
    }

    void Update()
    {
        // อัปเดตข้อมูลบน Canvas HUD เสมอ
        UpdateQuestTrackerCanvas();

        if (!isDialogActive) return;

        // บังคับให้เมาส์แสดงและผู้เล่นถูกตรึงไว้เสมอขณะที่หน้าต่างบทสนทนายังเปิดอยู่
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
        }

        if (Time.time < canAcceptInputTime) return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        var gamepad = Gamepad.current;

        bool isClickPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool isSpaceOrEnterPressed = keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        bool isGamepadAdvance = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;

        bool advanceOrClose = isClickPressed || isSpaceOrEnterPressed || isGamepadAdvance;

        // 1. หากกำลังแสดงหน้าต่างตอบกลับ (Response UI) ผู้เล่นสามารถคลิกหรือกดปุ่มเพื่อจบ/ปิดได้ทันที
        if (isShowingResponse)
        {
            bool anyDismissKey = (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame))
                              || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

            if (advanceOrClose || anyDismissKey)
            {
                CancelInvoke(nameof(CloseDialog));
                CancelInvoke(nameof(FinishAcceptAndClose));
                CancelInvoke(nameof(FinishDeclineAndClose));
                if (isPendingDecline)
                {
                    FinishDeclineAndClose();
                }
                else if (isPendingAccept)
                {
                    FinishAcceptAndClose();
                }
                else
                {
                    CloseDialog();
                }
            }
            return;
        }

        // 2. หากกำลังแสดงบทสนทนาข้อความทั่วไป (Simple Dialogue เช่น ส่งเควสสำเร็จ หรือรอรับผล)
        if (isSimpleDialogue)
        {
            if (advanceOrClose)
            {
                if (currentDialoguePageIndex < currentDialoguePages.Count - 1)
                {
                    // ไปบทสนทนาถัดไป
                    canAcceptInputTime = Time.time + 0.2f;
                    ShowDialoguePage(currentDialoguePageIndex + 1);
                }
                else
                {
                    // หน้าสุดท้ายแล้ว -> จบบทสนทนา
                    Action callback = onSimpleDialogueFinished;
                    onSimpleDialogueFinished = null;
                    isSimpleDialogue = false;
                    CloseDialog();
                    callback?.Invoke();
                }
            }
            return;
        }

        // 3. หากอยู่หน้าตัดสินใจเลือกเควส (Choices Page - มีปุ่ม ยอมรับ / ปฏิเสธ / สอบถาม)
        if (isChoicesPage)
        {
            // กด E หรือ Space หรือ Enter หรือ 1 เพื่อรับเควส
            if (isSpaceOrEnterPressed || (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame)))
            {
                OnAcceptClicked();
            }
            // กด Q หรือ 2 หรือ Escape เพื่อปฏิเสธเควส
            else if (keyboard != null && (keyboard.qKey.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                OnDeclineClicked();
            }
            // กด F หรือ 3 เพื่อสอบถามเพิ่มเติม
            else if (keyboard != null && (keyboard.fKey.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame))
            {
                if (inquiryButton != null && inquiryButton.gameObject.activeInHierarchy)
                {
                    OnInquiryClicked();
                }
            }
            return;
        }

        // 4. กำลังอยู่หน้าเนื้อหาบทสนทนาก่อนถึงหน้าเลือกเควส (!isChoicesPage)
        // ผู้เล่นสามารถกดคลิก หรือ Spacebar (หรือ E / Enter) เพื่อไปบทสนทนาต่อไป
        if (advanceOrClose || (keyboard != null && keyboard.eKey.wasPressedThisFrame))
        {
            canAcceptInputTime = Time.time + 0.2f;
            ShowDialoguePage(currentDialoguePageIndex + 1);
        }
    }

    /// <summary>
    /// แสดงหน้าต่างบทสนทนาเควสเมื่อ NPC มาถึงหน้าผู้เล่น
    /// รองรับการคลิกหรือกด Spacebar เพื่อเปลี่ยนไปบทสนทนาต่อไปทีละช่วง
    /// </summary>
    public void ShowQuestDialog(NPCController npc, QuestData quest)
    {
        currentNPC = npc;
        currentQuest = quest;
        isDialogActive = true;
        isShowingResponse = false;
        isSimpleDialogue = false;
        onSimpleDialogueFinished = null;
        canAcceptInputTime = Time.time + 0.35f;

        UnlockCursorAndFreezePlayer();

        // รวบรวมหน้าบทสนทนา (Dialogue Pages)
        currentDialoguePages.Clear();
        currentDialoguePageIndex = 0;

        // 1. บทสนทนาเปิด/ทักทาย (greetingDialogue)
        if (!string.IsNullOrEmpty(quest.greetingDialogue))
        {
            string[] paragraphs = quest.greetingDialogue.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in paragraphs)
            {
                string trimmed = p.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !currentDialoguePages.Contains(trimmed))
                {
                    currentDialoguePages.Add(trimmed);
                }
            }
        }

        // 2. รายละเอียดเควสหรือเรื่องราวความเดือดร้อน (questDescription)
        if (!string.IsNullOrEmpty(quest.questDescription))
        {
            string[] descParagraphs = quest.questDescription.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var dp in descParagraphs)
            {
                string trimmed = dp.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !currentDialoguePages.Contains(trimmed))
                {
                    currentDialoguePages.Add(trimmed);
                }
            }
        }

        if (currentDialoguePages.Count == 0)
        {
            currentDialoguePages.Add(quest.questTitle);
        }

        if (questDialogPanel != null)
        {
            questDialogPanel.SetActive(true);

            if (npcNameText != null) npcNameText.text = quest.npcName;
            if (questTitleText != null) questTitleText.text = quest.questTitle;

            if (npcPortraitImage != null)
            {
                if (quest.npcPortrait != null)
                {
                    npcPortraitImage.sprite = quest.npcPortrait;
                    npcPortraitImage.enabled = true;
                }
                else
                {
                    npcPortraitImage.enabled = false;
                }
            }

            if (responsePanel != null) responsePanel.SetActive(false);
            if (declineDialogueText != null)
            {
                declineDialogueText.text = "";
                declineDialogueText.gameObject.SetActive(false);
            }
            if (waitingDialogueText != null)
            {
                waitingDialogueText.text = "";
                waitingDialogueText.gameObject.SetActive(false);
            }

            // แสดงหน้าแรกของบทสนทนา
            ShowDialoguePage(0);
        }

        Debug.Log($"[QuestUIManager] 📜 เปิดบทสนทนาเควส: '{quest.questTitle}' จาก '{quest.npcName}' ({currentDialoguePages.Count} หน้า)");
    }

    /// <summary>
    /// แสดงบทสนทนาตามหน้าที่ระบุ
    /// </summary>
    private void ShowDialoguePage(int pageIndex)
    {
        currentDialoguePageIndex = pageIndex;
        int totalPages = currentDialoguePages.Count;

        // หากเป็นบทสนทนาข้อความทั่วไป (Simple Dialogue - ไม่มีตัวเลือกเควส)
        if (isSimpleDialogue)
        {
            isChoicesPage = false;
            if (questDescriptionText != null)
            {
                questDescriptionText.gameObject.SetActive(true);
                questDescriptionText.text = currentDialoguePages[pageIndex];
            }
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (declineButton != null) declineButton.gameObject.SetActive(false);
            if (inquiryButton != null) inquiryButton.gameObject.SetActive(false);
            if (questRewardText != null) questRewardText.gameObject.SetActive(false);

            if (questRequirementText != null)
            {
                questRequirementText.gameObject.SetActive(true);
                questRequirementText.text = pageIndex < totalPages - 1
                    ? $"<color=#FFD700>กด [คลิก] หรือ [Space] เพื่อไปบทสนทนาต่อไป ▶ ({pageIndex + 1}/{totalPages})</color>"
                    : "<color=#FFD700>กด [คลิก] หรือ [Space] เพื่อจบ</color>";
            }
            return;
        }

        // บทสนทนาเควส (มีปุ่ม ยอมรับ / ปฏิเสธ / สอบถาม)
        // ถ้ายังไม่ถึงหน้าสุดท้าย (หน้าเลือกรับเควส)
        if (pageIndex < totalPages - 1)
        {
            isChoicesPage = false;

            if (questDescriptionText != null)
            {
                questDescriptionText.gameObject.SetActive(true);
                questDescriptionText.text = $"\"{currentDialoguePages[pageIndex]}\"";
            }

            // แสดงปุ่มค้างไว้ตลอดตามต้องการ แต่ยังกดไม่ได้ (interactable = false) จนกว่าจะถึงหน้าสุดท้าย
            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(true);
                acceptButton.interactable = false;
                if (acceptButtonText != null) acceptButtonText.text = "ยอมรับ";
            }
            if (declineButton != null)
            {
                declineButton.gameObject.SetActive(true);
                declineButton.interactable = false;
                if (declineButtonText != null) declineButtonText.text = "ปฏิเสธ";
            }
            if (inquiryButton != null)
            {
                inquiryButton.gameObject.SetActive(true);
                inquiryButton.interactable = false;
                if (inquiryButtonText != null) inquiryButtonText.text = "สอบถามเพิ่มเติม";
            }
            if (questRewardText != null) questRewardText.gameObject.SetActive(false);

            // แสดงคำแนะนำกดคลิกหรือ Spacebar เพื่อไปบทสนทนาต่อไป
            if (questRequirementText != null)
            {
                questRequirementText.gameObject.SetActive(true);
                questRequirementText.text = $"<color=#FFD700>กด [คลิก] หรือ [Space] เพื่อไปบทสนทนาต่อไป ▶ ({pageIndex + 1}/{totalPages})</color>";
            }
        }
        else
        {
            // หน้าสุดท้าย -> แสดงหน้าเลือกเควส พร้อมเปิดให้กดปุ่มได้
            ShowChoicesPage();
        }
    }

    /// <summary>
    /// แสดงหน้าตัวเลือกเควส (สิ่งที่ต้องการ, รางวัล, ปุ่มยอมรับ/ปฏิเสธ/สอบถาม)
    /// </summary>
    private void ShowChoicesPage()
    {
        isChoicesPage = true;

        if (questDescriptionText != null)
        {
            questDescriptionText.gameObject.SetActive(true);
            int lastIndex = currentDialoguePages.Count - 1;
            questDescriptionText.text = currentDialoguePages[lastIndex];
        }

        if (questRequirementText != null)
        {
            if (currentQuest != null && !string.IsNullOrEmpty(currentQuest.requiredItemName))
            {
                questRequirementText.text = $"สิ่งที่ต้องการ: <color=#FFD700>{currentQuest.requiredItemName} x{currentQuest.requiredQuantity}</color>";
                questRequirementText.gameObject.SetActive(true);
            }
            else
            {
                questRequirementText.gameObject.SetActive(false);
            }
        }

        if (questRewardText != null && currentQuest != null)
        {
            string rewardText = !string.IsNullOrEmpty(currentQuest.rewardDescription) 
                ? currentQuest.rewardDescription 
                : (currentQuest.rewardMoney > 0 ? $"เงิน {currentQuest.rewardMoney:N0} เหรียญ" : "");

            if (!string.IsNullOrEmpty(currentQuest.rewardItemName))
            {
                rewardText += string.IsNullOrEmpty(rewardText) ? currentQuest.rewardItemName : $" + {currentQuest.rewardItemName}";
            }

            if (string.IsNullOrEmpty(rewardText))
            {
                rewardText = currentQuest.rewardMoney > 0 ? $"{currentQuest.rewardMoney:N0} เหรียญ" : "-";
            }

            questRewardText.text = $"รางวัลตอบแทน: <color=#00FF7F>{rewardText}</color>";
            questRewardText.gameObject.SetActive(true);
        }

        // เปิดปุ่มกดยอมรับ/ปฏิเสธให้กดได้แล้ว (interactable = true)
        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(true);
            acceptButton.interactable = true;
            if (acceptButtonText != null) acceptButtonText.text = "ยอมรับ [E/Space]";
        }
        if (declineButton != null)
        {
            declineButton.gameObject.SetActive(true);
            declineButton.interactable = true;
            if (declineButtonText != null) declineButtonText.text = "ปฏิเสธ [Q]";
        }

        // ปุ่ม "สอบถามเพิ่มเติม" (เปิดให้กดได้เสมอตามระดับชื่อเสียงรวม)
        if (inquiryButton != null)
        {
            int repLevel = ReputationManager.Instance != null ? ReputationManager.Instance.GetLevel() : 0;
            inquiryButton.gameObject.SetActive(true);
            inquiryButton.interactable = true;

            if (inquiryButtonText != null)
            {
                inquiryButtonText.text = repLevel >= 1
                    ? "💬 สอบถามเพิ่มเติม [F]"
                    : "🔒 สอบถามเพิ่มเติม (ต้องมีระดับ 1)";
            }
        }
    }

    /// <summary>
    /// แสดงบทสนทนาข้อความแบบปิดได้ด้วยคลิกหรือ Spacebar (เช่น ตอนเตือนความจำเควส หรือตอนส่งเควสเสร็จ)
    /// </summary>
    public void ShowSimpleDialogue(NPCController npc, string title, string message, Sprite portrait = null, Action onFinish = null)
    {
        currentNPC = npc;
        currentQuest = null;
        isDialogActive = true;
        isShowingResponse = false;
        isSimpleDialogue = true;
        isChoicesPage = false;
        onSimpleDialogueFinished = onFinish;
        canAcceptInputTime = Time.time + 0.35f;

        UnlockCursorAndFreezePlayer();

        currentDialoguePages.Clear();
        currentDialoguePageIndex = 0;

        string[] paragraphs = message.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (paragraphs != null && paragraphs.Length > 0)
        {
            foreach (var p in paragraphs)
            {
                string trimmed = p.Trim();
                if (!string.IsNullOrEmpty(trimmed)) currentDialoguePages.Add(trimmed);
            }
        }
        else
        {
            currentDialoguePages.Add(message);
        }

        if (questDialogPanel != null)
        {
            questDialogPanel.SetActive(true);

            if (npcNameText != null) npcNameText.text = npc != null && npc.questData != null ? npc.questData.npcName : (npc != null ? npc.name : "NPC");
            if (questTitleText != null) questTitleText.text = title;

            if (npcPortraitImage != null)
            {
                if (portrait != null)
                {
                    npcPortraitImage.sprite = portrait;
                    npcPortraitImage.enabled = true;
                }
                else
                {
                    npcPortraitImage.enabled = false;
                }
            }

            if (responsePanel != null) responsePanel.SetActive(false);
            if (declineDialogueText != null) declineDialogueText.gameObject.SetActive(false);
            if (waitingDialogueText != null) waitingDialogueText.gameObject.SetActive(false);

            ShowDialoguePage(0);
        }
    }

    public void OnAcceptClicked()
    {
        if (!isChoicesPage) return; // ยังไม่ถึงหน้าสุดท้าย ไม่สามารถกดยอมรับได้
        Debug.Log($"[QuestUIManager] 🔘 OnAcceptClicked ถูกกดสำหรับเควส: '{currentQuest?.questTitle}'");
        if (currentNPC == null || currentQuest == null) return;

        // บันทึกเควสเข้าสู่รายการเควสที่กำลังทำ (Active Quests)
        if (!activeQuests.Contains(currentQuest))
        {
            activeQuests.Add(currentQuest);
        }

        // หากเป็นเควสมินิเกม -> ปิด Dialog ทันทีเพื่อเริ่มเล่นมินิเกม
        if (currentQuest.requiredMinigameSequence != null && currentQuest.requiredMinigameSequence.Count > 0)
        {
            NPCController npc = currentNPC;
            CloseDialog();
            npc.OnQuestAccepted();
            return;
        }

        // หากเป็นเควสส่งของทั่วไป
        string waitMsg = !string.IsNullOrEmpty(currentQuest.waitingDialogue)
            ? currentQuest.waitingDialogue
            : "ขอบคุณมากที่รับปากช่วยข้า!";

        // หากมีกล่องข้อความตอนรอ (waitingDialogueText) ในหน้าต่างเควส ให้แสดงบนหน้าต่างนี้ก่อนปิด
        if (waitingDialogueText != null)
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (declineButton != null) declineButton.gameObject.SetActive(false);
            if (inquiryButton != null) inquiryButton.gameObject.SetActive(false);
            if (questRewardText != null) questRewardText.gameObject.SetActive(false);

            // ซ่อนคำอธิบายเควสเดิมเพื่อป้องกันตัวอักษรขึ้นมาแสดงซ้อนทับกัน
            if (questDescriptionText != null && questDescriptionText != waitingDialogueText)
                questDescriptionText.gameObject.SetActive(false);

            if (questTitleText != null)
                questTitleText.text = $"<color=#00FF7F>✅ รับเควสสำเร็จ</color> — {currentQuest.npcName}";

            waitingDialogueText.text = $"\"{waitMsg}\"";
            waitingDialogueText.gameObject.SetActive(true);

            if (questRequirementText != null)
            {
                questRequirementText.gameObject.SetActive(true);
                questRequirementText.text = "<color=#FFD700>กด [คลิก] หรือ [Space] เพื่อจบ</color>";
            }

            isShowingResponse = true;
            isPendingAccept = true;
            canAcceptInputTime = Time.time + 0.25f;

            CancelInvoke(nameof(FinishAcceptAndClose));
            Invoke(nameof(FinishAcceptAndClose), 3.5f);
            return;
        }

        // Fallback: หากไม่มีกล่องข้อความตอนรอ ให้ปิด Dialog แล้วแสดงใน ReadingDialog ของผู้เล่น
        NPCController npc2 = currentNPC;
        CloseDialog();
        npc2.OnQuestAccepted();

        if (InteractionUIManager.Instance != null)
        {
            string title = $"✅ {currentQuest?.npcName ?? "NPC"} — รับงานแล้ว";
            InteractionUIManager.Instance.ShowReadingDialog(title, $"\"{waitMsg}\"");
        }
    }

    public void OnDeclineClicked()
    {
        if (!isChoicesPage) return; // ยังไม่ถึงหน้าสุดท้าย ไม่สามารถกดปฏิเสธได้
        Debug.Log($"[QuestUIManager] 🔘 OnDeclineClicked ถูกกดสำหรับเควส: '{currentQuest?.questTitle}'");
        if (currentNPC == null || currentQuest == null) return;

        responseMessage = !string.IsNullOrEmpty(currentQuest.declineDialogue) 
            ? currentQuest.declineDialogue 
            : "น่าเสียดายจัง... ไม่เป็นไรนะ โอกาสหน้าข้าจะมาใหม่";

        // หากมีกล่องข้อความปฏิเสธ (declineDialogueText) ในหน้าต่างเควส ให้แสดงบนหน้าต่างนี้ก่อนปิด
        if (declineDialogueText != null)
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (declineButton != null) declineButton.gameObject.SetActive(false);
            if (inquiryButton != null) inquiryButton.gameObject.SetActive(false);
            if (questRewardText != null) questRewardText.gameObject.SetActive(false);

            // ซ่อนคำอธิบายเควสเดิมทันที เพื่อไม่ให้ตัวอักษรขึ้นมาซ้อนทับกับข้อความปฏิเสธ
            if (questDescriptionText != null && questDescriptionText != declineDialogueText)
                questDescriptionText.gameObject.SetActive(false);

            if (questTitleText != null)
                questTitleText.text = $"<color=#FF5555>❌ ปฏิเสธเควส</color> — {currentQuest.npcName}";

            declineDialogueText.text = $"\"{responseMessage}\"";
            declineDialogueText.gameObject.SetActive(true);

            if (questRequirementText != null)
            {
                questRequirementText.gameObject.SetActive(true);
                questRequirementText.text = "<color=#FFD700>กด [คลิก] หรือ [Space] เพื่อจบ</color>";
            }

            if (currentNPC != null && currentNPC.declineSound != null)
                AudioSource.PlayClipAtPoint(currentNPC.declineSound, currentNPC.transform.position);

            isShowingResponse = true;
            isPendingDecline = true;
            canAcceptInputTime = Time.time + 0.25f;

            CancelInvoke(nameof(FinishDeclineAndClose));
            Invoke(nameof(FinishDeclineAndClose), 3.5f);
            return;
        }

        // แสดง UI ปฏิเสธเควสก่อนให้ผู้เล่นอ่าน — NPC จะยังไม่เดินจากไปจนกว่าหน้าต่างนี้จะปิด (Fallback)
        ShowResponseAndClose(false);
    }

    /// <summary>
    /// ผู้เล่นกดปุ่ม 'สอบถามเพิ่มเติม' — NPC จะเปิดเผยข้อมูลตามระดับชื่อเสียงในกล่องข้อความด้านล่าง (ไม่เปิดกล่องอ่านข้อมูลตรงกลางจอ)
    /// </summary>
    public void OnInquiryClicked()
    {
        if (!isChoicesPage) return; // ยังไม่ถึงหน้าสุดท้าย ไม่สามารถกดสอบถามได้
        Debug.Log($"[QuestUIManager] 🔘 OnInquiryClicked ถูกกด");
        if (currentNPC == null || currentQuest == null) return;

        int repLevel = ReputationManager.Instance != null ? ReputationManager.Instance.GetLevel() : 0;

        // ดึงบทสนทนาตามระดับชื่อเสียงของผู้เล่น
        string inquiry = "";
        if (currentQuest.inquiryDialogues != null && currentQuest.inquiryDialogues.Length > repLevel)
        {
            inquiry = currentQuest.inquiryDialogues[repLevel];
        }

        if (string.IsNullOrEmpty(inquiry))
        {
            inquiry = repLevel >= 1 
                ? "ความจริงมีเรื่องราวที่ข้าอยากบอกท่าน..." 
                : "ข้ายังไม่รู้จักท่านดีพอ ขออภัยที่ยังบอกอะไรไม่ได้ (ต้องมีชื่อเสียงระดับ 1 ขึ้นไป)";
        }

        // แสดงผลในกล่องข้อความบทสนทนาด้านล่าง (questDescriptionText)
        if (questDescriptionText != null)
        {
            questDescriptionText.gameObject.SetActive(true);
            questDescriptionText.text = repLevel >= 1
                ? $"\"{inquiry}\""
                : $"\"{inquiry}\"\n<size=85%><color=#FFAAAA>(ต้องการชื่อเสียงระดับ 1 ขึ้นไปเพื่อรับข้อมูลเพิ่มเติม)</color></size>";
        }

        // ซ่อนหน้าต่างอ่านข้อมูลตรงกลางจอ (ReadingDialog) หากมีเปิดอยู่
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HideReadingDialog();
        }
        if (playerInteraction != null && playerInteraction.IsReading())
        {
            playerInteraction.CloseReading();
        }

        UnlockCursorAndFreezePlayer();

        Debug.Log($"[QuestUIManager] 💬 สอบถาม '{currentQuest.npcName}' (ชื่อเสียงระดับ {repLevel}): '{inquiry}'");
    }

    private void ShowResponseAndClose(bool accepted)
    {
        isShowingResponse = true;

        // ───── กรณีปฏิเสธเควส: ปิด Quest Dialog ทันที แล้วแสดงคำพูด NPC ใน ReadingDialog ของผู้เล่น ─────
        if (!accepted)
        {
            // เล่นเสียงปฏิเสธ
            if (currentNPC != null && currentNPC.declineSound != null)
                AudioSource.PlayClipAtPoint(currentNPC.declineSound, currentNPC.transform.position);

            // บันทึก NPC ก่อน CloseDialog() เคลียร์ค่า
            NPCController declinedNPC = currentNPC;
            QuestData declinedQuest = currentQuest;

            // ปิด Quest Dialog Panel ทันที (isShowingResponse ยังไม่ถูก reset = false จะถูก set ใน CloseDialog)
            isShowingResponse = false; // ตั้ง false ก่อนเพื่อไม่ให้ CloseDialog ส่ง wasDeclined
            CloseDialog();

            // แสดงคำพูด NPC ใน ReadingDialog (UI ที่ผู้เล่นสร้างเอง)
            if (InteractionUIManager.Instance != null)
            {
                string title = $"❌ {declinedQuest?.npcName ?? "NPC"} — ปฏิเสธงาน";
                string body  = $"\"{responseMessage}\"\n\n<size=85%><color=#FFAAAA>(NPC กำลังจะเดินออกจากตำหนัก...)</color></size>";
                string guide = "จะปิดอัตโนมัติใน 4 วิ หรือกด <color=#FFD700>[E]</color> / <color=#FFD700>[Esc]</color> เพื่อปิด";
                InteractionUIManager.Instance.ShowReadingDialog(title, body, guide);

                // ให้ PlayerInteraction รู้ว่ากำลังอ่านอยู่ เพื่อให้กด E/Esc ปิดได้ และปิดตัวเองอัตโนมัติหลังผ่านไป 4 วินาที
                if (playerInteraction != null)
                {
                    playerInteraction.OpenReadingExternal(title, body, 4.0f);
                }
            }

            // Fallback เผื่อไม่มี playerInteraction ให้ปิด UI อัตโนมัติหลังผ่านไป 4 วินาที
            CancelInvoke(nameof(AutoCloseDeclineReading));
            Invoke(nameof(AutoCloseDeclineReading), 4.0f);

            // NPC เดินออกจากตำหนักทันที
            if (declinedNPC != null)
            {
                declinedNPC.OnQuestDeclined();
            }
            return;
        }

        // ───── กรณีรับเควส: แสดงการตอบกลับใน Quest Dialog เดิม ─────
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (declineButton != null) declineButton.gameObject.SetActive(false);
        if (inquiryButton != null) inquiryButton.gameObject.SetActive(false);
        if (questRequirementText != null) questRequirementText.gameObject.SetActive(false);
        if (questRewardText != null) questRewardText.gameObject.SetActive(false);

        if (questTitleText != null)
            questTitleText.text = $"<color=#00FF7F>✅ รับเควสสำเร็จ</color> — {currentQuest.npcName}";

        if (questDescriptionText != null)
            questDescriptionText.text = $"<b>{currentQuest.npcName}</b> กล่าวว่า:\n\n<size=115%><color=#FFD700>\"{responseMessage}\"</color></size>\n\n<size=85%><color=#A0E6FF>(บันทึกภารกิจลงสมุดเควสแล้ว...)</color></size>";

        if (responsePanel != null)
        {
            RectTransform rt = responsePanel.GetComponent<RectTransform>();
            if (rt != null && (rt.localScale.x < 0.5f || rt.anchoredPosition.sqrMagnitude > 10000f))
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
            }
            if (responseDialogueText != null)
                responseDialogueText.text = $"\"{responseMessage}\"";
            responsePanel.SetActive(true);
        }

        if (InteractionUIManager.Instance != null)
            InteractionUIManager.Instance.ShowNotification($"<color=#00FF7F>[รับเควสแล้ว]</color> {currentQuest.npcName}: \"{responseMessage}\"", 3.0f);

        CancelInvoke(nameof(CloseDialog));
        Invoke(nameof(CloseDialog), 2.2f);
    }

    public void CompleteQuest(QuestData quest)
    {
        if (quest == null) return;
        quest.isCompleted = true;
        activeQuests.Remove(quest);
        UpdateQuestTrackerCanvas();
        Debug.Log($"[QuestUIManager] 🏆 ส่งมอบเควส '{quest.questTitle}' สำเร็จ!");
    }

    public void RegisterMinigameFailForQuest(QuestData quest)
    {
        if (quest == null) return;
        quest.currentMinigameFails++;

        // ทำมินิเกมแพ้เพียงครั้งเดียว -> ล้มเหลวทั้งเควสทันที
        FailQuest(quest);
    }

    public void FailQuest(QuestData quest)
    {
        if (quest == null) return;
        
        quest.isCompleted = false;
        if (activeQuests.Contains(quest))
        {
            activeQuests.Remove(quest);
        }
        UpdateQuestTrackerCanvas();

        Debug.Log($"[QuestUIManager] ❌ เควส '{quest.questTitle}' ล้มเหลวจากการทำมินิเกมพลาด!");

        if (GhostCurseManager.Instance != null)
        {
            GhostCurseManager.Instance.AttachGhost($"ทำเควส {quest.npcName} ล้มเหลว");
        }

        // ลดค่าความดีเมื่อทำเควสพลาด (อาจลดจนเหลือ 0 นำไปสู่ฉากโดนกลืนกิน)
        if (KarmaManager.Instance != null)
        {
            int penalty = quest.karmaReward > 0 ? quest.karmaReward : 20;
            KarmaManager.Instance.ApplyKarma(-penalty, $"ทำเควสพลาด: {quest.questTitle}");
        }

        string failDialogue = !string.IsNullOrEmpty(quest.failDialogue) 
            ? quest.failDialogue 
            : "ไม่เห็นเก่งเลยนี่หว่า... ข้าไปหาคนอื่นดีกว่า!";

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                $"<color=#FF3333>[เควสล้มเหลว]</color> {quest.npcName}: \"{failDialogue}\"", 4.5f);
        }

        // ค้นหา NPC แล้วสั่งให้เดินออกจากตำหนักทันที
        NPCController targetNPC = null;
        NPCController[] npcs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc != null && npc.questData != null && npc.questData.questId == quest.questId)
            {
                targetNPC = npc;
                break;
            }
        }

        if (targetNPC == null && HallManager.Instance != null)
        {
            targetNPC = HallManager.Instance.currentActiveNPC;
        }

        if (targetNPC != null)
        {
            targetNPC.StartLeaving();
        }
    }

    private void FinishDeclineAndClose()
    {
        CancelInvoke(nameof(FinishDeclineAndClose));
        isPendingDecline = false;
        NPCController declinedNPC = currentNPC;
        CloseDialog();
        if (declinedNPC != null)
        {
            declinedNPC.OnQuestDeclined();
        }
    }

    private void FinishAcceptAndClose()
    {
        CancelInvoke(nameof(FinishAcceptAndClose));
        isPendingAccept = false;
        NPCController acceptedNPC = currentNPC;
        CloseDialog();
        if (acceptedNPC != null)
        {
            acceptedNPC.OnQuestAccepted();
        }
    }

    public void CloseDialog()
    {
        CancelInvoke(nameof(CloseDialog));
        CancelInvoke(nameof(AutoCloseDeclineReading));
        CancelInvoke(nameof(FinishDeclineAndClose));
        CancelInvoke(nameof(FinishAcceptAndClose));

        bool wasDeclined = isPendingDecline || (!isShowingResponse
            ? false
            : (currentQuest != null && !currentQuest.isAccepted));
        NPCController departingNPC = currentNPC;

        isDialogActive = false;
        isShowingResponse = false;
        isPendingDecline = false;
        isPendingAccept = false;
        isChoicesPage = false;
        isSimpleDialogue = false;
        onSimpleDialogueFinished = null;
        currentDialoguePageIndex = 0;
        currentDialoguePages.Clear();

        if (declineDialogueText != null)
        {
            declineDialogueText.text = "";
            declineDialogueText.gameObject.SetActive(false);
        }
        if (waitingDialogueText != null)
        {
            waitingDialogueText.text = "";
            waitingDialogueText.gameObject.SetActive(false);
        }

        if (questDialogPanel != null) questDialogPanel.SetActive(false);
        if (responsePanel != null) responsePanel.SetActive(false);

        // คืนค่าปุ่มและข้อความให้กลับมาเปิดสำหรับครั้งถัดไป
        if (questDescriptionText != null) questDescriptionText.gameObject.SetActive(true);
        if (questRequirementText != null) questRequirementText.gameObject.SetActive(true);
        if (questRewardText != null) questRewardText.gameObject.SetActive(true);

        if (acceptButton != null) acceptButton.gameObject.SetActive(true);
        if (declineButton != null) declineButton.gameObject.SetActive(true);
        if (inquiryButton != null) inquiryButton.gameObject.SetActive(true);

        LockCursorAndUnfreezePlayer();

        // หากเป็นการปฏิเสธเควส หลังจากหน้าต่าง UI ปิดลงแล้ว NPC จึงจะเริ่มเดินจากไป
        if (wasDeclined && departingNPC != null)
        {
            departingNPC.OnQuestDeclined();
        }
    }

    private void AutoCloseDeclineReading()
    {
        if (playerInteraction != null && playerInteraction.IsReading())
        {
            playerInteraction.CloseReading();
        }
        else if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HideReadingDialog();
        }
    }

    private void UnlockCursorAndFreezePlayer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    private void LockCursorAndUnfreezePlayer()
    {
        if (playerInteraction != null && (playerInteraction.IsInventoryOpen() || playerInteraction.IsReading()))
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    public bool IsDialogActive() => isDialogActive;

    // ==========================================
    // อัปเดตข้อมูลบน Canvas Quest Tracker
    // ==========================================
    private void UpdateQuestTrackerCanvas()
    {
        if (questTrackerPanel == null) return;

        bool hasActiveQuests = activeQuests != null && activeQuests.Count > 0;
        questTrackerPanel.SetActive(hasActiveQuests && showTrackerOnHUD);

        if (!hasActiveQuests) return;

        if (trackerHeaderText != null)
        {
            trackerHeaderText.text = "📌 ภารกิจที่กำลังทำ";
        }

        if (trackerContentText != null)
        {
            string trackerText = "";
            for (int i = 0; i < activeQuests.Count; i++)
            {
                var q = activeQuests[i];
                string titleText = !string.IsNullOrEmpty(q.problemType) ? q.problemType : q.questTitle;
                trackerText += $"<b><color=#FFD700>• ปัญหา: {titleText}</color></b> ({q.npcName})\n";

                if (!string.IsNullOrEmpty(q.requiredItemName))
                {
                    int currentCount = GetPlayerItemCount(q.requiredItemName);
                    bool isReady = currentCount >= q.requiredQuantity;
                    string countColor = isReady ? "#00FF7F" : "#FF6347";
                    string statusTag = isReady ? " <color=#00FF7F>[พร้อมส่ง]</color>" : "";

                    trackerText += $"   - หา: {q.requiredItemName} (<color={countColor}>{currentCount}/{q.requiredQuantity}</color>){statusTag}\n";
                }
                else if (q.requiredMinigameSequence != null && q.requiredMinigameSequence.Count > 0)
                {
                    int compCount = q.completedMinigameSequence != null ? q.completedMinigameSequence.Count : 0;
                    int totalCount = q.requiredMinigameSequence.Count;
                    bool isReady = q.isTaskCompleted;
                    string statusTag = isReady ? " <color=#00FF7F>[ทำครบแล้ว! พร้อมส่ง]</color>" : $" ({compCount}/{totalCount})";

                    trackerText += $"   - ลำดับพิธีกรรม{statusTag}:\n";
                    for (int j = 0; j < totalCount; j++)
                    {
                        var mType = q.requiredMinigameSequence[j];
                        string mName = QuestData.GetMinigameNameThai(mType);
                        if (j < compCount)
                        {
                            trackerText += $"     <color=#00FF7F>✓ {j + 1}. {mName} (สำเร็จ)</color>\n";
                        }
                        else if (j == compCount)
                        {
                            trackerText += $"     <color=#FFD700>👉 {j + 1}. {mName} (กำลังทำ)</color>\n";
                        }
                        else
                        {
                            trackerText += $"     <color=#778899>🔒 {j + 1}. {mName}</color>\n";
                        }
                    }
                }
                else
                {
                    trackerText += $"   - {q.questDescription}\n";
                }

                if (!string.IsNullOrEmpty(q.rewardDescription))
                {
                    trackerText += $"   <color=#A0E6FF>🎁 รางวัล: {q.rewardDescription}</color>\n";
                }

                if (i < activeQuests.Count - 1) trackerText += "\n";
            }

            trackerContentText.text = trackerText;
        }
    }

    /// <summary>
    /// นับจำนวนไอเทมที่ตรงกับชื่อที่ผู้เล่นถือหรือมีในกระเป๋า
    /// </summary>
    public int GetPlayerItemCount(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return 0;
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        if (playerInteraction == null) return 0;

        int count = 0;
        if (playerInteraction.inventoryItems != null)
        {
            foreach (var item in playerInteraction.inventoryItems)
            {
                if (item != null && (item.itemName.Equals(itemName, StringComparison.OrdinalIgnoreCase) 
                    || item.itemName.Contains(itemName, StringComparison.OrdinalIgnoreCase) 
                    || itemName.Contains(item.itemName, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                }
            }
        }

        // ตรวจสอบไอเทมที่ถืออยู่ที่มือ
        if (playerInteraction.currentlyHeldItem != null)
        {
            var held = playerInteraction.currentlyHeldItem;
            if (held.itemName.Equals(itemName, StringComparison.OrdinalIgnoreCase) 
                || held.itemName.Contains(itemName, StringComparison.OrdinalIgnoreCase) 
                || itemName.Contains(held.itemName, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    // ==========================================
    // OnGUI Fallback สำหรับแสดงผลอัตโนมัติ (ทั้งหน้าต่างและ HUD ซ้ายจอ)
    // ==========================================
    void OnGUI()
    {
        if (!enableBuiltInFallbackUI) return;

        // ----------------------------------------------------
        // 1. แถบแสดงรายการเควสทางซ้ายจอ (Quest Tracker HUD)
        // ----------------------------------------------------
        if (showTrackerOnHUD && activeQuests != null && activeQuests.Count > 0 && (questTrackerPanel == null || !questTrackerPanel.activeInHierarchy))
        {
            float trackerW = 270f;
            float trackerX = 20f;
            float trackerY = 70f; // แสดงถัดลงมาจากปุ่มเปิดตำหนัก
            float headerH = 28f;
            float itemH = 65f;
            float totalH = headerH + (activeQuests.Count * itemH) + 15f;

            // กรอบพื้นหลังโปร่งแสง
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            GUI.Box(new Rect(trackerX, trackerY, trackerW, totalH), GUIContent.none);
            GUI.color = Color.white;

            // หัวข้อภารกิจ
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 13;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(trackerX + 10, trackerY + 6, trackerW - 20, 22), $"📌 ภารกิจที่กำลังทำ ({activeQuests.Count})", headerStyle);

            GUIStyle taskTitleStyle = new GUIStyle(GUI.skin.label);
            taskTitleStyle.fontSize = 12;
            taskTitleStyle.fontStyle = FontStyle.Bold;
            taskTitleStyle.normal.textColor = Color.white;

            GUIStyle taskDetailStyle = new GUIStyle(GUI.skin.label);
            taskDetailStyle.fontSize = 11;
            taskDetailStyle.wordWrap = true;
            taskDetailStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            float currentY = trackerY + headerH + 4;
            for (int i = 0; i < activeQuests.Count; i++)
            {
                var q = activeQuests[i];

                // ชื่องาน และผู้มอบหมาย
                string titleText = !string.IsNullOrEmpty(q.problemType) ? q.problemType : q.questTitle;
                GUI.Label(new Rect(trackerX + 12, currentY, trackerW - 24, 18), $"• ปัญหา: {titleText} ({q.npcName})", taskTitleStyle);

                // สิ่งที่ต้องทำ และจำนวนไอเทม
                string detailStr = "";
                if (!string.IsNullOrEmpty(q.requiredItemName))
                {
                    int count = GetPlayerItemCount(q.requiredItemName);
                    bool ready = count >= q.requiredQuantity;
                    string readyTag = ready ? " <color=#00FF7F>[พร้อมส่ง]</color>" : "";
                    detailStr = $"   - หา: {q.requiredItemName} ({count}/{q.requiredQuantity}){readyTag}";
                }
                else if (q.requiredMinigameSequence != null && q.requiredMinigameSequence.Count > 0)
                {
                    int compCount = q.completedMinigameSequence != null ? q.completedMinigameSequence.Count : 0;
                    int totalCount = q.requiredMinigameSequence.Count;
                    bool isReady = q.isTaskCompleted;
                    string statusTag = isReady ? " <color=#00FF7F>[พร้อมส่ง]</color>" : $" ({compCount}/{totalCount})";
                    
                    detailStr = $"   - พิธี{statusTag}: ";
                    for (int j = 0; j < totalCount; j++)
                    {
                        var mType = q.requiredMinigameSequence[j];
                        string mName = QuestData.GetMinigameNameThai(mType);
                        if (j < compCount) detailStr += $"<color=#00FF7F>✓{mName}</color> ";
                        else if (j == compCount) detailStr += $"<color=#FFD700>👉{mName}</color> ";
                        else detailStr += $"<color=#778899>🔒{mName}</color> ";
                    }
                }
                else
                {
                    detailStr = $"   - {q.questDescription}";
                }

                GUI.Label(new Rect(trackerX + 12, currentY + 18, trackerW - 24, 38), detailStr, taskDetailStyle);
                currentY += itemH;
            }
        }

        // ----------------------------------------------------
        // 2. หน้าต่าง Dialog เสนอเควส (Quest Dialog Modal)
        // ----------------------------------------------------
        if (!isDialogActive) return;
        if (questDialogPanel != null && questDialogPanel.activeInHierarchy) return;

        float boxWidth = Mathf.Min(580f, Screen.width * 0.9f);
        float boxHeight = Mathf.Min(380f, Screen.height * 0.75f);
        float posX = (Screen.width - boxWidth) / 2f;
        float posY = (Screen.height - boxHeight) / 2f;

        GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), GUIContent.none);

        GUIStyle modalHeaderStyle = new GUIStyle(GUI.skin.label);
        modalHeaderStyle.fontSize = 20;
        modalHeaderStyle.fontStyle = FontStyle.Bold;
        modalHeaderStyle.alignment = TextAnchor.UpperCenter;
        modalHeaderStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

        string title = currentQuest != null
            ? $"📜 {currentQuest.questTitle} ({currentQuest.npcName})"
            : "เควส";
        GUI.Label(new Rect(posX + 20, posY + 15, boxWidth - 40, 30), title, modalHeaderStyle);

        if (isShowingResponse)
        {
            bool isAccepted = currentQuest != null && currentQuest.isAccepted;
            GUIStyle respHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            respHeader.normal.textColor = isAccepted ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            string header = isAccepted ? "✅ รับเควสสำเร็จ" : "❌ ปฏิเสธเควส";
            GUI.Label(new Rect(posX + 20, posY + 15, boxWidth - 40, 28), $"{header} ({currentQuest?.npcName})", respHeader);

            GUIStyle respStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            respStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

            GUI.Label(new Rect(posX + 30, posY + 70, boxWidth - 60, 150), $"\"{responseMessage}\"", respStyle);

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.LowerCenter
            };
            hintStyle.normal.textColor = Color.gray;
            string hint = isAccepted ? "(กำลังเริ่มบันทึกภารกิจ... กด Esc หรือ Space เพื่อปิด)" : "(กำลังปิดหน้าต่างและ NPC จะเดินจากไป... กด Esc หรือ Space เพื่อปิด)";
            GUI.Label(new Rect(posX + 20, posY + boxHeight - 40, boxWidth - 40, 25), hint, hintStyle);
            return;
        }

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 14;
        bodyStyle.wordWrap = true;
        bodyStyle.normal.textColor = Color.white;

        string contentText = "";
        if (currentQuest != null)
        {
            if (!string.IsNullOrEmpty(currentQuest.greetingDialogue))
            {
                contentText += $"<i>\"{currentQuest.greetingDialogue}\"</i>\n\n";
            }
            contentText += currentQuest.questDescription + "\n\n";

            if (!string.IsNullOrEmpty(currentQuest.requiredItemName))
            {
                contentText += $"<color=#FFD700>📌 สิ่งที่ต้องการ:</color> {currentQuest.requiredItemName} (x{currentQuest.requiredQuantity})\n";
            }
            contentText += $"<color=#00FF7F>🎁 รางวัลตอบแทน:</color> {currentQuest.rewardDescription}";
        }

        GUI.Label(new Rect(posX + 30, posY + 55, boxWidth - 60, boxHeight - 140), contentText, bodyStyle);

        // ปุ่ม [รับเควส]
        float btnWidth = (boxWidth - 70f) / 2f;
        float btnHeight = 42f;
        float btnY = posY + boxHeight - 60f;

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUI.Button(new Rect(posX + 25, btnY, btnWidth, btnHeight), "✅ รับเควส [E]"))
        {
            OnAcceptClicked();
        }

        // ปุ่ม [ปฏิเสธ]
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUI.Button(new Rect(posX + 35 + btnWidth, btnY, btnWidth, btnHeight), "❌ ปฏิเสธเควส [Q]"))
        {
            OnDeclineClicked();
        }

        GUI.backgroundColor = Color.white;
    }
}
