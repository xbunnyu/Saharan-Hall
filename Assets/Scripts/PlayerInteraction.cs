using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("ระยะห่างสูงสุดที่สามารถโต้ตอบกับสิ่งของได้ (เมตร)")]
    public float interactDistance = 3.0f;
    [Tooltip("Layer ที่ต้องการตรวจสอบ (Default: Everything)")]
    public LayerMask interactLayer = ~0;
    public Camera playerCamera;

    [Header("Inventory (ช่องเก็บของ)")]
    [Tooltip("รายการไอเทมในกระเป๋า (แบบละเอียด)")]
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    [Tooltip("รายการชื่อไอเทมในกระเป๋า (สำหรับดูแบบย่อ)")]
    public List<string> inventory = new List<string>();

    [Header("Hand / Equip System (การถือไอเทมที่มือ)")]
    [Tooltip("จุดสำหรับถือไอเทมที่มือ (ถ้าเว้นว่างจะสร้างให้อัตโนมัติติดกับกล้อง)")]
    public Transform handSocket;
    [Tooltip("ไอเทมที่กำลังถืออยู่ในมือขณะนี้")]
    public InventoryItem currentlyHeldItem;
    [HideInInspector]
    public int selectedInventoryIndex = -1;
    private GameObject spawnedHeldObject;

    [Header("UI Settings (Built-in OnGUI)")]
    public bool enableBuiltInUI = true;
    public bool freezePlayerWhileReading = true;

    // สถานะปัจจุบัน
    private InteractableItem currentTarget;
    private bool isReading = false;
    private bool isInventoryOpen = false;
    private string readingTitle = "";
    private string readingContent = "";
    private PlayerController playerController;

    // ข้อความแจ้งเตือนเมื่อเก็บไอเทม (Toast notification)
    private string notificationMessage = "";
    private float notificationTimer = 0f;

    void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main;
            }
        }

        // สร้าง Hand Socket อัตโนมัติถ้ายังไม่ได้กำหนด
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
        // จัดการตัวนับเวลาแจ้งเตือน
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        // จัดการปุ่มลัดขณะถือไอเทมที่มือ (Equipped Item Shortcuts)
        if (currentlyHeldItem != null && !isInventoryOpen && !isReading)
        {
            // กด Q เพื่อเก็บไอเทมที่ถือกลับเข้ากระเป๋า
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
            {
                UnequipItem(true);
            }
            // กด G เพื่อทิ้ง/วางไอเทมที่ถือลงพื้น
            else if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                DropHeldItem();
            }
        }

        // กด Tab เพื่อเปิด/ปิดกระเป๋าเก็บของ (Inventory)
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            if (!isReading)
            {
                ToggleInventory();
            }
        }

        // กรณีที่เปิดกระเป๋าอยู่
        if (isInventoryOpen)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }
            return;
        }

        // กรณีที่กำลังอ่านข้อมูลอยู่
        if (isReading)
        {
            // กด E, Space, หรือ Escape เพื่อปิดหน้าต่างอ่านข้อมูล
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.escapeKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame)
                {
                    CloseReading();
                }
            }
            return;
        }

        // ยิง Raycast จากกลางหน้าจอกล้องเพื่อตรวจหาสิ่งของ
        CheckForInteractable();

        // ส่งข้อมูลไปอัปเดต Canvas UI Prompt
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdatePrompt(currentTarget, isReading, isInventoryOpen);
        }

        // ตรวจสอบการกดปุ่มโต้ตอบ
        HandleInput();
    }

    private void CheckForInteractable()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item == null)
            {
                item = hit.collider.GetComponentInParent<InteractableItem>();
            }

            currentTarget = item;
        }
        else
        {
            currentTarget = null;
        }
    }

    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        var gamepad = Gamepad.current;

        // หากกำลังถือไอเทมอยู่ และกดคลิกซ้าย หรือกด E ใส่เป้าหมาย
        bool pressUseWithHeld = false;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) pressUseWithHeld = true;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame) pressUseWithHeld = true;

        if (pressUseWithHeld && currentlyHeldItem != null && currentTarget != null)
        {
            bool usedSuccess = currentTarget.TryUseWithHeldItem(this, currentlyHeldItem);
            if (usedSuccess)
            {
                if (currentTarget.consumeHeldItemOnUse)
                {
                    UnequipItem(false); // ลบไอเทมที่ถือออกเนื่องจากถูกใช้ไปแล้ว
                }
                return;
            }
        }

        if (currentTarget == null) return;

        // กด E เพื่ออ่านข้อมูล (หากไม่ได้ถือไอเทมที่จะนำมาใช้ หรือวัตถุรองรับการอ่าน)
        bool pressRead = false;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame) pressRead = true;
        if (gamepad != null && gamepad.buttonWest.wasPressedThisFrame) pressRead = true; // ปุ่ม X บนจอย Xbox

        if (pressRead && currentTarget.canRead)
        {
            OpenReading(currentTarget);
            currentTarget.OnRead(this);
            return;
        }

        // กด F เพื่อเก็บไอเทม
        bool pressCollect = false;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame) pressCollect = true;
        if (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame) pressCollect = true; // ปุ่ม Y บนจอย Xbox

        if (pressCollect && currentTarget.canCollect)
        {
            InventoryItem itemData = currentTarget.GetInventoryData();
            inventoryItems.Add(itemData);
            inventory.Add(itemData.itemName);

            Debug.Log($"[PlayerInteraction] 📦 เก็บไอเทม: '{itemData.itemName}' สำเร็จ! (ปัจจุบันมีในกระเป๋า {inventoryItems.Count} ชิ้น)");

            ShowNotification("เก็บ [" + itemData.itemName + "] เรียบร้อยแล้ว! (กด Tab เพื่อดูกระเป๋า)");

            InteractableItem itemToCollect = currentTarget;
            currentTarget = null;
            itemToCollect.OnCollect(this);
        }
    }

    // ==========================================
    // จัดการระบบถือไอเทมที่มือ (Equip & Hold System)
    // ==========================================
    /// <summary>
    /// หยิบไอเทมจากกระเป๋าออกมาถือไว้ที่มือ
    /// </summary>
    public void EquipItem(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        // ถ้ามีของถืออยู่แล้ว ให้เก็บกลับเข้ากระเป๋าก่อน
        if (currentlyHeldItem != null)
        {
            UnequipItem(true);
        }

        InventoryItem item = inventoryItems[index];
        inventoryItems.RemoveAt(index);
        inventory.Remove(item.itemName);
        currentlyHeldItem = item;

        // ปิดหน้าต่างกระเป๋า
        CloseInventory();

        // สร้างโมเดล 3D ที่มือ
        SpawnHeldObject(item);

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdateHeldItemHUD(currentlyHeldItem);
        }

        ShowNotification($"🖐️ ถือ [{item.itemName}] ไว้ที่มือแล้ว (กด Q เก็บ / กด G วาง)");
        Debug.Log($"[PlayerInteraction] 🖐️ ถือไอเทม '{item.itemName}' ที่มือเรียบร้อย");
    }

    private void SpawnHeldObject(InventoryItem item)
    {
        if (spawnedHeldObject != null)
        {
            Destroy(spawnedHeldObject);
        }

        if (handSocket == null) return;

        if (item.worldPrefab != null)
        {
            spawnedHeldObject = Instantiate(item.worldPrefab, handSocket);
        }
        else
        {
            // Fallback จำลองโมเดล 3D ชั่วคราวหากยังไม่ได้ใส่ worldPrefab
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

        // ปิด Collider เพื่อไม่ให้ชนกับตัวละครผู้เล่น
        Collider[] colliders = spawnedHeldObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // ปิด Rigidbody ไม่ให้ตก
        Rigidbody rb = spawnedHeldObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // ปิดสคริปต์ InteractableItem บนตัวที่ถืออยู่
        InteractableItem interactScript = spawnedHeldObject.GetComponent<InteractableItem>();
        if (interactScript != null)
        {
            interactScript.enabled = false;
        }

        spawnedHeldObject.SetActive(true);
    }

    /// <summary>
    /// ปลด/เก็บไอเทมที่ถืออยู่
    /// </summary>
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
                inventoryItems.Add(currentlyHeldItem);
                inventory.Add(currentlyHeldItem.itemName);
                ShowNotification($"เก็บ [{currentlyHeldItem.itemName}] กลับเข้ากระเป๋าแล้ว");
            }
            currentlyHeldItem = null;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.UpdateHeldItemHUD(null);
        }
    }

    /// <summary>
    /// วาง/ทิ้งไอเทมที่กำลังถืออยู่ลงบนพื้น
    /// </summary>
    public void DropHeldItem()
    {
        if (currentlyHeldItem == null) return;

        Vector3 dropPosition = GetDropPosition();
        SpawnWorldItem(currentlyHeldItem, dropPosition);

        ShowNotification($"วาง [{currentlyHeldItem.itemName}] ลงบนพื้นแล้ว");
        UnequipItem(false); // ปลดของโดยไม่ต้องเอาเข้ากระเป๋า
    }

    /// <summary>
    /// ทิ้งไอเทมออกจากกระเป๋าโดยตรงลงบนพื้น
    /// </summary>
    public void DropItemFromInventory(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        InventoryItem item = inventoryItems[index];
        inventoryItems.RemoveAt(index);
        inventory.Remove(item.itemName);

        Vector3 dropPosition = GetDropPosition();
        SpawnWorldItem(item, dropPosition);

        ShowNotification($"ทิ้ง [{item.itemName}] ลงบนพื้นแล้ว");

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.RefreshInventorySlots(inventoryItems);
            if (inventoryItems.Count > 0)
            {
                int nextIndex = Mathf.Clamp(index, 0, inventoryItems.Count - 1);
                InteractionUIManager.Instance.SelectItem(nextIndex, inventoryItems);
            }
            else
            {
                InteractionUIManager.Instance.SelectItem(-1, inventoryItems);
            }
        }
    }

    private Vector3 GetDropPosition()
    {
        Vector3 origin = playerCamera != null ? playerCamera.transform.position : transform.position + Vector3.up * 1.5f;
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origin, forward, out hit, 2.5f, interactLayer))
        {
            return hit.point + Vector3.up * 0.15f;
        }
        return origin + forward * 1.5f;
    }

    private void SpawnWorldItem(InventoryItem item, Vector3 position)
    {
        GameObject worldObj = null;
        if (item.worldPrefab != null)
        {
            worldObj = Instantiate(item.worldPrefab, position, Quaternion.identity);
        }
        else
        {
            worldObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            worldObj.name = item.itemName;
            worldObj.transform.position = position;
            worldObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }

        worldObj.SetActive(true);

        // เปิด Collider ใหม่อีกครั้ง
        Collider[] colliders = worldObj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = true;

        // ตรวจสอบ InteractableItem
        InteractableItem interactScript = worldObj.GetComponent<InteractableItem>();
        if (interactScript == null)
        {
            interactScript = worldObj.AddComponent<InteractableItem>();
        }

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
    // จัดการระบบกระเป๋าเก็บของ (Inventory)
    // ==========================================
    public void ToggleInventory()
    {
        if (isInventoryOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        Debug.Log($"[PlayerInteraction] ⌨️ กด Tab เปิดกระเป๋า (ส่งไอเทม {inventoryItems.Count} ชิ้นไปที่ UI)");

        // ปลดล็อคเมาส์เพื่อให้คลิกเลือกของได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // หยุดการเคลื่อนที่ของตัวละคร
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.OpenInventory(inventoryItems);
        }
        else
        {
            Debug.LogError("[PlayerInteraction] ❌ ไม่พบ InteractionUIManager.Instance ในฉาก!");
        }
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;

        // ล็อคเมาส์กลับมาเป็นโหมดมุมมองบุคคลที่หนึ่ง
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // เปิดการเคลื่อนที่ของตัวละครกลับมา
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.CloseInventory();
        }
    }

    /// <summary>
    /// สั่งใช้งานไอเทมจาก Index ในกระเป๋า
    /// </summary>
    public void UseItem(int index)
    {
        if (index < 0 || index >= inventoryItems.Count) return;

        InventoryItem item = inventoryItems[index];
        if (!item.isUsable) return;

        // เล่นเสียงเมื่อกดใช้
        if (item.useSound != null)
        {
            AudioSource.PlayClipAtPoint(item.useSound, transform.position);
        }

        // เรียก Event การทำงานของไอเทม
        item.onUse?.Invoke();

        ShowNotification("ใช้งาน [" + item.itemName + "] สำเร็จ!");

        // ลบไอเทมออกจากกระเป๋า
        string removedName = item.itemName;
        inventoryItems.RemoveAt(index);
        inventory.Remove(removedName);

        // อัปเดต UI กระเป๋าใหม่
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.RefreshInventorySlots(inventoryItems);
            if (inventoryItems.Count > 0)
            {
                int nextIndex = Mathf.Clamp(index, 0, inventoryItems.Count - 1);
                InteractionUIManager.Instance.SelectItem(nextIndex, inventoryItems);
            }
        }
    }

    /// <summary>
    /// ตรวจสอบว่าในกระเป๋ามีไอเทมชื่อนี้หรือไม่ (สำหรับระบบประตู, กลไก, NPC)
    /// </summary>
    public bool HasItem(string itemName)
    {
        return inventoryItems.Exists(x => x.itemName == itemName) || inventory.Contains(itemName) || (currentlyHeldItem != null && currentlyHeldItem.itemName == itemName);
    }

    /// <summary>
    /// ลบไอเทมออกจากกระเป๋าหรือมือตามชื่อ (เช่น ใช้กุญแจเปิดประตูแล้วกุญแจหาย)
    /// </summary>
    public bool RemoveItemByName(string itemName)
    {
        if (currentlyHeldItem != null && currentlyHeldItem.itemName == itemName)
        {
            UnequipItem(false);
            return true;
        }

        int index = inventoryItems.FindIndex(x => x.itemName == itemName);
        if (index >= 0)
        {
            inventoryItems.RemoveAt(index);
            inventory.Remove(itemName);

            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.RefreshInventorySlots(inventoryItems);
            }
            return true;
        }
        return false;
    }

    // ==========================================
    // จัดการหน้าต่างอ่านข้อมูล (Reading Modal)
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
        notificationMessage = msg;
        notificationTimer = duration;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(msg, duration);
        }
    }

    public bool IsReading() => isReading;
    public bool IsInventoryOpen() => isInventoryOpen;
    public InteractableItem GetCurrentTarget() => currentTarget;

    // ==========================================
    // ส่วนแสดงผล UI อัตโนมัติ (Built-in OnGUI Fallback)
    // ==========================================
    void OnGUI()
    {
        // 1. เป้าเล็งตรงกลางหน้าจอ (Crosshair)
        if (!isReading && !isInventoryOpen)
        {
            if (!enableBuiltInUI && InteractionUIManager.Instance != null) { /* ใช้ Canvas แทน */ }
            else
            {
                float crosshairSize = 6f;
                float centerX = Screen.width / 2f;
                float centerY = Screen.height / 2f;
                GUI.color = currentTarget != null ? Color.green : new Color(1f, 1f, 1f, 0.6f);
                GUI.Box(new Rect(centerX - crosshairSize / 2, centerY - crosshairSize / 2, crosshairSize, crosshairSize), GUIContent.none);
                GUI.color = Color.white;
            }
        }

        // 2. แสดงแถบ HUD ถือไอเทมที่มือ (ถ้าไม่ได้ใช้ Canvas HUD)
        if (currentlyHeldItem != null && !isInventoryOpen && !isReading && (InteractionUIManager.Instance == null || InteractionUIManager.Instance.heldItemHUDPanel == null))
        {
            GUIStyle heldStyle = new GUIStyle(GUI.skin.box);
            heldStyle.fontSize = 14;
            heldStyle.alignment = TextAnchor.MiddleCenter;
            heldStyle.normal.textColor = Color.white;

            string heldText = $"🖐️ กำลังถือ: <b>{currentlyHeldItem.itemName}</b>   |   <color=#FFD700>[E / คลิกซ้าย]</color> ใช้/วาง   |   <color=#00FF7F>[G]</color> วางลงพื้น   |   <color=#00BFFF>[Q]</color> เก็บกระเป๋า";
            float barWidth = 620f;
            float barHeight = 40f;
            float barX = (Screen.width - barWidth) / 2f;
            float barY = Screen.height - 70f;

            GUI.Box(new Rect(barX, barY, barWidth, barHeight), heldText, heldStyle);
        }

        // 3. แสดงแผงรายละเอียดไอเทมในกระเป๋า (Fallback หากใน Canvas ยังไม่ได้ผูก Item Detail Panel)
        if (isInventoryOpen && (InteractionUIManager.Instance == null || InteractionUIManager.Instance.itemDetailPanel == null))
        {
            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventoryItems.Count)
            {
                InventoryItem selItem = inventoryItems[selectedInventoryIndex];

                float detailW = 280f;
                float detailH = 340f;
                float detailX = Screen.width - detailW - 60f;
                float detailY = (Screen.height - detailH) / 2f;

                GUI.Box(new Rect(detailX, detailY, detailW, detailH), GUIContent.none);

                GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                nameStyle.fontSize = 18;
                nameStyle.fontStyle = FontStyle.Bold;
                nameStyle.alignment = TextAnchor.UpperCenter;
                nameStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(detailX + 15, detailY + 15, detailW - 30, 30), selItem.itemName, nameStyle);

                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = 14;
                descStyle.wordWrap = true;
                descStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(detailX + 20, detailY + 55, detailW - 40, 140), selItem.description, descStyle);

                // ปุ่ม ถือที่มือ
                if (GUI.Button(new Rect(detailX + 25, detailY + 205, detailW - 50, 35), "🖐️ ถือที่มือ (Equip)"))
                {
                    EquipItem(selectedInventoryIndex);
                }

                // ปุ่ม ใช้งาน
                if (selItem.isUsable)
                {
                    if (GUI.Button(new Rect(detailX + 25, detailY + 248, detailW - 50, 35), "⚡ ใช้งาน (Use)"))
                    {
                        UseItem(selectedInventoryIndex);
                    }
                }

                // ปุ่ม วางลงพื้น
                if (GUI.Button(new Rect(detailX + 25, detailY + 291, detailW - 50, 35), "🗑️ วางลงพื้น (Drop)"))
                {
                    DropItemFromInventory(selectedInventoryIndex);
                }
            }
        }

        // หากมี InteractionUIManager (Canvas) ให้ข้าม OnGUI ส่วนอื่นๆ
        if (!enableBuiltInUI || InteractionUIManager.Instance != null) return;

        // 3. แสดง Prompt คำแนะนำเมื่อมองไปที่ไอเทม
        if (currentTarget != null && !isReading && !isInventoryOpen)
        {
            GUIStyle promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.fontSize = 16;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;

            string promptText = "<b>" + currentTarget.itemName + "</b>\n";
            if (currentlyHeldItem != null)
            {
                promptText += $"<color=#FFD700>[E/LMB]</color> ใช้ {currentlyHeldItem.itemName}   ";
            }
            if (currentTarget.canRead) promptText += "<color=#FFD700>[E]</color> อ่านข้อมูล   ";
            if (currentTarget.canCollect) promptText += "<color=#00FF7F>[F]</color> เก็บไอเทม";

            float boxWidth = 340f;
            float boxHeight = 65f;
            float posX = (Screen.width - boxWidth) / 2f;
            float posY = Screen.height / 2f + 40f;

            GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), promptText, promptStyle);
        }

        // 4. แสดงหน้าต่างอ่านข้อมูล (Reading Modal)
        if (isReading)
        {
            float winWidth = Mathf.Min(560f, Screen.width * 0.85f);
            float winHeight = Mathf.Min(340f, Screen.height * 0.7f);
            float winX = (Screen.width - winWidth) / 2f;
            float winY = (Screen.height - winHeight) / 2f;

            GUI.Box(new Rect(winX, winY, winWidth, winHeight), GUIContent.none);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(winX + 20, winY + 20, winWidth - 40, 35), readingTitle, titleStyle);

            GUIStyle contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.fontSize = 15;
            contentStyle.wordWrap = true;
            contentStyle.alignment = TextAnchor.UpperLeft;
            contentStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(winX + 30, winY + 65, winWidth - 60, winHeight - 125), readingContent, contentStyle);

            GUIStyle footerStyle = new GUIStyle(GUI.skin.label);
            footerStyle.fontSize = 13;
            footerStyle.alignment = TextAnchor.LowerCenter;
            footerStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(winX + 20, winY + winHeight - 35, winWidth - 40, 25), "กด [E] หรือ [Space] หรือ [Esc] เพื่อปิด", footerStyle);
        }

        // 5. แสดงข้อความแจ้งเตือนเมื่อเก็บของ (Notification)
        if (notificationTimer > 0)
        {
            GUIStyle notifStyle = new GUIStyle(GUI.skin.box);
            notifStyle.fontSize = 14;
            notifStyle.alignment = TextAnchor.MiddleCenter;
            notifStyle.normal.textColor = new Color(0.2f, 1f, 0.5f);

            float notifWidth = 320f;
            float notifHeight = 40f;
            float notifX = (Screen.width - notifWidth) / 2f;
            float notifY = 30f;

            GUI.Box(new Rect(notifX, notifY, notifWidth, notifHeight), notificationMessage, notifStyle);
        }
    }
}
