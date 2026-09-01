using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallManager : MonoBehaviour
{
    public static HallManager Instance { get; private set; }

    [Header("1. สถานะตำหนัก (Hall Status)")]
    [Tooltip("ตำหนักกำลังเปิดรับผู้คนอยู่หรือไม่")]
    public bool isHallOpen = false;
    [Tooltip("เริ่มเปิดตำหนักทันทีเมื่อเริ่มเกมหรือไม่")]
    public bool openOnStart = false;

    [Header("2. จุดตำแหน่งสำคัญ (Waypoints)")]
    [Tooltip("จุดเกิดของ NPC (สามารถใส่ได้หลายจุดเพื่อสุ่ม)")]
    public Transform[] spawnPoints;
    [Tooltip("จุดที่ NPC จะเดินมาหยุดเพื่อสนทนา/เสนอเควสกับผู้เล่น (หน้าโต๊ะรับแขก)")]
    public Transform receptionPoint;
    [Tooltip("จุดที่ NPC จะเดินไปเพื่อออกจากตำหนักเมื่อสนทนาเสร็จ")]
    public Transform exitPoint;
    [Tooltip("ตำแหน่งของผู้เล่น (ถ้าเว้นว่างจะค้นหาให้อัตโนมัติ)")]
    public Transform playerTransform;

    [Header("3. ข้อมูล NPC & เควส (NPC Prefabs & Quests)")]
    [Tooltip("Prefab ของตัวละคร NPC (ถ้ามี ให้ใส่ที่นี่ หากเว้นว่างไว้ระบบจะสร้างตัวละครจำลองอัตโนมัติ)")]
    public GameObject defaultNpcPrefab;
    [Tooltip("รายการเควสและบทสนทนาที่จะสุ่ม/วนส่ง NPC เข้ามา")]
    public List<QuestData> questList = new List<QuestData>();

    [Header("4. การตั้งค่ารอบการปล่อย NPC (Queue Settings)")]
    [Tooltip("ระยะเวลาหน่วงก่อนปล่อย NPC คนแรกเมื่อเริ่มเปิดตำหนัก (วินาที)")]
    public float initialSpawnDelay = 1.5f;
    [Tooltip("ระยะเวลาหน่วงก่อนปล่อย NPC คนถัดไปหลังจากคนก่อนหน้าเดินออกไป (วินาที)")]
    public float delayBetweenNPCs = 2.0f;
    [Tooltip("จำนวน NPC สูงสุดต่อรอบการเปิดตำหนัก (0 = ไม่จำกัด)")]
    public int maxNpcPerSession = 0;

    [Header("5. สถานะปัจจุบัน (Runtime Info)")]
    public int currentQueueIndex = 0;
    public int npcsServedThisSession = 0;
    public NPCController currentActiveNPC;

    private Coroutine queueCoroutine;

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
        // ค้นหาตำแหน่งผู้เล่นอัตโนมัติหากยังไม่ได้กำหนด
        if (playerTransform == null)
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null) playerTransform = player.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        // สร้างจุด Reception และ Spawn จำลองอัตโนมัติหากยังไม่ได้กำหนดในฉาก
        SetupDefaultWaypointsIfMissing();

        // ใส่รายการเควสเริ่มต้นจำลองหากยังไม่มีใน List
        PopulateDefaultQuestsIfEmpty();

        if (openOnStart)
        {
            OpenHall();
        }
    }

    private void SetupDefaultWaypointsIfMissing()
    {
        if (receptionPoint == null && playerTransform != null)
        {
            GameObject recObj = new GameObject("Default_ReceptionPoint");
            recObj.transform.position = playerTransform.position + playerTransform.forward * 2.2f;
            receptionPoint = recObj.transform;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject spObj = new GameObject("Default_SpawnPoint");
            Vector3 spawnPos = playerTransform != null 
                ? playerTransform.position + playerTransform.forward * 10f + playerTransform.right * 3f 
                : new Vector3(0, 0, 10f);
            spObj.transform.position = spawnPos;
            spawnPoints = new Transform[] { spObj.transform };
        }

        if (exitPoint == null)
        {
            GameObject exitObj = new GameObject("Default_ExitPoint");
            Vector3 exitPos = playerTransform != null 
                ? playerTransform.position + playerTransform.forward * 10f - playerTransform.right * 3f 
                : new Vector3(-5f, 0, 10f);
            exitObj.transform.position = exitPos;
            exitPoint = exitObj.transform;
        }
    }

    private void PopulateDefaultQuestsIfEmpty()
    {
        if (questList.Count == 0)
        {
            questList.Add(new QuestData
            {
                questId = "quest_01",
                questTitle = "ตามหานมแก้หิว",
                npcName = "ลุงสมชาย (ชาวบ้าน)",
                greetingDialogue = "สวัสดีจ้ะพ่อหนุ่ม วันนี้ข้าเดินทางมาไกล รู้สึกคอแห้งเหลือเกิน...",
                questDescription = "ช่วยหานมกล่อง (นมบูด / MilkCartonRed) มาให้ข้าสัก 1 ชิ้นได้หรือไม่?",
                acceptDialogue = "ขอบใจมากนะพ่อหนุ่ม ข้าจะยืนรอของอยู่ตรงนี้นะ",
                waitingDialogue = "ข้ายังรอนมจากท่านอยู่นะพ่อหนุ่ม...",
                completeDialogue = "ขอบใจมากนะพ่อหนุ่ม! ได้นมแล้วข้าชื่นใจจริง ๆ ข้าขอตัวก่อนนะ",
                declineDialogue = "เฮ้อ... ไม่เป็นไร เดี๋ยวข้าลองไปถามคนอื่นต่อ",
                requiredItemName = "นม",
                requiredQuantity = 1,
                rewardDescription = "เงิน 150 เหรียญ และคำอวยพร",
                rewardMoney = 150
            });

            questList.Add(new QuestData
            {
                questId = "quest_02",
                questTitle = "ตามหาของศักดิ์สิทธิ์",
                npcName = "ป้าสมศรี (แม่ค้า)",
                greetingDialogue = "ท่านผู้ดูแลตำหนัก ช่วยข้าด้วยเถิด!",
                questDescription = "ข้าทำของสำคัญหายไปในบริเวณตำหนัก ช่วยตรวจสอบให้ข้าทีเถิด",
                acceptDialogue = "สาธุ ขอให้ท่านเจริญรุ่งเรือง ข้าฝากด้วยนะ!",
                declineDialogue = "โธ่... ข้าคงต้องลองหาดูเองต่อไป",
                requiredItemName = "",
                rewardDescription = "เงิน 300 เหรียญ",
                rewardMoney = 300
            });

            questList.Add(new QuestData
            {
                questId = "quest_03",
                questTitle = "ขับไล่สิ่งอัปมงคล",
                npcName = "ทิดมั่น (คนทรง)",
                greetingDialogue = "บรรยากาศในตำหนักวันนี้ดูแปลกๆ ท่านสัมผัสได้หรือไม่?",
                questDescription = "มีพลังงานบางอย่างรบกวนรอบๆ ตำหนัก ช่วยทำพิธีปัดเป่าให้สงบเรียบร้อยที",
                acceptDialogue = "เยี่ยมมาก ตำหนักนี้จะกลับมาสงบร่มเย็นอีกครั้ง",
                declineDialogue = "ถ้าท่านไม่สะดวก ข้าก็คงทำอะไรไม่ได้...",
                requiredItemName = "",
                rewardDescription = "เครื่องรางนำโชค",
                rewardMoney = 200
            });
        }
    }

    /// <summary>
    /// สั่งเริ่มเปิดตำหนัก (NPC จะเริ่มทยอยเดินเข้ามาทีละคน)
    /// </summary>
    public void OpenHall()
    {
        if (isHallOpen)
        {
            Debug.Log("[HallManager] ⚠️ ตำหนักเปิดอยู่แล้ว");
            return;
        }

        isHallOpen = true;
        npcsServedThisSession = 0;
        currentQueueIndex = 0;

        Debug.Log("[HallManager] [เปิดตำหนัก] เริ่มเปิดตำหนักเรียบร้อยแล้ว! กำลังส่ง NPC คนแรกเข้ามา...");
        
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification("เปิดตำหนักแล้ว! กำลังมีผู้มาเยือนเดินเข้ามา...", 3.0f);
        }

        if (queueCoroutine != null) StopCoroutine(queueCoroutine);
        queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(initialSpawnDelay));
    }

    /// <summary>
    /// สั่งปิดตำหนัก (หยุดการปล่อย NPC)
    /// </summary>
    public void CloseHall()
    {
        if (!isHallOpen) return;

        isHallOpen = false;
        if (queueCoroutine != null)
        {
            StopCoroutine(queueCoroutine);
            queueCoroutine = null;
        }

        Debug.Log("[HallManager] ปิดตำหนักเรียบร้อยแล้ว");
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification("ปิดตำหนักเรียบร้อยแล้ว (ไม่มีผู้มาเยือนใหม่)", 3.0f);
        }
    }

    /// <summary>
    /// สลับสถานะเปิด/ปิดตำหนัก
    /// </summary>
    public void ToggleHall()
    {
        if (isHallOpen) CloseHall();
        else OpenHall();
    }

    private IEnumerator SpawnNextNPCRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isHallOpen) yield break;

        // ตรวจสอบลิมิตต่อรอบ
        if (maxNpcPerSession > 0 && npcsServedThisSession >= maxNpcPerSession)
        {
            Debug.Log("[HallManager] 🏁 ครบจำนวนผู้มาเยือนในรอบนี้แล้ว กำลังปิดตำหนักอัตโนมัติ");
            CloseHall();
            yield break;
        }

        SpawnSingleNPC();
    }

    private void SpawnSingleNPC()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[HallManager] ❌ ไม่พบจุดเกิด NPC (Spawn Points)!");
            return;
        }

        // สุ่มจุดเกิด
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // ดึงข้อมูลเควสจากรายการ
        QuestData currentQuest = null;
        if (questList != null && questList.Count > 0)
        {
            currentQuest = questList[currentQueueIndex % questList.Count];
            currentQueueIndex++;
        }

        GameObject npcObj = null;

        // สร้าง NPC จาก Prefab หรือสร้างตัวละครจำลอง (Capsule Humanoid)
        if (defaultNpcPrefab != null)
        {
            npcObj = Instantiate(defaultNpcPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            npcObj = CreatePlaceholderNPC(spawnPoint.position, currentQuest != null ? currentQuest.npcName : "NPC");
        }

        NPCController controller = npcObj.GetComponent<NPCController>();
        if (controller == null)
        {
            controller = npcObj.AddComponent<NPCController>();
        }

        currentActiveNPC = controller;
        npcsServedThisSession++;

        // เริ่มต้นให้ NPC เดินเข้าหาจุดรับแขก
        controller.Initialize(currentQuest, receptionPoint, exitPoint, playerTransform, this);

        Debug.Log($"[HallManager] 👤 NPC '{currentQuest?.npcName}' เกิดที่ {spawnPoint.position} และกำลังเดินเข้ามาที่จุดรับแขก");
    }

    /// <summary>
    /// ทำงานเมื่อ NPC ปัจจุบันคุยเสร็จและเดินออกจากตำหนักแล้ว
    /// </summary>
    public void OnNPCDeparted(NPCController npc)
    {
        if (currentActiveNPC == npc)
        {
            currentActiveNPC = null;
        }

        if (isHallOpen)
        {
            Debug.Log($"[HallManager] ⏳ NPC เดินออกเรียบร้อย จะส่งคนถัดไปเข้ามาในอีก {delayBetweenNPCs} วินาที");
            if (queueCoroutine != null) StopCoroutine(queueCoroutine);
            queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(delayBetweenNPCs));
        }
    }

    /// <summary>
    /// สร้างตัวละครจำลองอัตโนมัติ (Fallback หากไม่มี 3D Model NPC)
    /// </summary>
    private GameObject CreatePlaceholderNPC(Vector3 position, string name)
    {
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = $"NPC_{name}";
        npc.transform.position = position;
        npc.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);

        // เปลี่ยนสีตัวละครให้ดูเด่นชัด
        Renderer rend = npc.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.2f, 0.6f, 1.0f);
        }

        // ดวงตา/ด้านหน้าจำลองเพื่อให้เห็นทิศทางการหันหน้า
        GameObject headIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headIndicator.name = "FaceIndicator";
        headIndicator.transform.SetParent(npc.transform);
        headIndicator.transform.localPosition = new Vector3(0, 0.6f, 0.45f);
        headIndicator.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Renderer headRend = headIndicator.GetComponent<Renderer>();
        if (headRend != null) headRend.material.color = Color.yellow;
        Destroy(headIndicator.GetComponent<Collider>());

        return npc;
    }

    // ==========================================
    // OnGUI Fallback Controls
    // ==========================================
    void OnGUI()
    {
        // แถบปุ่มควบคุมเปิด/ปิดตำหนักมุมบนซ้ายของหน้าจอ (สำหรับทดสอบสะดวก)
        float btnWidth = 180f;
        float btnHeight = 40f;
        float posX = 20f;
        float posY = 20f;

        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 13;
        style.fontStyle = FontStyle.Bold;

        if (isHallOpen)
        {
            GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
            if (GUI.Button(new Rect(posX, posY, btnWidth, btnHeight), "⛩️ [เปิดอยู่] กดเพื่อปิดตำหนัก", style))
            {
                CloseHall();
            }

            GUI.backgroundColor = Color.white;
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.fontSize = 12;
            statusStyle.normal.textColor = Color.yellow;
            string npcStatus = currentActiveNPC != null ? $"สถานะ: กำลังต้อนรับ {currentActiveNPC.questData.npcName}" : "สถานะ: กำลังรอคิวถัดไป...";
            GUI.Label(new Rect(posX, posY + 45f, 300f, 25f), npcStatus, statusStyle);
        }
        else
        {
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUI.Button(new Rect(posX, posY, btnWidth, btnHeight), "⛩️ กดเริ่มเปิดตำหนัก", style))
            {
                OpenHall();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
