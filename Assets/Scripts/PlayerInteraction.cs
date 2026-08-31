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

    [Header("Inventory (รายการไอเทมที่เก็บ)")]
    public List<string> inventory = new List<string>();

    [Header("UI Settings (Built-in OnGUI)")]
    public bool enableBuiltInUI = true;
    public bool freezePlayerWhileReading = true;

    // สถานะปัจจุบัน
    private InteractableItem currentTarget;
    private bool isReading = false;
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
    }

    void Update()
    {
        // จัดการตัวนับเวลาแจ้งเตือน
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
        }

        // กรณีที่กำลังอ่านข้อมูลอยู่
        if (isReading)
        {
            // กด E, Space, หรือ Escape เพื่อปิดหน้าต่างอ่านข้อมูล
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame ||
                    Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.escapeKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame)
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
            InteractionUIManager.Instance.UpdatePrompt(currentTarget, isReading);
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
        if (currentTarget == null) return;

        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // กด E เพื่ออ่านข้อมูล
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
            string itemName = currentTarget.itemName;
            inventory.Add(itemName);
            ShowNotification("เก็บ [" + itemName + "] เรียบร้อยแล้ว!");

            InteractableItem itemToCollect = currentTarget;
            currentTarget = null;
            itemToCollect.OnCollect(this);
        }
    }

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

    public bool IsReading()
    {
        return isReading;
    }

    public InteractableItem GetCurrentTarget()
    {
        return currentTarget;
    }

    // ==========================================
    // ส่วนแสดงผล UI อัตโนมัติ (Built-in OnGUI)
    // ==========================================
    void OnGUI()
    {
        // หากปิดการใช้งาน หรือมี InteractionUIManager (Canvas) ให้ข้าม OnGUI
        if (!enableBuiltInUI || InteractionUIManager.Instance != null) return;

        // 1. เป้าเล็งตรงกลางหน้าจอ (Crosshair)
        if (!isReading)
        {
            float crosshairSize = 6f;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            GUI.color = currentTarget != null ? Color.green : new Color(1f, 1f, 1f, 0.6f);
            GUI.Box(new Rect(centerX - crosshairSize / 2, centerY - crosshairSize / 2, crosshairSize, crosshairSize), GUIContent.none);
            GUI.color = Color.white;
        }

        // 2. แสดง Prompt คำแนะนำเมื่อมองไปที่ไอเทม
        if (currentTarget != null && !isReading)
        {
            GUIStyle promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.fontSize = 16;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;

            string promptText = "<b>" + currentTarget.itemName + "</b>\n";
            if (currentTarget.canRead) promptText += "<color=#FFD700>[E]</color> อ่านข้อมูล   ";
            if (currentTarget.canCollect) promptText += "<color=#00FF7F>[F]</color> เก็บไอเทม";

            float boxWidth = 320f;
            float boxHeight = 65f;
            float posX = (Screen.width - boxWidth) / 2f;
            float posY = Screen.height / 2f + 40f;

            GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), promptText, promptStyle);
        }

        // 3. แสดงหน้าต่างอ่านข้อมูล (Reading Modal)
        if (isReading)
        {
            float winWidth = Mathf.Min(560f, Screen.width * 0.85f);
            float winHeight = Mathf.Min(340f, Screen.height * 0.7f);
            float winX = (Screen.width - winWidth) / 2f;
            float winY = (Screen.height - winHeight) / 2f;

            // กรอบพื้นหลัง
            GUI.Box(new Rect(winX, winY, winWidth, winHeight), GUIContent.none);

            // หัวข้อ
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.UpperCenter;
            titleStyle.normal.textColor = new Color(1f, 0.84f, 0f);
            GUI.Label(new Rect(winX + 20, winY + 20, winWidth - 40, 35), readingTitle, titleStyle);

            // เนื้อหา
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.fontSize = 15;
            contentStyle.wordWrap = true;
            contentStyle.alignment = TextAnchor.UpperLeft;
            contentStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(winX + 30, winY + 65, winWidth - 60, winHeight - 125), readingContent, contentStyle);

            // ปุ่มหรือคำแนะนำการปิด
            GUIStyle footerStyle = new GUIStyle(GUI.skin.label);
            footerStyle.fontSize = 13;
            footerStyle.alignment = TextAnchor.LowerCenter;
            footerStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(winX + 20, winY + winHeight - 35, winWidth - 40, 25), "กด [E] หรือ [Space] หรือ [Esc] เพื่อปิด", footerStyle);
        }

        // 4. แสดงข้อความแจ้งเตือนเมื่อเก็บของ (Notification)
        if (notificationTimer > 0)
        {
            GUIStyle notifStyle = new GUIStyle(GUI.skin.box);
            notifStyle.fontSize = 14;
            notifStyle.alignment = TextAnchor.MiddleCenter;
            notifStyle.normal.textColor = new Color(0.2f, 1f, 0.5f);

            float notifWidth = 300f;
            float notifHeight = 40f;
            float notifX = (Screen.width - notifWidth) / 2f;
            float notifY = 30f;

            GUI.Box(new Rect(notifX, notifY, notifWidth, notifHeight), notificationMessage, notifStyle);
        }
    }
}
