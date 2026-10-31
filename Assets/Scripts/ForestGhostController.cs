using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ระบบควบคุมผีในป่า (Forest Ghost Controller)
/// ทำงานเมื่อผู้เล่นเก็บกล่องรางวัลจากการขุดดินในป่า:
/// 1. ผีจะสปอนออกมาและเดิน/วิ่งไล่ล่าผู้เล่น
/// 2. หากผีเดินชนหรือเข้าใกล้ผู้เล่น จะล็อกตัวผู้เล่นและเปิดมินิเกม Smash Key (กด Spacebar 20 ครั้งใน 7 วิ)
/// 3. หากผู้เล่นกดทัน: ผีจะถูกขับไล่/สตั๊น และผู้เล่นสามารถวิ่งหนีกลับไปที่รถได้
/// 4. หากกดไม่ทัน: จอดำเฟด และวาปผู้เล่นกลับไปยังซีน GamePlay พร้อมรีเซ็ตฉากป่าให้พร้อมสำหรับรอบต่อไป
/// </summary>
public class ForestGhostController : MonoBehaviour
{
    public static ForestGhostController Instance { get; private set; }

    public enum GhostState { Inactive, Spawning, Chasing, Grabbing, Stunned, Banished }

    [Header("1. โมเดลและจุดเกิดของผี (Ghost Visuals & Spawn)")]
    [Tooltip("GameObject โมเดลผีในฉาก (ถ้ามีโมเดลผีวางรอในฉากอยู่แล้วให้ลากมาใส่ที่นี่)")]
    public GameObject ghostModel;

    [Tooltip("Prefab โมเดลผี (กรณีไม่มีโมเดลในฉาก จะ Instantiate จาก Prefab นี้อัตโนมัติ เช่น ghost1)")]
    public GameObject ghostPrefab;

    [Tooltip("จุดเกิดของผี (Transform จุดที่ต้องการให้ผีโผล่ ถ้าเว้นว่างจะสุ่มเกิดในป่าห่างจากผู้เล่น 20 เมตร)")]
    public Transform customSpawnPoint;

    [Tooltip("ระยะห่างในการสปอนผีจากผู้เล่น (เมตร กรณีไม่ได้กำหนดจุดเกิด)")]
    public float spawnDistance = 22.0f;

    [Header("2. การเคลื่อนที่และการไล่ล่า (Chase Settings)")]
    [Tooltip("ความเร็วในการเดินไล่ผู้เล่น (เมตร/วินาที)")]
    public float chaseSpeed = 4.2f;

    [Tooltip("ความเร็วในการหมุนตัวหันหน้าหาผู้เล่น")]
    public float turnSpeed = 6.0f;

    [Tooltip("ความสูงชดเชยจากพื้นดิน (เมตร) สำหรับยกโมเดลขึ้นมาเหยียบพื้นพอดี เช่น 1.0 สำหรับ Capsule ที่ Pivot อยู่กลางตัว (ระบบจะคำนวณ Auto ให้ถ้าค่านี้เป็น 0)")]
    public float groundHeightOffset = 1.0f;

    [Tooltip("ระยะห่างที่ผีจะจับตัวผู้เล่นและเริ่มมินิเกม (เมตร)")]
    public float catchDistance = 1.8f;

    [Tooltip("ชื่อ Parameter ใน Animator สำหรับท่าเดิน/วิ่ง (ถ้ามี)")]
    public string animSpeedParam = "Speed";

    [Header("3. การตั้งค่ามินิเกม Smash Key (QTE Settings)")]
    [Tooltip("จำนวนครั้งที่ต้องกด Spacebar")]
    public int targetSmashCount = 20;

    [Tooltip("เวลาจำกัดในการกด (วินาที)")]
    public float smashTimeLimit = 7.0f;

    [Tooltip("เมื่อดิ้นหลุดแล้ว ให้ผีหายไปทันทีหรือไม่ (ถ้าติ๊กออกผีจะแค่สตั๊นชั่วคราว)")]
    public bool banishOnEscape = true;

    [Tooltip("ระยะเวลาที่ผีจะสตั๊นเมื่อดิ้นหลุด (วินาที กรณีไม่ให้ผีหายไปทันที)")]
    public float stunDurationOnEscape = 15.0f;

    [Header("4. การส่งตัวกลับตำหนักเมื่อพ่ายแพ้ (Defeat & Reset)")]
    [Tooltip("ชื่อซีนตำหนักที่ต้องการให้วาปกลับไปเมื่อแพ้")]
    public string returnSceneName = "GamePlay";

    [Tooltip("ชื่อจุดเกิดในตำหนักที่ต้องการให้ผู้เล่นไปยืน")]
    public string returnSpawnPointName = "CarReturnSpawnPoint";

    [Tooltip("ยึดกล่องรางวัลคืนเมื่อพ่ายแพ้หรือไม่ (เพื่อให้ต้องมาขุดใหม่ในรอบถัดไป)")]
    public bool removeBoxOnDefeat = true;

    [Tooltip("ชื่อไอเทมกล่องรางวัลที่จะนำออกจากตัวเมื่อแพ้")]
    public string boxItemName = "กล่องโบราณ";

    [Header("5. เสียงและเอฟเฟกต์ (Audio & FX)")]
    public AudioClip spawnSound;
    public AudioClip chaseSound;
    public AudioClip grabJumpscareSound;
    public AudioClip escapeSound;
    public AudioClip defeatJumpscareSound;
    public ParticleSystem banishEffect;

    [Header("สถานะปัจจุบัน (Debug State)")]
    public GhostState currentState = GhostState.Inactive;

    private Transform playerTransform;
    private PlayerController playerController;
    private PlayerInteraction playerInteraction;
    private NavMeshAgent navAgent;
    private Animator animator;
    private AudioSource audioSource;
    private AudioSource chaseLoopAudioSource;
    private float stunTimer = 0f;
    private GameObject activeGhostInstance;

    private void Reset()
    {
        groundHeightOffset = 1.0f;
        chaseSpeed = 4.2f;
        turnSpeed = 6.0f;
    }

    /// <summary>
    /// คำนวณความสูงชดเชยที่แท้จริง โดยระบบจะตรวจจับอัตโนมัติจากขนาด Collider ของผี
    /// เช่น ถ้าเป็น Capsule ที่มี Pivot อยู่ตรงกลางลำตัว จะชดเชย +1.0m ให้อัตโนมัติทันที
    /// แม้ใน Inspector ค่า groundHeightOffset จะยังเป็น 0 อยู่ก็ตาม
    /// </summary>
    public float GetEffectiveGroundOffset()
    {
        // 1. ถ้าผู้ใช้ตั้งค่า groundHeightOffset ใน Inspector ไว้อย่างชัดเจน (ไม่ใช่ 0) ให้ใช้ค่านั้นเป็นหลัก
        if (Mathf.Abs(groundHeightOffset) > 0.001f)
        {
            return groundHeightOffset;
        }

        // 2. ถ้าใน Inspector เป็น 0 ให้ตรวจจับอัตโนมัติจากขนาด Collider / Model
        GameObject target = activeGhostInstance != null ? activeGhostInstance : (IsSceneObject(ghostModel) ? ghostModel : null);
        if (target != null)
        {
            CapsuleCollider cap = target.GetComponentInChildren<CapsuleCollider>();
            if (cap != null)
            {
                // คำนวณระยะจากจุด Pivot (Center) ถึงขอบล่างสุดของแคปซูล
                float bottomDistance = (cap.height * 0.5f) - cap.center.y;
                float worldOffset = bottomDistance * target.transform.lossyScale.y;
                if (worldOffset > 0.01f) return worldOffset;
            }

            BoxCollider box = target.GetComponentInChildren<BoxCollider>();
            if (box != null)
            {
                float bottomDistance = (box.size.y * 0.5f) - box.center.y;
                float worldOffset = bottomDistance * target.transform.lossyScale.y;
                if (worldOffset > 0.01f) return worldOffset;
            }

            Renderer rend = target.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                float bottomDelta = target.transform.position.y - rend.bounds.min.y;
                if (bottomDelta > 0.05f) return bottomDelta;
            }
        }

        // ค่า Default มาตรฐานสำหรับ Capsule 2m ใน Unity
        return 1.0f;
    }

    /// <summary>
    /// ตรวจสอบว่า GameObject นี้เป็นวัตถุที่วางอยู่ใน Scene จริงๆ หรือเป็นเพียง Prefab Asset ในโฟลเดอร์ Project
    /// </summary>
    private bool IsSceneObject(GameObject go)
    {
        return go != null && go.scene.IsValid() && go.scene.rootCount > 0;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Auto-find ghostModel if not assigned
        if (ghostModel == null)
        {
            if (transform.childCount > 0)
            {
                ghostModel = transform.GetChild(0).gameObject;
            }
            else
            {
                // ค้นหาวัตถุที่มีชื่อว่า ghost ในฉาก (เฉพาะ Object ที่อยู่ใน Scene เท่านั้น)
                GameObject foundGhost = GameObject.Find("ghost1") ?? GameObject.Find("ghost2") ?? GameObject.Find("ghost3") ?? GameObject.Find("ForestGhost");
                if (foundGhost != null && foundGhost != this.gameObject && IsSceneObject(foundGhost))
                {
                    ghostModel = foundGhost;
                }
            }
        }

        // ซ่อนผีไว้ก่อนเสมอตอนเริ่มฉาก (เฉพาะกรณีเป็น Object ที่วางอยู่ใน Scene เท่านั้น ห้ามซ่อน Prefab ใน Project)
        if (IsSceneObject(ghostModel))
        {
            ghostModel.SetActive(false);
            activeGhostInstance = ghostModel;
        }
    }

    private void Start()
    {
        FindPlayer();
        if (IsSceneObject(ghostModel))
        {
            SetupComponents(ghostModel);
        }
    }

    private void FindPlayer()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerInteraction = playerController.GetComponent<PlayerInteraction>();
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                playerController = p.GetComponent<PlayerController>();
                playerInteraction = p.GetComponent<PlayerInteraction>();
            }
        }
    }

    private void SetupComponents(GameObject targetObj)
    {
        if (targetObj == null) return;
        navAgent = targetObj.GetComponent<NavMeshAgent>();
        animator = targetObj.GetComponentInChildren<Animator>();

        if (navAgent != null)
        {
            navAgent.baseOffset = GetEffectiveGroundOffset();
        }

        if (chaseSound != null && chaseLoopAudioSource == null)
        {
            chaseLoopAudioSource = targetObj.AddComponent<AudioSource>();
            chaseLoopAudioSource.clip = chaseSound;
            chaseLoopAudioSource.loop = true;
            chaseLoopAudioSource.spatialBlend = 1f;
            chaseLoopAudioSource.minDistance = 2f;
            chaseLoopAudioSource.maxDistance = 25f;
            chaseLoopAudioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// คำนวณความสูงของพื้นดิน และบวกค่าชดเชย groundHeightOffset เพื่อให้โมเดลยืนเต็มตัวอยู่บนพื้น ไม่จมดิน
    /// </summary>
    private float GetGroundHeight(Vector3 worldPos)
    {
        float surfaceY = worldPos.y;
        if (Terrain.activeTerrain != null)
        {
            surfaceY = Terrain.activeTerrain.SampleHeight(worldPos) + Terrain.activeTerrain.transform.position.y;
        }
        else
        {
            // ยิง Raycast ลงมาหาพื้น โดยคัดกรองข้าม Collider ของตัวผีเอง
            RaycastHit[] hits = Physics.RaycastAll(worldPos + Vector3.up * 50f, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y)); // จากบนลงล่าง
            foreach (var h in hits)
            {
                if (activeGhostInstance != null && (h.collider.gameObject == activeGhostInstance || h.collider.transform.IsChildOf(activeGhostInstance.transform)))
                {
                    continue; // ข้ามตัวผีเอง
                }
                surfaceY = h.point.y;
                break;
            }
        }

        return surfaceY + GetEffectiveGroundOffset();
    }

    /// <summary>
    /// สั่งให้ผีสปอนออกมาและเริ่มไล่ล่าผู้เล่นทันที (เรียกจาก DigRewardBox เมื่อเก็บกล่อง)
    /// </summary>
    [ContextMenu("🧪 ทดสอบสปอนผีไล่ล่า (Spawn Ghost)")]
    public void SpawnAndChasePlayer()
    {
        if (currentState == GhostState.Chasing || currentState == GhostState.Grabbing) return;

        FindPlayer();
        if (playerTransform == null)
        {
            Debug.LogWarning("[ForestGhostController] ⚠️ ไม่พบตัวละครผู้เล่นในฉาก!");
            return;
        }

        // 1. ตรวจสอบและสร้างอินสแตนซ์โมเดลผีในฉาก (รองรับทั้ง Prefab จาก Project และ Scene Object)
        if (activeGhostInstance == null)
        {
            if (IsSceneObject(ghostModel))
            {
                activeGhostInstance = ghostModel;
            }
            else
            {
                GameObject sourcePrefab = (ghostPrefab != null) ? ghostPrefab : ghostModel;
                if (sourcePrefab != null)
                {
                    activeGhostInstance = Instantiate(sourcePrefab, transform);
                    activeGhostInstance.name = "ForestGhost_ActiveInstance";
                }
                else
                {
                    // ถ้าไม่มี prefab ให้สร้างโมเดลจำลองขึ้นมาเพื่อไม่ให้ระบบค้าง
                    activeGhostInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    activeGhostInstance.name = "GhostModel_Generated";
                    activeGhostInstance.transform.SetParent(transform);
                    var renderer = activeGhostInstance.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = new Color(0.9f, 0.1f, 0.1f, 0.8f);
                }
            }
            SetupComponents(activeGhostInstance);
        }

        // 2. คำนวณจุดเกิด
        Vector3 spawnPos;
        if (customSpawnPoint != null)
        {
            spawnPos = customSpawnPoint.position;
            spawnPos.y = GetGroundHeight(spawnPos);
        }
        else
        {
            // สุ่มเกิดในทิศทางด้านหลังหรือมุมมืดห่างจากผู้เล่นตามระยะ spawnDistance
            Vector3 randomOffset = -playerTransform.forward * spawnDistance + (Random.insideUnitSphere * 5f);
            randomOffset.y = 0;
            spawnPos = playerTransform.position + randomOffset;
            spawnPos.y = GetGroundHeight(spawnPos);
        }

        // 3. วางตำแหน่งผีและเปิดการแสดงผล (บังคับเปิด Renderer ทุกชิ้นให้มองเห็นได้แน่นอน 100%)
        activeGhostInstance.transform.position = spawnPos;
        activeGhostInstance.transform.LookAt(new Vector3(playerTransform.position.x, spawnPos.y, playerTransform.position.z));
        activeGhostInstance.SetActive(true);

        foreach (var rend in activeGhostInstance.GetComponentsInChildren<Renderer>(true))
        {
            rend.enabled = true;
        }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.Warp(spawnPos);
            navAgent.speed = chaseSpeed;
            navAgent.isStopped = false;
        }

        // 4. เล่นเสียงสปอนและเสียงหลอนตามหลอก
        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }

        if (chaseLoopAudioSource != null)
        {
            chaseLoopAudioSource.Play();
        }

        // 5. แจ้งเตือนผู้เล่นบนหน้าจอ
        if (playerInteraction != null)
        {
            playerInteraction.ShowNotification("⚠️ <color=#FF2222><b>มีบางสิ่งตื่นขึ้นมาในป่าลึก! มันกำลังพุ่งตรงมาที่คุณ! รีบหนีไปที่รถ!</b></color>", 4.5f);
        }

        currentState = GhostState.Chasing;
        Debug.Log($"[ForestGhostController] 👻 ผีสปอนสำเร็จที่พิกัด {spawnPos} และเริ่มไล่ล่าผู้เล่น!");
    }

    private void Update()
    {
        if (currentState == GhostState.Chasing)
        {
            HandleChaseMovement();
        }
        else if (currentState == GhostState.Stunned)
        {
            HandleStunnedState();
        }
    }

    private void HandleChaseMovement()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        Transform targetTransform = activeGhostInstance != null ? activeGhostInstance.transform : (IsSceneObject(ghostModel) ? ghostModel.transform : transform);
        float distanceToPlayer = Vector3.Distance(targetTransform.position, playerTransform.position);

        // 1. ตรวจสอบว่าถึงตัวผู้เล่นหรือยัง
        if (distanceToPlayer <= catchDistance)
        {
            TriggerCatchPlayer();
            return;
        }

        // 2. เคลื่อนที่เข้าหาผู้เล่น (รองรับทั้ง NavMeshAgent และ Direct Walk Fallback)
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(playerTransform.position);
        }
        else
        {
            // เคลื่อนที่ตรงหาผู้เล่นพร้อมคำนวณความสูงตามพื้นดิน
            Vector3 direction = (playerTransform.position - targetTransform.position);
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                targetTransform.position += direction * chaseSpeed * Time.deltaTime;

                // หมุนหน้าหาผู้เล่นอย่างนุ่มนวล
                Quaternion targetRot = Quaternion.LookRotation(direction);
                targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // ปรับระดับความสูงให้ยืนอยู่บนพื้นดินพอดี ไม่จมดิน
            float groundY = GetGroundHeight(targetTransform.position);
            targetTransform.position = new Vector3(targetTransform.position.x, groundY, targetTransform.position.z);
        }

        // 3. ควบคุม Animation
        if (animator != null && !string.IsNullOrEmpty(animSpeedParam))
        {
            animator.SetFloat(animSpeedParam, chaseSpeed);
        }
    }

    private void TriggerCatchPlayer()
    {
        currentState = GhostState.Grabbing;

        // หยุดเสียงไล่ล่า
        if (chaseLoopAudioSource != null && chaseLoopAudioSource.isPlaying)
        {
            chaseLoopAudioSource.Stop();
        }

        // หยุดการเคลื่อนที่
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }

        if (animator != null && !string.IsNullOrEmpty(animSpeedParam))
        {
            animator.SetFloat(animSpeedParam, 0f);
        }

        // เล่นเสียงโดนจับ
        if (grabJumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(grabJumpscareSound);
        }

        // ล็อคการเคลื่อนไหวของผู้เล่นและหันหน้าเข้าหาผี
        Transform ghostT = activeGhostInstance != null ? activeGhostInstance.transform : (IsSceneObject(ghostModel) ? ghostModel.transform : transform);
        if (playerController != null)
        {
            playerController.isMovementLocked = true;
            if (playerController.playerCamera != null)
            {
                Vector3 lookTarget = ghostT.position + Vector3.up * 1.5f;
                playerController.playerCamera.LookAt(lookTarget);
            }
        }

        // เริ่มมินิเกม Smash Key (กด Spacebar 20 ครั้งใน 7 วิ)
        if (SmashKeyMinigame.Instance != null)
        {
            SmashKeyMinigame.Instance.StartSmashMinigame(
                targetSmashCount,
                smashTimeLimit,
                OnEscapeSuccess,
                OnCatchFail,
                "⚠️ ผีร้ายจับตัวคุณไว้แน่น! กด [ SPACEBAR ] รัวๆ เพื่อดิ้นให้หลุด!"
            );
        }
        else
        {
            // กรณีไม่มี SmashKeyMinigame ในซีน ให้สร้าง Component ขึ้นมาทันที
            GameObject minigameObj = new GameObject("SmashKeyMinigame_Auto");
            var minigame = minigameObj.AddComponent<SmashKeyMinigame>();
            minigame.StartSmashMinigame(
                targetSmashCount,
                smashTimeLimit,
                OnEscapeSuccess,
                OnCatchFail,
                "⚠️ ผีร้ายจับตัวคุณไว้แน่น! กด [ SPACEBAR ] รัวๆ เพื่อดิ้นให้หลุด!"
            );
        }

        Debug.Log("[ForestGhostController] 😱 ผีจับตัวผู้เล่นได้แล้ว! เริ่มมินิเกม Smash Key!");
    }

    /// <summary>
    /// ผู้เล่นกด Spacebar ครบตามเป้าหมายสำเร็จ (ดิ้นหลุด)
    /// </summary>
    private void OnEscapeSuccess()
    {
        Debug.Log("[ForestGhostController] 💨 ผู้เล่นดิ้นหลุดจากการจับของผีสำเร็จ!");

        if (escapeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(escapeSound);
        }

        if (playerInteraction != null)
        {
            playerInteraction.ShowNotification("💨 <color=#00FF7F><b>คุณสลัดหลุดจากผีร้ายแล้ว! รีบวิ่งกลับไปที่รถเร็ว!</b></color>", 4.0f);
        }

        if (banishOnEscape)
        {
            currentState = GhostState.Banished;

            if (banishEffect != null)
            {
                banishEffect.Play();
            }

            if (activeGhostInstance != null)
            {
                activeGhostInstance.SetActive(false);
            }
            else if (IsSceneObject(ghostModel))
            {
                ghostModel.SetActive(false);
            }
        }
        else
        {
            // สตั๊นผีชั่วคราว
            currentState = GhostState.Stunned;
            stunTimer = stunDurationOnEscape;
        }

        // ปลดล็อคผู้เล่นให้วิ่งหนีได้
        if (playerController != null)
        {
            playerController.isMovementLocked = false;
        }
    }

    private void HandleStunnedState()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            // ผีฟื้นจากสตั๊นและกลับมาไล่ล่าต่อ
            currentState = GhostState.Chasing;
            if (navAgent != null && navAgent.isOnNavMesh) navAgent.isStopped = false;
            if (chaseLoopAudioSource != null) chaseLoopAudioSource.Play();
            Debug.Log("[ForestGhostController] 👻 ผีฟื้นจากอาการสตั๊นแล้ว และเริ่มไล่ล่าต่อ!");
        }
    }

    /// <summary>
    /// ผู้เล่นกดไม่ทันภายในเวลาที่กำหนด (พ่ายแพ้)
    /// </summary>
    private void OnCatchFail()
    {
        // 1. ตรวจสอบว่าได้รับการคุ้มครองจากศาลตายายหรือไม่
        if (ForestShrineInteractable.Instance != null && ForestShrineInteractable.Instance.HasActiveProtection())
        {
            Debug.Log("[ForestGhostController] ✨ บารมีศาลตายายทำงาน! คุ้มครองผู้เล่นและขับไล่ผีชั่วคราว");
            ForestShrineInteractable.Instance.TriggerProtection(this);
            return;
        }

        Debug.Log("[ForestGhostController] 💀 ผู้เล่นพ่ายแพ้! ผีจับตัวได้สำเร็จ กำลังวาปกลับตำหนักและรีเซ็ตฉาก...");
        StartCoroutine(DefeatRoutine());
    }

    /// <summary>
    /// สลายผีชั่วคราวด้วยบารมีศาลตายาย แล้วเกิดใหม่ตามเวลาที่กำหนด (เช่น 5 วินาที)
    /// </summary>
    public void DispelAndRespawnAfter(float delay)
    {
        StartCoroutine(DispelAndRespawnRoutine(delay));
    }

    private IEnumerator DispelAndRespawnRoutine(float delay)
    {
        currentState = GhostState.Banished;

        // ซ่อนตัวผีทันที
        if (activeGhostInstance != null)
        {
            activeGhostInstance.SetActive(false);
        }
        else if (IsSceneObject(ghostModel))
        {
            ghostModel.SetActive(false);
        }

        if (chaseLoopAudioSource != null && chaseLoopAudioSource.isPlaying)
        {
            chaseLoopAudioSource.Stop();
        }

        if (banishEffect != null)
        {
            banishEffect.Play();
        }

        // ปลดล็อคผู้เล่นให้เคลื่อนไหวและวิ่งหนีได้ทันที
        if (playerController != null)
        {
            playerController.isMovementLocked = false;
        }

        Debug.Log($"[ForestGhostController] ✨ ผีสลายตัวแล้ว! กำลังนับถอยหลัง {delay} วินาทีเพื่อเกิดใหม่...");

        yield return new WaitForSeconds(delay);

        // คำนวณจุดเกิดใหม่ให้ห่างจากตัวผู้เล่นอย่างน้อย spawnDistance เมตร (ไม่โผล่มาทับตัวผู้เล่น)
        FindPlayer();
        Vector3 respawnPos;
        if (customSpawnPoint != null)
        {
            respawnPos = customSpawnPoint.position;
            respawnPos.y = GetGroundHeight(respawnPos);
        }
        else if (playerTransform != null)
        {
            Vector3 randomOffset = -playerTransform.forward * spawnDistance + (UnityEngine.Random.insideUnitSphere * 4f);
            randomOffset.y = 0;
            respawnPos = playerTransform.position + randomOffset;
            respawnPos.y = GetGroundHeight(respawnPos);
        }
        else
        {
            respawnPos = transform.position;
            respawnPos.y = GetGroundHeight(respawnPos);
        }

        if (activeGhostInstance != null)
        {
            activeGhostInstance.transform.position = respawnPos;
            if (playerTransform != null)
            {
                activeGhostInstance.transform.LookAt(new Vector3(playerTransform.position.x, respawnPos.y, playerTransform.position.z));
            }
            activeGhostInstance.SetActive(true);

            foreach (var rend in activeGhostInstance.GetComponentsInChildren<Renderer>(true))
            {
                rend.enabled = true;
            }
        }
        else if (IsSceneObject(ghostModel))
        {
            ghostModel.transform.position = respawnPos;
            ghostModel.SetActive(true);
        }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.Warp(respawnPos);
            navAgent.isStopped = false;
        }

        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }

        if (chaseLoopAudioSource != null)
        {
            chaseLoopAudioSource.Play();
        }

        if (playerInteraction != null)
        {
            playerInteraction.ShowNotification("⚠️ <color=#FF2222><b>ผีร้ายฟื้นคืนชีพแล้ว! มันกำลังพุ่งตรงมาที่คุณอีกครั้ง! รีบวิ่งไปที่รถ!</b></color>", 4.5f);
        }

        currentState = GhostState.Chasing;
        Debug.Log($"[ForestGhostController] 👻 ผีเกิดใหม่สำเร็จที่ {respawnPos} และเริ่มไล่ล่าต่อ!");
    }

    private IEnumerator DefeatRoutine()
    {
        // 1. เล่นเสียง Jumpscare สุดท้าย
        if (defeatJumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(defeatJumpscareSound);
        }

        // 2. ยึดกล่องรางวัลออกจากกระเป๋า (หากเปิดใช้งาน removeBoxOnDefeat)
        if (removeBoxOnDefeat && playerInteraction != null)
        {
            bool removed = playerInteraction.RemoveItem(boxItemName);
            if (removed)
            {
                Debug.Log($"[ForestGhostController] 📦 ยึด [{boxItemName}] คืนจากกระเป๋าแล้ว ผู้เล่นต้องกลับมาขุดใหม่ในรอบต่อไป");
            }
        }

        // แจ้งเตือนผู้เล่น
        if (playerInteraction != null)
        {
            playerInteraction.ShowNotification("💀 <color=#FF2222><b>คุณหนีไม่พ้นและหมดสติไป... ทุกอย่างในป่าถูกรีเซ็ตใหม่</b></color>", 4.5f);
        }

        yield return new WaitForSeconds(0.6f);

        // 3. สั่งเฟดดำและวาปผู้เล่นกลับไปยังซีน GamePlay ที่จุดเกิดที่กำหนด
        if (CarSceneFader.Instance != null)
        {
            CarSceneFader.Instance.Travel(
                returnSceneName,
                returnSpawnPointName,
                1.0f,
                defeatJumpscareSound,
                transform.position
            );
        }
        else
        {
            // Fallback กรณีไม่มี CarSceneFader
            UnityEngine.SceneManagement.SceneManager.LoadScene(returnSceneName);
        }

        if (activeGhostInstance != null)
        {
            activeGhostInstance.SetActive(false);
        }
        else if (IsSceneObject(ghostModel))
        {
            ghostModel.SetActive(false);
        }

        currentState = GhostState.Inactive;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        if (customSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(customSpawnPoint.position, 1.2f);
            Gizmos.DrawLine(customSpawnPoint.position, customSpawnPoint.position + Vector3.up * 2.5f);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1.2f);
        }
    }
}
