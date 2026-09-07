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
    public TextMeshProUGUI acceptButtonText;
    public TextMeshProUGUI declineButtonText;

    [Header("Response Dialogue Panel (เมื่อกดรับ/ปฏิเสธ)")]
    public GameObject responsePanel;
    public TextMeshProUGUI responseDialogueText;

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
    private string responseMessage = "";
    private float canAcceptInputTime = 0f;
    private PlayerController playerController;
    private PlayerInteraction playerInteraction;

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

        UpdateQuestTrackerCanvas();
    }

    void Update()
    {
        // อัปเดตข้อมูลบน Canvas HUD เสมอ
        UpdateQuestTrackerCanvas();

        if (!isDialogActive) return;
        if (Time.time < canAcceptInputTime) return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // หากกำลังแสดงหน้าต่างตอบกลับ (Response UI) ผู้เล่นสามารถกดปุ่มเพื่อปิดข้ามได้ทันที
            if (isShowingResponse)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame 
                    || keyboard.enterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame 
                    || keyboard.qKey.wasPressedThisFrame)
                {
                    CancelInvoke(nameof(CloseDialog));
                    CloseDialog();
                }
                return;
            }

            // ปุ่มลัด: กด E หรือ 1 เพื่อรับเควส
            if (keyboard.eKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame)
            {
                OnAcceptClicked();
            }
            // ปุ่มลัด: กด Q หรือ 2 หรือ Escape เพื่อปฏิเสธเควส
            else if (keyboard.qKey.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                OnDeclineClicked();
            }
        }
    }

    /// <summary>
    /// แสดงหน้าต่างเควสเมื่อ NPC มาถึงหน้าผู้เล่น
    /// </summary>
    public void ShowQuestDialog(NPCController npc, QuestData quest)
    {
        currentNPC = npc;
        currentQuest = quest;
        isDialogActive = true;
        isShowingResponse = false;
        canAcceptInputTime = Time.time + 0.35f;

        UnlockCursorAndFreezePlayer();

        // อัปเดตข้อมูลบน Canvas UI (ถ้ามี)
        if (questDialogPanel != null)
        {
            questDialogPanel.SetActive(true);

            if (npcNameText != null) npcNameText.text = quest.npcName;
            if (questTitleText != null) questTitleText.text = quest.questTitle;
            if (questDescriptionText != null)
            {
                string desc = !string.IsNullOrEmpty(quest.greetingDialogue) 
                    ? $"\"{quest.greetingDialogue}\"\n\n{quest.questDescription}" 
                    : quest.questDescription;
                questDescriptionText.text = desc;
            }

            if (questRequirementText != null)
            {
                if (!string.IsNullOrEmpty(quest.requiredItemName))
                {
                    questRequirementText.text = $"สิ่งที่ต้องการ: <color=#FFD700>{quest.requiredItemName} x{quest.requiredQuantity}</color>";
                    questRequirementText.gameObject.SetActive(true);
                }
                else
                {
                    questRequirementText.gameObject.SetActive(false);
                }
            }

            if (questRewardText != null)
            {
                questRewardText.text = $"รางวัลตอบแทน: <color=#00FF7F>{quest.rewardDescription}</color>";
                questRewardText.gameObject.SetActive(true);
            }

            // เปิดปุ่มกดยอมรับ/ปฏิเสธ
            if (acceptButton != null) acceptButton.gameObject.SetActive(true);
            if (declineButton != null) declineButton.gameObject.SetActive(true);

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
        }

        Debug.Log($"[QuestUIManager] 📜 เปิดหน้าต่างเควส: '{quest.questTitle}' จาก '{quest.npcName}'");
    }

    public void OnAcceptClicked()
    {
        if (currentNPC == null || currentQuest == null) return;

        // บันทึกเควสเข้าสู่รายการเควสที่กำลังทำ (Active Quests)
        if (!activeQuests.Contains(currentQuest))
        {
            activeQuests.Add(currentQuest);
        }

        // หากเป็นเควสมินิเกม (เช่น ท่องคาถา Rhythm Game) -> ปิด Dialog ทันทีเพื่อเริ่มเล่นมินิเกม
        if (currentQuest.minigameType != MinigameType.None)
        {
            NPCController npc = currentNPC;
            CloseDialog();
            npc.OnQuestAccepted();
            return;
        }

        // หากเป็นเควสส่งของทั่วไป -> แสดงข้อความตอบรับบน UI ก่อนปิด
        responseMessage = !string.IsNullOrEmpty(currentQuest.acceptDialogue) 
            ? currentQuest.acceptDialogue 
            : "ขอบคุณมากที่รับปากช่วยข้า!";

        ShowResponseAndClose(true);
        currentNPC.OnQuestAccepted();
    }

    public void OnDeclineClicked()
    {
        if (currentNPC == null || currentQuest == null) return;

        responseMessage = !string.IsNullOrEmpty(currentQuest.declineDialogue) 
            ? currentQuest.declineDialogue 
            : "น่าเสียดายจัง... ไม่เป็นไรนะ โอกาสหน้าข้าจะมาใหม่";

        // แสดง UI ปฏิเสธเควสก่อนให้ผู้เล่นอ่าน — NPC จะยังไม่เดินจากไปจนกว่าหน้าต่างนี้จะปิด
        ShowResponseAndClose(false);
    }

    private void ShowResponseAndClose(bool accepted)
    {
        isShowingResponse = true;

        // ซ่อนปุ่มเลือกเควส เพื่อให้เห็นเฉพาะข้อความตอบกลับของ NPC
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (declineButton != null) declineButton.gameObject.SetActive(false);
        if (questRequirementText != null) questRequirementText.gameObject.SetActive(false);
        if (questRewardText != null) questRewardText.gameObject.SetActive(false);

        // อัปเดตหัวข้อและคำพูดของ NPC บน Quest Dialog Panel ให้ชัดเจน
        if (questTitleText != null)
        {
            questTitleText.text = accepted 
                ? $"<color=#00FF7F>✅ รับเควสสำเร็จ</color> — {currentQuest.npcName}" 
                : $"<color=#FF6347>❌ ปฏิเสธเควส</color> — {currentQuest.npcName}";
        }

        if (questDescriptionText != null)
        {
            string hint = accepted
                ? "<size=85%><color=#A0E6FF>(บันทึกภารกิจลงสมุดเควสแล้ว...)</color></size>"
                : "<size=85%><color=#FFAAAA>(NPC รับทราบและกำลังจะเดินออกจากตำหนัก...)</color></size>";

            questDescriptionText.text = $"<b>{currentQuest.npcName}</b> กล่าวว่า:\n\n<size=115%><color=#FFD700>\"{responseMessage}\"</color></size>\n\n{hint}";
        }

        // จัดการ Response Panel (หากมีใน Canvas)
        if (responsePanel != null)
        {
            RectTransform rt = responsePanel.GetComponent<RectTransform>();
            if (rt != null && (rt.localScale.x < 0.5f || rt.anchoredPosition.sqrMagnitude > 10000f))
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
            }

            if (responseDialogueText != null)
            {
                responseDialogueText.text = $"\"{responseMessage}\"";
            }
            responsePanel.SetActive(true);
        }

        if (InteractionUIManager.Instance != null)
        {
            string statusTag = accepted ? "<color=#00FF7F>[รับเควสแล้ว]</color>" : "<color=#FF6347>[ปฏิเสธเควส]</color>";
            InteractionUIManager.Instance.ShowNotification($"{statusTag} {currentQuest.npcName}: \"{responseMessage}\"", 3.0f);
        }

        // เล่นเสียงปฏิเสธทันที (ถ้าปฏิเสธ)
        if (!accepted && currentNPC != null && currentNPC.declineSound != null)
        {
            AudioSource.PlayClipAtPoint(currentNPC.declineSound, currentNPC.transform.position);
        }

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

    public void CloseDialog()
    {
        CancelInvoke(nameof(CloseDialog));

        bool wasDeclined = isShowingResponse && (currentQuest != null && !currentQuest.isAccepted);
        NPCController departingNPC = currentNPC;

        isDialogActive = false;
        isShowingResponse = false;

        if (questDialogPanel != null) questDialogPanel.SetActive(false);
        if (responsePanel != null) responsePanel.SetActive(false);

        // คืนค่าปุ่มให้กลับมาเปิดสำหรับครั้งถัดไป
        if (acceptButton != null) acceptButton.gameObject.SetActive(true);
        if (declineButton != null) declineButton.gameObject.SetActive(true);

        LockCursorAndUnfreezePlayer();

        // หากเป็นการปฏิเสธเควส หลังจากหน้าต่าง UI ปิดลงแล้ว NPC จึงจะเริ่มเดินจากไป
        if (wasDeclined && departingNPC != null)
        {
            departingNPC.OnQuestDeclined();
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
                trackerText += $"<b><color=#FFD700>• {q.questTitle}</color></b> ({q.npcName})\n";

                if (!string.IsNullOrEmpty(q.requiredItemName))
                {
                    int currentCount = GetPlayerItemCount(q.requiredItemName);
                    bool isReady = currentCount >= q.requiredQuantity;
                    string countColor = isReady ? "#00FF7F" : "#FF6347";
                    string statusTag = isReady ? " <color=#00FF7F>[พร้อมส่ง]</color>" : "";

                    trackerText += $"   - หา: {q.requiredItemName} (<color={countColor}>{currentCount}/{q.requiredQuantity}</color>){statusTag}\n";
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
                GUI.Label(new Rect(trackerX + 12, currentY, trackerW - 24, 18), $"• {q.questTitle} ({q.npcName})", taskTitleStyle);

                // สิ่งที่ต้องทำ และจำนวนไอเทม
                string detailStr = "";
                if (!string.IsNullOrEmpty(q.requiredItemName))
                {
                    int count = GetPlayerItemCount(q.requiredItemName);
                    bool ready = count >= q.requiredQuantity;
                    string readyTag = ready ? " <color=#00FF7F>[พร้อมส่ง]</color>" : "";
                    detailStr = $"   - หา: {q.requiredItemName} ({count}/{q.requiredQuantity}){readyTag}";
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
