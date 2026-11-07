using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบจัดการอีเวนต์จดหมายเชิญไปป่า (Forest Invitation Event Manager)
/// - เมื่อถึงวันที่ 2 และทำเควส NPC ของวันนั้นเสร็จสิ้นทั้งหมด จดหมายเชิญจะปรากฏขึ้นบนโต๊ะรับแขก
/// - ผู้เล่นสามารถเดินไปกด [E] เพื่อเปิดหน้าต่างอีเวนต์และกดยอมรับคำเชิญ
/// - เมื่อกดรับคำเชิญแล้ว รถยนต์หน้าตำหนัก (CarToForestInteractable) จะปลดล็อคให้ขับไปป่าได้ทันที
/// </summary>
public class ForestInvitationManager : MonoBehaviour
{
    public static ForestInvitationManager Instance { get; private set; }

    [Header("1. วันและเงื่อนไขการส่งจดหมาย (Trigger Conditions)")]
    [Tooltip("วันที่จดหมายเชิญจะปรากฏ (ค่าเริ่มต้นคือวันที่ 2)")]
    public int targetDay = 2;

    [Tooltip("จดหมายถูกส่งมาถึงตำหนักแล้วหรือยัง")]
    public bool isLetterDelivered = false;

    [Tooltip("ผู้เล่นได้กดตอบรับคำเชิญแล้วหรือยัง")]
    public bool isInvitationAccepted = false;

    [Header("2. ตำแหน่งและ Prefab ของจดหมายในฉาก 3D")]
    [Tooltip("จุดที่จดหมายจะปรากฏในฉาก (ถ้าเว้นว่างจะค้นหาโต๊ะรับแขกของ HallManager ให้อัตโนมัติ)")]
    public Transform customLetterSpawnPoint;

    [Tooltip("Prefab ของจดหมาย (ถ้าเว้นว่างระบบจะสร้างจดหมาย 3D จำลองพร้อมตราประทับสีแดงให้อัตโนมัติ)")]
    public GameObject customLetterPrefab;

    [Header("3. เสียงประกอบ (Audio)")]
    public AudioClip letterArrivalSound;
    public AudioClip acceptSound;

    [Header("4. UI หน้าต่างอีเวนต์ (Event Modal Dialog)")]
    public Canvas eventCanvas;
    public GameObject eventDialogPanel;
    public Button acceptButton;
    public Button closeButton;
    public TMP_Text eventTitleText;
    public TMP_Text eventDescriptionText;
    public TMP_Text eventRewardText;

    // Runtime state
    private bool isEventUIOpen = false;
    private GameObject activeLetterInstance;
    private float checkTimer = 0f;
    private PlayerController playerController;

    public bool IsInvitationAccepted => isInvitationAccepted;
    public bool IsLetterDelivered => isLetterDelivered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureManager()
    {
        if (Instance == null && FindFirstObjectByType<ForestInvitationManager>() == null)
        {
            GameObject go = new GameObject("ForestInvitationManager_AutoCreated");
            go.AddComponent<ForestInvitationManager>();
            Debug.Log("[ForestInvitationManager] 🌟 ติดตั้งระบบอีเวนต์จดหมายเชิญไปป่าอัตโนมัติเรียบร้อยแล้ว");
        }
    }

    private void Awake()
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DayManager.OnDayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        DayManager.OnDayChanged += HandleDayChanged;
        EnsureEventUI();
        CheckAndRestoreLetterInScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // เมื่อโหลดซีน GamePlay กลับมา ให้ตรวจสอบและสร้างโมเดลจดหมายบนโต๊ะอีกครั้ง (ถ้ายังอยู่ในสถานะส่งแล้ว)
        if (scene.name == "GamePlay")
        {
            EnsureEventUI();
            CheckAndRestoreLetterInScene();
        }
    }

    private void HandleDayChanged(int newDay)
    {
        // ตรวจสอบทันทีเมื่อเปลี่ยนวัน
        CheckConditions();
    }

    private void Update()
    {
        // หากหน้าต่างอีเวนต์เปิดอยู่ ให้รับคีย์ลัด
        if (isEventUIOpen)
        {
            HandleEventUIInput();
            return;
        }

        // ตรวจสอบเงื่อนไขทุกๆ 0.5 วินาที
        checkTimer += Time.deltaTime;
        if (checkTimer >= 0.5f)
        {
            checkTimer = 0f;
            CheckConditions();
        }
    }

    /// <summary>
    /// ตรวจสอบเงื่อนไขว่าถึงเวลาส่งจดหมายเชิญแล้วหรือยัง
    /// </summary>
    public void CheckConditions()
    {
        if (isLetterDelivered)
        {
            // ถ้าส่งแล้ว แต่โมเดลในฉากหายไป (เช่น เพิ่งเปลี่ยนซีนกลับมา GamePlay) ให้สร้างกลับมาใหม่
            if (activeLetterInstance == null && SceneManager.GetActiveScene().name == "GamePlay")
            {
                CheckAndRestoreLetterInScene();
            }
            return;
        }

        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;

        // ต้องเป็นวันที่ 2 (หรือมากกว่า 2 ในกรณีที่ผู้เล่นยังไม่ได้รับจดหมาย)
        if (currentDay < targetDay)
        {
            return;
        }

        // ตรวจสอบว่าเควส NPC ประจำวันเสร็จสิ้นแล้วหรือไม่
        if (AreDayQuestsFinished())
        {
            DeliverLetter();
        }
    }

    /// <summary>
    /// ตรวจสอบว่าเควส NPC ทั้งหมดของวันนั้นๆ ทำเสร็จสิ้นแล้วหรือไม่
    /// </summary>
    public bool AreDayQuestsFinished()
    {
        // 1. ตรวจสอบว่ายังมีเควสที่ค้างอยู่ใน QuestUIManager หรือไม่
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.activeQuests != null && QuestUIManager.Instance.activeQuests.Count > 0)
        {
            return false;
        }

        // 2. ตรวจสอบสถานะตำหนักของ HallManager
        if (HallManager.Instance != null)
        {
            // ยังมี NPC หรือพ่อค้าอยู่ในตำหนัก
            if (HallManager.Instance.currentActiveNPC != null || HallManager.Instance.isMerchantActive)
            {
                return false;
            }

            int target = HallManager.Instance.maxNpcPerSession > 0 ? HallManager.Instance.maxNpcPerSession : 5;
            int served = HallManager.Instance.npcsServedThisSession;

            // ตำหนักยังเปิดอยู่ หรือยังบริการไม่ครบตามเป้าหมาย
            if (HallManager.Instance.isHallOpen || served < target)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// ส่งจดหมายเชิญมายังตำหนัก (แสดงการแจ้งเตือน + วางจดหมาย 3D บนโต๊ะรับแขก)
    /// </summary>
    public void DeliverLetter()
    {
        if (isLetterDelivered) return;

        isLetterDelivered = true;
        Debug.Log("[ForestInvitationManager] ✉️ [อีเวนต์พิเศษ]: เงื่อนไขวันที่ 2 ครบถ้วน! จดหมายเชิญลึกลับถูกส่งมาถึงตำหนักแล้ว");

        // เล่นเสียงแจ้งเตือนจดหมายมาถึง (ถ้ามี)
        if (letterArrivalSound != null)
        {
            AudioSource.PlayClipAtPoint(letterArrivalSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // แสดงแบนเนอร์แจ้งเตือนผู้เล่น
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                "<color=#FFD700>✉️ [อีเวนต์พิเศษ]: มีจดหมายเชิญปริศนาส่งมาถึงตำหนัก!</color>\n(ตรวจดูจดหมายที่โต๊ะรับแขกเพื่อเปิดรับภารกิจ)", 5.0f);
        }

        // สร้างโมเดลจดหมายในฉาก
        SpawnLetterInScene();
    }

    /// <summary>
    /// กู้คืนหรือสร้างโมเดลจดหมายในฉาก GamePlay
    /// </summary>
    private void CheckAndRestoreLetterInScene()
    {
        if (!isLetterDelivered) return;
        if (SceneManager.GetActiveScene().name != "GamePlay") return;

        if (activeLetterInstance == null)
        {
            SpawnLetterInScene();
        }
    }

    /// <summary>
    /// วางวัตถุจดหมาย 3D ลงบนโต๊ะรับแขก
    /// </summary>
    private void SpawnLetterInScene()
    {
        // ถ้ามีจดหมายในฉากอยู่แล้ว
        if (activeLetterInstance != null) return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // 1. ถ้ามี customLetterSpawnPoint ที่ผู้ใช้กำหนด
        if (customLetterSpawnPoint != null)
        {
            spawnPos = customLetterSpawnPoint.position;
            spawnRot = customLetterSpawnPoint.rotation;
        }
        else
        {
            // 2. ค้นหาจุด ReceptionPoint ของตำหนักอัตโนมัติ
            Transform recPoint = HallManager.Instance != null ? HallManager.Instance.receptionPoint : null;
            if (recPoint == null)
            {
                GameObject foundRec = GameObject.Find("ReceptionPoint") ?? GameObject.Find("Default_ReceptionPoint");
                if (foundRec != null) recPoint = foundRec.transform;
            }

            if (recPoint != null)
            {
                // วางไว้บนโต๊ะรับแขก (ถอยร่นลงมาทางผู้เล่นเล็กน้อย เพื่อให้หยิบ/อ่านได้ง่าย)
                spawnPos = recPoint.position + (recPoint.forward * 0.45f);
                spawnRot = recPoint.rotation;
            }
            else
            {
                // Fallback: วางข้างผู้เล่น
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    spawnPos = pc.transform.position + pc.transform.forward * 1.5f;
                    spawnRot = Quaternion.identity;
                }
            }

            // ยิง Raycast ลงพื้น/ผิวโต๊ะเพื่อให้จดหมายวางทาบผิวโต๊ะพอดี
            if (Physics.Raycast(spawnPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 3.5f))
            {
                spawnPos = hit.point + Vector3.up * 0.015f; // ยกขึ้น 1.5 ซม. ไม่ให้ติด z-fighting กับโต๊ะ
            }
        }

        // สร้าง GameObject จดหมาย
        if (customLetterPrefab != null)
        {
            activeLetterInstance = Instantiate(customLetterPrefab, spawnPos, spawnRot);
        }
        else
        {
            activeLetterInstance = CreateProceduralLetterObject(spawnPos, spawnRot);
        }

        activeLetterInstance.name = "Forest_InvitationLetter";

        // ตรวจสอบคอมโพเนนต์ InvitationLetterInteractable
        var letterInteract = activeLetterInstance.GetComponent<InvitationLetterInteractable>();
        if (letterInteract == null)
        {
            letterInteract = activeLetterInstance.AddComponent<InvitationLetterInteractable>();
        }

        letterInteract.UpdatePromptState();

        Debug.Log($"[ForestInvitationManager] 📜 วางจดหมายเชิญลงที่ {spawnPos} เรียบร้อยแล้ว!");
    }

    /// <summary>
    /// สร้างโมเดลจดหมาย 3D จำลองพร้อมตราประทับสีแดงแบบ Procedural
    /// </summary>
    private GameObject CreateProceduralLetterObject(Vector3 position, Quaternion rotation)
    {
        // แผ่นจดหมาย/ซองจดหมายสีงาช้าง
        GameObject letter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        letter.transform.position = position;
        letter.transform.rotation = rotation;
        letter.transform.localScale = new Vector3(0.28f, 0.015f, 0.20f);

        // สีเนื้อกระดาษโบราณ (Warm Parchment)
        Renderer rend = letter.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.95f, 0.91f, 0.82f);
        }

        // ตราประทับครั่งสีแดง (Wax Seal) กลางซอง
        GameObject seal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        seal.name = "WaxSeal_Red";
        seal.transform.SetParent(letter.transform, false);
        seal.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        seal.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
        Renderer sealRend = seal.GetComponent<Renderer>();
        if (sealRend != null)
        {
            sealRend.material.color = new Color(0.78f, 0.12f, 0.12f); // สีแดงชาด
        }
        Destroy(seal.GetComponent<Collider>());

        // ลายเส้นประทับทองจางๆ
        GameObject stampGold = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stampGold.name = "Seal_GoldEmblem";
        stampGold.transform.SetParent(seal.transform, false);
        stampGold.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        stampGold.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
        Renderer goldRend = stampGold.GetComponent<Renderer>();
        if (goldRend != null)
        {
            goldRend.material.color = new Color(1.0f, 0.85f, 0.25f);
        }
        Destroy(stampGold.GetComponent<Collider>());

        return letter;
    }

    // ══════════════════════════════════════════════════════════════════════
    // ระบบหน้าต่างอีเวนต์ (Event Modal Window)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// เปิดหน้าต่างอีเวนต์พิเศษให้ผู้เล่นอ่านและกดยอมรับคำเชิญ
    /// </summary>
    public void OpenInvitationEventUI()
    {
        EnsureEventUI();

        isEventUIOpen = true;

        if (eventDialogPanel != null)
        {
            eventDialogPanel.SetActive(true);
        }

        // ตรึงผู้เล่นและเปิดเมาส์
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateDialogContent();

        Debug.Log("[ForestInvitationManager] 📖 เปิดหน้าต่างอีเวนต์จดหมายเชิญไปป่า");
    }

    /// <summary>
    /// ปิดหน้าต่างอีเวนต์
    /// </summary>
    public void CloseInvitationEventUI()
    {
        isEventUIOpen = false;

        if (eventDialogPanel != null)
        {
            eventDialogPanel.SetActive(false);
        }

        // คืนการควบคุมให้ผู้เล่น
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// ผู้เล่นกดยอมรับคำเชิญ (Accept Event)
    /// </summary>
    public void AcceptInvitation()
    {
        isInvitationAccepted = true;
        Debug.Log("[ForestInvitationManager] 🎉 ผู้เล่นกดยอมรับคำเชิญไปป่าเรียบร้อยแล้ว! รถยนต์หน้าตำหนักถูกปลดล็อคแล้ว");

        // เล่นเสียงตอบรับ
        if (acceptSound != null)
        {
            AudioSource.PlayClipAtPoint(acceptSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        CloseInvitationEventUI();

        // แจ้งเตือนความสำเร็จบนหน้าจอ
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                "<color=#00FF7F><b>🎉 ตอบรับคำเชิญเรียบร้อยแล้ว!</b></color>\n<color=#FFD700>🚗 รถยนต์หน้าตำหนักได้รับการปลดล็อคแล้ว — สามารถเดินไปขับรถเพื่อเดินทางไปป่าได้ทันที!</color>", 6.0f);
        }

        // อัปเดตข้อความ Prompt ของจดหมายในฉาก
        if (activeLetterInstance != null)
        {
            var letterInteract = activeLetterInstance.GetComponent<InvitationLetterInteractable>();
            if (letterInteract != null)
            {
                letterInteract.UpdatePromptState();
            }
        }
    }

    private void HandleEventUIInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // กด E, Enter หรือ Space เพื่อตอบรับคำเชิญ
        if (!isInvitationAccepted && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
        {
            AcceptInvitation();
        }
        // กด Esc หรือ Q เพื่อปิดหน้าต่าง
        else if (keyboard.escapeKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)
        {
            CloseInvitationEventUI();
        }
    }

    private void UpdateDialogContent()
    {
        if (eventTitleText != null)
        {
            eventTitleText.text = "<size=115%><color=#FFD700><b>📜 สาส์นเชิญสู่ผืนป่าต้องสาป</b></color></size>\n<size=75%><color=#BBBBBB><i>ตราประทับโบราณส่งตรงถึงผู้ดูแลตำหนักสาฮารัน</i></color></size>";
        }

        if (eventDescriptionText != null)
        {
            eventDescriptionText.text =
                "<b>ถึง ท่านผู้ดูแลตำหนักสาฮารัน,</b>\n\n" +
                "หลังจากที่ท่านได้ปัดเป่าเคราะห์ภัยและช่วยเหลือชาวบ้านในตำหนักจนหมดสิ้นแล้ว บัดนี้มีไออาถรรพ์และเสียงสวดลึกลับแว่วมาจากป่าลึกท้ายหมู่บ้าน...\n\n" +
                "ชาวบ้านต่างพบเห็นดวงไฟประหลาดและหมอกควันดำปกคลุม จึงใคร่ขออาราธนาท่านผู้มีวิชาอาคม เดินทางไปยังป่าช้าและป่าลึกเพื่อสำรวจและสะกดสิ่งชั่วร้าย\n\n" +
                "<color=#00FFFF><b>รถยนต์หน้าตำหนักได้รับการเติมน้ำมันและเตรียมความพร้อมสำหรับการเดินทางแล้ว!</b></color>";
        }

        if (eventRewardText != null)
        {
            string statusStr = isInvitationAccepted
                ? "<color=#00FF7F><b>✅ ตอบรับคำเชิญแล้ว (รถยนต์พร้อมออกเดินทาง)</b></color>"
                : "<color=#FFD700><b>🎯 ผลลัพธ์:</b></color> <color=#00FF7F>ปลดล็อครถยนต์เพื่อเดินทางไปยังป่า (Forest Scene) ได้ทันที!</color>";

            eventRewardText.text = statusStr;
        }

        if (acceptButton != null)
        {
            TMP_Text btnText = acceptButton.GetComponentInChildren<TMP_Text>();
            if (isInvitationAccepted)
            {
                if (btnText != null) btnText.text = "✅ ตอบรับแล้ว";
                acceptButton.interactable = false;
            }
            else
            {
                if (btnText != null) btnText.text = "✅ ตอบรับคำเชิญ [E/Enter]";
                acceptButton.interactable = true;
            }
        }
    }

    /// <summary>
    /// ข้อความแจ้งเตือนเมื่อผู้เล่นไปกดที่รถยนต์แต่ยังไม่ได้รับอนุญาตให้ไปป่า
    /// </summary>
    public string GetCarLockReason()
    {
        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;

        if (currentDay < targetDay)
        {
            return $"<color=#FF4500>🔒 รถยนต์ยังไม่พร้อมใช้งาน!</color>\nต้องรอให้ถึงวันที่ {targetDay} และทำเควส NPC ประจำวันให้เสร็จสิ้นก่อน";
        }

        if (!AreDayQuestsFinished())
        {
            int served = HallManager.Instance != null ? HallManager.Instance.npcsServedThisSession : 0;
            int target = HallManager.Instance != null ? HallManager.Instance.maxNpcPerSession : 5;
            return $"<color=#FF4500>🔒 ยังไม่สามารถออกเดินทางได้!</color>\nต้องต้อนรับผู้มาเยือนและเคลียร์เควส NPC ให้ครบก่อน ({served}/{target} คน)";
        }

        if (!isInvitationAccepted)
        {
            return "<color=#FFD700>✉️ มีจดหมายเชิญรออยู่ที่โต๊ะรับแขก!</color>\nกรุณาไปเปิดอ่านจดหมายและกดยอมรับคำเชิญก่อนออกเดินทาง";
        }

        return string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════
    // UI Builder (สร้าง Canvas และหน้าต่างอีเวนต์อัตโนมัติหากยังไม่มี)
    // ══════════════════════════════════════════════════════════════════════

    private void EnsureEventUI()
    {
        if (eventDialogPanel != null) return;

        // 1. ค้นหา Canvas ที่มีอยู่แล้ว หรือสร้างใหม่
        GameObject existingCanvas = GameObject.Find("ForestInvitation_Canvas");
        if (existingCanvas != null)
        {
            eventCanvas = existingCanvas.GetComponent<Canvas>();
        }

        if (eventCanvas == null)
        {
            GameObject canvasGO = new GameObject("ForestInvitation_Canvas");
            DontDestroyOnLoad(canvasGO);
            eventCanvas = canvasGO.AddComponent<Canvas>();
            eventCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            eventCanvas.sortingOrder = 2500;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // 2. แผ่นพื้นหลังทึบแสงบางๆ ครอบจอ (Dim Overlay)
        GameObject modalGO = new GameObject("EventModal_Panel");
        modalGO.transform.SetParent(eventCanvas.transform, false);
        RectTransform modalRT = modalGO.AddComponent<RectTransform>();
        modalRT.anchorMin = Vector2.zero;
        modalRT.anchorMax = Vector2.one;
        modalRT.offsetMin = Vector2.zero;
        modalRT.offsetMax = Vector2.zero;

        Image modalBg = modalGO.AddComponent<Image>();
        modalBg.color = new Color(0f, 0f, 0f, 0.75f);

        // 3. กล่องการ์ดหน้าต่างอีเวนต์ (Dialog Card)
        GameObject cardGO = new GameObject("DialogCard");
        cardGO.transform.SetParent(modalGO.transform, false);
        RectTransform cardRT = cardGO.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(720f, 540f);
        cardRT.anchoredPosition = Vector2.zero;

        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.12f, 0.10f, 0.08f, 0.96f); // สีชาเข้ม / ไม้กฤษณา

        // กรอบเงาสีทองรอบการ์ด
        var outline = cardGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.70f, 0.25f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        // 4. แถบป้าย "⭐ อีเวนต์พิเศษ ⭐" ด้านบน
        GameObject badgeGO = new GameObject("SpecialEvent_Badge");
        badgeGO.transform.SetParent(cardGO.transform, false);
        RectTransform badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.5f, 1f);
        badgeRT.anchorMax = new Vector2(0.5f, 1f);
        badgeRT.pivot     = new Vector2(0.5f, 1f);
        badgeRT.anchoredPosition = new Vector2(0f, -18f);
        badgeRT.sizeDelta = new Vector2(660f, 30f);

        var badgeText = badgeGO.AddComponent<TextMeshProUGUI>();
        badgeText.text = "⭐ อีเวนต์พิเศษ (SPECIAL EVENT) ⭐";
        badgeText.fontSize = 17;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.color = new Color(1.0f, 0.85f, 0.25f, 1f);
        badgeText.alignment = TextAlignmentOptions.Center;

        // 5. หัวข้อจดหมาย (Title)
        GameObject titleGO = new GameObject("EventTitle_Text");
        titleGO.transform.SetParent(cardGO.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -50f);
        titleRT.sizeDelta = new Vector2(660f, 60f);

        eventTitleText = titleGO.AddComponent<TextMeshProUGUI>();
        eventTitleText.fontSize = 26;
        eventTitleText.fontStyle = FontStyles.Bold;
        eventTitleText.color = new Color(1f, 0.92f, 0.6f);
        eventTitleText.alignment = TextAlignmentOptions.Center;

        // เส้นคั่นแนวนอน (Divider Line)
        GameObject divGO = new GameObject("DividerLine");
        divGO.transform.SetParent(cardGO.transform, false);
        RectTransform divRT = divGO.AddComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 1f);
        divRT.anchorMax = new Vector2(0.5f, 1f);
        divRT.pivot     = new Vector2(0.5f, 1f);
        divRT.anchoredPosition = new Vector2(0f, -115f);
        divRT.sizeDelta = new Vector2(640f, 2f);
        Image divImg = divGO.AddComponent<Image>();
        divImg.color = new Color(0.7f, 0.55f, 0.25f, 0.5f);

        // 6. เนื้อความจดหมาย (Story Body)
        GameObject descGO = new GameObject("EventDescription_Text");
        descGO.transform.SetParent(cardGO.transform, false);
        RectTransform descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.5f, 1f);
        descRT.anchorMax = new Vector2(0.5f, 1f);
        descRT.pivot     = new Vector2(0.5f, 1f);
        descRT.anchoredPosition = new Vector2(0f, -125f);
        descRT.sizeDelta = new Vector2(640f, 230f);

        eventDescriptionText = descGO.AddComponent<TextMeshProUGUI>();
        eventDescriptionText.fontSize = 17;
        eventDescriptionText.color = new Color(0.92f, 0.92f, 0.92f);
        eventDescriptionText.alignment = TextAlignmentOptions.TopLeft;

        // 7. กล่องแสดงเป้าหมายและรางวัล (Reward Box)
        GameObject rewardBoxGO = new GameObject("Reward_Box");
        rewardBoxGO.transform.SetParent(cardGO.transform, false);
        RectTransform rewardRT = rewardBoxGO.AddComponent<RectTransform>();
        rewardRT.anchorMin = new Vector2(0.5f, 0f);
        rewardRT.anchorMax = new Vector2(0.5f, 0f);
        rewardRT.pivot     = new Vector2(0.5f, 0f);
        rewardRT.anchoredPosition = new Vector2(0f, 85f);
        rewardRT.sizeDelta = new Vector2(640f, 55f);

        Image rewardBg = rewardBoxGO.AddComponent<Image>();
        rewardBg.color = new Color(0.08f, 0.12f, 0.16f, 0.9f);
        var rOutline = rewardBoxGO.AddComponent<Outline>();
        rOutline.effectColor = new Color(0.2f, 0.8f, 0.5f, 0.6f);
        rOutline.effectDistance = new Vector2(1f, -1f);

        GameObject rTextGO = new GameObject("Reward_Text");
        rTextGO.transform.SetParent(rewardBoxGO.transform, false);
        RectTransform rTextRT = rTextGO.AddComponent<RectTransform>();
        rTextRT.anchorMin = Vector2.zero;
        rTextRT.anchorMax = Vector2.one;
        rTextRT.offsetMin = new Vector2(15f, 5f);
        rTextRT.offsetMax = new Vector2(-15f, -5f);

        eventRewardText = rTextGO.AddComponent<TextMeshProUGUI>();
        eventRewardText.fontSize = 15;
        eventRewardText.alignment = TextAlignmentOptions.Center;

        // 8. ปุ่ม [ตอบรับคำเชิญ]
        GameObject acceptGO = new GameObject("Accept_Button");
        acceptGO.transform.SetParent(cardGO.transform, false);
        RectTransform acceptRT = acceptGO.AddComponent<RectTransform>();
        acceptRT.anchorMin = new Vector2(0.5f, 0f);
        acceptRT.anchorMax = new Vector2(0.5f, 0f);
        acceptRT.pivot     = new Vector2(0.5f, 0f);
        acceptRT.anchoredPosition = new Vector2(-130f, 20f);
        acceptRT.sizeDelta = new Vector2(240f, 48f);

        Image acceptImg = acceptGO.AddComponent<Image>();
        acceptImg.color = new Color(0.15f, 0.65f, 0.30f);
        acceptButton = acceptGO.AddComponent<Button>();
        acceptButton.onClick.AddListener(AcceptInvitation);

        GameObject aTextGO = new GameObject("Text");
        aTextGO.transform.SetParent(acceptGO.transform, false);
        RectTransform aTextRT = aTextGO.AddComponent<RectTransform>();
        aTextRT.anchorMin = Vector2.zero;
        aTextRT.anchorMax = Vector2.one;
        aTextRT.offsetMin = Vector2.zero;
        aTextRT.offsetMax = Vector2.zero;
        var aTMP = aTextGO.AddComponent<TextMeshProUGUI>();
        aTMP.text = "✅ ตอบรับคำเชิญ [E/Enter]";
        aTMP.fontSize = 17;
        aTMP.fontStyle = FontStyles.Bold;
        aTMP.color = Color.white;
        aTMP.alignment = TextAlignmentOptions.Center;

        // 9. ปุ่ม [ปิดไว้ก่อน]
        GameObject closeGO = new GameObject("Close_Button");
        closeGO.transform.SetParent(cardGO.transform, false);
        RectTransform closeRT = closeGO.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.5f, 0f);
        closeRT.anchorMax = new Vector2(0.5f, 0f);
        closeRT.pivot     = new Vector2(0.5f, 0f);
        closeRT.anchoredPosition = new Vector2(130f, 20f);
        closeRT.sizeDelta = new Vector2(180f, 48f);

        Image closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.35f, 0.35f, 0.35f);
        closeButton = closeGO.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseInvitationEventUI);

        GameObject cTextGO = new GameObject("Text");
        cTextGO.transform.SetParent(closeGO.transform, false);
        RectTransform cTextRT = cTextGO.AddComponent<RectTransform>();
        cTextRT.anchorMin = Vector2.zero;
        cTextRT.anchorMax = Vector2.one;
        cTextRT.offsetMin = Vector2.zero;
        cTextRT.offsetMax = Vector2.zero;
        var cTMP = cTextGO.AddComponent<TextMeshProUGUI>();
        cTMP.text = "❌ ปิดไว้ก่อน [Esc]";
        cTMP.fontSize = 16;
        cTMP.color = Color.white;
        cTMP.alignment = TextAlignmentOptions.Center;

        eventDialogPanel = modalGO;
        eventDialogPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Debug & Test Tools (สำหรับทดสอบใน Editor)
    // ══════════════════════════════════════════════════════════════════════

    [ContextMenu("🧪 [ทดสอบ] บังคับส่งจดหมายเชิญทันที")]
    public void TestForceDeliverLetter()
    {
        isLetterDelivered = false;
        DeliverLetter();
    }

    [ContextMenu("🧪 [ทดสอบ] บังคับตอบรับคำเชิญทันที")]
    public void TestForceAcceptInvitation()
    {
        AcceptInvitation();
    }

    [ContextMenu("🧪 [ทดสอบ] รีเซ็ตสถานะจดหมายเชิญทั้งหมด")]
    public void TestResetInvitation()
    {
        isLetterDelivered = false;
        isInvitationAccepted = false;
        if (activeLetterInstance != null)
        {
            Destroy(activeLetterInstance);
            activeLetterInstance = null;
        }
        CloseInvitationEventUI();
        Debug.Log("[ForestInvitationManager] 🔄 รีเซ็ตสถานะจดหมายเชิญเรียบร้อยแล้ว");
    }
}

