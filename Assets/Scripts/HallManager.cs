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

    [Header("5.5 Difficulty Weight (น้ำหนักการสุ่มระดับความยาก)")]
    [Tooltip("น้ำหนักการสุ่มเควสแต่ละระดับ (ผลรวมไม่จำเป็นต้องเท่ากับ 100)")]
    [Range(0, 100)] public int weightEasy     = 40;   // 40% ง่าย
    [Range(0, 100)] public int weightMedium   = 35;   // 35% ปานกลาง
    [Range(0, 100)] public int weightHard     = 20;   // 20% ยาก
    [Range(0, 100)] public int weightVeryHard = 5;    //  5% ยากมาก

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

        // ตรวจสอบและสร้าง Manager สำคัญอัตโนมัติหากยังไม่มีในฉาก
        SetupRequiredManagers();

        // สร้างจุด Reception และ Spawn จำลองอัตโนมัติหากยังไม่ได้กำหนดในฉาก
        SetupDefaultWaypointsIfMissing();

        // ใส่รายการเควสเริ่มต้นจำลองหากยังไม่มีใน List
        PopulateDefaultQuestsIfEmpty();

        if (openOnStart)
        {
            OpenHall();
        }
    }

    private void SetupRequiredManagers()
    {
        if (GhostCurseManager.Instance == null && FindFirstObjectByType<GhostCurseManager>() == null)
        {
            GameObject ghostObj = new GameObject("GhostCurseManager");
            ghostObj.AddComponent<GhostCurseManager>();
        }

        if (MinigameManager.Instance == null && FindFirstObjectByType<MinigameManager>() == null)
        {
            GameObject minigameObj = new GameObject("MinigameManager");
            minigameObj.AddComponent<MinigameManager>();
            minigameObj.AddComponent<RhythmGameManager>();
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
            // ─── EASY ───────────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_easy_chant",
                difficulty       = QuestDifficulty.Easy,
                minigameType     = MinigameType.RhythmChantWASD,
                questTitle       = "สวดมนต์สะเดาะเคราะห์",
                npcName          = "ลุงสมชาย (ชาวบ้าน)",
                greetingDialogue = "สวัสดีจ้ะพ่อหนุ่ม... ช่วงนี้ข้ารู้สึกดวงตกเหลือเกิน รบกวนช่วยท่องคาถาสะเดาะเคราะห์ให้ข้าทีเถิด",
                questDescription = "ช่วยทำพิธีท่องคาถาสะเดาะเคราะห์ (Rhythm Game W A S D) เพื่อปัดเป่าเคราะห์ร้าย",
                acceptDialogue   = "ขอบใจมากนะพ่อหนุ่ม! ข้าจะตั้งจิตร่วมพิธีเดี๋ยวนี้เลย",
                completeDialogue = "สาธุ! ข้ารู้สึกโล่งใจและดวงเปิดขึ้นทันที ขอบคุณท่านผู้ดูแลตำหนักมาก!",
                failDialogue     = "อ๊าก! ข้ารู้สึกหนาวสั่นแปลกๆ มีเงาดำลอยเข้ามา... พิธีล้มเหลวเสียแล้ว!",
                declineDialogue  = "เฮ้อ... ไม่เป็นไร เดี๋ยวข้าลองไปวัดอื่นดู",
                rewardDescription= "เงิน 150 เหรียญ",
                rewardMoney      = 150,
                karmaReward      = 15
            });

            // ─── MEDIUM ─────────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_medium_chant",
                difficulty       = QuestDifficulty.Medium,
                minigameType     = MinigameType.RhythmChantWASD,
                questTitle       = "สวดพระปริตรแก้คุณไสย",
                npcName          = "ป้าสมศรี (แม่ค้า)",
                greetingDialogue = "ท่านผู้ดูแลตำหนัก ช่วยข้าด้วยเถิด! มีคนทำคุณไสยใส่ร้านค้าของข้าจนขายของไม่ได้เลย",
                questDescription = "ช่วยทำพิธีท่องคาถาพระปริตรคุ้มครอง (Rhythm Game W A S D) ขับไล่คุณไสย",
                acceptDialogue   = "สาธุ ขอให้บารมีคุ้มครองร้านของข้าด้วยเถิด ข้าฝากด้วยนะ!",
                completeDialogue = "ยอดเยี่ยมมาก! กลิ่นอายมืดดำสลายไปหมดแล้ว ขอบพระคุณท่านจากใจจริง!",
                failDialogue     = "ว้ายย! ลมกรรโชกแรงมาก สิ่งชั่วร้ายสะท้อนกลับมาแล้ว... หนีเร็ว!",
                declineDialogue  = "โธ่... ข้าคงต้องทนรับเคราะห์ต่อไป",
                rewardDescription= "เงิน 350 เหรียญ",
                rewardMoney      = 350,
                karmaReward      = 25
            });

            // ─── HARD ───────────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_hard_chant",
                difficulty       = QuestDifficulty.Hard,
                minigameType     = MinigameType.RhythmChantWASD,
                questTitle       = "ท่องมหาเวทปราบสัมภเวสี",
                npcName          = "ทิดมั่น (คนทรง)",
                greetingDialogue = "ท่านผู้ดูแล... มีวิญญาณสัมภเวสีอาฆาตตามรังควานข้าไม่ยอมปล่อย ต้องใช้คาถามหาเวทขับไล่!",
                questDescription = "ทำพิธีท่องมหาเวทปราบผีร้าย (Rhythm Game W A S D จังหวะเร็ว) เพื่อสะกดวิญญาณ",
                acceptDialogue   = "เตรียมสมาธิให้ดี จังหวะคาถานี้รวดเร็วและอันตรายมาก!",
                completeDialogue = "สำเร็จแล้ว! วิญญาณร้ายถูกสะกดลงหม้อดินเรียบร้อย ฝีมือท่านยอดเยี่ยมสมคำร่ำลือ",
                failDialogue     = "แย่แล้ว! จิตของท่านหลุด จังหวะคาถาแตก... ผีร้ายตามติดตัวท่านไปแล้ว!",
                declineDialogue  = "ถ้าท่านไม่กล้าเสี่ยง ข้าก็คงต้องหนีต่อไป...",
                rewardDescription= "เงิน 800 เหรียญ",
                rewardMoney      = 800,
                karmaReward      = 40
            });

            // ─── VERY HARD ──────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_veryhard_chant",
                difficulty       = QuestDifficulty.VeryHard,
                minigameType     = MinigameType.RhythmChantWASD,
                questTitle       = "สวดพระมหาคาถาปราบพญามาร",
                npcName          = "หลวงพ่อสงัด (พระอาจารย์)",
                greetingDialogue = "เจริญพรท่านผู้ดูแลตำหนัก... พญามารตนใหญ่กำลังเข้าครอบงำตำหนัก ต้องใช้สมาธิขั้นสูงสวดพระมหาคาถา!",
                questDescription = "สวดพระมหาคาถาปราบพญามารขั้นสูงสุด (Rhythm Game W A S D ระดับยากมาก) ต้องกดให้แม่นยำเพื่อป้องกันอาถรรพ์",
                acceptDialogue   = "ขอตั้งมั่นในคุณพระรัตนตรัย เริ่มสวดพระคาถาได้!",
                completeDialogue = "สาธุ สาธุ! มารร้ายสูญสลาย ตำหนักนี้กลับมาบริสุทธิ์ผุดผ่องอีกครั้ง ท่านคือยอดคนแห่งยุค!",
                failDialogue     = "อนิจจา... พลังมารร้ายกลืนกินพิธีจนสิ้น อาถรรพ์พญามารได้เกาะกุมวิญญาณท่านแล้ว!",
                declineDialogue  = "เป็นเรื่องน่าเสียดายยิ่ง... พลังมารยังคงวนเวียนอยู่",
                rewardDescription= "เงิน 2,000 เหรียญ",
                rewardMoney      = 2000,
                karmaReward      = 60
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

        // ดึงข้อมูลเควสจากรายการโดยใช้ Weighted Random ตามระดับความยาก
        QuestData currentQuest = null;
        if (questList != null && questList.Count > 0)
        {
            currentQuest = PickQuestByWeight();
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
    /// เลือกเควสจาก questList โดยใช้ Weighted Random ตามระดับความยาก
    /// Easy: weightEasy%, Medium: weightMedium%, Hard: weightHard%, VeryHard: weightVeryHard%
    /// </summary>
    private QuestData PickQuestByWeight()
    {
        // แยก pool เควสตามระดับ
        var easy     = questList.FindAll(q => q.difficulty == QuestDifficulty.Easy);
        var medium   = questList.FindAll(q => q.difficulty == QuestDifficulty.Medium);
        var hard     = questList.FindAll(q => q.difficulty == QuestDifficulty.Hard);
        var veryHard = questList.FindAll(q => q.difficulty == QuestDifficulty.VeryHard);

        // สร้าง Weighted pool (ใส่เฉพาะระดับที่มีเควสอยู่)
        int totalWeight = 0;
        if (easy.Count     > 0) totalWeight += weightEasy;
        if (medium.Count   > 0) totalWeight += weightMedium;
        if (hard.Count     > 0) totalWeight += weightHard;
        if (veryHard.Count > 0) totalWeight += weightVeryHard;

        if (totalWeight <= 0)
        {
            // Fallback: วนตามลำดับเดิม
            return questList[currentQueueIndex % questList.Count];
        }

        int roll = Random.Range(0, totalWeight);
        int cursor = 0;

        if (easy.Count > 0)
        {
            cursor += weightEasy;
            if (roll < cursor) return easy[Random.Range(0, easy.Count)];
        }
        if (medium.Count > 0)
        {
            cursor += weightMedium;
            if (roll < cursor) return medium[Random.Range(0, medium.Count)];
        }
        if (hard.Count > 0)
        {
            cursor += weightHard;
            if (roll < cursor) return hard[Random.Range(0, hard.Count)];
        }
        if (veryHard.Count > 0)
        {
            return veryHard[Random.Range(0, veryHard.Count)];
        }

        return questList[currentQueueIndex % questList.Count];
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
