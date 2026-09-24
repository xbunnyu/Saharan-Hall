using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallManager : MonoBehaviour
{
    public static HallManager Instance { get; private set; }

    [Header("1. สถานะตำหนัก (Hall Status)")]
    [Tooltip("ตำหนักกำลังเปิดรับผู้คนอยู่หรือไม่")]
    public bool isHallOpen = false;
    [Tooltip("เริ่มเปิดตำหนักทันทีเมื่อเริ่มเกม (ตำหนักจะเปิดรับลูกค้าโดยอัตโนมัติ)")]
    public bool openOnStart = true;

    [Header("2. จุดตำแหน่งสำคัญ (Waypoints)")]
    [Tooltip("จุดเกิดของ NPC (สามารถใส่ได้หลายจุดเพื่อสุ่ม)")]
    public Transform[] spawnPoints;
    [Tooltip("จุดที่ NPC จะเดินมาหยุดเพื่อสนทนา/เสนอเควสกับผู้เล่น (หน้าโต๊ะรับแขก)")]
    public Transform receptionPoint;
    [Tooltip("จุดที่ NPC จะเดินไปเพื่อออกจากตำหนักเมื่อสนทนาเสร็จ")]
    public Transform exitPoint;
    [Tooltip("ตำแหน่งของผู้เล่น (ถ้าเว้นว่างจะค้นหาให้อัตโนมัติ)")]
    public Transform playerTransform;

    [Header("2.5 เส้นทางเดิน Waypoint (Optional Pathing)")]
    [Tooltip("เส้นทางเดินขาเข้าสำหรับ NPC (ลากอ็อบเจกต์ที่มีสคริปต์ NPCWaypointPath มาใส่)")]
    public NPCWaypointPath approachPath;
    [Tooltip("เส้นทางเดินขาออกสำหรับ NPC (ลากอ็อบเจกต์ที่มีสคริปต์ NPCWaypointPath มาใส่)")]
    public NPCWaypointPath exitPath;

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
    [Tooltip("จำนวน NPC สูงสุดต่อรอบการเปิดตำหนัก / ในแต่ละวัน (ต้องรับผู้มาเยือนครบก่อนจึงจะปิดตำหนักได้)")]
    public int maxNpcPerSession = 3;

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
                problemType      = "ดวงตก / เคราะห์ร้าย",
                difficulty       = QuestDifficulty.Easy,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "สวดมนต์สะเดาะเคราะห์",
                npcName          = "ลุงสมชาย (ชาวบ้าน)",
                greetingDialogue = "สวัสดีจ้ะพ่อหนุ่ม... ช่วงนี้ข้ารู้สึกดวงตกเหลือเกิน รบกวนช่วยท่องคาถาสะเดาะเคราะห์ให้ข้าทีเถิด",
                questDescription = "ช่วยทำพิธีท่องคาถาสะเดาะเคราะห์ (Rhythm Game W A S D) เพื่อปัดเป่าเคราะห์ร้าย",
                waitingDialogue  = "ขอบใจมากนะพ่อหนุ่ม! ข้าจะตั้งจิตร่วมพิธีเดี๋ยวนี้เลย",
                completeDialogue = "สาธุ! ข้ารู้สึกโล่งใจและดวงเปิดขึ้นทันที ขอบคุณท่านผู้ดูแลตำหนักมาก!",
                declineDialogue  = "เฮ้อ... ไม่เป็นไร เดี๋ยวข้าลองไปวัดอื่นดู",
                rewardDescription= "เงิน 150 เหรียญ",
                rewardMoney      = 150,
                karmaReward      = 15
            });

            // ─── MEDIUM ─────────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_medium_chant",
                problemType      = "โดนทำของ / คุณไสย",
                difficulty       = QuestDifficulty.Medium,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "สวดพระปริตรแก้คุณไสย",
                npcName          = "ป้าสมศรี (แม่ค้า)",
                greetingDialogue = "ท่านผู้ดูแลตำหนัก ช่วยข้าด้วยเถิด! มีคนทำคุณไสยใส่ร้านค้าของข้าจนขายของไม่ได้เลย",
                questDescription = "ช่วยทำพิธีท่องคาถาพระปริตรคุ้มครอง (Rhythm Game W A S D) ขับไล่คุณไสย",
                waitingDialogue  = "สาธุ ขอให้บารมีคุ้มครองร้านของข้าด้วยเถิด ข้าฝากด้วยนะ!",
                completeDialogue = "ยอดเยี่ยมมาก! กลิ่นอายมืดดำสลายไปหมดแล้ว ขอบพระคุณท่านจากใจจริง!",
                declineDialogue  = "โธ่... ข้าคงต้องทนรับเคราะห์ต่อไป",
                rewardDescription= "เงิน 350 เหรียญ",
                rewardMoney      = 350,
                karmaReward      = 25
            });

            // ─── HARD ───────────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_hard_chant",
                problemType      = "โดนวิญญาณอาฆาตตามรังควาน",
                difficulty       = QuestDifficulty.Hard,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "ท่องมหาเวทปราบสัมภเวสี",
                npcName          = "ทิดมั่น (คนทรง)",
                greetingDialogue = "ท่านผู้ดูแล... มีวิญญาณสัมภเวสีอาฆาตตามรังควานข้าไม่ยอมปล่อย ต้องใช้คาถามหาเวทขับไล่!",
                questDescription = "ทำพิธีท่องมหาเวทปราบผีร้าย (Rhythm Game W A S D จังหวะเร็ว) เพื่อสะกดวิญญาณ",
                waitingDialogue  = "เตรียมสมาธิให้ดี จังหวะคาถานี้รวดเร็วและอันตรายมาก!",
                completeDialogue = "สำเร็จแล้ว! วิญญาณร้ายถูกสะกดลงหม้อดินเรียบร้อย ฝีมือท่านยอดเยี่ยมสมคำร่ำลือ",
                declineDialogue  = "ถ้าท่านไม่กล้าเสี่ยง ข้าก็คงต้องหนีต่อไป...",
                rewardDescription= "เงิน 800 เหรียญ",
                rewardMoney      = 800,
                karmaReward      = 40
            });

            // ─── VERY HARD ──────────────────────────────────────
            questList.Add(new QuestData
            {
                questId          = "quest_veryhard_chant",
                problemType      = "พญามารเข้าครอบงำ",
                difficulty       = QuestDifficulty.VeryHard,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "สวดพระมหาคาถาปราบพญามาร",
                npcName          = "หลวงพ่อสงัด (พระอาจารย์)",
                greetingDialogue = "เจริญพรท่านผู้ดูแลตำหนัก... พญามารตนใหญ่กำลังเข้าครอบงำตำหนัก ต้องใช้สมาธิขั้นสูงสวดพระมหาคาถา!",
                questDescription = "สวดพระมหาคาถาปราบพญามารขั้นสูงสุด (Rhythm Game W A S D ระดับยากมาก) ต้องกดให้แม่นยำเพื่อป้องกันอาถรรพ์",
                waitingDialogue  = "ขอตั้งมั่นในคุณพระรัตนตรัย เริ่มสวดพระคาถาได้!",
                completeDialogue = "สาธุ สาธุ! มารร้ายสูญสลาย ตำหนักนี้กลับมาบริสุทธิ์ผุดผ่องอีกครั้ง ท่านคือยอดคนแห่งยุค!",
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

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 3;

        Debug.Log($"[HallManager] 🏮 [เปิดตำหนัก] เริ่มเปิดตำหนักเรียบร้อยแล้ว! กำหนดรับผู้มาเยือน {target} คน...");
        
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"เปิดตำหนักแล้ว! กำหนดรับผู้มาเยือน <color=#FFD700>{target} คน</color>", 3.5f);
        }

        if (queueCoroutine != null) StopCoroutine(queueCoroutine);
        queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(initialSpawnDelay));
    }

    /// <summary>
    /// สั่งปิดตำหนัก (หยุดการปล่อย NPC)
    /// - force = false: ตรวจสอบว่ารับ NPC ครบจำนวนแล้วหรือยัง หากยังไม่ครบจะไม่อนุญาตให้ปิด
    /// - force = true: บังคับปิดตำหนัก (เช่น เมื่อรับครบตามระบบอัตโนมัติ)
    /// </summary>
    public bool CloseHall(bool force = false)
    {
        if (!isHallOpen) return false;

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 3;

        // หากยังให้บริการ NPC ไม่ครบตามจำนวน และไม่ได้สั่งบังคับปิด (force = true) -> ห้ามปิดตำหนัก
        if (!force && npcsServedThisSession < target)
        {
            string warnMsg = $"<color=#FF4500>❌ ยังปิดตำหนักไม่ได้!</color>\nต้องรับผู้มาเยือนให้ครบก่อน (<color=#FFD700>{npcsServedThisSession}/{target} คน</color>)";
            
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(warnMsg, 3.5f);
            }

            Debug.Log($"[HallManager] 🔒 ปิดตำหนักไม่ได้ เนื่องจากบริการไปเพียง {npcsServedThisSession}/{target} คน");
            return false;
        }

        isHallOpen = false;
        if (queueCoroutine != null)
        {
            StopCoroutine(queueCoroutine);
            queueCoroutine = null;
        }

        Debug.Log("[HallManager] 🚪 ปิดตำหนักเรียบร้อยแล้ว");
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"<color=#00FF7F>🚪 ปิดตำหนักเรียบร้อยแล้ว!</color> (ให้บริการผู้มาเยือนครบ {npcsServedThisSession}/{target} คน)", 3.5f);
        }

        return true;
    }

    /// <summary>
    /// สลับสถานะเปิด/ปิดตำหนัก
    /// </summary>
    public bool ToggleHall()
    {
        if (isHallOpen) return CloseHall(false);
        else { OpenHall(); return true; }
    }

    private IEnumerator SpawnNextNPCRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isHallOpen) yield break;

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 3;

        // ตรวจสอบลิมิตต่อรอบ
        if (npcsServedThisSession >= target)
        {
            if (currentActiveNPC == null)
            {
                Debug.Log($"[HallManager] 🏁 ครบจำนวนผู้มาเยือนในรอบนี้แล้ว ({npcsServedThisSession}/{target}) กำลังปิดตำหนักอัตโนมัติ");
                CloseHall(true);
                yield break;
            }
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

        // เริ่มต้นให้ NPC เดินเข้าหาจุดรับแขกตามเส้นทาง Waypoint
        controller.Initialize(currentQuest, receptionPoint, exitPoint, playerTransform, this, approachPath, exitPath);

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

}

