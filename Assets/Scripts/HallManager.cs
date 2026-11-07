using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallManager : MonoBehaviour
{
    public static HallManager Instance { get; private set; }

    [Header("1. สถานะตำหนัก (Hall Status)")]
    [Tooltip("ตำหนักกำลังเปิดรับผู้คนอยู่หรือไม่")]
    public bool isHallOpen = false;
    [Tooltip("เริ่มเปิดตำหนักทันทีเมื่อเริ่มเกม (หากต้องการให้นั่งเก้าอี้ก่อนค่อยเปิด ให้ตั้งเป็น false)")]
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

    [Header("Waypoint Paths (ทางเดิน NPC)")]
    public NPCWaypointPath approachPath;
    public NPCWaypointPath exitPath;

    [Header("3. ข้อมูล NPC & เควส (NPC Prefabs & Quests)")]
    [Tooltip("Prefab ของตัวละคร NPC (ถ้ามี ให้ใส่ที่นี่ หากเว้นว่างไว้ระบบจะสร้างตัวละครจำลองอัตโนมัติ)")]
    public GameObject defaultNpcPrefab;
    [Tooltip("รายการเควสและบทสนทนาที่จะสุ่ม/วนส่ง NPC เข้ามา")]
    public List<QuestData> questList = new List<QuestData>();

    [Header("3.5 ระบบป้องกันเควสออกซ้ำ (No-Repeat Quest System)")]
    [Tooltip("ป้องกันไม่ให้สุ่มได้เควสที่เคยออกไปแล้วในวันเดียวกัน (จะไม่ซ้ำจนกว่าเควสทั้งหมดจะถูกใช้ครบ)")]
    public bool preventDuplicateQuests = true;
    [Tooltip("ห้ามสุ่มได้เควสเดิมซ้ำทันทีติดกันกับคนก่อนหน้า (No consecutive duplicate)")]
    public bool avoidConsecutiveRepeat = true;
    [Tooltip("เติมเควสพื้นฐานเพิ่มอัตโนมัติหากมีเควสในรายการน้อยกว่าจำนวน NPC ต่อวัน")]
    public bool autoSupplementDefaultQuests = true;

    // Runtime tracking สำหรับป้องกันเควสซ้ำ
    private HashSet<string> usedQuestIdsThisSession = new HashSet<string>();
    private string lastPickedQuestId = string.Empty;

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
    public int maxNpcPerSession = 5;

    [Header("5. สถานะปัจจุบัน (Runtime Info)")]
    public int currentQueueIndex = 0;
    public int npcsServedThisSession = 0;
    public NPCController currentActiveNPC;

    [Header("6. พ่อค้าเดินทาง (Merchant NPC Settings)")]
    [Tooltip("เปิด/ปิดระบบพ่อค้าสุ่มเดินเข้ามาในวันเลขคู่ (วันที่ 2, 4, 6, 8...)")]
    public bool enableMerchantOnEvenDays = true;
    [Tooltip("Prefab ของพ่อค้า (หากเว้นว่างจะสร้างตัวละครจำลอง พ่อค้าวัตถุมงคล)")]
    public GameObject merchantNpcPrefab;
    [Tooltip("จุดเกิดของพ่อค้า (ถ้าไม่ใส่จะใช้ spawnPoints[0])")]
    public Transform merchantSpawnPoint;
    [Tooltip("จุดที่พ่อค้าเดินมาหยุดเพื่อเปิดร้านค้า (ถ้าไม่ใส่จะใช้ receptionPoint)")]
    public Transform merchantReceptionPoint;
    [Tooltip("จุดที่พ่อค้าเดินออกไปเมื่อปิดร้าน (ถ้าไม่ใส่จะใช้ exitPoint)")]
    public Transform merchantExitPoint;
    [Tooltip("ทางเดินขาเข้าของพ่อค้า (ถ้าไม่ใส่จะใช้ approachPath)")]
    public NPCWaypointPath merchantApproachPath;
    [Tooltip("ทางเดินขาออกของพ่อค้า (ถ้าไม่ใส่จะใช้ exitPath)")]
    public NPCWaypointPath merchantExitPath;

    [Header("Merchant Runtime Info")]
    public bool isMerchantActive = false;
    public MerchantNPCController currentActiveMerchant;

    // ─────────────────────────────────────────────────────────────────
    [Header("7. Story NPC (ระบบ NPC เรื่อราวข้ามวัน)")]
    [Tooltip("รายชื่อ NPCStoryData ทั้งหมด (ลาก ScriptableObject มาใส่) — หรือใช้ ReturnNPCScheduler.Instance.RegisterStoryNPC()")]
    public System.Collections.Generic.List<NPCStoryData> storyNPCList = new System.Collections.Generic.List<NPCStoryData>();
    [Tooltip("ถ้า false จะเสนอ Story NPC ก่อน NPC ปกติในวันเดียวกัน | true = สลับกันสุ่ม")]
    public bool randomizeStoryWithNormal = false;

    // Runtime queue สำหรับ Story NPC ที่พร้อม spawn วันนี้
    private System.Collections.Generic.Queue<NPCStoryData> pendingStoryNPCQueue
        = new System.Collections.Generic.Queue<NPCStoryData>();

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

        // Auto-create ReputationManager
        if (ReputationManager.Instance == null && FindFirstObjectByType<ReputationManager>() == null)
        {
            GameObject repObj = new GameObject("ReputationManager");
            repObj.AddComponent<ReputationManager>();
        }

        // Auto-create ReturnNPCScheduler และลงทะเบียน Story NPC ที่กำหนดใน Inspector
        if (ReturnNPCScheduler.Instance == null && FindFirstObjectByType<ReturnNPCScheduler>() == null)
        {
            GameObject schedObj = new GameObject("ReturnNPCScheduler");
            ReturnNPCScheduler scheduler = schedObj.AddComponent<ReturnNPCScheduler>();
            foreach (var story in storyNPCList)
                scheduler.RegisterStoryNPC(story);
        }
        else if (ReturnNPCScheduler.Instance != null)
        {
            foreach (var story in storyNPCList)
                ReturnNPCScheduler.Instance.RegisterStoryNPC(story);
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
        List<QuestData> defaultQuests = new List<QuestData>()
        {
            // ─── EASY ─────────────────────────────────────────────────────────────
            new QuestData
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
            },
            new QuestData
            {
                questId          = "quest_timing_cleanse",
                problemType      = "เคราะห์ซ้ำกรรมซัด",
                difficulty       = QuestDifficulty.Easy,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.TimingBarQTE },
                questTitle       = "โปรยข้าวสารเสกปัดรังควาน",
                npcName          = "ยายบัว (คนแก่ท้ายซอย)",
                greetingDialogue = "ยายรู้สึกเหมือนมีอะไรจ้องมองตลอดเวลา เดินสะดุดหกล้มทุกวัน ช่วยทำพิธีโปรยข้าวสารเสกให้ยายทีเถอะ",
                questDescription = "กะจังหวะโปรยข้าวสารเสก (Timing Bar QTE) เพื่อปัดเป่าเสนียดจัญไรและเสริมสิริมงคล",
                waitingDialogue  = "ยายจะนั่งพนมมือรอนะจ๊ะ",
                completeDialogue = "รู้สึกอุ่นวาบและสบายใจขึ้นมากเลย ขอบน้ำใจพ่อหนุ่มมากนะจ๊ะ",
                declineDialogue  = "ไม่เป็นไรจ้ะลูก ยายกลับบ้านก่อนนะ",
                rewardDescription= "เงิน 180 เหรียญ",
                rewardMoney      = 180,
                karmaReward      = 20
            },

            // ─── MEDIUM ───────────────────────────────────────────────────────────
            new QuestData
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
            },
            new QuestData
            {
                questId          = "quest_talisman_protect",
                problemType      = "บ้านร้อนรน / อาถรรพ์ที่อยู่อาศัย",
                difficulty       = QuestDifficulty.Medium,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.TalismanDrawing },
                questTitle       = "เขียนยันต์โสฬสมหามงคล",
                npcName          = "กำนันบุญ",
                greetingDialogue = "ท่านอาจารย์... บ้านข้ามีเสียงดังบนหลังคาทุกคืน คนในบ้านเจ็บป่วยไม่สร่าง อยากให้ช่วยเขียนยันต์คุ้มบ้านให้ที",
                questDescription = "ช่วยเขียนยันต์โสฬสมงคล (Talisman Drawing QTE) เพื่อนำไปติดหน้าประตูบ้านขจัดอวมงคล",
                waitingDialogue  = "ขอบารมีครูบาอาจารย์ช่วยลงอักขระให้เข้มขลังด้วยเถิด",
                completeDialogue = "สาธุ! เส้นอักขระเปล่งรัศมีงดงาม ข้าจะรีบนำไปปิดบูชาที่บ้านทันที ขอบคุณท่านอาจารย์มาก!",
                declineDialogue  = "ถ้าท่านไม่ว่าง ข้าคงต้องหาเกจิท่านอื่นต่อไป",
                rewardDescription= "เงิน 400 เหรียญ",
                rewardMoney      = 400,
                karmaReward      = 25
            },

            // ─── HARD ─────────────────────────────────────────────────────────────
            new QuestData
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
            },
            new QuestData
            {
                questId          = "quest_slash_curse",
                problemType      = "เงาดำตามสิงสู่",
                difficulty       = QuestDifficulty.Hard,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.SequentialSlashQTE },
                questTitle       = "ตวัดมีดหมอปราบเงาอัปมงคล",
                npcName          = "หมอเขียว (หมอยาสมุนไพร)",
                greetingDialogue = "ช่วยด้วยท่าน! ข้าเดินเก็บยาสมุนไพรในป่าช้าแล้วมีเงาดำเกาะติดไหล่ไม่ยอมปล่อย แน่นหน้าอกหายใจไม่ออกแล้ว!",
                questDescription = "ใช้มีดหมอตวัดฟันตัดเงาอาถรรพ์ (Sequential Slash QTE) เพื่อตัดขาดพันธนาการของวิญญาณ",
                waitingDialogue  = "ระวังด้วยนะพ่อหนุ่ม มันเริ่มขยายเงาครอบร่างข้าแล้ว!",
                completeDialogue = "หลุดแล้ว! ข้ารู้สึกโล่งเบาตัวทันที มีดหมอของท่านช่างฉมังนัก ขอบคุณมาก!",
                declineDialogue  = "โอ๊ย... ข้าคงต้องทนทุกข์ต่อไป",
                rewardDescription= "เงิน 750 เหรียญ",
                rewardMoney      = 750,
                karmaReward      = 35
            },

            // ─── VERY HARD ────────────────────────────────────────────────────────
            new QuestData
            {
                questId          = "quest_veryhard_chant",
                problemType      = "พญามารเข้าครอบงำ",
                difficulty       = QuestDifficulty.VeryHard,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "สวดพระมหาคาถาปราบพญามาร",
                npcName          = "หลวงพ่อสงัด (พระอาจารย์)",
                greetingDialogue = "เจริญพรท่านผู้ดูแลตำหนัก... พญามารตนใหญ่กำลังเข้าครอบงำตำหนัก ต้องใช้สมาธิขั้นสูงสวดพระมหาคาถา!",
                questDescription = "สวดพระมหาคาถาปราบพญามารขั้นสูงสุด (Rhythm Game W A S D ระดับยากมาก) ต้องกดให้แม่นยำเพื่อป้องกันอาถรรพณ์",
                waitingDialogue  = "ขอตั้งมั่นในคุณพระรัตนตรัย เริ่มสวดพระคาถาได้!",
                completeDialogue = "สาธุ สาธุ! มารร้ายสูญสลาย ตำหนักนี้กลับมาบริสุทธิ์ผุดผ่องอีกครั้ง ท่านคือยอดคนแห่งยุค!",
                declineDialogue  = "เป็นเรื่องน่าเสียดายยิ่ง... พลังมารยังคงวนเวียนอยู่",
                rewardDescription= "เงิน 2,000 เหรียญ",
                rewardMoney      = 2000,
                karmaReward      = 60
            }
        };

        if (questList == null) questList = new List<QuestData>();

        if (questList.Count == 0)
        {
            questList.AddRange(defaultQuests);
            Debug.Log($"[HallManager] 📜 เพิ่มเควสเริ่มต้น {questList.Count} เควสเรียบร้อยแล้ว");
        }
        else if (autoSupplementDefaultQuests)
        {
            // หากมีเควสเดิมอยู่ในรายการ แต่น้อยกว่าจำนวน NPC สูงสุดต่อวัน ให้เสริมเควสมาตรฐานที่ไม่ซ้ำเข้าไป
            int targetNeeded = Mathf.Max(maxNpcPerSession, 6);
            if (questList.Count < targetNeeded)
            {
                int added = 0;
                foreach (var defQ in defaultQuests)
                {
                    if (questList.Count >= targetNeeded) break;
                    string defId = !string.IsNullOrEmpty(defQ.questId) ? defQ.questId : defQ.problemType;
                    if (!questList.Exists(q => (!string.IsNullOrEmpty(q.questId) && q.questId == defId) || q.problemType == defQ.problemType))
                    {
                        questList.Add(defQ);
                        added++;
                    }
                }
                if (added > 0)
                {
                    Debug.Log($"[HallManager] ➕ เติมเควสเสริม {added} เควส เพื่อป้องกันการสุ่มเควสซ้ำในแต่ละวัน (รวมทั้งหมด {questList.Count} เควส)");
                }
            }
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

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 5;

        // หากวันนี้รับผู้มาเยือนครบแล้ว
        if (npcsServedThisSession >= target)
        {
            Debug.Log("[HallManager] ⚠️ วันนี้รับผู้มาเยือนครบแล้ว ต้องรอขึ้นวันใหม่ (นอนหลับ)");
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification($"<color=#FFD700>🏮 วันนี้รับผู้มาเยือนครบ {target} คนแล้ว</color>\nโปรดไปนอนหลับเพื่อเริ่มวันถัดไป", 3.5f);
            }
            return;
        }

        isHallOpen = true;

        Debug.Log($"[HallManager] 🏮 [เปิดตำหนัก] เริ่มเปิดตำหนักเรียบร้อยแล้ว! กำหนดรับผู้มาเยือน {target} คน...");
        
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"🏮 เปิดตำหนักแล้ว! กำหนดรับผู้มาเยือน <color=#FFD700>{target} คน</color>", 3.5f);
        }

        if (queueCoroutine != null) StopCoroutine(queueCoroutine);

        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        bool isEvenDay = (currentDay % 2 == 0);

        // ดึง Story NPC ที่พร้อมสำหรับวันนี้เข้าคิว
        pendingStoryNPCQueue.Clear();
        if (npcsServedThisSession == 0)
        {
            usedQuestIdsThisSession.Clear();
            lastPickedQuestId = string.Empty;
        }
        if (ReturnNPCScheduler.Instance != null)
        {
            var readyStories = ReturnNPCScheduler.Instance.GetReadyStoryNPCs(currentDay);
            foreach (var story in readyStories)
            {
                pendingStoryNPCQueue.Enqueue(story);
            }
            if (readyStories.Count > 0)
            {
                Debug.Log($"[HallManager] 📖 พบ Story NPC พร้อมเข้าคิว {readyStories.Count} ตัว สำหรับวันที่ {currentDay}");
            }
        }

        if (enableMerchantOnEvenDays && isEvenDay)
        {
            Debug.Log($"[HallManager] 🛒 วันที่ {currentDay} เป็นวันเลขคู่ — พ่อค้าวัตถุมงคลจะเดินทางเข้ารถมาก่อน NPC เควส");
            queueCoroutine = StartCoroutine(SpawnMerchantRoutine(initialSpawnDelay));
        }
        else
        {
            queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(initialSpawnDelay));
        }
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

    /// <summary>
    /// เรียกโดย DayManager เมื่อผู้เล่นกดนอน (ขึ้นวันใหม่)
    /// Reset สถานะ session เพื่อให้เปิดตำหนักได้อีกครั้งในวันถัดไป
    /// </summary>
    public void OnNewDay()
    {
        // ถ้าตำหนักยังเปิดอยู่ให้ปิดก่อน (force)
        if (isHallOpen)
        {
            isHallOpen = false;
            if (queueCoroutine != null)
            {
                StopCoroutine(queueCoroutine);
                queueCoroutine = null;
            }
        }

        npcsServedThisSession = 0;
        currentQueueIndex = 0;
        isMerchantActive = false;
        if (currentActiveMerchant != null)
        {
            Destroy(currentActiveMerchant.gameObject);
            currentActiveMerchant = null;
        }

        if (currentActiveNPC != null)
        {
            Destroy(currentActiveNPC.gameObject);
            currentActiveNPC = null;
        }

        pendingStoryNPCQueue.Clear();
        usedQuestIdsThisSession.Clear();
        lastPickedQuestId = string.Empty;

        // แจ้ง ReturnNPCScheduler ให้ reset ด้วย
        if (ReturnNPCScheduler.Instance != null)
            ReturnNPCScheduler.Instance.OnNewDay();

        Debug.Log("[HallManager] 🌅 ขึ้นวันใหม่ — ตำหนักพร้อมเปิดรับผู้มาเยือนอีกครั้ง");
    }

    private IEnumerator SpawnMerchantRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isHallOpen) yield break;

        Transform spawnPt = merchantSpawnPoint != null ? merchantSpawnPoint : (spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints[0] : null);
        Transform recPt   = merchantReceptionPoint != null ? merchantReceptionPoint : receptionPoint;
        Transform exitPt  = merchantExitPoint != null ? merchantExitPoint : exitPoint;
        NPCWaypointPath appPath = merchantApproachPath != null ? merchantApproachPath : approachPath;
        NPCWaypointPath exPath  = merchantExitPath != null ? merchantExitPath : exitPath;

        if (spawnPt == null)
        {
            Debug.LogError("[HallManager] ❌ ไม่พบจุดเกิดพ่อค้า (Merchant Spawn Point)! สลับไปปล่อย NPC ปกติ");
            queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(0.5f));
            yield break;
        }

        GameObject merchantObj = null;
        if (merchantNpcPrefab != null)
        {
            merchantObj = Instantiate(merchantNpcPrefab, spawnPt.position, spawnPt.rotation);
        }
        else
        {
            merchantObj = CreatePlaceholderMerchant(spawnPt.position, "พ่อค้าวัตถุมงคล");
        }

        MerchantNPCController controller = merchantObj.GetComponent<MerchantNPCController>();
        if (controller == null)
        {
            controller = merchantObj.AddComponent<MerchantNPCController>();
        }

        isMerchantActive = true;
        currentActiveMerchant = controller;

        controller.Initialize(recPt, exitPt, playerTransform, this, appPath, exPath);
        Debug.Log($"[HallManager] 🛒 พ่อค้าวัตถุมงคลเกิดที่ {spawnPt.position} และกำลังเดินเข้ามายังตำหนัก");
    }

    /// <summary>
    /// ทำงานเมื่อพ่อค้าเปิดร้านเสร็จและเดินออกจากตำหนักเรียบร้อย
    /// </summary>
    public void OnMerchantDeparted()
    {
        isMerchantActive = false;
        currentActiveMerchant = null;
        Debug.Log("[HallManager] 🛒 พ่อค้าเดินทางออกจากตำหนักเรียบร้อย — เริ่มส่ง NPC เควสตามปกติ");

        if (isHallOpen)
        {
            if (queueCoroutine != null) StopCoroutine(queueCoroutine);
            queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(delayBetweenNPCs));
        }
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

        // ── Spawn Story NPC ก่อนถ้ายังมีในคิว ───────────────────────────
        bool shouldSpawnStory = false;
        if (pendingStoryNPCQueue.Count > 0)
        {
            if (!randomizeStoryWithNormal)
                shouldSpawnStory = true;
            else
                shouldSpawnStory = (Random.value < 0.5f) || (questList == null || questList.Count == 0);
        }

        if (shouldSpawnStory)
        {
            NPCStoryData nextStory = pendingStoryNPCQueue.Dequeue();
            SpawnStoryNPC(nextStory);
        }
        else
        {
            SpawnSingleNPC();
        }
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

    // ══════════════════════════════════════════════════════════════════
    // Story NPC Spawn
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawn Story NPC (NPC เรื่องราวข้ามวัน) โดยใช้ข้อมูลจาก NPCStoryData
    /// </summary>
    private void SpawnStoryNPC(NPCStoryData storyData)
    {
        if (storyData == null) return;

        StoryPhase phase = storyData.GetCurrentPhase();
        if (phase == null || phase.quest == null)
        {
            Debug.LogWarning($"[HallManager] ⚠️ Story NPC '{storyData.npcName}' Phase {storyData.currentPhaseIndex} ไม่มีข้อมูลเควส (Quest เป็น null)! กรุณาใส่ QuestData ในช่อง Quest ของ Phase ใน Inspector");
            // Fallback: spawn NPC ปกติแทน
            SpawnSingleNPC();
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[HallManager] ❌ ไม่พบจุดเกิด NPC!");
            return;
        }

        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        ReturnNPCScheduler.Instance?.MarkSpawnedToday(storyData, currentDay);

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Clone QuestData จาก Phase แล้วใส่ข้อมูล owner
        QuestData questClone = phase.quest.Clone();
        questClone.npcName     = storyData.npcName;
        questClone.npcPortrait = storyData.npcPortrait;
        questClone.npcGroupId  = storyData.npcGroupId;
        questClone.ownerStoryData  = storyData;
        questClone.ownerPhaseIndex = storyData.currentPhaseIndex;

        // ถ้า Phase มี introDialogue ให้ override greetingDialogue
        if (!string.IsNullOrEmpty(phase.introDialogue))
            questClone.greetingDialogue = phase.introDialogue;

        // Spawn NPC Object
        GameObject npcObj = null;
        GameObject prefabToUse = storyData.npcPrefab != null ? storyData.npcPrefab : defaultNpcPrefab;
        if (prefabToUse != null)
        {
            npcObj = Instantiate(prefabToUse, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            npcObj = CreatePlaceholderNPC(spawnPoint.position, storyData.npcName);
            // ให้สีพิเศษกับ Story NPC ให้สังเกตได้ง่าย (สีม่วงทอง)
            Renderer rend = npcObj.GetComponent<Renderer>();
            if (rend != null) rend.material.color = new Color(0.7f, 0.4f, 1.0f);
        }

        NPCController controller = npcObj.GetComponent<NPCController>();
        if (controller == null) controller = npcObj.AddComponent<NPCController>();

        currentActiveNPC = controller;
        npcsServedThisSession++;

        controller.Initialize(questClone, receptionPoint, exitPoint, playerTransform, this, approachPath, exitPath);

        Debug.Log($"[HallManager] 📖 Story NPC '{storyData.npcName}' Phase {storyData.currentPhaseIndex} เกิดที่ {spawnPoint.position}");
    }

    /// <summary>
    /// ได้รับคีย์ระบุตัวตนของเควสที่ไม่ซ้ำกัน
    /// </summary>
    private string GetQuestIdentifier(QuestData q)
    {
        if (q == null) return string.Empty;
        if (!string.IsNullOrEmpty(q.questId)) return q.questId.Trim();
        if (!string.IsNullOrEmpty(q.questTitle)) return q.questTitle.Trim();
        if (!string.IsNullOrEmpty(q.problemType)) return q.problemType.Trim();
        return q.npcName != null ? q.npcName.Trim() : string.Empty;
    }

    /// <summary>
    /// ตรวจสอบว่าเควสนี้กำลังค้างอยู่ในรายการเควสที่ผู้เล่นรับไว้แล้วหรือไม่ (Active in Quest Tracker)
    /// </summary>
    private bool IsQuestActiveInTracker(QuestData q)
    {
        if (q == null) return false;
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.activeQuests != null)
        {
            string targetId = GetQuestIdentifier(q);
            foreach (var active in QuestUIManager.Instance.activeQuests)
            {
                if (active != null && GetQuestIdentifier(active) == targetId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// ตรวจสอบว่าเควสนี้ถูกสุ่มไปแล้วในรอบนี้ หรือกำลังเปิดทำอยู่หรือไม่
    /// </summary>
    private bool IsQuestUsedOrActive(QuestData q)
    {
        if (q == null) return true;
        string id = GetQuestIdentifier(q);
        if (!string.IsNullOrEmpty(id) && usedQuestIdsThisSession.Contains(id)) return true;
        if (IsQuestActiveInTracker(q)) return true;
        return false;
    }

    /// <summary>
    /// เลือกเควสจาก questList โดยใช้ Weighted Random และป้องกันการออกซ้ำ (No-Repeat Weighted Randomizer)
    /// </summary>
    private QuestData PickQuestByWeight()
    {
        if (questList == null || questList.Count == 0) return null;

        // 1. คัดกรองเควสที่ยังไม่ได้ออกในรอบนี้ และไม่ได้กำลัง Active อยู่ใน Quest Tracker
        List<QuestData> eligiblePool = new List<QuestData>();

        if (preventDuplicateQuests)
        {
            eligiblePool = questList.FindAll(q => q != null && !IsQuestUsedOrActive(q));
        }
        else
        {
            eligiblePool = new List<QuestData>(questList);
        }

        // 2. หากเควสทั้งหมดถูกใช้ไปจนหมดแล้วในรอบนี้ -> รีเซ็ต Pool ใหม่เพื่อวนรอบถัดไป
        if (eligiblePool.Count == 0)
        {
            Debug.Log("[HallManager] 🔄 เควสทั้งหมดในคลังถูกสุ่มใช้งานครบ 1 รอบแล้ว — ทำการรีเซ็ตคลังเควสสำหรับรอบใหม่");
            usedQuestIdsThisSession.Clear();

            // กรองใหม่ โดยห้ามซ้ำกับเควสที่เพิ่งออกไปล่าสุด (avoidConsecutiveRepeat) และห้ามซ้ำกับเควสที่กำลัง Active
            if (avoidConsecutiveRepeat && questList.Count > 1)
            {
                eligiblePool = questList.FindAll(q => q != null && GetQuestIdentifier(q) != lastPickedQuestId && !IsQuestActiveInTracker(q));
            }

            // Fallback ถ้ายังไม่มีตัวเลือก
            if (eligiblePool.Count == 0)
            {
                eligiblePool = questList.FindAll(q => q != null && !IsQuestActiveInTracker(q));
            }
            if (eligiblePool.Count == 0)
            {
                eligiblePool = new List<QuestData>(questList);
            }
        }

        // 3. สุ่มเลือกเควสจาก eligiblePool โดยใช้ Weighted Random ตามระดับความยาก
        QuestData selectedQuest = SelectWeightedFromPool(eligiblePool);

        if (selectedQuest != null)
        {
            string id = GetQuestIdentifier(selectedQuest);
            if (!string.IsNullOrEmpty(id))
            {
                usedQuestIdsThisSession.Add(id);
                lastPickedQuestId = id;
            }
            Debug.Log($"[HallManager] 🎯 สุ่มเควสสำเร็จ (ไม่ซ้ำ): '{selectedQuest.problemType}' โดย {selectedQuest.npcName} [ID: {id}] (ออกไปแล้ว {usedQuestIdsThisSession.Count}/{questList.Count} เควส)");
        }

        return selectedQuest;
    }

    /// <summary>
    /// สุ่มเควสจาก Pool ที่ส่งเข้ามาตามน้ำหนักระดับความยาก
    /// </summary>
    private QuestData SelectWeightedFromPool(List<QuestData> pool)
    {
        if (pool == null || pool.Count == 0) return null;
        if (pool.Count == 1) return pool[0];

        // แยกตามระดับความยากจาก pool ที่กรองแล้ว
        var easy     = pool.FindAll(q => q.difficulty == QuestDifficulty.Easy);
        var medium   = pool.FindAll(q => q.difficulty == QuestDifficulty.Medium);
        var hard     = pool.FindAll(q => q.difficulty == QuestDifficulty.Hard);
        var veryHard = pool.FindAll(q => q.difficulty == QuestDifficulty.VeryHard);

        // สร้าง Weighted pool เฉพาะระดับที่มีเควสอยู่จริงใน eligible pool
        int totalWeight = 0;
        if (easy.Count     > 0) totalWeight += weightEasy;
        if (medium.Count   > 0) totalWeight += weightMedium;
        if (hard.Count     > 0) totalWeight += weightHard;
        if (veryHard.Count > 0) totalWeight += weightVeryHard;

        if (totalWeight <= 0)
        {
            return pool[Random.Range(0, pool.Count)];
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

        return pool[Random.Range(0, pool.Count)];
    }

    [ContextMenu("🔄 ล้างประวัติเควสที่เคยออก (Reset Used Quests)")]
    public void ResetUsedQuests()
    {
        usedQuestIdsThisSession.Clear();
        lastPickedQuestId = string.Empty;
        Debug.Log("[HallManager] 🔄 รีเซ็ตประวัติเควสที่เคยออกเรียบร้อยแล้ว");
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

    /// <summary>
    /// สร้างตัวละครจำลองสำหรับพ่อค้าวัตถุมงคล (Fallback หากไม่มี 3D Model Prefab)
    /// </summary>
    private GameObject CreatePlaceholderMerchant(Vector3 position, string name)
    {
        GameObject merchant = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        merchant.name = $"Merchant_{name}";
        merchant.transform.position = position;
        merchant.transform.localScale = new Vector3(0.95f, 1.85f, 0.95f);

        // สีส้มทองทรงคุณค่า (Gold / Orange)
        Renderer rend = merchant.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(1.0f, 0.75f, 0.1f);
        }

        // ดวงตา/ด้านหน้าจำลองสีเขียวมะนาว
        GameObject headIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headIndicator.name = "FaceIndicator";
        headIndicator.transform.SetParent(merchant.transform);
        headIndicator.transform.localPosition = new Vector3(0, 0.6f, 0.45f);
        headIndicator.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Renderer headRend = headIndicator.GetComponent<Renderer>();
        if (headRend != null) headRend.material.color = new Color(0.2f, 1.0f, 0.4f);
        Destroy(headIndicator.GetComponent<Collider>());

        return merchant;
    }
}
