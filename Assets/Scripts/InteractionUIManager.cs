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

    private float notificationTimer = 0f;
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
        playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        // ซ่อน UI ที่ยังไม่ได้ใช้งานตอนเริ่มเกม
        if (promptPanel != null) promptPanel.SetActive(false);
        if (readingPanel != null) readingPanel.SetActive(false);
        if (notificationPanel != null) notificationPanel.SetActive(false);

        // ผูก Event ปุ่มกดปิด (ถ้ามี)
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
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
    public void UpdatePrompt(InteractableItem item, bool isReading)
    {
        // ถ้ากำลังอ่านข้อความอยู่ หรือไม่ได้เล็งไอเทม ให้ซ่อน Prompt
        if (isReading || item == null)
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
                    actionText += "<color=#FFD700>[E]</color> อ่านข้อมูล";
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

    /// <summary>
    /// เปิดหน้าต่างอ่านข้อมูล (Reading Modal / Pop-up Dialog)
    /// </summary>
    public void ShowReadingDialog(string title, string description)
    {
        // ซ่อน Prompt ทันทีเมื่อเปิดหน้าต่างอ่าน
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

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

    /// <summary>
    /// ปิดหน้าต่างอ่านข้อมูล
    /// </summary>
    public void HideReadingDialog()
    {
        if (readingPanel != null)
        {
            readingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// แสดงกล่องข้อความแจ้งเตือน Toast เช่น เมื่อเก็บไอเทมสำเร็จ
    /// </summary>
    public void ShowNotification(string message, float duration = 2.5f)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            notificationTimer = duration;
        }
    }

    private void OnCloseButtonClicked()
    {
        if (playerInteraction != null)
        {
            playerInteraction.CloseReading();
        }
        else
        {
            HideReadingDialog();
        }
    }
}
