using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("1. Crosshair (เป้าเล็งกลางจอ)")]
    [Tooltip("Image ของเป้าเล็ง")]
    public Image crosshairImage;
    public Color normalCrosshairColor = new Color(1f, 1f, 1f, 0.6f);
    public Color interactCrosshairColor = new Color(0.2f, 1f, 0.4f, 1f);

    [Header("2. Interaction Prompt (กล่องคำแนะนำตอนมองไอเทม)")]
    [Tooltip("Panel หรือ GameObject ของกล่อง Prompt")]
    public GameObject promptPanel;
    [Tooltip("ข้อความแสดงชื่อไอเทม")]
    public TextMeshProUGUI promptItemNameText;
    [Tooltip("ข้อความแสดงปุ่มคำสั่ง เช่น [E] อ่าน [F] เก็บ")]
    public TextMeshProUGUI promptActionText;

    [Header("3. Reading Dialog (หน้าต่างอ่านข้อมูล/Pop-up)")]
    [Tooltip("Panel หรือ GameObject ของหน้าต่างอ่านข้อมูล")]
    public GameObject readingPanel;
    [Tooltip("ข้อความหัวข้อ")]
    public TextMeshProUGUI readingTitleText;
    [Tooltip("ข้อความเนื้อหา")]
    public TextMeshProUGUI readingDescriptionText;
    [Tooltip("ข้อความคำแนะนำการปิด เช่น กด [E] หรือ [Esc] เพื่อปิด")]
    public TextMeshProUGUI readingCloseGuideText;
    [Tooltip("ปุ่มกดสำหรับปิด (Optional)")]
    public Button closeButton;

    [Header("4. Notification (แถบแจ้งเตือนเมื่อเก็บของ)")]
    [Tooltip("Panel หรือ GameObject ของแถบแจ้งเตือน")]
    public GameObject notificationPanel;
    [Tooltip("ข้อความแจ้งเตือน")]
    public TextMeshProUGUI notificationText;

    [Header("5. Inventory (หน้าต่างช่องเก็บของ - Tab)")]
    [Tooltip("Panel หลักของช่องเก็บของ")]
    public GameObject inventoryPanel;
    [Tooltip("Transform ที่ใช้เป็นที่วาง Slot ไอเทม (เช่น Content ใน ScrollView หรือ Grid)")]
    public Transform inventoryGridContainer;
    [Tooltip("Prefab ของปุ่ม Slot ไอเทม (ที่มีคอมโพเนนต์ InventorySlotUI)")]
    public GameObject inventorySlotPrefab;
    [Tooltip("ข้อความเมื่อไม่มีไอเทม")]
    public TextMeshProUGUI emptyInventoryText;

    [Header("Inventory Item Detail (ส่วนแสดงรายละเอียดไอเทม)")]
    public GameObject itemDetailPanel;
    public Image detailIconImage;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;
    public Button equipButton;
    public TextMeshProUGUI equipButtonText;
    public Button useButton;
    public TextMeshProUGUI useButtonText;
    public Button dropButton;
    public TextMeshProUGUI dropButtonText;
    public Button closeInventoryButton;

    [Header("6. Held Item HUD (แถบแสดงสถานะไอเทมที่กำลังถืออยู่)")]
    [Tooltip("Panel แถบแสดงสถานะไอเทมที่ถือที่มือ")]
    public GameObject heldItemHUDPanel;
    public Image heldItemIcon;
    public TextMeshProUGUI heldItemNameText;
    public TextMeshProUGUI heldItemGuideText;

    private float notificationTimer = 0f;
    private PlayerInteraction playerInteraction;
    private int selectedItemIndex = -1;
    private List<InventorySlotUI> spawnedSlots = new List<InventorySlotUI>();

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
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        // ซ่อน UI ที่ยังไม่ได้ใช้งานตอนเริ่มเกม
        if (promptPanel != null) promptPanel.SetActive(false);
        if (readingPanel != null) readingPanel.SetActive(false);
        if (notificationPanel != null) notificationPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (itemDetailPanel != null) itemDetailPanel.SetActive(false);
        if (heldItemHUDPanel != null) heldItemHUDPanel.SetActive(false);

        // ผูก Event ปุ่มกดปิดหน้าต่างอ่าน
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseReadingClicked);
        }

        // ผูก Event ปุ่มปิดกระเป๋า
        if (closeInventoryButton != null)
        {
            closeInventoryButton.onClick.AddListener(OnCloseInventoryClicked);
        }

        // ผูก Event ปุ่มถือไอเทม
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }

        // ผูก Event ปุ่มกดใช้ไอเทม
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
        }

        // ผูก Event ปุ่มทิ้งไอเทม
        if (dropButton != null)
        {
            dropButton.onClick.AddListener(OnDropButtonClicked);
        }
    }

    void Update()
    {
        // จัดการนับเวลาถอยหลังแถบแจ้งเตือน
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0)
            {
                if (notificationPanel != null)
                {
                    notificationPanel.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// อัปเดตการแสดงผล Prompt ตามไอเทมที่ผู้เล่นกำลังเล็งอยู่
    /// </summary>
    public void UpdatePrompt(InteractableItem item, bool isReading, bool isInventoryOpen = false)
    {
        // ถ้ากำลังอ่านข้อความ หรือเปิดกระเป๋าอยู่ หรือไม่ได้เล็งไอเทม ให้ซ่อน Prompt
        if (isReading || isInventoryOpen || item == null)
        {
            if (promptPanel != null && promptPanel.activeSelf)
            {
                promptPanel.SetActive(false);
            }

            if (crosshairImage != null)
            {
                crosshairImage.color = normalCrosshairColor;
            }
            return;
        }

        // เปลี่ยนสีเป้าเล็งเป็นสีพร้อมโต้ตอบ
        if (crosshairImage != null)
        {
            crosshairImage.color = interactCrosshairColor;
        }

        // แสดงผลกล่อง Prompt
        if (promptPanel != null)
        {
            if (!promptPanel.activeSelf)
            {
                promptPanel.SetActive(true);
            }

            if (promptItemNameText != null)
            {
                promptItemNameText.text = item.itemName;
            }

            if (promptActionText != null)
            {
                string actionText = "";
                if (item.canRead)
                {
                    string promptLabel = !string.IsNullOrEmpty(item.customReadPromptText) ? item.customReadPromptText : "อ่านข้อมูล";
                    actionText += $"<color=#FFD700>[E]</color> {promptLabel}";
                }
                if (item.canCollect)
                {
                    if (!string.IsNullOrEmpty(actionText)) actionText += "   ";
                    actionText += "<color=#00FF7F>[F]</color> เก็บไอเทม";
                }
                promptActionText.text = actionText;
            }
        }
    }

    // ==========================================
    // ส่วนควบคุม Reading Dialog
    // ==========================================
    public void ShowReadingDialog(string title, string description)
    {
        if (promptPanel != null) promptPanel.SetActive(false);

        if (readingPanel != null)
        {
            readingPanel.SetActive(true);

            if (readingTitleText != null)
            {
                readingTitleText.text = title;
            }

            if (readingDescriptionText != null)
            {
                readingDescriptionText.text = description;
            }

            if (readingCloseGuideText != null)
            {
                readingCloseGuideText.text = "กด <color=#FFD700>[E]</color> หรือ <color=#FFD700>[Esc]</color> เพื่อปิด";
            }
        }
    }

    public void HideReadingDialog()
    {
        if (readingPanel != null)
        {
            readingPanel.SetActive(false);
        }
    }

    // ==========================================
    // ส่วนควบคุม Inventory UI
    // ==========================================
    public void OpenInventory(List<InventoryItem> items)
    {
        Debug.Log($"[InteractionUIManager] 🎒 เปิดกระเป๋า (จำนวนไอเทมในรายการ: {(items != null ? items.Count : 0)} ชิ้น)");

        if (promptPanel != null) promptPanel.SetActive(false);

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[InteractionUIManager] ⚠️ 'Inventory Panel' เป็น None ไม่ได้ถูกใส่ใน Inspector!");
        }

        selectedItemIndex = -1;
        RefreshInventorySlots(items);

        // เลือกไอเทมชิ้นแรกอัตโนมัติ (ถ้ามี)
        if (items != null && items.Count > 0)
        {
            SelectItem(0, items);
        }
        else
        {
            ClearDetailPanel();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        ClearDetailPanel();
        selectedItemIndex = -1;
    }

    public void RefreshInventorySlots(List<InventoryItem> items)
    {
        // ลบ Slot เดิมทิ้ง
        if (inventoryGridContainer != null)
        {
            foreach (Transform child in inventoryGridContainer)
            {
                Destroy(child.gameObject);
            }
        }
        spawnedSlots.Clear();

        bool hasItems = items != null && items.Count > 0;

        if (emptyInventoryText != null)
        {
            emptyInventoryText.gameObject.SetActive(!hasItems);
        }

        if (!hasItems)
        {
            Debug.Log("[InteractionUIManager] ไม่มีไอเทมในกระเป๋า (รายการว่าง)");
            ClearDetailPanel();
            return;
        }

        if (inventorySlotPrefab == null || inventoryGridContainer == null)
        {
            if (inventorySlotPrefab == null)
                Debug.LogError("[InteractionUIManager] ❌ ยังไม่ได้ใส่ 'Inventory Slot Prefab' ใน Inspector ของ InteractionUIManager!");
            if (inventoryGridContainer == null)
                Debug.LogError("[InteractionUIManager] ❌ ยังไม่ได้ใส่ 'Inventory Grid Container' ใน Inspector ของ InteractionUIManager!");
            return;
        }

        // สร้าง Slot ตามจำนวนไอเทม
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGridContainer, false);
            slotObj.SetActive(true);
            slotObj.transform.localScale = Vector3.one;

            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(items[i], index, (clickedIndex) => SelectItem(clickedIndex, items), index == selectedItemIndex);
                spawnedSlots.Add(slotUI);
                Debug.Log($"[InteractionUIManager] ✅ สร้าง Slot สำเร็จ: {items[i]?.itemName}");
            }
            else
            {
                Debug.LogError($"[InteractionUIManager] ❌ Prefab '{inventorySlotPrefab.name}' ไม่มีคอมโพเนนต์ InventorySlotUI ติดอยู่!");
            }
        }
    }

    public void SelectItem(int index, List<InventoryItem> items)
    {
        if (items == null || index < 0 || index >= items.Count)
        {
            ClearDetailPanel();
            return;
        }

        selectedItemIndex = index;
        InventoryItem item = items[index];
        Debug.Log($"[InteractionUIManager] 📋 กำลังแสดงรายละเอียดไอเทม: '{item.itemName}' (คำอธิบาย: '{item.description}')");

        // ไฮไลต์ Slot ที่ถูกเลือก
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
            {
                spawnedSlots[i].SetSelected(i == selectedItemIndex);
            }
        }

        // แจ้ง PlayerInteraction ทราบว่าเลือกไอเทมชิ้นนี้อยู่
        if (playerInteraction != null)
        {
            playerInteraction.selectedInventoryIndex = index;
        }

        // แสดงรายละเอียดใน Panel
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[InteractionUIManager] ⚠️ ช่อง 'Item Detail Panel' ใน Inspector ของ InteractionUIManager ยังเป็น None! กรุณาลาก GameObject Panel ฝั่งขวามาใส่");
        }

        if (detailNameText != null)
        {
            detailNameText.text = item.itemName;
        }
        else
        {
            Debug.LogWarning("[InteractionUIManager] ⚠️ ช่อง 'Detail Name Text' ยังเป็น None! (ยังไม่ได้ลาก Text สำหรับแสดงชื่อมาใส่)");
        }

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = item.description;
        }
        else
        {
            Debug.LogWarning("[InteractionUIManager] ⚠️ ช่อง 'Detail Description Text' ยังเป็น None! (ยังไม่ได้ลาก Text สำหรับแสดงคำอธิบายมาใส่)");
        }

        if (detailIconImage != null)
        {
            if (item.icon != null)
            {
                detailIconImage.sprite = item.icon;
                detailIconImage.enabled = true;
                Color c = detailIconImage.color;
                if (c.a == 0) c.a = 1f;
                detailIconImage.color = c;
            }
            else
            {
                detailIconImage.enabled = false;
            }
        }

        if (useButton != null)
        {
            useButton.interactable = item.isUsable;
        }

        if (useButtonText != null)
        {
            useButtonText.text = item.isUsable ? "ใช้งาน" : "ไม่สามารถใช้ได้";
        }
    }

    private void ClearDetailPanel()
    {
        if (itemDetailPanel != null) itemDetailPanel.SetActive(false);
        if (detailNameText != null) detailNameText.text = "";
        if (detailDescriptionText != null) detailDescriptionText.text = "";
        if (detailIconImage != null) detailIconImage.enabled = false;
    }

    private void OnEquipButtonClicked()
    {
        if (playerInteraction != null && selectedItemIndex >= 0)
        {
            playerInteraction.EquipItem(selectedItemIndex);
        }
    }

    private void OnUseButtonClicked()
    {
        if (playerInteraction != null && selectedItemIndex >= 0)
        {
            playerInteraction.UseItem(selectedItemIndex);
        }
    }

    private void OnDropButtonClicked()
    {
        if (playerInteraction != null && selectedItemIndex >= 0)
        {
            playerInteraction.DropItemFromInventory(selectedItemIndex);
        }
    }

    /// <summary>
    /// อัปเดตแถบ HUD แสดงสถานะไอเทมที่กำลังถืออยู่ที่มือ
    /// </summary>
    public void UpdateHeldItemHUD(InventoryItem heldItem)
    {
        if (heldItemHUDPanel == null) return;

        if (heldItem != null)
        {
            heldItemHUDPanel.SetActive(true);

            if (heldItemNameText != null)
            {
                heldItemNameText.text = heldItem.itemName;
            }

            if (heldItemIcon != null)
            {
                if (heldItem.icon != null)
                {
                    heldItemIcon.sprite = heldItem.icon;
                    heldItemIcon.enabled = true;
                }
                else
                {
                    heldItemIcon.enabled = false;
                }
            }

            if (heldItemGuideText != null)
            {
                heldItemGuideText.text = "<color=#FFD700>[E / LMB]</color> นำไปใช้/วาง  |  <color=#00FF7F>[G]</color> วางลงพื้น  |  <color=#00BFFF>[Q]</color> เก็บเข้ากระเป๋า";
            }
        }
        else
        {
            heldItemHUDPanel.SetActive(false);
        }
    }

    public void ShowNotification(string message, float duration = 2.5f)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            notificationTimer = duration;
        }
    }

    private void OnCloseReadingClicked()
    {
        if (playerInteraction != null) playerInteraction.CloseReading();
        else HideReadingDialog();
    }

    private void OnCloseInventoryClicked()
    {
        if (playerInteraction != null) playerInteraction.CloseInventory();
        else CloseInventory();
    }
}
