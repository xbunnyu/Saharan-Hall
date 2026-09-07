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

    [Header("Visual & Effects (เอฟเฟคหลอน)")]
    public bool enableScreenEffects = true;
    public Color ghostVignetteColor = new Color(0.7f, 0.05f, 0.05f, 0.35f);
    public AudioClip ghostAttachSound;
    public AudioClip gameOverSound;

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
    /// เพิ่มจำนวนผีร้ายตามติดตัวผู้เล่น (+1)
    /// </summary>
    public void AttachGhost(string reason = "")
    {
        if (isGameOver) return;

        currentGhostCount = Mathf.Min(currentGhostCount + 1, maxGhostLimit);
        TriggerScreenShake(0.6f, 12f);

        Debug.LogWarning($"[GhostCurseManager] 👻 โดนผีร้ายตามติด! ตอนนี้มีผีเกาะ {currentGhostCount}/{maxGhostLimit} ตัว ({reason})");

        if (ghostAttachSound != null)
        {
            AudioSource.PlayClipAtPoint(ghostAttachSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                $"<color=#FF2020>⚠️ วิญญาณชั่วร้ายตามติดตัวคุณ! ({currentGhostCount}/{maxGhostLimit})</color>", 3.5f);
        }

        if (currentGhostCount >= maxGhostLimit)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// ปลดปล่อยหรือล้างผีร้ายออกจากตัว
    /// </summary>
    public void CleanseGhosts(int amount = 1)
    {
        if (isGameOver) return;
        currentGhostCount = Mathf.Max(0, currentGhostCount - amount);
        Debug.Log($"[GhostCurseManager] ✨ ชำระล้างวิญญาณชั่วร้ายแล้ว เหลือ {currentGhostCount}/{maxGhostLimit} ตัว");

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                $"<color=#00FF7F>✨ ชำระล้างวิญญาณชั่วร้ายแล้ว (คงเหลือ {currentGhostCount}/{maxGhostLimit})</color>", 3.0f);
        }
    }

    public void TriggerScreenShake(float duration, float intensity)
    {
        screenShakeTimer = duration;
        screenShakeIntensity = intensity;
    }

    /// <summary>
    /// สั่งจบเกมเมื่อผีเกาะครบ 3 ตัว
    /// </summary>
    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.LogError("[GhostCurseManager] 💀 GAME OVER! ผีร้ายเข้าครอบงำวิญญาณครบ 3 ตัวแล้ว");

        if (gameOverSound != null)
        {
            AudioSource.PlayClipAtPoint(gameOverSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // ปิดการควบคุมผู้เล่น และปลดล็อคเมาส์
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// รีเซ็ตและเริ่มเล่นใหม่
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        currentGhostCount = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        // 2. ตัวเลขวงกลมสีแดงมุมขวาบน (ตามภาพตัวอย่าง)
        // ----------------------------------------------------
        DrawGhostIndicatorBadge();

        // ----------------------------------------------------
        // 3. หน้าต่าง GAME OVER เมื่อผีครบ 3 ตัว
        // ----------------------------------------------------
        if (isGameOver)
        {
            DrawGameOverModal();
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

    /// <summary>
    /// หน้าต่าง Game Over เมื่อโดนผีเกาะครบ 3 ตัว
    /// </summary>
    private void DrawGameOverModal()
    {
        // แผ่นหลังมืดทึบทั้งจอ
        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex != null ? blackTex : Texture2D.whiteTexture);
        GUI.color = Color.white;

        float boxW = Mathf.Min(560f, Screen.width * 0.9f);
        float boxH = 340f;
        float boxX = (Screen.width - boxW) / 2f;
        float boxY = (Screen.height - boxH) / 2f;

        // กรอบข้อความ
        GUI.color = new Color(0.15f, 0.02f, 0.02f, 0.9f);
        GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none);
        GUI.color = Color.white;

        // หัวข้อ GAME OVER
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 36;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.UpperCenter;
        titleStyle.normal.textColor = new Color(1f, 0.15f, 0.15f);
        GUI.Label(new Rect(boxX, boxY + 25, boxW, 50), "💀 วิญญาณแตกดับ 💀", titleStyle);

        // เนื้อหา Game Over
        GUIStyle descStyle = new GUIStyle(GUI.skin.label);
        descStyle.fontSize = 16;
        descStyle.alignment = TextAnchor.MiddleCenter;
        descStyle.wordWrap = true;
        descStyle.normal.textColor = new Color(0.9f, 0.85f, 0.85f);

        string msg = "คุณท่องคาถาผิดพลาดจนภูตผีปีศาจเข้าครอบงำครบ 3 ตน\n" +
                     "พลังชีวิตและวิญญาณของคุณถูกกลืนกินจนหมดสิ้น...\n\n" +
                     "<color=#FF6666>เกมจบลงแล้ว</color>";

        GUI.Label(new Rect(boxX + 30, boxY + 85, boxW - 60, 120), msg, descStyle);

        // ปุ่มเริ่มเล่นใหม่
        float btnW = 240f;
        float btnH = 50f;
        float btnX = boxX + (boxW - btnW) / 2f;
        float btnY = boxY + boxH - 75f;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 16;
        btnStyle.fontStyle = FontStyle.Bold;

        GUI.backgroundColor = new Color(0.85f, 0.2f, 0.2f);
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "🔄 เริ่มเล่นใหม่ (Restart)", btnStyle))
        {
            RestartGame();
        }
        GUI.backgroundColor = Color.white;
    }
}
