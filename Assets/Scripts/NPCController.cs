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

    [Header("Waypoint Path System (ทางเดินตามกำหนด)")]
    public NPCWaypointPath approachPath;
    public NPCWaypointPath exitPath;
    private int currentApproachIndex = 0;
    private int currentExitIndex = 0;

    private float GetNpcHeightOffset()
    {
        CapsuleCollider capCol = GetComponent<CapsuleCollider>();
        if (capCol != null)
        {
            return capCol.height * 0.5f * transform.localScale.y;
        }
        CharacterController charCol = GetComponent<CharacterController>();
        if (charCol != null)
        {
            return charCol.height * 0.5f * transform.localScale.y;
        }
        return 0.9f; // ค่าตั้งต้นครึ่งความสูงของ Capsule 1.8 เมตร
    }

    public void Initialize(QuestData data, Transform reception, Transform exit, Transform player, HallManager manager, NPCWaypointPath approach = null, NPCWaypointPath exitP = null)
    {
        this.questData = data != null ? data.Clone() : new QuestData();

        this.targetReceptionPoint = reception;
        this.targetExitPoint = exit;
        this.playerTransform = player;
        this.hallManager = manager;
        this.approachPath = approach;
        this.exitPath = exitP;
        this.currentApproachIndex = 0;
        this.currentExitIndex = 0;

        float heightOffset = GetNpcHeightOffset();

        // ปรับระดับพื้นตอนเกิด (ชดเชยความสูง ให้ยืนบนพื้นพอดี ไม่จมดิน!)
        RaycastHit[] initHits = Physics.RaycastAll(transform.position + Vector3.up * 2f, Vector3.down, 10f);
        System.Array.Sort(initHits, (a, b) => b.point.y.CompareTo(a.point.y));
        foreach (var h in initHits)
        {
            if (h.transform != transform && !h.transform.IsChildOf(transform) && !h.collider.isTrigger)
            {
                transform.position = new Vector3(transform.position.x, h.point.y + heightOffset, transform.position.z);
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

        // มั่นใจว่าความเร็วเดินไม่เป็น 0
        if (walkSpeed <= 0.1f) walkSpeed = 2.5f;
        if (arrivalDistance <= 0.1f) arrivalDistance = 0.8f;

        // ตรวจสอบ NavMeshAgent และพยายามเชื่อมต่อเข้ากับ NavMesh ในฉาก
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out navHit, 3.5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                navAgent.enabled = true;
                navAgent.baseOffset = heightOffset; // ยกโมเดลขึ้นเหนือ NavMesh ให้เท้าแตะพื้นพอดี!
                navAgent.Warp(navHit.position);    // Snap ตัวละครลงบน NavMesh surface อย่างถูกต้อง
                navAgent.speed = walkSpeed;
                navAgent.stoppingDistance = 0.2f;
                navAgent.isStopped = false;
                isNavMeshActive = true;
                Debug.Log($"[NPCController] 🟢 {questData.npcName} เชื่อมต่อกับ NavMesh ในฉากสำเร็จ! (BaseOffset: {heightOffset:F2}, Speed: {walkSpeed})");
            }
            else
            {
                isNavMeshActive = false;
                navAgent.enabled = false; // ปิดเพื่อไม่ให้ล็อกตำแหน่งตัวละคร
                Debug.LogWarning($"[NPCController] ⚠️ ไม่พบ NavMesh ที่อบไว้ใต้ตัว {questData.npcName}! สลับไปใช้ระบบเดินตาม Waypoint แบบตรงอิสระ (MoveTowards)");
            }
        }
        else
        {
            isNavMeshActive = false;
            Debug.Log($"[NPCController] ℹ️ {questData.npcName} ไม่มี NavMeshAgent — ใช้ระบบเดินตาม Waypoint แบบตรงอิสระ (MoveTowards)");
        }

        // หากไม่ได้ใช้ NavMeshAgent ให้ตั้งค่า Collider เป็น Trigger และ Rigidbody เป็น Kinematic
        // เพื่อป้องกันไม่ให้ NPC เดินติดขอบธรณีประตู ขอบไม้ หรือชนสิ่งกีดขวางแล้วค้าง
        if (!isNavMeshActive)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        StartApproaching();
    }

    private void Update()
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
            case NPCState.Leaving:
                HandleLeaving();
                break;
        }
    }

    public void StartApproaching()
    {
        currentState = NPCState.Approaching;
        currentApproachIndex = 0;

        // หากจุดแรกใกล้อยู่แล้ว ให้ขยับเป้าหมายไปจุดถัดไป
        if (approachPath != null && approachPath.PointCount > 1)
        {
            Vector3 p0 = approachPath.GetPointPosition(0);
            if (Vector3.Distance(transform.position, p0) <= 0.8f)
            {
                currentApproachIndex = 1;
            }
        }

        SetAnimationWalking(true);

        Vector3 firstTarget = GetNextApproachTarget();
        if (isNavMeshActive && navAgent != null && navAgent.enabled && firstTarget != Vector3.zero)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(firstTarget);
        }
    }

    private Vector3 GetNextApproachTarget()
    {
        if (approachPath != null && approachPath.PointCount > 0 && currentApproachIndex < approachPath.PointCount)
        {
            return approachPath.GetPointPosition(currentApproachIndex);
        }
        return targetReceptionPoint != null ? targetReceptionPoint.position : Vector3.zero;
    }

    private void HandleApproaching()
    {
        Vector3 currentTarget = GetNextApproachTarget();
        if (currentTarget == Vector3.zero)
        {
            if (playerTransform != null)
            {
                SetAnimationWalking(true);
                MoveTowards(playerTransform.position + playerTransform.forward * 1.5f);
                if (Vector3.Distance(transform.position, playerTransform.position) <= 2.2f)
                {
                    ArrivedAtReception();
                }
            }
            return;
        }

        SetAnimationWalking(true);

        // เช็คระยะห่างทางกายภาพจริง XZ
        float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currentTarget.x, currentTarget.z));
        float checkDist = Mathf.Max(arrivalDistance, 1.0f);

        if (isNavMeshActive && navAgent != null && navAgent.enabled)
        {
            if (!navAgent.pathPending)
            {
                // หาก NavMesh มีปัญหา หรือเส้นทางขาด ให้สลับเป็น MoveTowards
                if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    isNavMeshActive = false;
                    navAgent.enabled = false;

                    Collider col = GetComponent<Collider>();
                    if (col != null) col.isTrigger = true;
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;

                    Debug.LogWarning($"[NPCController] ⚠️ NavMesh ขาดช่วงที่จุด {currentApproachIndex}! สลับใช้ระบบ MoveTowards อิสระ");
                }
                else if (distXZ <= checkDist || (navAgent.hasPath && navAgent.remainingDistance <= checkDist))
                {
                    AdvanceApproachWaypoint();
                }
            }
        }
        else
        {
            MoveTowards(currentTarget);
            if (distXZ <= checkDist)
            {
                AdvanceApproachWaypoint();
            }
        }
    }

    private void AdvanceApproachWaypoint()
    {
        currentApproachIndex++;

        // 1. ถ้ายังมีจุดถัดไปใน approachPath -> เดินไปยังจุดถัดไป
        if (approachPath != null && currentApproachIndex < approachPath.PointCount)
        {
            Vector3 nextTarget = approachPath.GetPointPosition(currentApproachIndex);
            if (isNavMeshActive && navAgent != null)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(nextTarget);
            }
            return;
        }

        // 2. ถ้าเดินครบทุกจุดใน approachPath แล้ว แต่ยังไม่ถึง targetReceptionPoint -> เดินไปยังโต๊ะรับแขก
        if (targetReceptionPoint != null)
        {
            Vector3 receptionPos = targetReceptionPoint.position;
            float distToReception = Vector3.Distance(transform.position, receptionPos);
            
            if (distToReception > 1.2f)
            {
                if (isNavMeshActive && navAgent != null)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(receptionPos);
                }
                return;
            }
        }

        // 3. เมื่อถึงโต๊ะรับแขกเรียบร้อยแล้ว -> สั่ง ArrivedAtReception()
        ArrivedAtReception();
    }

    private void ArrivedAtReception()
    {
        currentState = NPCState.WaitingAtReception;
        SetAnimationWalking(false);

        if (isNavMeshActive && navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath();
        }

        if (greetingSound != null)
        {
            AudioSource.PlayClipAtPoint(greetingSound, transform.position);
        }

        if (interactItem != null)
        {
            interactItem.customReadPromptText = $"พูดคุย / รับเควส ({questData.npcName})";
        }

        // แสดงการแจ้งเตือนสั้นๆ ให้ผู้เล่นทราบว่าผู้มาเยือนเดินมาถึงแล้ว
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"{questData.npcName} เดินมาถึงแล้ว (เดินเข้าไปกด [E] เพื่อพูดคุย)", 3.0f);
        }

        // ยืนรอที่โต๊ะรับแขก หันหน้าหาผู้เล่น จนกว่าผู้เล่นจะเดินมากด [E] คุยด้วยตัวเอง
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
                string waitMsg = !string.IsNullOrEmpty(questData.waitingDialogue) ? questData.waitingDialogue : "ข้ากำลังรอผลการทำพิธีอยู่นะ...";

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
    /// ผู้เล่นกดรับเควส ➔ NPC จะยืนรออยู่ที่เดิม เพื่อให้ผู้เล่นเดินไปโต้ตอบกดทำพิธีที่วัตถุในฉากด้วยตัวเอง
    /// </summary>
    public void OnQuestAccepted()
    {
        questData.isAccepted = true;
        SetAnimationTalking(false);

        // NPC เปลี่ยนสถานะเป็นยืนรอผู้เล่นทำภารกิจและนำผลลัพธ์มาส่งมอบ
        currentState = NPCState.WaitingForDelivery;

        if (acceptSound != null)
        {
            AudioSource.PlayClipAtPoint(acceptSound, transform.position);
        }

        string hint = (questData.requiredMinigameSequence != null && questData.requiredMinigameSequence.Count > 0)
            ? $" (โปรดเดินไปกด [E] ทำพิธีมินิเกมที่วัตถุ/แท่นพิธีในตำหนัก แล้วกลับมารายงาน {questData.npcName})"
            : "";

        if (InteractionUIManager.Instance != null)
        {
            string msg = !string.IsNullOrEmpty(questData.waitingDialogue) ? questData.waitingDialogue : "ขอบพระคุณมาก!";
            InteractionUIManager.Instance.ShowNotification(
                $"<color=#00FF7F>[รับเควสสำเร็จ]</color> {questData.npcName}: \"{msg}\"{hint}", 4.0f);
        }

        Debug.Log($"[NPCController] ✅ ผู้เล่นรับเควส: '{questData.questTitle}' (NPC ยืนรอที่เดิม โปรดเดินไปกด [E] ทำพิธีที่วัตถุในฉาก)");
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
            // ทำภารกิจไม่ผ่าน -> เควสล้มเหลวและ NPC เดินออกจากตำหนักทันที
            Debug.LogWarning($"[NPCController] 💀 มินิเกมล้มเหลว! เควส '{questData.questTitle}'");

            if (QuestUIManager.Instance != null && questData != null)
            {
                QuestUIManager.Instance.FailQuest(questData);
            }
            else
            {
                if (GhostCurseManager.Instance != null && questData != null)
                {
                    GhostCurseManager.Instance.AttachGhost($"ทำเควส {questData.npcName} ล้มเหลว");
                }

                string failMsg = (questData != null && !string.IsNullOrEmpty(questData.failDialogue))
                    ? questData.failDialogue
                    : "ไม่เห็นเก่งเลยนี่หว่า... ข้าไปหาคนอื่นดีกว่า!";

                if (InteractionUIManager.Instance != null)
                {
                    InteractionUIManager.Instance.ShowNotification(
                        $"<color=#FF3333>[เควสล้มเหลว!]</color> {(questData != null ? questData.npcName : "NPC")}: \"{failMsg}\"", 4.5f);
                }

                StartLeaving();
            }
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
        currentExitIndex = 0;
        SetAnimationWalking(true);

        // ปิดการ Interact ระหว่างกำลังเดินออก
        if (interactItem != null)
        {
            interactItem.enabled = false;
        }

        // ตั้งเวลาลบ NPC ออกจากฉากกรณีค้าง (ขยายเป็น 15 วินาทีเพื่อให้ครอบคลุมทางเดินหลายจุด)
        CancelInvoke(nameof(DespawnNPC));
        Invoke(nameof(DespawnNPC), 15.0f);

        Vector3 firstExitTarget = GetNextExitTarget();
        if (isNavMeshActive && navAgent != null && firstExitTarget != Vector3.zero)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(firstExitTarget);
        }
    }

    private Vector3 GetNextExitTarget()
    {
        if (exitPath != null && exitPath.PointCount > 0 && currentExitIndex < exitPath.PointCount)
        {
            return exitPath.GetPointPosition(currentExitIndex);
        }
        return targetExitPoint != null ? targetExitPoint.position : Vector3.zero;
    }

    private void HandleLeaving()
    {
        Vector3 currentTarget = GetNextExitTarget();
        if (currentTarget == Vector3.zero)
        {
            DespawnNPC();
            return;
        }

        SetAnimationWalking(true);

        // เช็คระยะห่างทางกายภาพจริง XZ
        float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currentTarget.x, currentTarget.z));
        float checkDist = Mathf.Max(arrivalDistance, 1.0f);

        if (isNavMeshActive && navAgent != null && navAgent.enabled)
        {
            if (!navAgent.pathPending)
            {
                if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    isNavMeshActive = false;
                    navAgent.enabled = false;

                    Collider col = GetComponent<Collider>();
                    if (col != null) col.isTrigger = true;
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;

                    Debug.LogWarning($"[NPCController] ⚠️ NavMesh ขาดช่วงระหว่างขาออก! สลับใช้ระบบ MoveTowards อิสระ");
                }
                else if (distXZ <= checkDist || (navAgent.hasPath && navAgent.remainingDistance <= checkDist))
                {
                    AdvanceExitWaypoint();
                }
            }
        }
        else
        {
            MoveTowards(currentTarget);
            if (distXZ <= checkDist)
            {
                AdvanceExitWaypoint();
            }
        }
    }

    private void AdvanceExitWaypoint()
    {
        currentExitIndex++;

        // 1. ถ้ายังมีจุดถัดไปใน exitPath -> เดินไปยังจุดถัดไป
        if (exitPath != null && currentExitIndex < exitPath.PointCount)
        {
            Vector3 nextTarget = exitPath.GetPointPosition(currentExitIndex);
            if (isNavMeshActive && navAgent != null)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(nextTarget);
            }
            return;
        }

        // 2. ถ้าเดินครบทุกจุดใน exitPath แล้ว แต่ยังไม่ถึง targetExitPoint -> เดินไปยังจุดออกจากฉาก
        if (targetExitPoint != null)
        {
            Vector3 exitPos = targetExitPoint.position;
            float distToExit = Vector3.Distance(transform.position, exitPos);
            
            if (distToExit > 1.2f)
            {
                if (isNavMeshActive && navAgent != null)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(exitPos);
                }
                return;
            }
        }

        DespawnNPC();
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
                    transform.position = new Vector3(transform.position.x, hit.point.y + GetNpcHeightOffset(), transform.position.z);
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
        if (hallManager == null)
        {
            hallManager = HallManager.Instance;
        }

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
