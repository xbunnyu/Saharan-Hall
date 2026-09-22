using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ระบบเก้าอี้ที่นั่ง (Chair / Seat Component)
/// - เมื่อกดนั่ง: ตัวละครจะนั่งล็อกอยู่กับที่ (เดินไม่ได้) แต่ยังคงหมุนกล้องเมาส์หันมอง และกด Interact โต้ตอบกับสิ่งของ/NPC ได้ตามปกติ
/// - ไม่ยุ่งเกี่ยวหรือผูกกับการเปิด/ปิดตำหนักโดยตรง (การกดเปิด/ปิดตำหนักจะทำหน้าที่เปลี่ยนสถานะตำหนักเพียงอย่างเดียว ไม่ส่งผลให้ตัวละครลุกขึ้นอัตโนมัติ)
/// - การลุกยืน: ต้องเกิดจากการกดสั่งลุกเองของผู้เล่น (กด [E], [Q], [Esc] หรือคลิกปุ่ม) และจะลุกได้สำเร็จเฉพาะเมื่อ "สถานะตำหนักปิดอยู่" เท่านั้น
/// </summary>
public class HallSeatInteractable : InteractableItem
{
    public static HallSeatInteractable Instance { get; private set; }

    [Header("Seat Positions (จุดตำแหน่งที่นั่งและจุดลุกยืน)")]
    [Tooltip("จุดสำหรับจัดตำแหน่งตัวละครตอนนั่ง (หากเว้นว่างจะใช้อัตโนมัติที่ตำแหน่งเก้าอี้)")]
    public Transform seatPoint;
    [Tooltip("จุดสำหรับให้ตัวละครไปยืนเมื่อลุกขึ้น (หากเว้นว่างจะวางข้างๆ เก้าอี้)")]
    public Transform standUpPoint;
    [Tooltip("ความสูงชดเชยของตัวละครขณะนั่ง (Y Offset)")]
    public float seatingYOffset = 0.2f;

    [Header("Runtime State")]
    public bool isSeated = false;

    private PlayerInteraction activePlayer;
    private PlayerController playerController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        readDescription = "";
        readTitle = "";
    }

    private void Reset()
    {
        itemName = "เก้าอี้";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "นั่งเก้าอี้";
        readDescription = "";
        readTitle = "";
    }

    private void Update()
    {
        if (!isSeated) return;

        // ตรวจจับเฉพาะการกดปุ่มสั่งลุกขึ้นจากผู้เล่นเท่านั้น (ไม่ลุกขึ้นเองโดยอัตโนมัติ)
        bool triggerStandUp = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.eKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                triggerStandUp = true;
            }
        }

        // Fallback รองรับ Legacy Input
        try
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
            {
                triggerStandUp = true;
            }
        }
        catch { }

        if (triggerStandUp)
        {
            TryStandUp();
        }
    }

    /// <summary>
    /// เมื่อผู้เล่นกด [E] โต้ตอบกับเก้าอี้
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        if (isSeated)
        {
            TryStandUp();
        }
        else
        {
            SitDown(interactor);
        }
    }

    /// <summary>
    /// สั่งนั่งลงเก้าอี้ (ล็อกการเคลื่อนที่ แต่ขยับเมาส์และ Interact ได้ปกติ)
    /// </summary>
    public void SitDown(PlayerInteraction interactor)
    {
        activePlayer = interactor != null ? interactor : FindFirstObjectByType<PlayerInteraction>();
        if (activePlayer == null) return;

        playerController = activePlayer.GetComponent<PlayerController>();

        // 1. จัดตำแหน่งตัวละครให้อยู่บนเก้าอี้
        CharacterController charController = activePlayer.GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;

        Vector3 targetPos = seatPoint != null ? seatPoint.position : transform.position + Vector3.up * seatingYOffset;
        Quaternion targetRot = seatPoint != null ? seatPoint.rotation : transform.rotation;

        activePlayer.transform.position = targetPos;
        activePlayer.transform.rotation = targetRot;

        Physics.SyncTransforms();
        if (charController != null) charController.enabled = true;

        // 2. ล็อคการเคลื่อนที่ (เดิน/กระโดดไม่ได้) แต่ยังคงขยับเมาส์หันมองรอบๆ ได้
        if (playerController != null)
        {
            playerController.isMovementLocked = true;
            playerController.enabled = true;
        }

        isSeated = true;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification("<color=#FFD700>🪑 นั่งลงบนเก้าอี้เรียบร้อยแล้ว</color>", 2.5f);
        }

        Debug.Log("[HallSeat] 🪑 ผู้เล่นนั่งลงบนเก้าอี้แล้ว (ล็อกการเดิน แต่ขยับเมาส์และ Interact ได้ปกติ)");
    }

    /// <summary>
    /// พยายามลุกขึ้นยืนเมื่อผู้เล่นกดสั่ง (จะลุกได้เฉพาะเมื่อตำหนักปิดอยู่เท่านั้น)
    /// </summary>
    public void TryStandUp()
    {
        if (!isSeated) return;

        // ตรวจสอบสถานะตำหนักปัจจุบัน
        bool isHallOpen = HallManager.Instance != null && HallManager.Instance.isHallOpen;

        if (isHallOpen)
        {
            // หากตำหนักเปิดอยู่ ห้ามลุกขึ้น!
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification("<color=#FF4500>⚠️ ไม่สามารถลุกขึ้นได้ในขณะที่ตำหนักเปิดอยู่!</color>", 3.0f);
            }
            Debug.Log("[HallSeat] 🔒 ตำหนักเปิดอยู่ ไม่สามารถลุกขึ้นได้");
            return;
        }

        // หากตำหนักปิดอยู่ อนุญาตให้ลุกขึ้นยืนได้
        StandUp();
    }

    /// <summary>
    /// สั่งลุกขึ้นยืน ปลดล็อคการเดินและย้ายผู้เล่นออกจากเก้าอี้
    /// </summary>
    public void StandUp()
    {
        if (!isSeated) return;

        if (activePlayer == null) activePlayer = FindFirstObjectByType<PlayerInteraction>();
        if (playerController == null && activePlayer != null) playerController = activePlayer.GetComponent<PlayerController>();

        // 1. ย้ายตัวละครไปยังจุดยืนลุก
        if (activePlayer != null)
        {
            CharacterController charController = activePlayer.GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;

            Vector3 standPos = standUpPoint != null 
                ? standUpPoint.position 
                : transform.position + transform.right * 1.3f + Vector3.up * 0.1f;

            activePlayer.transform.position = standPos;

            Physics.SyncTransforms();
            if (charController != null) charController.enabled = true;
        }

        // 2. ปลดล็อคการเคลื่อนที่กลับคืนมา 100%
        if (playerController != null)
        {
            playerController.isMovementLocked = false;
            playerController.enabled = true;
        }

        isSeated = false;

        // 3. คืนค่าเคอร์เซอร์เข้าสู่โหมดการเล่นปกติ
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification("<color=#00FF7F>🚪 ลุกขึ้นจากเก้าอี้เรียบร้อยแล้ว</color>", 2.5f);
        }

        Debug.Log("[HallSeat] 🚪 ลุกขึ้นยืนเรียบร้อยแล้ว");
    }

    // ==========================================
    // UI Overlay ขณะนั่ง (แสดงสถานะและปุ่มกดลุกขึ้น)
    // ==========================================
    private void OnGUI()
    {
        if (!isSeated) return;

        bool isHallOpen = HallManager.Instance != null && HallManager.Instance.isHallOpen;

        float w = 450f;
        float h = 85f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - h - 30f;

        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Box(new Rect(x + 4, y + 4, w - 8, h - 8), "");

        GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        if (isHallOpen)
        {
            statusStyle.normal.textColor = new Color(1f, 0.35f, 0.2f);
            GUI.Label(new Rect(x, y + 10, w, 25), "🔴 ตำหนักกำลังเปิดอยู่ (ห้ามลุกขึ้นจนกว่าตำหนักจะปิด)", statusStyle);
        }
        else
        {
            statusStyle.normal.textColor = new Color(0.2f, 1f, 0.5f);
            GUI.Label(new Rect(x, y + 10, w, 25), "🟢 ตำหนักปิดอยู่ (สามารถลุกขึ้นได้)", statusStyle);
        }

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = Color.white;

        if (isHallOpen)
        {
            GUI.Label(new Rect(x, y + 36, w, 25), "ขยับเมาส์หันมองและกด [E] เพื่อโต้ตอบกับผู้มาเยือนได้", hintStyle);
            GUI.enabled = false;
            GUI.Button(new Rect(x + (w - 200f) / 2f, y + 54, 200f, 24f), "🔒 ตำหนักเปิดอยู่ (ลุกไม่ได้)");
            GUI.enabled = true;
        }
        else
        {
            GUI.Label(new Rect(x, y + 36, w, 25), "กด [E], [Q] หรือ [Esc] เพื่อลุกขึ้นยืน", hintStyle);
            if (GUI.Button(new Rect(x + (w - 200f) / 2f, y + 54, 200f, 24f), "🚪 ลุกขึ้นยืน"))
            {
                TryStandUp();
            }
        }
    }
}
