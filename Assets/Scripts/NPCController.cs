using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum NPCState
{
    Spawning,
    Approaching,
    WaitingAtReception,
    WaitingForDelivery, // ยืนรอผู้เล่นนำของมาส่งมอบเควส
    InDialogue,
    Leaving,
    Finished
}

public class NPCController : MonoBehaviour
{
    [Header("NPC Data & Quest")]
    public QuestData questData = new QuestData();

    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float rotationSpeed = 6.0f;
    public float arrivalDistance = 1.0f;

    [Header("Visual / Animation (Optional)")]
    public Animator animator;
    public string walkAnimParam = "isWalking";
    public string talkAnimParam = "isTalking";

    [Header("Audio (Optional)")]
    public AudioClip greetingSound;
    public AudioClip acceptSound;
    public AudioClip declineSound;

    [Header("State Status (Read Only)")]
    public NPCState currentState = NPCState.Spawning;

    private Transform targetReceptionPoint;
    private Transform targetExitPoint;
    private Transform playerTransform;
    private HallManager hallManager;
    private InteractableItem interactItem;

    private bool isNavMeshActive = false;
    private UnityEngine.AI.NavMeshAgent navAgent;

    public void Initialize(QuestData data, Transform reception, Transform exit, Transform player, HallManager manager)
    {
        this.questData = data != null ? data.Clone() : new QuestData();
        this.targetReceptionPoint = reception;
        this.targetExitPoint = exit;
        this.playerTransform = player;
        this.hallManager = manager;

        // ปรับระดับพื้นตอนเกิด (ข้าม Collider ของตัวเอง)
        RaycastHit[] initHits = Physics.RaycastAll(transform.position + Vector3.up * 2f, Vector3.down, 10f);
        System.Array.Sort(initHits, (a, b) => b.point.y.CompareTo(a.point.y));
        foreach (var h in initHits)
        {
            if (h.transform != transform && !h.transform.IsChildOf(transform) && !h.collider.isTrigger)
            {
                transform.position = new Vector3(transform.position.x, h.point.y, transform.position.z);
                break;
            }
        }

        // เพิ่ม InteractableItem เพื่อให้ผู้เล่นเดินไปเล็งแล้วกด [E] คุย/ส่งเควสได้
        interactItem = GetComponent<InteractableItem>();
        if (interactItem == null)
        {
            interactItem = gameObject.AddComponent<InteractableItem>();
        }
        interactItem.itemName = questData != null ? questData.npcName : "ผู้มาเยือน";
        interactItem.canRead = true;
        interactItem.canCollect = false;
        interactItem.readTitle = "";
        interactItem.readDescription = "";
        interactItem.customReadPromptText = "พูดคุย / รับเควส";
        if (interactItem.onRead == null)
        {
            interactItem.onRead = new UnityEngine.Events.UnityEvent();
        }
        interactItem.onRead.RemoveAllListeners();
        interactItem.onRead.AddListener(TriggerQuestDialogue);

        // ตรวจสอบ NavMeshAgent ถ้ามีและอบ NavMesh ไว้
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            if (navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                isNavMeshActive = true;
                navAgent.speed = walkSpeed;
            }
            else
            {
                isNavMeshActive = false;
                navAgent.enabled = false;
            }
        }

        StartApproaching();
    }

    void Start()
    {
        if (playerTransform == null)
        {
            PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCState.Approaching:
                HandleApproaching();
                break;
            case NPCState.WaitingAtReception:
                HandleWaitingAtReception();
                break;
            case NPCState.WaitingForDelivery:
                HandleWaitingForDelivery();
                break;
            case NPCState.InDialogue:
                FacePlayer();
                break;
            case NPCState.Leaving:
                HandleLeaving();
                break;
        }
    }

    public void StartApproaching()
    {
        currentState = NPCState.Approaching;
        SetAnimationWalking(true);

        if (isNavMeshActive && navAgent != null && targetReceptionPoint != null)
        {
            navAgent.SetDestination(targetReceptionPoint.position);
        }
    }

    private void HandleApproaching()
    {
        if (targetReceptionPoint == null)
        {
            if (playerTransform != null)
            {
                MoveTowards(playerTransform.position + playerTransform.forward * 1.5f);
                if (Vector3.Distance(transform.position, playerTransform.position) <= 2.2f)
                {
                    ArrivedAtReception();
                }
            }
            return;
        }

        Vector3 targetPos = targetReceptionPoint.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (isNavMeshActive && navAgent != null)
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= arrivalDistance)
            {
                ArrivedAtReception();
            }
        }
        else
        {
            MoveTowards(targetPos);
            if (distance <= arrivalDistance)
            {
                ArrivedAtReception();
            }
        }
    }

    private void ArrivedAtReception()
    {
        currentState = NPCState.WaitingAtReception;
        SetAnimationWalking(false);

        if (isNavMeshActive && navAgent != null)
        {
            navAgent.ResetPath();
        }

        if (greetingSound != null)
        {
            AudioSource.PlayClipAtPoint(greetingSound, transform.position);
        }

        // เริ่มแสดงหน้าต่างเสนอเควสต่อผู้เล่นทันทีเมื่อมาถึง
        TriggerQuestDialogue();
    }

    private void HandleWaitingAtReception()
    {
        FacePlayer();
    }

    private void HandleWaitingForDelivery()
    {
        FacePlayer();

        // อัปเดตข้อความ Prompt เมื่อผู้เล่นมองมาที่ NPC
        if (interactItem != null)
        {
            if (HasRequiredItems())
            {
                string req = !string.IsNullOrEmpty(questData.requiredItemName) ? questData.requiredItemName : "ภารกิจ";
                interactItem.customReadPromptText = $"ส่งมอบเควส ({req} ครบถ้วนแล้ว)";
            }
            else
            {
                string req = !string.IsNullOrEmpty(questData.requiredItemName)
                    ? $"ต้องการ: {questData.requiredItemName} x{questData.requiredQuantity}"
                    : "ภารกิจยังไม่เสร็จ";
                interactItem.customReadPromptText = $"พูดคุย ({req})";
            }
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;

        Vector3 lookDir = playerTransform.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นกดคุยกับ NPC หรือเมื่อ NPC เดินมาถึง
    /// </summary>
    public void TriggerQuestDialogue()
    {
        // ── ตรวจว่ากำลังรอส่งมอบอยู่จริง (สถานะ WaitingForDelivery หรือเควสถูกรับแล้ว)
        bool isWaitingDelivery = currentState == NPCState.WaitingForDelivery && questData.isAccepted;

        if (isWaitingDelivery)
        {
            if (HasRequiredItems())
            {
                CompleteDelivery();
            }
            else
            {
                // ผู้เล่นยังไม่มีของ หรือยังทำภารกิจไม่เสร็จ -> แจ้งเตือนข้อความเตือนความจำ และห้ามส่งเควส
                string waitMsg = "ข้ากำลังรอของจากท่านอยู่นะ...";

                string reqText = !string.IsNullOrEmpty(questData.requiredItemName)
                    ? $" (ต้องการ: {questData.requiredItemName} x{questData.requiredQuantity})"
                    : " (ภารกิจยังไม่เสร็จสิ้น)";

                if (InteractionUIManager.Instance != null)
                {
                    InteractionUIManager.Instance.ShowNotification(
                        $"{questData.npcName}: \"{waitMsg}\"{reqText}", 3.0f);
                }
            }
            return;
        }

        // ยังไม่ได้รับเควส -> เปิดหน้าต่าง Dialog
        currentState = NPCState.InDialogue;
        SetAnimationTalking(true);

        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.ShowQuestDialog(this, questData);
        }
    }

    /// <summary>
    /// ตรวจสอบว่าผู้เล่นมีไอเทมตามที่เควสต้องการครบหรือไม่ (หรือทำภารกิจเสร็จสิ้นแล้วหรือไม่)
    /// </summary>
    public bool HasRequiredItems()
    {
        if (questData == null) return false;

        // 1. กรณีเควสระบุชื่อไอเทม: ต้องมีไอเทมในกระเป๋าหรือในมือครบตามจำนวน
        if (!string.IsNullOrEmpty(questData.requiredItemName))
        {
            if (QuestUIManager.Instance == null) return false;
            int currentCount = QuestUIManager.Instance.GetPlayerItemCount(questData.requiredItemName);
            return currentCount >= questData.requiredQuantity;
        }

        // 2. กรณีเควสไม่ได้ระบุชื่อไอเทม (เช่น เควสทำพิธี/มินิเกม): ต้องมีเงื่อนไข isTaskCompleted เป็นจริงเท่านั้น
        // ป้องกันบัคส่งเควสผ่านทันทีโดยที่ยังไม่ได้ทำอะไร
        return questData.isTaskCompleted;
    }

    /// <summary>
    /// ส่งมอบเควสสำเร็จ (ลบไอเทม, รับรางวัล, NPC กล่าวขอบคุณ และเดินออกจากตำหนัก)
    /// </summary>
    private void CompleteDelivery()
    {
        // ── Safety check — ตรวจสอบให้แน่ใจว่าได้รับเควสแล้ว และมีของ/เงื่อนไขครบ ────────────────────
        if (!questData.isAccepted || questData.isCompleted)
        {
            Debug.LogWarning($"[NPCController] ⚠️ ไม่สามารถส่งมอบเควสได้: '{questData.questTitle}'");
            return;
        }
        if (!HasRequiredItems())
        {
            Debug.LogWarning($"[NPCController] ⚠️ ผู้เล่นไม่มีไอเทมครบ/ภารกิจยังไม่เสร็จ — ยกเลิกการส่งเควส '{questData.questTitle}'");
            return;
        }

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();

        // ลบไอเทมที่ส่งมอบออกจากกระเป๋า/มือของผู้เล่น
        if (player != null && !string.IsNullOrEmpty(questData.requiredItemName))
        {
            for (int i = 0; i < questData.requiredQuantity; i++)
            {
                player.RemoveItemByName(questData.requiredItemName);
            }
        }

        // บันทึกเควสสำเร็จ และลบออกจาก Quest Tracker
        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.CompleteQuest(questData);
        }

        // จ่ายรางวัลเงินผ่าน PlayerWalletManager
        if (questData.rewardMoney > 0)
        {
            if (PlayerWalletManager.Instance != null)
            {
                PlayerWalletManager.Instance.EarnMoney(questData.rewardMoney);
            }
            else
            {
                // Fallback: ยังไม่มี WalletManager — แสดง notification เฉยๆ
                if (InteractionUIManager.Instance != null)
                {
                    InteractionUIManager.Instance.ShowNotification(
                        $"+{questData.rewardMoney:N0} บาท!", 2.5f);
                }
            }
        }

        if (acceptSound != null)
        {
            AudioSource.PlayClipAtPoint(acceptSound, transform.position);
        }

        // ให้ Karma ตามค่าที่กำหนดในเควส (ซ่อนจากผู้เล่น)
        if (KarmaManager.Instance != null)
        {
            KarmaManager.Instance.ApplyKarma(questData.karmaReward, questData.questTitle);
        }

        string completeMsg = !string.IsNullOrEmpty(questData.completeDialogue)
            ? questData.completeDialogue
            : "ขอบพระคุณท่านมาก! ได้ของครบถ้วนแล้ว ข้าขอตัวลาก่อน";

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"<color=#00FF7F>[ส่งเควสสำเร็จ!]</color> {questData.npcName}: \"{completeMsg}\"", 3.5f);
        }

        Debug.Log($"[NPCController] 🏆 ส่งมอบเควส '{questData.questTitle}' สำเร็จ! NPC กำลังเดินออกจากตำหนัก");
        StartCoroutine(LeaveRoutine());
    }

    /// <summary>
    /// ผู้เล่นกดรับเควส
    /// </summary>
    public void OnQuestAccepted()
    {
        questData.isAccepted = true;
        SetAnimationTalking(false);

        // 1. กรณีเควสมีมินิเกม (เช่น Rhythm Game ท่องคาถา W A S D)
        if (questData.minigameType != MinigameType.None)
        {
            currentState = NPCState.WaitingForDelivery;

            if (MinigameManager.Instance != null)
            {
                MinigameManager.Instance.StartMinigame(questData, this, OnMinigameResult);
            }
            else
            {
                // Fallback: ค้นหาหรือสร้าง MinigameManager
                MinigameManager mgr = FindFirstObjectByType<MinigameManager>();
                if (mgr == null)
                {
                    GameObject mgrObj = new GameObject("MinigameManager");
                    mgr = mgrObj.AddComponent<MinigameManager>();
                }
                mgr.StartMinigame(questData, this, OnMinigameResult);
            }
            return;
        }

        // 2. กรณีเควสทั่วไป/ส่งของ (NPC จะยืนรอรับของ)
        currentState = NPCState.WaitingForDelivery;

        if (acceptSound != null)
        {
            AudioSource.PlayClipAtPoint(acceptSound, transform.position);
        }

        Debug.Log($"[NPCController] ✅ ผู้เล่นรับเควส: '{questData.questTitle}' (NPC จะยืนรอส่งมอบอยู่ที่เดิม)");
    }

    /// <summary>
    /// Callback ผลลัพธ์จากมินิเกม (สำเร็จ หรือ ล้มเหลว)
    /// </summary>
    private void OnMinigameResult(bool isSuccess)
    {
        if (isSuccess)
        {
            // ทำภารกิจสำเร็จ!
            questData.isTaskCompleted = true;
            Debug.Log($"[NPCController] 🏆 มินิเกมผ่านฉลุย! กำลังจ่ายรางวัลเควส '{questData.questTitle}'");
            CompleteDelivery();
        }
        else
        {
            // ทำภารกิจไม่ผ่าน -> โดนผีร้ายตามติด!
            Debug.LogWarning($"[NPCController] 💀 มินิเกมล้มเหลว! เควส '{questData.questTitle}'");

            if (GhostCurseManager.Instance != null)
            {
                GhostCurseManager.Instance.AttachGhost($"ท่องคาถาให้ {questData.npcName} ล้มเหลว");
            }

            string failMsg = !string.IsNullOrEmpty(questData.failDialogue)
                ? questData.failDialogue
                : "อ๊ากก! มีสิ่งชั่วร้ายเข้าครอบงำ... พิธีล้มเหลวแล้ว!";

            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification(
                    $"<color=#FF3333>[พิธีล้มเหลว!]</color> {questData.npcName}: \"{failMsg}\"", 4.0f);
            }

            // ลบเควสออกจาก Tracker
            if (QuestUIManager.Instance != null)
            {
                QuestUIManager.Instance.activeQuests.Remove(questData);
            }

            StartCoroutine(LeaveRoutine());
        }
    }

    /// <summary>
    /// ผู้เล่นกดปฏิเสธเควส (ทำงานหลังจากหน้าต่าง UI ปฏิเสธแสดงผลเสร็จสิ้นแล้ว)
    /// </summary>
    public void OnQuestDeclined()
    {
        questData.isAccepted = false;
        SetAnimationTalking(false);

        if (declineSound != null)
        {
            AudioSource.PlayClipAtPoint(declineSound, transform.position);
        }

        Debug.Log($"[NPCController] ❌ ผู้เล่นปฏิเสธเควส: '{questData.questTitle}' จาก {questData.npcName} — กำลังเดินออกจากตำหนัก");
        StartLeaving();
    }

    private IEnumerator LeaveRoutine()
    {
        yield return new WaitForSeconds(1.2f);
        StartLeaving();
    }

    public void StartLeaving()
    {
        currentState = NPCState.Leaving;
        SetAnimationWalking(true);

        // ปิดการ Interact ระหว่างกำลังเดินออก
        if (interactItem != null)
        {
            interactItem.enabled = false;
        }

        // ตั้งเวลาลบ NPC ออกจากฉากแน่นอน (ภายใน 5 วินาที)
        CancelInvoke(nameof(DespawnNPC));
        Invoke(nameof(DespawnNPC), 5.0f);

        if (isNavMeshActive && navAgent != null && targetExitPoint != null)
        {
            navAgent.SetDestination(targetExitPoint.position);
        }
    }

    private void HandleLeaving()
    {
        if (targetExitPoint == null)
        {
            DespawnNPC();
            return;
        }

        Vector3 targetPos = targetExitPoint.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (isNavMeshActive && navAgent != null)
        {
            if (!navAgent.pathPending && navAgent.remainingDistance <= arrivalDistance)
            {
                DespawnNPC();
            }
        }
        else
        {
            MoveTowards(targetPos);
            if (distance <= arrivalDistance)
            {
                DespawnNPC();
            }
        }
    }

    private void MoveTowards(Vector3 destination)
    {
        Vector3 targetPlane = new Vector3(destination.x, transform.position.y, destination.z);
        Vector3 direction = (targetPlane - transform.position);

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            transform.position = Vector3.MoveTowards(transform.position, targetPlane, walkSpeed * Time.deltaTime);

            // ปรับระดับความสูงตามพื้นดิน (ข้าม Collider ของตัวเอง)
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 2.0f, Vector3.down, 10f);
            System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));

            foreach (var hit in hits)
            {
                if (hit.transform != transform && !hit.transform.IsChildOf(transform) && !hit.collider.isTrigger)
                {
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                    break;
                }
            }
        }
    }

    private void DespawnNPC()
    {
        CancelInvoke(nameof(DespawnNPC));
        if (currentState == NPCState.Finished) return;

        currentState = NPCState.Finished;
        SetAnimationWalking(false);

        // แจ้งเตือน HallManager ว่า NPC คนนี้ออกจากตำหนักแล้ว เพื่อส่งคนถัดไปเข้ามา
        if (hallManager != null)
        {
            hallManager.OnNPCDeparted(this);
        }

        // ลบ NPC ออกจากฉากอย่างสมบูรณ์
        Destroy(gameObject);
    }

    private void SetAnimationWalking(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkAnimParam))
        {
            animator.SetBool(walkAnimParam, walking);
        }
    }

    private void SetAnimationTalking(bool talking)
    {
        if (animator != null && !string.IsNullOrEmpty(talkAnimParam))
        {
            animator.SetBool(talkAnimParam, talking);
        }
    }
}
