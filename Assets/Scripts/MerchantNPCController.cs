using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum MerchantState
{
    Spawning,
    Approaching,
    WaitingAtReception,
    Leaving,
    Finished
}

/// <summary>
/// ตัวจัดการพ่อค้าวัตถุมงคล (Merchant NPC Controller)
/// - เดินเข้ามาในตำหนักตาม Waypoint / NavMesh เหมือน NPC ปกติ
/// - เมื่อถึงจุดรับแขกจะหยุดยืนรอผู้เล่นเข้ามากด [E] เพื่อเปิดร้านค้า
/// - เมื่อผู้เล่นปิดหน้าต่างร้านค้า (ไม่ว่าจะซื้อหรือไม่ซื้อ) พ่อค้าจะเดินออกจากตำหนักทันที
/// - ส่งสัญญาณบอก HallManager เพื่อเริ่มสุ่ม NPC เควสคนถัดไป
/// </summary>
public class MerchantNPCController : MonoBehaviour
{
    [Header("Merchant Info")]
    public string merchantName = "พ่อค้าวัตถุมงคล";

    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float rotationSpeed = 6.0f;
    public float arrivalDistance = 1.0f;

    [Header("Visual / Animation (Optional)")]
    public Animator animator;
    public string walkAnimParam = "isWalking";

    [Header("Audio (Optional)")]
    public AudioClip greetingSound;
    public AudioClip departSound;

    [Header("State Status")]
    public MerchantState currentState = MerchantState.Spawning;

    [Header("Waypoint Path System (ทางเดินตามกำหนด)")]
    public NPCWaypointPath approachPath;
    public NPCWaypointPath exitPath;

    private Transform targetReceptionPoint;
    private Transform targetExitPoint;
    private Transform playerTransform;
    private HallManager hallManager;
    private InteractableItem interactItem;

    private bool isNavMeshActive = false;
    private NavMeshAgent navAgent;

    private int currentApproachIndex = 0;
    private int currentExitIndex = 0;
    private bool isSubscribedToShopClose = false;
    private bool hasOpenedShop = false;

    private float GetNpcHeightOffset()
    {
        CapsuleCollider capCol = GetComponent<CapsuleCollider>();
        if (capCol != null) return capCol.height * 0.5f * transform.localScale.y;
        CharacterController charCol = GetComponent<CharacterController>();
        if (charCol != null) return charCol.height * 0.5f * transform.localScale.y;
        return 0.9f;
    }

    public void Initialize(Transform reception, Transform exit, Transform player, HallManager manager, NPCWaypointPath approach = null, NPCWaypointPath exitP = null)
    {
        this.targetReceptionPoint = reception;
        this.targetExitPoint = exit;
        this.playerTransform = player;
        this.hallManager = manager;
        this.approachPath = approach;
        this.exitPath = exitP;
        this.currentApproachIndex = 0;
        this.currentExitIndex = 0;

        float heightOffset = GetNpcHeightOffset();

        // ปรับระดับพื้นตอนเกิด
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

        // เพิ่ม InteractableItem
        interactItem = GetComponent<InteractableItem>();
        if (interactItem == null)
        {
            interactItem = gameObject.AddComponent<InteractableItem>();
        }
        interactItem.itemName = merchantName;
        interactItem.canRead = true;
        interactItem.canCollect = false;
        interactItem.readTitle = "";
        interactItem.readDescription = "";
        interactItem.customReadPromptText = "เปิดร้านค้า";
        if (interactItem.onRead == null) interactItem.onRead = new UnityEngine.Events.UnityEvent();
        interactItem.onRead.RemoveAllListeners();
        interactItem.onRead.AddListener(OnInteractWithMerchant);

        if (walkSpeed <= 0.1f) walkSpeed = 2.5f;
        if (arrivalDistance <= 0.1f) arrivalDistance = 0.8f;

        // NavMesh Setup
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(transform.position, out navHit, 3.5f, NavMesh.AllAreas))
            {
                navAgent.enabled = true;
                navAgent.baseOffset = heightOffset;
                navAgent.Warp(navHit.position);
                navAgent.speed = walkSpeed;
                navAgent.stoppingDistance = 0.2f;
                navAgent.isStopped = false;
                isNavMeshActive = true;
            }
            else
            {
                isNavMeshActive = false;
                navAgent.enabled = false;
            }
        }
        else
        {
            isNavMeshActive = false;
        }

        if (!isNavMeshActive)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        StartApproaching();
    }

    private void Update()
    {
        switch (currentState)
        {
            case MerchantState.Approaching:
                HandleApproaching();
                break;
            case MerchantState.WaitingAtReception:
                HandleWaitingAtReception();
                break;
            case MerchantState.Leaving:
                HandleLeaving();
                break;
        }
    }

    public void StartApproaching()
    {
        currentState = MerchantState.Approaching;
        currentApproachIndex = 0;

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

        ArrivedAtReception();
    }

    private void ArrivedAtReception()
    {
        currentState = MerchantState.WaitingAtReception;
        SetAnimationWalking(false);

        if (isNavMeshActive && navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath();
        }

        if (greetingSound != null)
        {
            AudioSource.PlayClipAtPoint(greetingSound, transform.position);
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"<color=#FFD700>🛒 {merchantName} เดินทางมาถึงแล้ว!</color> (เดินเข้าไปกด [E] เพื่อเปิดร้านค้า)", 3.5f);
        }
    }

    private void HandleWaitingAtReception()
    {
        FacePlayer();

        // 1. ตรวจจับการเปิดร้านค้า (ไม่ว่าจะเปิดผ่าน InteractableItem หรือปุ่มใดก็ตาม)
        if (ShopController.Instance != null && ShopController.Instance.isShopOpen)
        {
            hasOpenedShop = true;
        }
        // 2. เมื่อเคยเปิดร้านค้าไปแล้ว และตอนนี้ร้านค้าปิดลง -> สั่งพ่อค้าเดินออกจากตำหนักทันที!
        else if (hasOpenedShop && (ShopController.Instance == null || !ShopController.Instance.isShopOpen))
        {
            Debug.Log("[MerchantNPCController] 🚪 ตรวจพบร้านค้าปิดลงแล้ว -> พ่อค้าเริ่มเดินทางออกจากตำหนัก!");
            StartLeaving();
        }
    }

    private void OnInteractWithMerchant()
    {
        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player == null && playerTransform != null) player = playerTransform.GetComponent<PlayerInteraction>();

        ShopController shop = ShopController.Instance;
        if (shop == null)
        {
            shop = FindFirstObjectByType<ShopController>();
            if (shop == null)
            {
                GameObject shopObj = new GameObject("ShopController");
                shop = shopObj.AddComponent<ShopController>();
            }
        }

        if (shop != null)
        {
            hasOpenedShop = true; // บันทึกว่าเปิดร้านค้าแล้ว

            if (!isSubscribedToShopClose)
            {
                shop.onShopClosed += OnShopClosed;
                isSubscribedToShopClose = true;
            }
            shop.OpenShop(player);
        }
    }

    private void OnShopClosed()
    {
        if (isSubscribedToShopClose && ShopController.Instance != null)
        {
            ShopController.Instance.onShopClosed -= OnShopClosed;
            isSubscribedToShopClose = false;
        }

        // ปิดร้านค้าเสร็จเรียบร้อย -> พ่อค้าเดินออกจากตำหนักทันที!
        StartLeaving();
    }

    public void StartLeaving()
    {
        if (currentState == MerchantState.Leaving || currentState == MerchantState.Finished) return;

        currentState = MerchantState.Leaving;
        currentExitIndex = 0;

        if (interactItem != null)
        {
            interactItem.canRead = false;
        }

        if (departSound != null)
        {
            AudioSource.PlayClipAtPoint(departSound, transform.position);
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"<color=#FFD700>🛒 {merchantName} เดินทางออกจากตำหนักแล้ว...</color>", 3.0f);
        }

        if (exitPath != null && exitPath.PointCount > 0)
        {
            int closestIndex = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < exitPath.PointCount; i++)
            {
                float d = Vector3.Distance(transform.position, exitPath.GetPointPosition(i));
                if (d < minDist)
                {
                    minDist = d;
                    closestIndex = i;
                }
            }
            currentExitIndex = closestIndex;
        }

        SetAnimationWalking(true);

        Vector3 firstExitTarget = GetNextExitTarget();
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = false;
            if (firstExitTarget != Vector3.zero)
            {
                navAgent.SetDestination(firstExitTarget);
            }
        }
    }

    private Vector3 GetNextExitTarget()
    {
        if (exitPath != null && exitPath.PointCount > 0 && currentExitIndex < exitPath.PointCount)
        {
            return exitPath.GetPointPosition(currentExitIndex);
        }
        if (targetExitPoint != null)
        {
            return targetExitPoint.position;
        }
        if (approachPath != null && approachPath.PointCount > 0)
        {
            return approachPath.GetPointPosition(0);
        }
        if (playerTransform != null)
        {
            return playerTransform.position - playerTransform.forward * 8f;
        }
        return transform.position - transform.forward * 8f;
    }

    private void HandleLeaving()
    {
        Vector3 currentTarget = GetNextExitTarget();
        if (currentTarget == Vector3.zero)
        {
            DepartComplete();
            return;
        }

        SetAnimationWalking(true);

        float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currentTarget.x, currentTarget.z));
        float checkDist = Mathf.Max(arrivalDistance, 1.2f);

        if (isNavMeshActive && navAgent != null && navAgent.enabled)
        {
            if (navAgent.isStopped) navAgent.isStopped = false;

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
                }
                else
                {
                    if (!navAgent.hasPath || Vector3.Distance(navAgent.destination, currentTarget) > 0.5f)
                    {
                        navAgent.SetDestination(currentTarget);
                    }

                    if (distXZ <= checkDist || (navAgent.hasPath && navAgent.remainingDistance <= checkDist))
                    {
                        AdvanceExitWaypoint();
                    }
                }
            }
        }

        if (!isNavMeshActive || navAgent == null || !navAgent.enabled)
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

        DepartComplete();
    }

    private void DepartComplete()
    {
        currentState = MerchantState.Finished;
        SetAnimationWalking(false);

        if (isSubscribedToShopClose && ShopController.Instance != null)
        {
            ShopController.Instance.onShopClosed -= OnShopClosed;
            isSubscribedToShopClose = false;
        }

        if (hallManager != null)
        {
            hallManager.OnMerchantDeparted();
        }

        Destroy(gameObject);
    }

    private void MoveTowards(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position);
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.position += direction.normalized * walkSpeed * Time.deltaTime;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;
        Vector3 direction = (playerTransform.position - transform.position);
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    private void SetAnimationWalking(bool isWalking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkAnimParam))
        {
            animator.SetBool(walkAnimParam, isWalking);
        }
    }

    private void OnDestroy()
    {
        if (isSubscribedToShopClose && ShopController.Instance != null)
        {
            ShopController.Instance.onShopClosed -= OnShopClosed;
        }
    }
}
