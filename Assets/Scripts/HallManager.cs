using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallManager : MonoBehaviour
{
    public static HallManager Instance { get; private set; }

    [Header("1. เธชเธ–เธฒเธเธฐเธ•เธณเธซเธเธฑเธ (Hall Status)")]
    [Tooltip("เธ•เธณเธซเธเธฑเธเธเธณเธฅเธฑเธเน€เธเธดเธ”เธฃเธฑเธเธเธนเนเธเธเธญเธขเธนเนเธซเธฃเธทเธญเนเธกเน")]
    public bool isHallOpen = false;
    [Tooltip("เน€เธฃเธดเนเธกเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธเธ—เธฑเธเธ—เธตเน€เธกเธทเนเธญเน€เธฃเธดเนเธกเน€เธเธก (เธ•เธณเธซเธเธฑเธเธเธฐเน€เธเธดเธ”เธฃเธฑเธเธฅเธนเธเธเนเธฒเนเธ”เธขเธญเธฑเธ•เนเธเธกเธฑเธ•เธด)")]
    public bool openOnStart = true;

    [Header("2. เธเธธเธ”เธ•เธณเนเธซเธเนเธเธชเธณเธเธฑเธ (Waypoints)")]
    [Tooltip("เธเธธเธ”เน€เธเธดเธ”เธเธญเธ NPC (เธชเธฒเธกเธฒเธฃเธ–เนเธชเนเนเธ”เนเธซเธฅเธฒเธขเธเธธเธ”เน€เธเธทเนเธญเธชเธธเนเธก)")]
    public Transform[] spawnPoints;
    [Tooltip("เธเธธเธ”เธ—เธตเน NPC เธเธฐเน€เธ”เธดเธเธกเธฒเธซเธขเธธเธ”เน€เธเธทเนเธญเธชเธเธ—เธเธฒ/เน€เธชเธเธญเน€เธเธงเธชเธเธฑเธเธเธนเนเน€เธฅเนเธ (เธซเธเนเธฒเนเธ•เนเธฐเธฃเธฑเธเนเธเธ)")]
    public Transform receptionPoint;
    [Tooltip("เธเธธเธ”เธ—เธตเน NPC เธเธฐเน€เธ”เธดเธเนเธเน€เธเธทเนเธญเธญเธญเธเธเธฒเธเธ•เธณเธซเธเธฑเธเน€เธกเธทเนเธญเธชเธเธ—เธเธฒเน€เธชเธฃเนเธ")]
    public Transform exitPoint;
    [Tooltip("เธ•เธณเนเธซเธเนเธเธเธญเธเธเธนเนเน€เธฅเนเธ (เธ–เนเธฒเน€เธงเนเธเธงเนเธฒเธเธเธฐเธเนเธเธซเธฒเนเธซเนเธญเธฑเธ•เนเธเธกเธฑเธ•เธด)")]
    public Transform playerTransform;

    [Header("3. เธเนเธญเธกเธนเธฅ NPC & เน€เธเธงเธช (NPC Prefabs & Quests)")]
    [Tooltip("Prefab เธเธญเธเธ•เธฑเธงเธฅเธฐเธเธฃ NPC (เธ–เนเธฒเธกเธต เนเธซเนเนเธชเนเธ—เธตเนเธเธตเน เธซเธฒเธเน€เธงเนเธเธงเนเธฒเธเนเธงเนเธฃเธฐเธเธเธเธฐเธชเธฃเนเธฒเธเธ•เธฑเธงเธฅเธฐเธเธฃเธเธณเธฅเธญเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธด)")]
    public GameObject defaultNpcPrefab;
    [Tooltip("เธฃเธฒเธขเธเธฒเธฃเน€เธเธงเธชเนเธฅเธฐเธเธ—เธชเธเธ—เธเธฒเธ—เธตเนเธเธฐเธชเธธเนเธก/เธงเธเธชเนเธ NPC เน€เธเนเธฒเธกเธฒ")]
    public List<QuestData> questList = new List<QuestData>();

    [Header("5.5 Difficulty Weight (เธเนเธณเธซเธเธฑเธเธเธฒเธฃเธชเธธเนเธกเธฃเธฐเธ”เธฑเธเธเธงเธฒเธกเธขเธฒเธ)")]
    [Tooltip("เธเนเธณเธซเธเธฑเธเธเธฒเธฃเธชเธธเนเธกเน€เธเธงเธชเนเธ•เนเธฅเธฐเธฃเธฐเธ”เธฑเธ (เธเธฅเธฃเธงเธกเนเธกเนเธเธณเน€เธเนเธเธ•เนเธญเธเน€เธ—เนเธฒเธเธฑเธ 100)")]
    [Range(0, 100)] public int weightEasy     = 40;   // 40% เธเนเธฒเธข
    [Range(0, 100)] public int weightMedium   = 35;   // 35% เธเธฒเธเธเธฅเธฒเธ
    [Range(0, 100)] public int weightHard     = 20;   // 20% เธขเธฒเธ
    [Range(0, 100)] public int weightVeryHard = 5;    //  5% เธขเธฒเธเธกเธฒเธ

    [Header("4. เธเธฒเธฃเธ•เธฑเนเธเธเนเธฒเธฃเธญเธเธเธฒเธฃเธเธฅเนเธญเธข NPC (Queue Settings)")]
    [Tooltip("เธฃเธฐเธขเธฐเน€เธงเธฅเธฒเธซเธเนเธงเธเธเนเธญเธเธเธฅเนเธญเธข NPC เธเธเนเธฃเธเน€เธกเธทเนเธญเน€เธฃเธดเนเธกเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ (เธงเธดเธเธฒเธ—เธต)")]
    public float initialSpawnDelay = 1.5f;
    [Tooltip("เธฃเธฐเธขเธฐเน€เธงเธฅเธฒเธซเธเนเธงเธเธเนเธญเธเธเธฅเนเธญเธข NPC เธเธเธ–เธฑเธ”เนเธเธซเธฅเธฑเธเธเธฒเธเธเธเธเนเธญเธเธซเธเนเธฒเน€เธ”เธดเธเธญเธญเธเนเธ (เธงเธดเธเธฒเธ—เธต)")]
    public float delayBetweenNPCs = 2.0f;
    [Tooltip("เธเธณเธเธงเธ NPC เธชเธนเธเธชเธธเธ”เธ•เนเธญเธฃเธญเธเธเธฒเธฃเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ / เนเธเนเธ•เนเธฅเธฐเธงเธฑเธ (เธ•เนเธญเธเธฃเธฑเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธเธเธฃเธเธเนเธญเธเธเธถเธเธเธฐเธเธดเธ”เธ•เธณเธซเธเธฑเธเนเธ”เน)")]
    public int maxNpcPerSession = 3;

    [Header("5. เธชเธ–เธฒเธเธฐเธเธฑเธเธเธธเธเธฑเธ (Runtime Info)")]
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
        // เธเนเธเธซเธฒเธ•เธณเนเธซเธเนเธเธเธนเนเน€เธฅเนเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธดเธซเธฒเธเธขเธฑเธเนเธกเนเนเธ”เนเธเธณเธซเธเธ”
        if (playerTransform == null)
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null) playerTransform = player.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        // เธ•เธฃเธงเธเธชเธญเธเนเธฅเธฐเธชเธฃเนเธฒเธ Manager เธชเธณเธเธฑเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธดเธซเธฒเธเธขเธฑเธเนเธกเนเธกเธตเนเธเธเธฒเธ
        SetupRequiredManagers();

        // เธชเธฃเนเธฒเธเธเธธเธ” Reception เนเธฅเธฐ Spawn เธเธณเธฅเธญเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธดเธซเธฒเธเธขเธฑเธเนเธกเนเนเธ”เนเธเธณเธซเธเธ”เนเธเธเธฒเธ
        SetupDefaultWaypointsIfMissing();

        // เนเธชเนเธฃเธฒเธขเธเธฒเธฃเน€เธเธงเธชเน€เธฃเธดเนเธกเธ•เนเธเธเธณเธฅเธญเธเธซเธฒเธเธขเธฑเธเนเธกเนเธกเธตเนเธ List
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
            // โ”€โ”€โ”€ EASY โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
            questList.Add(new QuestData
            {
                questId          = "quest_easy_chant",
                problemType      = "เธ”เธงเธเธ•เธ / เน€เธเธฃเธฒเธฐเธซเนเธฃเนเธฒเธข",
                difficulty       = QuestDifficulty.Easy,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "เธชเธงเธ”เธกเธเธ•เนเธชเธฐเน€เธ”เธฒเธฐเน€เธเธฃเธฒเธฐเธซเน",
                npcName          = "เธฅเธธเธเธชเธกเธเธฒเธข (เธเธฒเธงเธเนเธฒเธ)",
                greetingDialogue = "เธชเธงเธฑเธชเธ”เธตเธเนเธฐเธเนเธญเธซเธเธธเนเธก... เธเนเธงเธเธเธตเนเธเนเธฒเธฃเธนเนเธชเธถเธเธ”เธงเธเธ•เธเน€เธซเธฅเธทเธญเน€เธเธดเธ เธฃเธเธเธงเธเธเนเธงเธขเธ—เนเธญเธเธเธฒเธ–เธฒเธชเธฐเน€เธ”เธฒเธฐเน€เธเธฃเธฒเธฐเธซเนเนเธซเนเธเนเธฒเธ—เธตเน€เธ–เธดเธ”",
                questDescription = "เธเนเธงเธขเธ—เธณเธเธดเธเธตเธ—เนเธญเธเธเธฒเธ–เธฒเธชเธฐเน€เธ”เธฒเธฐเน€เธเธฃเธฒเธฐเธซเน (Rhythm Game W A S D) เน€เธเธทเนเธญเธเธฑเธ”เน€เธเนเธฒเน€เธเธฃเธฒเธฐเธซเนเธฃเนเธฒเธข",
                waitingDialogue  = "เธเธญเธเนเธเธกเธฒเธเธเธฐเธเนเธญเธซเธเธธเนเธก! เธเนเธฒเธเธฐเธ•เธฑเนเธเธเธดเธ•เธฃเนเธงเธกเธเธดเธเธตเน€เธ”เธตเนเธขเธงเธเธตเนเน€เธฅเธข",
                completeDialogue = "เธชเธฒเธเธธ! เธเนเธฒเธฃเธนเนเธชเธถเธเนเธฅเนเธเนเธเนเธฅเธฐเธ”เธงเธเน€เธเธดเธ”เธเธถเนเธเธ—เธฑเธเธ—เธต เธเธญเธเธเธธเธ“เธ—เนเธฒเธเธเธนเนเธ”เธนเนเธฅเธ•เธณเธซเธเธฑเธเธกเธฒเธ!",
                declineDialogue  = "เน€เธฎเนเธญ... เนเธกเนเน€เธเนเธเนเธฃ เน€เธ”เธตเนเธขเธงเธเนเธฒเธฅเธญเธเนเธเธงเธฑเธ”เธญเธทเนเธเธ”เธน",
                rewardDescription= "เน€เธเธดเธ 150 เน€เธซเธฃเธตเธขเธ",
                rewardMoney      = 150,
                karmaReward      = 15
            });

            // โ”€โ”€โ”€ MEDIUM โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
            questList.Add(new QuestData
            {
                questId          = "quest_medium_chant",
                problemType      = "เนเธ”เธเธ—เธณเธเธญเธ / เธเธธเธ“เนเธชเธข",
                difficulty       = QuestDifficulty.Medium,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "เธชเธงเธ”เธเธฃเธฐเธเธฃเธดเธ•เธฃเนเธเนเธเธธเธ“เนเธชเธข",
                npcName          = "เธเนเธฒเธชเธกเธจเธฃเธต (เนเธกเนเธเนเธฒ)",
                greetingDialogue = "เธ—เนเธฒเธเธเธนเนเธ”เธนเนเธฅเธ•เธณเธซเธเธฑเธ เธเนเธงเธขเธเนเธฒเธ”เนเธงเธขเน€เธ–เธดเธ”! เธกเธตเธเธเธ—เธณเธเธธเธ“เนเธชเธขเนเธชเนเธฃเนเธฒเธเธเนเธฒเธเธญเธเธเนเธฒเธเธเธเธฒเธขเธเธญเธเนเธกเนเนเธ”เนเน€เธฅเธข",
                questDescription = "เธเนเธงเธขเธ—เธณเธเธดเธเธตเธ—เนเธญเธเธเธฒเธ–เธฒเธเธฃเธฐเธเธฃเธดเธ•เธฃเธเธธเนเธกเธเธฃเธญเธ (Rhythm Game W A S D) เธเธฑเธเนเธฅเนเธเธธเธ“เนเธชเธข",
                waitingDialogue  = "เธชเธฒเธเธธ เธเธญเนเธซเนเธเธฒเธฃเธกเธตเธเธธเนเธกเธเธฃเธญเธเธฃเนเธฒเธเธเธญเธเธเนเธฒเธ”เนเธงเธขเน€เธ–เธดเธ” เธเนเธฒเธเธฒเธเธ”เนเธงเธขเธเธฐ!",
                completeDialogue = "เธขเธญเธ”เน€เธขเธตเนเธขเธกเธกเธฒเธ! เธเธฅเธดเนเธเธญเธฒเธขเธกเธทเธ”เธ”เธณเธชเธฅเธฒเธขเนเธเธซเธกเธ”เนเธฅเนเธง เธเธญเธเธเธฃเธฐเธเธธเธ“เธ—เนเธฒเธเธเธฒเธเนเธเธเธฃเธดเธ!",
                declineDialogue  = "เนเธเน... เธเนเธฒเธเธเธ•เนเธญเธเธ—เธเธฃเธฑเธเน€เธเธฃเธฒเธฐเธซเนเธ•เนเธญเนเธ",
                rewardDescription= "เน€เธเธดเธ 350 เน€เธซเธฃเธตเธขเธ",
                rewardMoney      = 350,
                karmaReward      = 25
            });

            // โ”€โ”€โ”€ HARD โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
            questList.Add(new QuestData
            {
                questId          = "quest_hard_chant",
                problemType      = "เนเธ”เธเธงเธดเธเธเธฒเธ“เธญเธฒเธเธฒเธ•เธ•เธฒเธกเธฃเธฑเธเธเธงเธฒเธ",
                difficulty       = QuestDifficulty.Hard,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "เธ—เนเธญเธเธกเธซเธฒเน€เธงเธ—เธเธฃเธฒเธเธชเธฑเธกเธ เน€เธงเธชเธต",
                npcName          = "เธ—เธดเธ”เธกเธฑเนเธ (เธเธเธ—เธฃเธ)",
                greetingDialogue = "เธ—เนเธฒเธเธเธนเนเธ”เธนเนเธฅ... เธกเธตเธงเธดเธเธเธฒเธ“เธชเธฑเธกเธ เน€เธงเธชเธตเธญเธฒเธเธฒเธ•เธ•เธฒเธกเธฃเธฑเธเธเธงเธฒเธเธเนเธฒเนเธกเนเธขเธญเธกเธเธฅเนเธญเธข เธ•เนเธญเธเนเธเนเธเธฒเธ–เธฒเธกเธซเธฒเน€เธงเธ—เธเธฑเธเนเธฅเน!",
                questDescription = "เธ—เธณเธเธดเธเธตเธ—เนเธญเธเธกเธซเธฒเน€เธงเธ—เธเธฃเธฒเธเธเธตเธฃเนเธฒเธข (Rhythm Game W A S D เธเธฑเธเธซเธงเธฐเน€เธฃเนเธง) เน€เธเธทเนเธญเธชเธฐเธเธ”เธงเธดเธเธเธฒเธ“",
                waitingDialogue  = "เน€เธ•เธฃเธตเธขเธกเธชเธกเธฒเธเธดเนเธซเนเธ”เธต เธเธฑเธเธซเธงเธฐเธเธฒเธ–เธฒเธเธตเนเธฃเธงเธ”เน€เธฃเนเธงเนเธฅเธฐเธญเธฑเธเธ•เธฃเธฒเธขเธกเธฒเธ!",
                completeDialogue = "เธชเธณเน€เธฃเนเธเนเธฅเนเธง! เธงเธดเธเธเธฒเธ“เธฃเนเธฒเธขเธ–เธนเธเธชเธฐเธเธ”เธฅเธเธซเธกเนเธญเธ”เธดเธเน€เธฃเธตเธขเธเธฃเนเธญเธข เธเธตเธกเธทเธญเธ—เนเธฒเธเธขเธญเธ”เน€เธขเธตเนเธขเธกเธชเธกเธเธณเธฃเนเธณเธฅเธทเธญ",
                declineDialogue  = "เธ–เนเธฒเธ—เนเธฒเธเนเธกเนเธเธฅเนเธฒเน€เธชเธตเนเธขเธ เธเนเธฒเธเนเธเธเธ•เนเธญเธเธซเธเธตเธ•เนเธญเนเธ...",
                rewardDescription= "เน€เธเธดเธ 800 เน€เธซเธฃเธตเธขเธ",
                rewardMoney      = 800,
                karmaReward      = 40
            });

            // โ”€โ”€โ”€ VERY HARD โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
            questList.Add(new QuestData
            {
                questId          = "quest_veryhard_chant",
                problemType      = "เธเธเธฒเธกเธฒเธฃเน€เธเนเธฒเธเธฃเธญเธเธเธณ",
                difficulty       = QuestDifficulty.VeryHard,
                requiredMinigameSequence = new List<MinigameType> { MinigameType.RhythmChantWASD },
                questTitle       = "เธชเธงเธ”เธเธฃเธฐเธกเธซเธฒเธเธฒเธ–เธฒเธเธฃเธฒเธเธเธเธฒเธกเธฒเธฃ",
                npcName          = "เธซเธฅเธงเธเธเนเธญเธชเธเธฑเธ” (เธเธฃเธฐเธญเธฒเธเธฒเธฃเธขเน)",
                greetingDialogue = "เน€เธเธฃเธดเธเธเธฃเธ—เนเธฒเธเธเธนเนเธ”เธนเนเธฅเธ•เธณเธซเธเธฑเธ... เธเธเธฒเธกเธฒเธฃเธ•เธเนเธซเธเนเธเธณเธฅเธฑเธเน€เธเนเธฒเธเธฃเธญเธเธเธณเธ•เธณเธซเธเธฑเธ เธ•เนเธญเธเนเธเนเธชเธกเธฒเธเธดเธเธฑเนเธเธชเธนเธเธชเธงเธ”เธเธฃเธฐเธกเธซเธฒเธเธฒเธ–เธฒ!",
                questDescription = "เธชเธงเธ”เธเธฃเธฐเธกเธซเธฒเธเธฒเธ–เธฒเธเธฃเธฒเธเธเธเธฒเธกเธฒเธฃเธเธฑเนเธเธชเธนเธเธชเธธเธ” (Rhythm Game W A S D เธฃเธฐเธ”เธฑเธเธขเธฒเธเธกเธฒเธ) เธ•เนเธญเธเธเธ”เนเธซเนเนเธกเนเธเธขเธณเน€เธเธทเนเธญเธเนเธญเธเธเธฑเธเธญเธฒเธ–เธฃเธฃเธเน",
                waitingDialogue  = "เธเธญเธ•เธฑเนเธเธกเธฑเนเธเนเธเธเธธเธ“เธเธฃเธฐเธฃเธฑเธ•เธเธ•เธฃเธฑเธข เน€เธฃเธดเนเธกเธชเธงเธ”เธเธฃเธฐเธเธฒเธ–เธฒเนเธ”เน!",
                completeDialogue = "เธชเธฒเธเธธ เธชเธฒเธเธธ! เธกเธฒเธฃเธฃเนเธฒเธขเธชเธนเธเธชเธฅเธฒเธข เธ•เธณเธซเธเธฑเธเธเธตเนเธเธฅเธฑเธเธกเธฒเธเธฃเธดเธชเธธเธ—เธเธดเนเธเธธเธ”เธเนเธญเธเธญเธตเธเธเธฃเธฑเนเธ เธ—เนเธฒเธเธเธทเธญเธขเธญเธ”เธเธเนเธซเนเธเธขเธธเธ!",
                declineDialogue  = "เน€เธเนเธเน€เธฃเธทเนเธญเธเธเนเธฒเน€เธชเธตเธขเธ”เธฒเธขเธขเธดเนเธ... เธเธฅเธฑเธเธกเธฒเธฃเธขเธฑเธเธเธเธงเธเน€เธงเธตเธขเธเธญเธขเธนเน",
                rewardDescription= "เน€เธเธดเธ 2,000 เน€เธซเธฃเธตเธขเธ",
                rewardMoney      = 2000,
                karmaReward      = 60
            });
        }
    }

    /// <summary>
    /// เธชเธฑเนเธเน€เธฃเธดเนเธกเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ (NPC เธเธฐเน€เธฃเธดเนเธกเธ—เธขเธญเธขเน€เธ”เธดเธเน€เธเนเธฒเธกเธฒเธ—เธตเธฅเธฐเธเธ)
    /// </summary>
    /// <summary>
    /// เธชเธฑเนเธเน€เธฃเธดเนเธกเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ (NPC เธเธฐเน€เธฃเธดเนเธกเธ—เธขเธญเธขเน€เธ”เธดเธเน€เธเนเธฒเธกเธฒเธ—เธตเธฅเธฐเธเธ)
    /// </summary>
    public void OpenHall()
    {
        if (isHallOpen)
        {
            Debug.Log("[HallManager] โ ๏ธ เธ•เธณเธซเธเธฑเธเน€เธเธดเธ”เธญเธขเธนเนเนเธฅเนเธง");
            return;
        }

        isHallOpen = true;
        npcsServedThisSession = 0;
        currentQueueIndex = 0;

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 3;

        Debug.Log($"[HallManager] ๐ฎ [เน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ] เน€เธฃเธดเนเธกเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธเน€เธฃเธตเธขเธเธฃเนเธญเธขเนเธฅเนเธง! เธเธณเธซเธเธ”เธฃเธฑเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธ {target} เธเธ...");
        
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"เน€เธเธดเธ”เธ•เธณเธซเธเธฑเธเนเธฅเนเธง! เธเธณเธซเธเธ”เธฃเธฑเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธ <color=#FFD700>{target} เธเธ</color>", 3.5f);
        }

        if (queueCoroutine != null) StopCoroutine(queueCoroutine);
        queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(initialSpawnDelay));
    }

    /// <summary>
    /// เธชเธฑเนเธเธเธดเธ”เธ•เธณเธซเธเธฑเธ (เธซเธขเธธเธ”เธเธฒเธฃเธเธฅเนเธญเธข NPC)
    /// - force = false: เธ•เธฃเธงเธเธชเธญเธเธงเนเธฒเธฃเธฑเธ NPC เธเธฃเธเธเธณเธเธงเธเนเธฅเนเธงเธซเธฃเธทเธญเธขเธฑเธ เธซเธฒเธเธขเธฑเธเนเธกเนเธเธฃเธเธเธฐเนเธกเนเธญเธเธธเธเธฒเธ•เนเธซเนเธเธดเธ”
    /// - force = true: เธเธฑเธเธเธฑเธเธเธดเธ”เธ•เธณเธซเธเธฑเธ (เน€เธเนเธ เน€เธกเธทเนเธญเธฃเธฑเธเธเธฃเธเธ•เธฒเธกเธฃเธฐเธเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธด)
    /// </summary>
    public bool CloseHall(bool force = false)
    {
        if (!isHallOpen) return false;

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 3;

        // เธซเธฒเธเธขเธฑเธเนเธซเนเธเธฃเธดเธเธฒเธฃ NPC เนเธกเนเธเธฃเธเธ•เธฒเธกเธเธณเธเธงเธ เนเธฅเธฐเนเธกเนเนเธ”เนเธชเธฑเนเธเธเธฑเธเธเธฑเธเธเธดเธ” (force = true) -> เธซเนเธฒเธกเธเธดเธ”เธ•เธณเธซเธเธฑเธ
        if (!force && npcsServedThisSession < target)
        {
            string warnMsg = $"<color=#FF4500>โ เธขเธฑเธเธเธดเธ”เธ•เธณเธซเธเธฑเธเนเธกเนเนเธ”เน!</color>\nเธ•เนเธญเธเธฃเธฑเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธเนเธซเนเธเธฃเธเธเนเธญเธ (<color=#FFD700>{npcsServedThisSession}/{target} เธเธ</color>)";
            
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(warnMsg, 3.5f);
            }

            Debug.Log($"[HallManager] ๐”’ เธเธดเธ”เธ•เธณเธซเธเธฑเธเนเธกเนเนเธ”เน เน€เธเธทเนเธญเธเธเธฒเธเธเธฃเธดเธเธฒเธฃเนเธเน€เธเธตเธขเธ {npcsServedThisSession}/{target} เธเธ");
            return false;
        }

        isHallOpen = false;
        if (queueCoroutine != null)
        {
            StopCoroutine(queueCoroutine);
            queueCoroutine = null;
        }

        Debug.Log("[HallManager] ๐ช เธเธดเธ”เธ•เธณเธซเธเธฑเธเน€เธฃเธตเธขเธเธฃเนเธญเธขเนเธฅเนเธง");
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"<color=#00FF7F>๐ช เธเธดเธ”เธ•เธณเธซเธเธฑเธเน€เธฃเธตเธขเธเธฃเนเธญเธขเนเธฅเนเธง!</color> (เนเธซเนเธเธฃเธดเธเธฒเธฃเธเธนเนเธกเธฒเน€เธขเธทเธญเธเธเธฃเธ {npcsServedThisSession}/{target} เธเธ)", 3.5f);
        }

        return true;
    }

    /// <summary>
    /// เธชเธฅเธฑเธเธชเธ–เธฒเธเธฐเน€เธเธดเธ”/เธเธดเธ”เธ•เธณเธซเธเธฑเธ
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
        Debug.Log("[HallManager] 🌅 ขึ้นวันใหม่ — ตำหนักพร้อมเปิดรับผู้มาเยือนอีกครั้ง");
    }

    private IEnumerator SpawnNextNPCRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isHallOpen) yield break;

        int target = maxNpcPerSession > 0 ? maxNpcPerSession : 3;

        // เธ•เธฃเธงเธเธชเธญเธเธฅเธดเธกเธดเธ•เธ•เนเธญเธฃเธญเธ
        if (npcsServedThisSession >= target)
        {
            if (currentActiveNPC == null)
            {
                Debug.Log($"[HallManager] ๐ เธเธฃเธเธเธณเธเธงเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธเนเธเธฃเธญเธเธเธตเนเนเธฅเนเธง ({npcsServedThisSession}/{target}) เธเธณเธฅเธฑเธเธเธดเธ”เธ•เธณเธซเธเธฑเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธด");
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
            Debug.LogError("[HallManager] โ เนเธกเนเธเธเธเธธเธ”เน€เธเธดเธ” NPC (Spawn Points)!");
            return;
        }

        // เธชเธธเนเธกเธเธธเธ”เน€เธเธดเธ”
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // เธ”เธถเธเธเนเธญเธกเธนเธฅเน€เธเธงเธชเธเธฒเธเธฃเธฒเธขเธเธฒเธฃเนเธ”เธขเนเธเน Weighted Random เธ•เธฒเธกเธฃเธฐเธ”เธฑเธเธเธงเธฒเธกเธขเธฒเธ
        QuestData currentQuest = null;
        if (questList != null && questList.Count > 0)
        {
            currentQuest = PickQuestByWeight();
            currentQueueIndex++;
        }

        GameObject npcObj = null;

        // เธชเธฃเนเธฒเธ NPC เธเธฒเธ Prefab เธซเธฃเธทเธญเธชเธฃเนเธฒเธเธ•เธฑเธงเธฅเธฐเธเธฃเธเธณเธฅเธญเธ (Capsule Humanoid)
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

        // เน€เธฃเธดเนเธกเธ•เนเธเนเธซเน NPC เน€เธ”เธดเธเน€เธเนเธฒเธซเธฒเธเธธเธ”เธฃเธฑเธเนเธเธ
        controller.Initialize(currentQuest, receptionPoint, exitPoint, playerTransform, this);

        Debug.Log($"[HallManager] ๐‘ค NPC '{currentQuest?.npcName}' เน€เธเธดเธ”เธ—เธตเน {spawnPoint.position} เนเธฅเธฐเธเธณเธฅเธฑเธเน€เธ”เธดเธเน€เธเนเธฒเธกเธฒเธ—เธตเนเธเธธเธ”เธฃเธฑเธเนเธเธ");
    }

    /// <summary>
    /// เธ—เธณเธเธฒเธเน€เธกเธทเนเธญ NPC เธเธฑเธเธเธธเธเธฑเธเธเธธเธขเน€เธชเธฃเนเธเนเธฅเธฐเน€เธ”เธดเธเธญเธญเธเธเธฒเธเธ•เธณเธซเธเธฑเธเนเธฅเนเธง
    /// </summary>
    public void OnNPCDeparted(NPCController npc)
    {
        if (currentActiveNPC == npc)
        {
            currentActiveNPC = null;
        }

        if (isHallOpen)
        {
            Debug.Log($"[HallManager] โณ NPC เน€เธ”เธดเธเธญเธญเธเน€เธฃเธตเธขเธเธฃเนเธญเธข เธเธฐเธชเนเธเธเธเธ–เธฑเธ”เนเธเน€เธเนเธฒเธกเธฒเนเธเธญเธตเธ {delayBetweenNPCs} เธงเธดเธเธฒเธ—เธต");
            if (queueCoroutine != null) StopCoroutine(queueCoroutine);
            queueCoroutine = StartCoroutine(SpawnNextNPCRoutine(delayBetweenNPCs));
        }
    }

    /// <summary>
    /// เน€เธฅเธทเธญเธเน€เธเธงเธชเธเธฒเธ questList เนเธ”เธขเนเธเน Weighted Random เธ•เธฒเธกเธฃเธฐเธ”เธฑเธเธเธงเธฒเธกเธขเธฒเธ
    /// Easy: weightEasy%, Medium: weightMedium%, Hard: weightHard%, VeryHard: weightVeryHard%
    /// </summary>
    private QuestData PickQuestByWeight()
    {
        // เนเธขเธ pool เน€เธเธงเธชเธ•เธฒเธกเธฃเธฐเธ”เธฑเธ
        var easy     = questList.FindAll(q => q.difficulty == QuestDifficulty.Easy);
        var medium   = questList.FindAll(q => q.difficulty == QuestDifficulty.Medium);
        var hard     = questList.FindAll(q => q.difficulty == QuestDifficulty.Hard);
        var veryHard = questList.FindAll(q => q.difficulty == QuestDifficulty.VeryHard);

        // เธชเธฃเนเธฒเธ Weighted pool (เนเธชเนเน€เธเธเธฒเธฐเธฃเธฐเธ”เธฑเธเธ—เธตเนเธกเธตเน€เธเธงเธชเธญเธขเธนเน)
        int totalWeight = 0;
        if (easy.Count     > 0) totalWeight += weightEasy;
        if (medium.Count   > 0) totalWeight += weightMedium;
        if (hard.Count     > 0) totalWeight += weightHard;
        if (veryHard.Count > 0) totalWeight += weightVeryHard;

        if (totalWeight <= 0)
        {
            // Fallback: เธงเธเธ•เธฒเธกเธฅเธณเธ”เธฑเธเน€เธ”เธดเธก
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
    /// เธชเธฃเนเธฒเธเธ•เธฑเธงเธฅเธฐเธเธฃเธเธณเธฅเธญเธเธญเธฑเธ•เนเธเธกเธฑเธ•เธด (Fallback เธซเธฒเธเนเธกเนเธกเธต 3D Model NPC)
    /// </summary>
    private GameObject CreatePlaceholderNPC(Vector3 position, string name)
    {
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = $"NPC_{name}";
        npc.transform.position = position;
        npc.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);

        // เน€เธเธฅเธตเนเธขเธเธชเธตเธ•เธฑเธงเธฅเธฐเธเธฃเนเธซเนเธ”เธนเน€เธ”เนเธเธเธฑเธ”
        Renderer rend = npc.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.2f, 0.6f, 1.0f);
        }

        // เธ”เธงเธเธ•เธฒ/เธ”เนเธฒเธเธซเธเนเธฒเธเธณเธฅเธญเธเน€เธเธทเนเธญเนเธซเนเน€เธซเนเธเธ—เธดเธจเธ—เธฒเธเธเธฒเธฃเธซเธฑเธเธซเธเนเธฒ
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

