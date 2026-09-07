using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("1. Interaction Settings")]
    [Tooltip("ระยะห่างสูงสุดที่สามารถโต้ตอบกับสิ่งของได้ (เมตร)")]
    public float interactDistance = 3.0f;
    [Tooltip("Layer ที่ต้องการตรวจสอบ (Default: Everything)")]
    public LayerMask interactLayer = ~0;
    public Camera playerCamera;

    [Header("2. Inventory (ช่องเก็บของ)")]
    [Tooltip("รายการไอเทมในกระเป๋า (แบบละเอียด)")]
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    [Tooltip("รายการชื่อไอเทมในกระเป๋า (สำหรับดูแบบย่อ)")]
    public List<string> inventory = new List<string>();

    [Header("3. Hand / Equip System (การถือไอเทมที่มือ)")]
    [Tooltip("จุดสำหรับถือไอเทมที่มือ (ถ้าเว้นว่างจะสร้างให้อัตโนมัติติดกับกล้อง)")]
    public Transform handSocket;
    [Tooltip("ไอเทมที่กำลังถืออยู่ในมือขณะนี้")]
    public InventoryItem currentlyHeldItem;
    [HideInInspector]
    public int selectedInventoryIndex = -1;
    private GameObject spawnedHeldObject;

    [Header("4. Settings")]
    public bool freezePlayerWhileReading = true;

    // สถานะภายใน
    private InteractableItem currentTarget;
    private bool isReading = false;
    private bool isInventoryOpen = false;
    private string readingTitle = "";
    private string readingContent = "";
    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();

        // กำหนดกล้องผู้เล่น
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main;
            }
        }

        // สร้าง Hand Socket อัตโนมัติหากยังไม่ได้กำหนด
        if (handSocket == null && playerCamera != null)
        {
            GameObject socketObj = new GameObject("HandSocket");
            socketObj.transform.SetParent(playerCamera.transform, false);
            socketObj.transform.localPosition = new Vector3(0.35f, -0.3f, 0.6f);
            socketObj.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);
            handSocket = socketObj.transform;
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;

        // 1. จัดการการเปิด/ปิดกระเป๋า (Tab)
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && !isReading)
        {
            ToggleInventory();
        }

        // 2. หากเปิดหน้าต่างเควสอยู่ ให้ข้ามการโต้ตอบอื่น
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.IsDialogActive())
        {
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.UpdatePrompt(null, true, false);
            }
            return;
        }

        // 2.5 หากเปิด Altar Upgrade Dialog อยู่ ให้หยุดรับ input อื่น
        if (AltarUpgradeUI.Instance != null && AltarUpgradeUI.Instance.IsOpen())
        {
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.UpdatePrompt(null, true, false);
            }
            return;
        }

        // 2.7 หากกำลังเล่นมินิเกมอยู่ (เช่น Rhythm Game ท่องคาถา) ให้ข้ามการโต้ตอบอื่น
        if (MinigameManager.Instance != null && MinigameManager.Instance.IsPlaying())
        {
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.UpdatePrompt(null, true, false);
            }
            return;
        }

        // 3. หากเปิดกระเป๋าอยู่
        if (isInventoryOpen)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }
            return;
        }

        // 4. หากกำลังเปิดหน้าต่างอ่านข้อมูลอยู่
        if (isReading)
        {
            if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                CloseReading();
            }
            return;
        }

        // 5. จัดการปุ่มลัดไอเทมที่ถืออยู่ (Q เก็บ / G วาง)
        HandleHeldItemShortcuts(keyboard);

        // 6. ยิง Raycast ตรวจหาวัตถุตรงหน้า
        CheckForInteractable();

        // 7. อัปเดต UI Prompt
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdatePrompt(currentTarget, isReading, isInventoryOpen);
        }

        // 8. จัดการการกดปุ่มโต้ตอบ (E / F / LMB)
        HandleInteractionInput();
    }

    // ==========================================
    // ตรวจจับและโต้ตอบ (Raycast & Interaction)
    // ==========================================
    private void CheckForInteractable()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>() 
                ?? hit.collider.GetComponentInParent<InteractableItem>();

            currentTarget = item;
        }
        else
        {
            currentTarget = null;
        }
    }

    private void HandleInteractionInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        var gamepad = Gamepad.current;

        bool pressE = (keyboard != null && keyboard.eKey.wasPressedThisFrame) || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);
        bool pressT = (keyboard != null && keyboard.tKey.wasPressedThisFrame);
        bool pressLMB = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool pressF = (keyboard != null && keyboard.fKey.wasPressedThisFrame) || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        // นำไอเทมที่ถือมาใช้กับวัตถุเป้าหมาย
        if ((pressE || pressLMB) && currentlyHeldItem != null && currentTarget != null)
        {
            if (currentTarget.TryUseWithHeldItem(this, currentlyHeldItem))
            {
                if (currentTarget.consumeHeldItemOnUse)
                {
                    UnequipItem(false);
                }
                return;
            }
        }

        if (currentTarget == null) return;

        // ตรวจสอบปุ่มสำหรับโต้ตอบตาม interactionKeyText (รองรับปุ่ม T และ E)
        string targetKey = !string.IsNullOrEmpty(currentTarget.interactionKeyText) ? currentTarget.interactionKeyText.ToUpper() : "E";
        bool isInteractPressed = false;
        if (targetKey == "T")
        {
            isInteractPressed = pressT || pressE;
        }
        else
        {
            isInteractPressed = pressE;
        }

        // กด [E] หรือ [T] พูดคุย / อ่าน / สลับสวิตช์ / อัพเกรด
        if (isInteractPressed && currentTarget.canRead)
        {
            bool isNPC = currentTarget.GetComponent<NPCController>() != null || currentTarget.GetComponentInParent<NPCController>() != null;
            bool isDirectAction = currentTarget is HallTrigger || currentTarget is BuddhistAltarManager || string.IsNullOrEmpty(currentTarget.readDescription) || isNPC;

            if (isDirectAction)
            {
                currentTarget.OnRead(this);
            }
            else
            {
                OpenReading(currentTarget);
                currentTarget.OnRead(this);
            }
            return;
        }

        // กด [F] เก็บไอเทมเข้ากระเป๋า
        if (pressF && currentTarget.canCollect)
        {
            CollectItem(currentTarget);
        }
    }

    private void CollectItem(InteractableItem item)
    {
        InventoryItem itemData = item.GetInventoryData();
        AddItem(itemData);

        ShowNotification($"เก็บ [{itemData.itemName}] เรียบร้อยแล้ว! (กด Tab เพื่อดูกระเป๋า)");

        InteractableItem itemToCollect = item;
        currentTarget = null;
        itemToCollect.OnCollect(this);
    }

    // ==========================================
    // ระบบกระเป๋าและช่องเก็บของ (Inventory)
    // ==========================================
    public void AddItem(InventoryItem item)
    {
        if (item == null) return;
        inventoryItems.Add(item);
        inventory.Add(item.itemName);
    }

    public void ToggleInventory()
    {
        if (isInventoryOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerController != null) playerController.enabled = false;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.OpenInventory(inventoryItems);
        }
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController != null) playerController.enabled = true;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.CloseInventory();
        }
    }

    public void UseItem(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        InventoryItem item = inventoryItems[index];
        if (!item.isUsable) return;

        if (item.useSound != null)
        {
            AudioSource.PlayClipAtPoint(item.useSound, transform.position);
        }

        item.onUse?.Invoke();
        ShowNotification($"ใช้งาน [{item.itemName}] สำเร็จ!");

        RemoveItemAt(index);
    }

    public void DropItemFromInventory(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        InventoryItem item = inventoryItems[index];
        RemoveItemAt(index);

        SpawnWorldItem(item, GetDropPosition());
        ShowNotification($"ทิ้ง [{item.itemName}] ลงบนพื้นแล้ว");
    }

    private void RemoveItemAt(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        string removedName = inventoryItems[index].itemName;
        inventoryItems.RemoveAt(index);
        inventory.Remove(removedName);

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.RefreshInventorySlots(inventoryItems);
            int nextIndex = inventoryItems.Count > 0 ? Mathf.Clamp(index, 0, inventoryItems.Count - 1) : -1;
            InteractionUIManager.Instance.SelectItem(nextIndex, inventoryItems);
        }
    }

    public bool HasItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        return inventoryItems.Exists(x => x != null && (x.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) || x.itemName.Contains(itemName) || itemName.Contains(x.itemName))) 
            || (currentlyHeldItem != null && (currentlyHeldItem.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) || currentlyHeldItem.itemName.Contains(itemName) || itemName.Contains(currentlyHeldItem.itemName)));
    }

    public bool RemoveItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        if (currentlyHeldItem != null && (currentlyHeldItem.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) || currentlyHeldItem.itemName.Contains(itemName) || itemName.Contains(currentlyHeldItem.itemName)))
        {
            UnequipItem(false);
            return true;
        }

        int index = inventoryItems.FindIndex(x => x != null && (x.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) || x.itemName.Contains(itemName) || itemName.Contains(x.itemName)));
        if (index >= 0)
        {
            RemoveItemAt(index);
            return true;
        }
        return false;
    }

    // ==========================================
    // ระบบการถือและวางไอเทมที่มือ (Equip & Drop)
    // ==========================================
    private void HandleHeldItemShortcuts(Keyboard keyboard)
    {
        if (currentlyHeldItem == null || keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame)
        {
            UnequipItem(true);
        }
        else if (keyboard.gKey.wasPressedThisFrame)
        {
            DropHeldItem();
        }
    }

    public void EquipItem(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        if (currentlyHeldItem != null)
        {
            UnequipItem(true);
        }

        InventoryItem item = inventoryItems[index];
        inventoryItems.RemoveAt(index);
        inventory.Remove(item.itemName);
        currentlyHeldItem = item;

        CloseInventory();
        SpawnHeldObject(item);

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdateHeldItemHUD(currentlyHeldItem);
        }

        ShowNotification($"🖐️ ถือ [{item.itemName}] ไว้ที่มือ (กด Q เก็บ / กด G วาง)");
    }

    public void UnequipItem(bool returnToInventory = true)
    {
        if (spawnedHeldObject != null)
        {
            Destroy(spawnedHeldObject);
            spawnedHeldObject = null;
        }

        if (currentlyHeldItem != null)
        {
            if (returnToInventory)
            {
                AddItem(currentlyHeldItem);
                ShowNotification($"เก็บ [{currentlyHeldItem.itemName}] กลับเข้ากระเป๋าแล้ว");
            }
            currentlyHeldItem = null;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdateHeldItemHUD(null);
        }
    }

    public void DropHeldItem()
    {
        if (currentlyHeldItem == null) return;

        SpawnWorldItem(currentlyHeldItem, GetDropPosition());
        ShowNotification($"วาง [{currentlyHeldItem.itemName}] ลงบนพื้นแล้ว");
        UnequipItem(false);
    }

    private void SpawnHeldObject(InventoryItem item)
    {
        if (spawnedHeldObject != null) Destroy(spawnedHeldObject);
        if (handSocket == null) return;

        if (item.worldPrefab != null)
        {
            spawnedHeldObject = Instantiate(item.worldPrefab, handSocket);
        }
        else
        {
            spawnedHeldObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedHeldObject.name = $"Held_{item.itemName}";
            spawnedHeldObject.transform.SetParent(handSocket, false);
            spawnedHeldObject.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        }

        spawnedHeldObject.transform.localPosition = item.holdOffset != Vector3.zero ? item.holdOffset : Vector3.zero;
        spawnedHeldObject.transform.localRotation = Quaternion.Euler(item.holdRotation);
        if (item.worldPrefab != null && item.holdScale != Vector3.zero)
        {
            spawnedHeldObject.transform.localScale = item.holdScale;
        }

        // ปิด Collider และ Rigidbody ไม่ให้ชนกับผู้เล่น
        foreach (var col in spawnedHeldObject.GetComponentsInChildren<Collider>()) col.enabled = false;
        var rb = spawnedHeldObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        var interactScript = spawnedHeldObject.GetComponent<InteractableItem>();
        if (interactScript != null) interactScript.enabled = false;

        spawnedHeldObject.SetActive(true);
    }

    private Vector3 GetDropPosition()
    {
        Vector3 origin = playerCamera != null ? playerCamera.transform.position : transform.position + Vector3.up * 1.5f;
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;

        if (Physics.Raycast(origin, forward, out RaycastHit hit, 2.5f, interactLayer))
        {
            return hit.point + Vector3.up * 0.15f;
        }
        return origin + forward * 1.5f;
    }

    private void SpawnWorldItem(InventoryItem item, Vector3 position)
    {
        GameObject worldObj = item.worldPrefab != null 
            ? Instantiate(item.worldPrefab, position, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        worldObj.name = item.itemName;
        worldObj.transform.position = position;
        if (item.worldPrefab == null) worldObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        worldObj.SetActive(true);
        foreach (var col in worldObj.GetComponentsInChildren<Collider>()) col.enabled = true;

        var interactScript = worldObj.GetComponent<InteractableItem>() ?? worldObj.AddComponent<InteractableItem>();
        interactScript.enabled = true;
        interactScript.itemName = item.itemName;
        interactScript.itemIcon = item.icon;
        interactScript.itemDescription = item.description;
        interactScript.isUsable = item.isUsable;
        interactScript.useSound = item.useSound;
        interactScript.onUse = item.onUse;
        interactScript.worldPrefab = item.worldPrefab;
    }

    // ==========================================
    // การอ่านข้อมูล & แจ้งเตือน (Reading & Notification)
    // ==========================================
    public void OpenReading(InteractableItem item)
    {
        isReading = true;
        readingTitle = string.IsNullOrEmpty(item.readTitle) ? item.itemName : item.readTitle;
        readingContent = item.readDescription;

        if (freezePlayerWhileReading && playerController != null)
        {
            playerController.enabled = false;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowReadingDialog(readingTitle, readingContent);
        }
    }

    public void CloseReading()
    {
        isReading = false;
        readingTitle = "";
        readingContent = "";

        if (freezePlayerWhileReading && playerController != null)
        {
            playerController.enabled = true;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HideReadingDialog();
        }
    }

    public void ShowNotification(string msg, float duration = 2.5f)
    {
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(msg, duration);
        }
    }

    public bool IsReading() => isReading;
    public bool IsInventoryOpen() => isInventoryOpen;
    public InteractableItem GetCurrentTarget() => currentTarget;
}
