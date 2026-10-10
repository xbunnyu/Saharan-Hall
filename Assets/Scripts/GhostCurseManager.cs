using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบจัดการผีร้ายตามติด (Ghost Curse System)
/// - นับจำนวนผีร้ายที่เกาะติดตัวผู้เล่น (0-3 ตัว)
/// - แสดงตัวเลขวงกลมสีแดงมุมขวาบนตามภาพตัวอย่าง
/// - แสดงเอฟเฟคหมอกดำ/ขอบจอดำแดงหลอนๆ รอบตัวผู้เล่นตามระดับความรุนแรง
/// - จัดการ Game Over เมื่อผีเกาะครบ 3 ตัว พร้อมปุ่มเริ่มเล่นใหม่
/// </summary>
public class GhostCurseManager : MonoBehaviour
{
    public static GhostCurseManager Instance { get; private set; }

    [Header("Ghost Count & Limits (จำนวนผีร้าย)")]
    [Tooltip("จำนวนผีร้ายที่ตามติดตัวผู้เล่นขณะนี้ (0 - 3)")]
    [Range(0, 3)]
    public int currentGhostCount = 0;
    public int maxGhostLimit = 3;

    [Header("Ghost Models & Spawn Points (ระบบโมเดลผีปรากฏในตำหนัก)")]
    [Tooltip("Prefab โมเดลผีที่ทำไว้ (ลาก Prefab โมเดลผีมาใส่ในช่องนี้)")]
    public GameObject ghostPrefab;

    [Tooltip("รายการ Prefab โมเดลผีเพิ่มเติม (กรณีมีหลายท่าทาง เช่น หมอบคลาน, เกาะเสา, ยืนเกาะหน้าต่าง)")]
    public GameObject[] ghostPrefabs;

    [Tooltip("จุดสำหรับสุ่มวางโมเดลผีในตำหนัก (ลาก Transform ของจุดที่วางไว้ในฉากมาใส่)")]
    public Transform[] ghostSpawnPoints;

    [Tooltip("หรือจะลากโมเดลผีที่วางรอไว้ในฉากโดยตรงมาใส่ที่นี่ (ระบบจะเปิด/ปิด และสลับตำแหน่งให้อัตโนมัติ)")]
    public GameObject[] sceneGhostObjects;

    [Header("Random & Shuffling (การสุ่มและสลับตำแหน่ง)")]
    [Tooltip("สุ่มสลับตำแหน่งของผีทุกครั้งที่มีผีตัวใหม่โผล่ขึ้นมา (สุ่มโผล่แบบสลับตำแหน่ง)")]
    public bool shufflePositionsOnNewGhost = true;

    [Tooltip("เสียงผีโผล่/ปรากฏตัว")]
    public AudioClip ghostSpawnSound;

    [Header("UI Indicator Settings")]
    [Tooltip("เปิด/ปิด การแสดงตัวเลขผีบน UI มุมขวาบน (ปิดไว้เพื่อให้ใช้โมเดลผีในฉากแทนตัวเลข)")]
    public bool showNumberBadgeOnUI = false;

    [Tooltip("ข้อความแจ้งเตือนเมื่อผีโผล่ (ไม่แสดงตัวเลข)")]
    public string ghostSpawnNotification = "<color=#FF2020>👻 วิญญาณร้ายปรากฏตัวขึ้นในตำหนัก!</color>";

    [Header("Visual & Effects (เอฟเฟคหลอน)")]
    public bool enableScreenEffects = true;
    public Color ghostVignetteColor = new Color(0.7f, 0.05f, 0.05f, 0.35f);
    public AudioClip ghostAttachSound;
    public AudioClip gameOverSound;

    [Header("Cleanse Jumpscare Settings")]
    [Tooltip("เสียงเอฟเฟค Jumpscare ตอนชำระล้างผี (ถ้าไม่ใส่จะใช้ ghostAttachSound)")]
    public AudioClip cleanseJumpscareSound;
    [Tooltip("ข้อความ Jumpscare ตอนชำระล้างผี")]
    public string jumpscareText = "BOO!";
    [Tooltip("ระยะเวลาค้างจอดำพร้อมคำว่า BOO! (วินาที)")]
    public float jumpscareHoldDuration = 0.8f;
    [Tooltip("ระยะเวลา Fade จอดำหายไป (วินาที)")]
    public float jumpscareFadeDuration = 1.5f;

    [Header("Game Over Status")]
    public bool isGameOver = false;

    // Internal textures & timers
    private Texture2D redCircleTex;
    private Texture2D vignetteTex;
    private Texture2D blackTex;
    private float pulseTimer = 0f;
    private float screenShakeTimer = 0f;
    private float screenShakeIntensity = 0f;
    private PlayerController playerController;

    private bool isJumpscareActive = false;
    private float jumpscareAlpha = 0f;
    private Coroutine jumpscareCoroutine;

    // รายการ Instance ของผีที่ถูก Spawn ในฉาก
    private List<GameObject> activeGhostInstances = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateRuntimeTextures();
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        EnsureSpawnPointsExist();
        UpdateGhostModels(shuffle: false);
    }

    void Update()
    {
        pulseTimer += Time.deltaTime * (1.5f + currentGhostCount * 0.8f);

        if (screenShakeTimer > 0)
        {
            screenShakeTimer -= Time.deltaTime;
        }
    }

    private void CreateRuntimeTextures()
    {
        // 1. สร้าง Texture วงกลมสีแดงสำหรับ Badge มุมขวาบน
        int size = 128;
        redCircleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] circleColors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Gradient สีแดงเข้มเหลือบส้มเล็กน้อย ให้ดูมีมิติ
                    float factor = 1f - (dist / radius) * 0.4f;
                    circleColors[y * size + x] = new Color(0.75f * factor, 0.12f * factor, 0.12f * factor, 0.95f);
                }
                else
                {
                    circleColors[y * size + x] = Color.clear;
                }
            }
        }
        redCircleTex.SetPixels(circleColors);
        redCircleTex.Apply();

        // 2. สร้าง Texture ขอบมืด (Vignette)
        int vSize = 128;
        vignetteTex = new Texture2D(vSize, vSize, TextureFormat.RGBA32, false);
        Color[] vColors = new Color[vSize * vSize];
        Vector2 vCenter = new Vector2(vSize / 2f, vSize / 2f);
        float maxDist = Vector2.Distance(Vector2.zero, vCenter);

        for (int y = 0; y < vSize; y++)
        {
            for (int x = 0; x < vSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), vCenter);
                float norm = Mathf.Clamp01(dist / maxDist);
                float alpha = Mathf.SmoothStep(0f, 1f, norm);
                vColors[y * vSize + x] = new Color(0f, 0f, 0f, alpha);
            }
        }
        vignetteTex.SetPixels(vColors);
        vignetteTex.Apply();

        // 3. Black Texture
        blackTex = new Texture2D(1, 1);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();
    }

    /// <summary>
    /// เพิ่มจำนวนผีร้ายสะสมในตำหนัก (+1) และสุ่มโผล่โมเดลผีสลับตำแหน่ง
    /// </summary>
    public void AttachGhost(string reason = "")
    {
        if (isGameOver) return;

        currentGhostCount = Mathf.Min(currentGhostCount + 1, maxGhostLimit);
        TriggerScreenShake(0.6f, 14f);

        Debug.LogWarning($"[GhostCurseManager] 👻 ผีร้ายปรากฏในตำหนัก! ตอนนี้สะสม {currentGhostCount}/{maxGhostLimit} ตัว ({reason})");

        AudioClip soundToPlay = ghostSpawnSound != null ? ghostSpawnSound : ghostAttachSound;
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        if (InteractionUIManager.Instance != null && !string.IsNullOrEmpty(ghostSpawnNotification))
        {
            InteractionUIManager.Instance.ShowNotification(ghostSpawnNotification, 3.5f);
        }

        // อัปเดตโมเดลผีในตำหนัก และสลับตำแหน่งแบบสุ่ม
        UpdateGhostModels(shuffle: shufflePositionsOnNewGhost);

        if (currentGhostCount >= maxGhostLimit)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// ปลดปล่อยหรือล้างผีร้ายออกจากตำหนัก
    /// </summary>
    public void CleanseGhosts(int amount = 1)
    {
        if (isGameOver) return;
        currentGhostCount = Mathf.Max(0, currentGhostCount - amount);
        Debug.Log($"[GhostCurseManager] ✨ ชำระล้างวิญญาณชั่วร้ายแล้ว เหลือ {currentGhostCount}/{maxGhostLimit} ตัว");

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                $"<color=#00FF7F>✨ ชำระล้างวิญญาณชั่วร้ายแล้ว</color>", 3.0f);
        }

        // อัปเดตโมเดลผีในตำหนัก
        UpdateGhostModels(shuffle: true);

        // แสดงผล Jumpscare (ขึ้นคำว่า BOO! + จอดำแล้วค่อยๆ Fade หายไป)
        TriggerCleanseJumpscare();
    }

    // ══════════════════════════════════════════════════════════════════
    // ระบบโมเดลผีปรากฏในตำหนัก (Ghost Visual Spawning & Shuffling)
    // ══════════════════════════════════════════════════════════════════

    // Dictionary ติดตาม GameObject ที่ถูก Instantiate ออกมาตาม Index คู่ (ghostPrefabs[i] + ghostSpawnPoints[i])
    private Dictionary<int, GameObject> activeGhostMap = new Dictionary<int, GameObject>();

    /// <summary>
    /// อัปเดตโมเดลผีในฉากโดยจับคู่ ghostPrefabs[i] กับ ghostSpawnPoints[i] แล้วสุ่มเลือกเฉพาะว่าจะให้คู่อินเด็กซ์ไหนเกิดก่อน
    /// </summary>
    public void UpdateGhostModels(bool shuffle = true)
    {
        int targetGhostCount = Mathf.Clamp(currentGhostCount, 0, maxGhostLimit);

        // กรณีที่ 1: ตั้งค่า ghostPrefabs และ ghostSpawnPoints ใน Inspector (ตรงตามภาพ)
        if (ghostPrefabs != null && ghostPrefabs.Length > 0 && ghostSpawnPoints != null && ghostSpawnPoints.Length > 0)
        {
            UpdatePrefabsWithMatchingSpawnPoints(targetGhostCount, shuffle);
            return;
        }

        // กรณีที่ 2: ตั้งค่า sceneGhostObjects ไว้ใน Inspector
        if (sceneGhostObjects != null && sceneGhostObjects.Length > 0)
        {
            UpdateSceneGhostObjects(targetGhostCount, shuffle);
            return;
        }

        EnsureSceneGhostObjectsExist();
        if (sceneGhostObjects != null && sceneGhostObjects.Length > 0)
        {
            UpdateSceneGhostObjects(targetGhostCount, shuffle);
        }
    }

    private void UpdatePrefabsWithMatchingSpawnPoints(int targetCount, bool shuffle)
    {
        int maxPairs = Mathf.Min(ghostPrefabs.Length, ghostSpawnPoints.Length);
        if (maxPairs == 0) return;

        // หา Index ที่ยังไม่ได้เกิด (Available) และที่เกิดไปแล้ว (Active)
        List<int> availableIndices = new List<int>();
        List<int> activeIndices = new List<int>();

        for (int i = 0; i < maxPairs; i++)
        {
            if (activeGhostMap.ContainsKey(i) && activeGhostMap[i] != null)
            {
                activeIndices.Add(i);
            }
            else
            {
                if (ghostPrefabs[i] != null && ghostSpawnPoints[i] != null)
                {
                    availableIndices.Add(i);
                }
            }
        }

        // หากต้องเพิ่มจำนวนผี (activeIndices.Count < targetCount)
        while (activeIndices.Count < targetCount && availableIndices.Count > 0)
        {
            // สุ่มเลือกว่าจะให้อินเด็กซ์ไหนโผล่ออกมาก่อน (เช่น สุ่มเลือกระหว่าง 0, 1, 2)
            int pickPos = (shuffle && availableIndices.Count > 1) ? Random.Range(0, availableIndices.Count) : 0;
            int chosenIndex = availableIndices[pickPos];

            GameObject prefab = ghostPrefabs[chosenIndex];
            Transform spawnPt = ghostSpawnPoints[chosenIndex];

            GameObject instance = null;

            // ตรวจสอบว่า prefab เป็น GameObject ที่อยู่ในฉากแล้ว หรือเป็น Prefab Asset นอกฉาก
            if (prefab.scene.IsValid())
            {
                prefab.transform.position = spawnPt.position;
                prefab.transform.rotation = spawnPt.rotation;
                prefab.SetActive(true);
                instance = prefab;
            }
            else
            {
                instance = Instantiate(prefab, spawnPt.position, spawnPt.rotation);
                instance.name = $"Ghost_{prefab.name}_Index{chosenIndex}";
                instance.SetActive(true); // ปลดล็อคความซ่อน ปรับเปิดตา SetActive(true)
            }

            activeGhostMap[chosenIndex] = instance;

            availableIndices.RemoveAt(pickPos);
            activeIndices.Add(chosenIndex);

            Debug.Log($"[GhostCurseManager] 👻 เกิดผี Index {chosenIndex}: '{prefab.name}' ที่ตำแหน่ง '{spawnPt.name}' (Active: {instance.activeSelf})");
        }

        // หากต้องลดจำนวนผี (กรณีชำระล้าง: activeIndices.Count > targetCount)
        while (activeIndices.Count > targetCount && activeIndices.Count > 0)
        {
            int removePos = activeIndices.Count - 1;
            int indexToRemove = activeIndices[removePos];

            if (activeGhostMap.ContainsKey(indexToRemove) && activeGhostMap[indexToRemove] != null)
            {
                GameObject objToRemove = activeGhostMap[indexToRemove];
                if (objToRemove.scene.IsValid() && ghostPrefabs != null && chosenIndexIsSceneObject(indexToRemove))
                {
                    objToRemove.SetActive(false); // ปิดตา
                }
                else
                {
                    Destroy(objToRemove);
                }
                activeGhostMap.Remove(indexToRemove);
            }

            activeIndices.RemoveAt(removePos);
        }
    }

    private bool chosenIndexIsSceneObject(int index)
    {
        if (ghostPrefabs != null && index >= 0 && index < ghostPrefabs.Length)
        {
            return ghostPrefabs[index] != null && ghostPrefabs[index].scene.IsValid();
        }
        return false;
    }

    private void UpdateSceneGhostObjects(int targetCount, bool shuffle)
    {
        List<GameObject> activeObjects = new List<GameObject>();
        List<GameObject> inactiveObjects = new List<GameObject>();

        foreach (var obj in sceneGhostObjects)
        {
            if (obj == null) continue;
            if (obj.activeSelf) activeObjects.Add(obj);
            else inactiveObjects.Add(obj);
        }

        while (activeObjects.Count < targetCount && inactiveObjects.Count > 0)
        {
            int pickIndex = (shuffle && inactiveObjects.Count > 1) ? Random.Range(0, inactiveObjects.Count) : 0;
            GameObject toEnable = inactiveObjects[pickIndex];
            toEnable.SetActive(true);
            inactiveObjects.RemoveAt(pickIndex);
            activeObjects.Add(toEnable);
        }

        while (activeObjects.Count > targetCount && activeObjects.Count > 0)
        {
            int removeIndex = activeObjects.Count - 1;
            GameObject toDisable = activeObjects[removeIndex];
            toDisable.SetActive(false);
            activeObjects.RemoveAt(removeIndex);
            inactiveObjects.Add(toDisable);
        }
    }

    private void EnsureSceneGhostObjectsExist()
    {
        if (sceneGhostObjects != null && sceneGhostObjects.Length > 0) return;

        List<GameObject> foundList = new List<GameObject>();
        GameObject group = GameObject.Find("Ghosts") ?? GameObject.Find("GhostObjects");

        if (group != null)
        {
            foreach (Transform child in group.transform)
                foundList.Add(child.gameObject);
        }
        else
        {
            var allObjs = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var o in allObjs)
            {
                if (o != null && o.name.ToLower().Contains("ghost") && !o.name.Contains("Manager") && !o.name.Contains("Canvas"))
                    foundList.Add(o);
            }
        }

        if (foundList.Count > 0)
        {
            sceneGhostObjects = foundList.ToArray();
            Debug.Log($"[GhostCurseManager] 👻 ตรวจพบโมเดลผีในฉากอัตโนมัติ {sceneGhostObjects.Length} ตัว");
        }
    }

    /// <summary>
    /// ตรวจสอบและค้นหา/สร้างจุดเกิดผีในตำหนักอัตโนมัติ หากยังไม่ได้กำหนดใน Inspector
    /// </summary>
    private void EnsureSpawnPointsExist()
    {
        if (ghostSpawnPoints != null && ghostSpawnPoints.Length > 0)
        {
            var validPoints = new List<Transform>();
            foreach (var pt in ghostSpawnPoints)
            {
                if (pt != null) validPoints.Add(pt);
            }
            if (validPoints.Count > 0)
            {
                ghostSpawnPoints = validPoints.ToArray();
                return;
            }
        }

        // ค้นหาในฉาก
        List<Transform> foundPoints = new List<Transform>();
        GameObject spawnParent = GameObject.Find("GhostSpawnPoints") ?? GameObject.Find("GhostPoints") ?? GameObject.Find("GhostPositions");
        if (spawnParent != null)
        {
            foreach (Transform child in spawnParent.transform)
            {
                foundPoints.Add(child);
            }
        }

        if (foundPoints.Count == 0)
        {
            GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjs)
            {
                if (obj.name.ToLower().Contains("ghostspawn") || obj.name.ToLower().Contains("ghostpoint"))
                {
                    foundPoints.Add(obj.transform);
                }
            }
        }

        // สร้างจุดจำลองในตำหนักตามภาพตัวอย่าง Failure System (ขวาโอ่ง, ซ้ายระเบียง, เสาขวา, หลังห้อง)
        if (foundPoints.Count == 0)
        {
            Vector3 centerPos = Vector3.zero;
            if (HallManager.Instance != null && HallManager.Instance.receptionPoint != null)
            {
                centerPos = HallManager.Instance.receptionPoint.position;
            }
            else if (Camera.main != null)
            {
                centerPos = Camera.main.transform.position + Camera.main.transform.forward * 2.5f;
                centerPos.y = 0f;
            }

            GameObject autoGroup = GameObject.Find("GhostSpawnPoints_Auto");
            if (autoGroup == null)
            {
                autoGroup = new GameObject("GhostSpawnPoints_Auto");
                autoGroup.transform.position = centerPos;

                // จุดที่ 1: ฝั่งขวาหน้าโอ่ง/พื้น (เหมือนในภาพ FAIL x1)
                GameObject p1 = new GameObject("GhostPoint_RightFloor");
                p1.transform.SetParent(autoGroup.transform);
                p1.transform.position = centerPos + new Vector3(1.8f, 0f, 1.2f);
                p1.transform.rotation = Quaternion.Euler(0, -120f, 0);
                foundPoints.Add(p1.transform);

                // จุดที่ 2: ฝั่งซ้ายระเบียง/หน้าต่าง (เหมือนในภาพ FAIL x2)
                GameObject p2 = new GameObject("GhostPoint_LeftWindow");
                p2.transform.SetParent(autoGroup.transform);
                p2.transform.position = centerPos + new Vector3(-2.2f, 0.4f, 1.8f);
                p2.transform.rotation = Quaternion.Euler(0, 100f, 0);
                foundPoints.Add(p2.transform);

                // จุดที่ 3: เสาไม้ฝั่งขวา (เหมือนในภาพ FAIL x3)
                GameObject p3 = new GameObject("GhostPoint_RightPillar");
                p3.transform.SetParent(autoGroup.transform);
                p3.transform.position = centerPos + new Vector3(1.5f, 1.2f, 2.5f);
                p3.transform.rotation = Quaternion.Euler(0, -145f, 0);
                foundPoints.Add(p3.transform);

                // จุดที่ 4: มุมด้านหลังตำหนัก
                GameObject p4 = new GameObject("GhostPoint_BackCorner");
                p4.transform.SetParent(autoGroup.transform);
                p4.transform.position = centerPos + new Vector3(-1.0f, 0f, 3.5f);
                p4.transform.rotation = Quaternion.Euler(0, 180f, 0);
                foundPoints.Add(p4.transform);

                Debug.Log("[GhostCurseManager] 👻 สร้างจุดสุ่มโผล่ผีอัตโนมัติ 4 จุดในตำหนักเรียบร้อยแล้ว (สามารถสร้าง Transform มาใส่เองในช่อง ghostSpawnPoints ใน Inspector ได้)");
            }
            else
            {
                foreach (Transform child in autoGroup.transform) foundPoints.Add(child);
            }
        }

        ghostSpawnPoints = foundPoints.ToArray();
    }

    /// <summary>
    /// สร้างตัวละครเงาผีจำลอง (Fallback หากผู้เล่นยังไม่ได้กำหนด ghostPrefab ใน Inspector)
    /// </summary>
    private GameObject CreatePlaceholderGhost(Vector3 position, Quaternion rotation)
    {
        GameObject ghost = new GameObject($"Ghost_Placeholder_{activeGhostInstances.Count + 1}");
        ghost.transform.position = position;
        ghost.transform.rotation = rotation;

        // ลำตัวหมอบคลาน
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "GhostBody";
        body.transform.SetParent(ghost.transform);
        body.transform.localPosition = new Vector3(0, 0.45f, 0);
        body.transform.localRotation = Quaternion.Euler(45f, 0, 0);
        body.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);

        // หัว
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "GhostHead";
        head.transform.SetParent(ghost.transform);
        head.transform.localPosition = new Vector3(0, 0.8f, 0.35f);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        // Material สีดำคล้ำสยองขวัญ
        Renderer bodyRend = body.GetComponent<Renderer>();
        Renderer headRend = head.GetComponent<Renderer>();
        Shader targetShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
        Material ghostMat = new Material(targetShader);
        ghostMat.color = new Color(0.12f, 0.04f, 0.04f, 0.95f);
        if (bodyRend != null) bodyRend.material = ghostMat;
        if (headRend != null) headRend.material = ghostMat;

        // ดวงตาสีแดงเรืองแสงหลอน
        GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.name = "EyeL";
        eyeL.transform.SetParent(head.transform);
        eyeL.transform.localPosition = new Vector3(-0.25f, 0.1f, 0.4f);
        eyeL.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);

        GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.name = "EyeR";
        eyeR.transform.SetParent(head.transform);
        eyeR.transform.localPosition = new Vector3(0.25f, 0.1f, 0.4f);
        eyeR.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);

        Material eyeMat = new Material(targetShader);
        eyeMat.color = Color.red;
        if (eyeMat.HasProperty("_EmissionColor"))
        {
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", Color.red * 2.5f);
        }
        Renderer eLRend = eyeL.GetComponent<Renderer>();
        Renderer eRRend = eyeR.GetComponent<Renderer>();
        if (eLRend != null) eLRend.material = eyeMat;
        if (eRRend != null) eRRend.material = eyeMat;

        // ลบ Colliders เพื่อไม่ให้ขวางทางเดิน
        foreach (var col in ghost.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }

        return ghost;
    }

    private void ClearActiveGhostInstances()
    {
        foreach (var ghost in activeGhostInstances)
        {
            if (ghost != null) Destroy(ghost);
        }
        activeGhostInstances.Clear();
    }

    public void TriggerCleanseJumpscare()
    {
        if (jumpscareCoroutine != null) StopCoroutine(jumpscareCoroutine);
        jumpscareCoroutine = StartCoroutine(CleanseJumpscareRoutine());
    }

    private IEnumerator CleanseJumpscareRoutine()
    {
        isJumpscareActive = true;
        jumpscareAlpha = 1f;

        TriggerScreenShake(0.4f, 18f);

        AudioClip soundToPlay = cleanseJumpscareSound != null ? cleanseJumpscareSound : ghostAttachSound;
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // ค้างจอดำ + คำว่า BOO! ไว้สักครู่
        yield return new WaitForSeconds(jumpscareHoldDuration);

        // ค่อยๆ Fade จอดำหายไป
        float fadeDuration = jumpscareFadeDuration > 0.1f ? jumpscareFadeDuration : 1.5f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            jumpscareAlpha = Mathf.Clamp01(1f - (t / fadeDuration));
            yield return null;
        }

        jumpscareAlpha = 0f;
        isJumpscareActive = false;
    }

    public void TriggerScreenShake(float duration, float intensity)
    {
        screenShakeTimer = duration;
        screenShakeIntensity = intensity;
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.LogError("[GhostCurseManager] 💀 GAME OVER! ผีร้ายเข้าครอบงำวิญญาณครบ 3 ตัวแล้ว");

        if (gameOverSound != null)
        {
            AudioSource.PlayClipAtPoint(gameOverSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        if (GameEndingManager.Instance != null)
        {
            GameEndingManager.Instance.TriggerEnding(GameEndingType.GhostGameOver);
        }
        else
        {
            Debug.LogError("ไม่พบ GameEndingManager ในฉาก!");
            // ปิดการควบคุมผู้เล่น และปลดล็อคเมาส์ เผื่อไม่มี GameManager
            if (playerController != null) playerController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// รีเซ็ตและเริ่มเล่นใหม่
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        currentGhostCount = 0;
        ClearActiveGhostInstances();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        ClearActiveGhostInstances();
    }

    void OnGUI()
    {
        // ----------------------------------------------------
        // 1. เอฟเฟคขอบจอหลอน (Ghost Haunting Screen Overlay)
        // ----------------------------------------------------
        if (enableScreenEffects && currentGhostCount > 0 && vignetteTex != null)
        {
            float pulse = Mathf.Sin(pulseTimer) * 0.15f + 0.85f;
            float ghostAlpha = (currentGhostCount / 3f) * 0.7f * pulse;

            // ขอบจอดำ
            GUI.color = new Color(0f, 0f, 0f, ghostAlpha * 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTex);

            // ขอบจอสีแดงสยองขวัญ
            GUI.color = new Color(0.8f, 0.05f, 0.05f, ghostAlpha * 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTex);

            GUI.color = Color.white;
        }

        // ----------------------------------------------------
        // 2. ตัวเลขวงกลมสีแดงมุมขวาบน (ปิดไว้เมื่อใช้โมเดลในฉาก)
        // ----------------------------------------------------
        if (showNumberBadgeOnUI)
        {
            DrawGhostIndicatorBadge();
        }

        // ----------------------------------------------------
        // 3. Jumpscare Overlay ("BOO!" + Fade จอดำ)
        // ----------------------------------------------------
        if (isJumpscareActive && jumpscareAlpha > 0f)
        {
            GUI.depth = -1000; // วาดทับไว้บนสุด
            GUI.color = new Color(0f, 0f, 0f, jumpscareAlpha);
            if (blackTex != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
            }
            else
            {
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            }

            GUIStyle booStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(100 * (0.85f + jumpscareAlpha * 0.15f)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            booStyle.normal.textColor = new Color(0.95f, 0.1f, 0.1f, jumpscareAlpha);

            float jitterX = (jumpscareAlpha > 0.6f) ? Random.Range(-5f, 5f) : 0f;
            float jitterY = (jumpscareAlpha > 0.6f) ? Random.Range(-5f, 5f) : 0f;

            GUI.Label(new Rect(jitterX, jitterY, Screen.width, Screen.height), jumpscareText, booStyle);

            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// วาดวงกลมสีแดงแสดงจำนวนผีร้ายที่มุมบนขวา
    /// </summary>
    private void DrawGhostIndicatorBadge()
    {
        float badgeSize = 65f;
        float paddingX = 25f;
        float paddingY = 25f;
        float posX = Screen.width - badgeSize - paddingX;
        float posY = paddingY;

        // วาดภาพวงกลมสีแดง
        if (redCircleTex != null)
        {
            // เอฟเฟคเต้นตุบๆ ตามจังหวะหัวใจถ้ามีผีเกาะ
            float scaleMod = (currentGhostCount > 0) ? (Mathf.Sin(pulseTimer * 1.5f) * 3f) : 0f;
            Rect circleRect = new Rect(posX - scaleMod / 2f, posY - scaleMod / 2f, badgeSize + scaleMod, badgeSize + scaleMod);

            GUI.color = (currentGhostCount > 0) ? new Color(1f, 0.3f, 0.3f, 0.95f) : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            GUI.DrawTexture(circleRect, redCircleTex);
            GUI.color = Color.white;
        }

        // วาดตัวเลขตรงกลางวงกลม
        GUIStyle numStyle = new GUIStyle(GUI.skin.label);
        numStyle.fontSize = 32;
        numStyle.fontStyle = FontStyle.Bold;
        numStyle.alignment = TextAnchor.MiddleCenter;
        numStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(posX, posY - 2f, badgeSize, badgeSize), currentGhostCount.ToString(), numStyle);

        // ข้อความกำกับด้านล่างวงกลม
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 11;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.UpperCenter;
        labelStyle.normal.textColor = currentGhostCount > 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.8f, 0.8f, 0.8f);

        string label = currentGhostCount > 0 ? $"ผีร้ายตามติด ({currentGhostCount}/3)" : "ปลอดภัย";
        GUI.Label(new Rect(posX - 40f, posY + badgeSize + 2f, badgeSize + 80f, 20f), label, labelStyle);
    }

    // ==========================================
    // ContextMenu สำหรับทดสอบเพิ่ม/ล้างผีใน Inspector
    // ==========================================
    [ContextMenu("🧪 ทดสอบ: เพิ่มผี +1 (AttachGhost)")]
    public void TestAttachGhost() => AttachGhost("ทดสอบจาก Inspector");

    [ContextMenu("🧪 ทดสอบ: ชำระล้างผี -1 (CleanseGhosts)")]
    public void TestCleanseGhost() => CleanseGhosts(1);
}
