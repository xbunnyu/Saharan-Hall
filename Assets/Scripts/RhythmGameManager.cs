using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ตัวจัดการมินิเกม Rhythm Game (W A S D) ท่องคาถา
/// - จำลอง UI 4 เลนแนวตั้ง (W, A, S, D) ตามภาพตัวอย่าง
/// - แถบความคืบหน้าการท่องคาถา (%) ด้านบน
/// - โซนรับจังหวะ (Target Receptors) ด้านล่าง พร้อมภาพมือพนมทำพิธี
/// - ระบบคำนวณความแม่นยำ Perfect, Good, Miss และคอมโบ
/// - ระบบสร้างเสียงระฆัง/ขันสวดมนต์อัตโนมัติ (Procedural Audio Synth)
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance { get; private set; }

    [Header("Lane & Key Mapping")]
    public readonly string[] LaneLabels = { "W", "A", "S", "D" };
    public readonly Key[] LaneKeys = { Key.W, Key.A, Key.S, Key.D };

    [Header("Audio Settings")]
    public AudioClip chantHitSound;
    public AudioClip chantMissSound;
    public AudioClip chantSuccessSound;
    public AudioClip chantFailSound;

    [Header("Game State (Read Only)")]
    public bool isPlaying = false;
    public float chantingProgress = 0f; // 0% - 100%
    public int currentScore = 0;
    public int comboCount = 0;
    public int maxCombo = 0;
    public int perfectCount = 0;
    public int goodCount = 0;
    public int missCount = 0;

    // Note Data Structure
    private class RhythmNote
    {
        public int lane;            // 0:W, 1:A, 2:S, 3:D
        public float targetTime;    // เวลาที่โน้ตควรถึงจุดกด (วินาที)
        public bool isHit = false;
        public bool isMissed = false;
    }

    private List<RhythmNote> activeNotes = new List<RhythmNote>();
    private QuestData currentQuest;
    private Action<bool> onCompleteCallback;
    private float gameStartTime = 0f;
    private float noteTravelTime = 2.4f; // เวลาที่โน้ตใช้เดินทางจากบนลงล่าง
    private float gameDuration = 0f;
    private int totalNotesCount = 0;
    private bool isGameEnding = false;
    private PlayerController playerController;

    // Visual feedback popups
    private string lastJudgmentText = "";
    private Color lastJudgmentColor = Color.white;
    private float lastJudgmentTimer = 0f;
    private float[] lanePressFeedback = new float[4]; // ตัวจับเวลาแสงกระพริบตอนกด

    // Procedural Textures & Audio
    private Texture2D circleTalismanTex;
    private Texture2D targetReceptorTex;
    private Texture2D glowingLineTex;
    private Texture2D progressBarBgTex;
    private Texture2D progressBarFillTex;
    private Texture2D prayingHandsTex;
    private AudioSource synthAudioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CreateProceduralTextures();
        SetupAudioSource();
    }

    void Update()
    {
        if (!isPlaying) return;

        float elapsedTime = Time.time - gameStartTime;

        // อัปเดตตัวนับ Feedback แสงกด
        for (int i = 0; i < 4; i++)
        {
            if (lanePressFeedback[i] > 0)
            {
                lanePressFeedback[i] -= Time.deltaTime * 3.5f;
            }
        }

        if (lastJudgmentTimer > 0)
        {
            lastJudgmentTimer -= Time.deltaTime;
        }

        // 1. ตรวจสอบการกดปุ่มของผู้เล่น
        HandlePlayerInput(elapsedTime);

        // 2. ตรวจสอบโน้ตที่หลุดเลน (Miss Auto-check)
        CheckMissedNotes(elapsedTime);

        // 3. ตรวจสอบการสิ้นสุดมินิเกม
        if (elapsedTime >= gameDuration + 1.2f && !isGameEnding)
        {
            StartCoroutine(FinishGameRoutine());
        }
    }

    /// <summary>
    /// สั่งเริ่มเล่นมินิเกมท่องคาถา Rhythm Game
    /// </summary>
    public void StartRhythmGame(QuestData quest, Action<bool> onComplete)
    {
        currentQuest = quest;
        onCompleteCallback = onComplete;

        // รีเซ็ตสถิติ
        isPlaying = true;
        isGameEnding = false;
        chantingProgress = 0f;
        currentScore = 0;
        comboCount = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        lastJudgmentText = "";
        lastJudgmentTimer = 0f;

        // กำหนดความยาก และสร้าง Chart โน้ต
        GenerateChartForDifficulty(quest != null ? quest.difficulty : QuestDifficulty.Easy);

        // ล็อคการควบคุมและการหันหน้าของผู้เล่น
        LockPlayerControls(true);

        gameStartTime = Time.time;
        PlaySynthChime(440f, 0.3f); // เสียงระฆังเริ่มพิธี

        Debug.Log($"[RhythmGame] 🎵 เริ่มพิธีท่องคาถา! โน้ตทั้งหมด: {totalNotesCount} ตัว, เวลา: {gameDuration:F1}s");
    }

    /// <summary>
    /// สร้าง Note Chart อัตโนมัติตามระดับความยากของเควส
    /// </summary>
    private void GenerateChartForDifficulty(QuestDifficulty difficulty)
    {
        activeNotes.Clear();

        float initialDelay = 1.6f;
        float noteInterval = 0.8f;
        int noteCount = 16;
        noteTravelTime = 2.4f;

        switch (difficulty)
        {
            case QuestDifficulty.Easy:
                noteCount = 16;
                noteInterval = 0.85f;
                noteTravelTime = 2.6f;
                break;

            case QuestDifficulty.Medium:
                noteCount = 24;
                noteInterval = 0.68f;
                noteTravelTime = 2.2f;
                break;

            case QuestDifficulty.Hard:
                noteCount = 36;
                noteInterval = 0.52f;
                noteTravelTime = 1.85f;
                break;

            case QuestDifficulty.VeryHard:
                noteCount = 48;
                noteInterval = 0.40f;
                noteTravelTime = 1.55f;
                break;
        }

        // ถ้าเควสกำหนดค่าเฉพาะมา ให้ใช้ค่านั้น
        if (currentQuest != null)
        {
            if (currentQuest.customNoteCount > 0) noteCount = currentQuest.customNoteCount;
            if (currentQuest.customNoteSpeed > 0) noteTravelTime = currentQuest.customNoteSpeed;
        }

        totalNotesCount = noteCount;
        float currentTime = initialDelay;
        int lastLane = -1;

        // รูปแบบจังหวะที่เป็นเอกลักษณ์สไตล์สวดมนต์บทสวด
        for (int i = 0; i < noteCount; i++)
        {
            // สุ่มเลนโดยไม่ให้ซ้ำเลนเดิมติดกันเกิน 2 ครั้ง
            int lane = UnityEngine.Random.Range(0, 4);
            if (lane == lastLane && UnityEngine.Random.value < 0.7f)
            {
                lane = (lane + UnityEngine.Random.Range(1, 4)) % 4;
            }
            lastLane = lane;

            activeNotes.Add(new RhythmNote
            {
                lane = lane,
                targetTime = currentTime + noteTravelTime,
                isHit = false,
                isMissed = false
            });

            // เพิ่มความหลากหลายของจังหวะ (เช่น จังหวะคู่ หรือเว้นวรรคหายใจ)
            if (i % 6 == 5)
            {
                currentTime += noteInterval * 1.5f; // เว้นวรรคจังหวะบทสวด
            }
            else
            {
                currentTime += noteInterval;
            }
        }

        gameDuration = currentTime + noteTravelTime;
    }

    /// <summary>
    /// ตรวจสอบการกดปุ่มของผู้เล่น (รองรับทั้ง Keyboard และ Gamepad)
    /// </summary>
    private void HandlePlayerInput(float elapsedTime)
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        for (int lane = 0; lane < 4; lane++)
        {
            bool pressed = false;

            // ตรวจสอบผ่าน New Input System
            switch (lane)
            {
                case 0: // W / Up
                    pressed = (keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
                           || (gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame));
                    break;
                case 1: // A / Left
                    pressed = (keyboard != null && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame))
                           || (gamepad != null && (gamepad.dpad.left.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame));
                    break;
                case 2: // S / Down
                    pressed = (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
                           || (gamepad != null && (gamepad.dpad.down.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame));
                    break;
                case 3: // D / Right
                    pressed = (keyboard != null && (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
                           || (gamepad != null && (gamepad.dpad.right.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame));
                    break;
            }

            if (pressed)
            {
                lanePressFeedback[lane] = 1f;
                JudgeHit(lane, elapsedTime);
            }
        }
    }

    /// <summary>
    /// ประเมินความแม่นยำของการกดในแต่ละเลน
    /// </summary>
    private void JudgeHit(int lane, float elapsedTime)
    {
        RhythmNote closestNote = null;
        float closestDiff = float.MaxValue;

        foreach (var note in activeNotes)
        {
            if (note.lane == lane && !note.isHit && !note.isMissed)
            {
                float diff = Mathf.Abs(note.targetTime - elapsedTime);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestNote = note;
                }
            }
        }

        // โบนัสช่วยเหลือจากระดับโต๊ะหมู่บูชา (Buddhist Altar Assist Bonus)
        float altarBonus = 0f;
        if (BuddhistAltarManager.Instance != null)
        {
            altarBonus = (BuddhistAltarManager.Instance.currentLevel - 1) * 0.025f;
        }

        float perfectWindow = 0.14f + altarBonus;
        float goodWindow = 0.28f + altarBonus * 1.5f;

        // Timing Windows:
        // Perfect: <= perfectWindow
        // Good:    <= goodWindow
        // Miss:    > goodWindow
        if (closestNote != null && closestDiff <= goodWindow + 0.05f)
        {
            closestNote.isHit = true;
            comboCount++;
            if (comboCount > maxCombo) maxCombo = comboCount;

            if (closestDiff <= perfectWindow)
            {
                // 🌟 PERFECT
                perfectCount++;
                currentScore += 100 + (comboCount * 5);
                chantingProgress = Mathf.Clamp01(chantingProgress + (1.0f / totalNotesCount) * 1.15f);

                lastJudgmentText = "🌟 PERFECT! 🌟";
                lastJudgmentColor = new Color(1f, 0.85f, 0.2f);
                lastJudgmentTimer = 0.55f;

                PlaySynthChime(523.25f + lane * 60f, 0.2f); // High chime
            }
            else
            {
                // ✨ GOOD
                goodCount++;
                currentScore += 60 + (comboCount * 2);
                chantingProgress = Mathf.Clamp01(chantingProgress + (1.0f / totalNotesCount) * 0.95f);

                lastJudgmentText = "✨ GOOD! ✨";
                lastJudgmentColor = new Color(0.9f, 0.95f, 1f);
                lastJudgmentTimer = 0.45f;

                PlaySynthChime(392.0f + lane * 40f, 0.18f); // Mid chime
            }
        }
        else
        {
            // กดผิดจังหวะ หรือไม่มีโน้ตใกล้เคียง
            if (closestNote != null && closestDiff <= 0.5f)
            {
                TriggerMiss(closestNote);
            }
        }
    }

    /// <summary>
    /// ตรวจสอบโน้ตที่หลุดเลยเป้าไปด้านล่าง
    /// </summary>
    private void CheckMissedNotes(float elapsedTime)
    {
        foreach (var note in activeNotes)
        {
            if (!note.isHit && !note.isMissed)
            {
                if (elapsedTime - note.targetTime > 0.32f)
                {
                    TriggerMiss(note);
                }
            }
        }
    }

    [Header("Mistake Limit (กฎการพลาด)")]
    [Tooltip("จำนวนครั้งสูงสุดที่อนุญาตให้พลาดได้ (พลาดเกินจำนวนนี้ = ล้มเหลวทันที)")]
    public int maxAllowedMisses = 2;

    private void TriggerMiss(RhythmNote note)
    {
        note.isMissed = true;
        missCount++;
        comboCount = 0; // สลายคอมโบ

        // ลดความคืบหน้า
        chantingProgress = Mathf.Clamp01(chantingProgress - 0.05f);

        PlaySynthMissTone();
        if (GhostCurseManager.Instance != null)
        {
            GhostCurseManager.Instance.TriggerScreenShake(0.2f, 5.0f);
        }

        // ตรวจสอบเงื่อนไข: ห้ามพลาดเกิน 2 ครั้ง (ครั้งที่ 3 = ล้มเหลวทันที)
        if (missCount > maxAllowedMisses)
        {
            lastJudgmentText = "💀 พลาดเกิน 2 ครั้ง! พิธีกรรมล้มเหลว 💀";
            lastJudgmentColor = new Color(1f, 0.15f, 0.15f);
            lastJudgmentTimer = 2.5f;

            if (!isGameEnding)
            {
                StartCoroutine(FinishGameRoutine(forcedFail: true));
            }
        }
        else
        {
            int remainingChances = maxAllowedMisses - missCount;
            lastJudgmentText = $"❌ MISS ❌ (เตือน: เหลือโอกาส {remainingChances} ครั้ง)";
            lastJudgmentColor = new Color(1f, 0.35f, 0.35f);
            lastJudgmentTimer = 0.65f;
        }
    }

    private IEnumerator FinishGameRoutine(bool forcedFail = false)
    {
        isGameEnding = true;

        // คำนวณเปอร์เซ็นต์ผลคะแนนรวม
        float maxPossibleScore = totalNotesCount * 100f;
        float actualPercentage = maxPossibleScore > 0 ? (currentScore / maxPossibleScore) * 100f : 0f;
        int passRequired = currentQuest != null ? currentQuest.passPercentage : 60;

        // กฎการผ่าน: พลาดไม่เกิน 2 ครั้ง และคะแนน/ความคืบหน้าถึงเกณฑ์
        bool isPassed = !forcedFail && (missCount <= maxAllowedMisses) && (actualPercentage >= passRequired || chantingProgress >= 0.70f);

        if (isPassed)
        {
            lastJudgmentText = "✨ ทำพิธีสำเร็จลุล่วง! ✨";
            lastJudgmentColor = new Color(0.2f, 1f, 0.5f);
            lastJudgmentTimer = 2.0f;
            PlaySynthChime(659.25f, 0.5f); // Victory high bell
        }
        else
        {
            if (missCount > maxAllowedMisses)
            {
                lastJudgmentText = "💀 พลาดเกิน 2 ครั้ง! พิธีกรรมล้มเหลว 💀";
            }
            else
            {
                lastJudgmentText = "💀 พิธีกรรมล้มเหลว! 💀";
            }
            lastJudgmentColor = new Color(1f, 0.2f, 0.2f);
            lastJudgmentTimer = 2.0f;
            PlaySynthFailTone();
        }

        yield return new WaitForSeconds(1.8f);

        isPlaying = false;
        isGameEnding = false;

        // ปลดล็อคคืนค่าการควบคุมให้ผู้เล่น
        LockPlayerControls(false);

        onCompleteCallback?.Invoke(isPassed);
    }

    /// <summary>
    /// หยุดการเคลื่อนที่และมุมกล้องของผู้เล่นระหว่างเล่นมินิเกม Rhythm
    /// </summary>
    private void LockPlayerControls(bool lockState)
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController != null)
        {
            playerController.enabled = !lockState;
        }

        if (lockState)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (GhostCurseManager.Instance != null && GhostCurseManager.Instance.isGameOver)
            {
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ==========================================
    // OnGUI Rendering (การวาด UI 4 เลนแบบประณีต)
    // ==========================================
    void OnGUI()
    {
        if (!isPlaying) return;

        float elapsedTime = Time.time - gameStartTime;

        // 1. วาดหัวข้อด้านบนซ้าย ("ท่องคาถา") และแถบแสดงโอกาสพลาด
        DrawHeaderTopLeft();

        // 2. วาดแถบความคืบหน้าตรงกลางด้านบน ("ความคืบหน้าการท่องคาถา")
        DrawProgressBarTopCenter();

        // 3. วาดเส้นเลนและโซนรับโน้ต (4 เลน W A S D)
        DrawRhythmBoard(elapsedTime);

        // 4. วาดตัวหนังสือผลการกด (Perfect/Good/Miss/Combo)
        DrawJudgementAndCombo();

        // 5. วาดคำอธิบายสัญลักษณ์ด้านล่าง (Perfect, Good, Miss Legend)
        DrawLegendBottom();
    }

    /// <summary>
    /// วาดหัวข้อด้านบนซ้าย และแถบแสดงโอกาสพลาด (Strike Counter)
    /// </summary>
    private void DrawHeaderTopLeft()
    {
        float posX = 40f;
        float posY = 30f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);

        GUI.Label(new Rect(posX, posY, 300, 35), "ท่องคาถา", titleStyle);

        // เส้นขีดสีแดงใต้หัวข้อ
        GUI.color = new Color(0.85f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(posX, posY + 32, 110, 3), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 13;
        subStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        GUI.Label(new Rect(posX, posY + 40, 300, 32), "ท่องคาถาให้ครบถ้วนเพื่อทำพิธีสำเร็จ", subStyle);

        // แสดงแถบจำนวนครั้งที่พลาด (Strike Counter / ห้ามพลาดเกิน 2 ครั้ง)
        GUIStyle strikeStyle = new GUIStyle(GUI.skin.label);
        strikeStyle.fontSize = 12;
        strikeStyle.fontStyle = FontStyle.Bold;

        string strike1 = missCount >= 1 ? "<color=#FF3333>✖</color>" : "<color=#00FF7F>✔</color>";
        string strike2 = missCount >= 2 ? "<color=#FF3333>✖</color>" : "<color=#00FF7F>✔</color>";
        string missColor = missCount == 0 ? "#00FF7F" : (missCount <= maxAllowedMisses ? "#FFD700" : "#FF3333");

        strikeStyle.normal.textColor = Color.white;
        string strikeText = $"โอกาสพลาด: [{strike1} {strike2}] (<color={missColor}>{missCount}/{maxAllowedMisses}</color>) <size=10><color=#FF9999>ห้ามพลาดเกิน 2 ครั้ง</color></size>";
        GUI.Label(new Rect(posX, posY + 72, 350, 24), strikeText, strikeStyle);
    }

    /// <summary>
    /// วาดแถบความคืบหน้าการท่องคาถาด้านบนกลางจอ
    /// </summary>
    private void DrawProgressBarTopCenter()
    {
        float barWidth = Mathf.Min(480f, Screen.width * 0.45f);
        float barHeight = 8f;
        float posX = (Screen.width - barWidth) / 2f;
        float posY = 35f;

        // ข้อความหัวข้อความคืบหน้า
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.UpperCenter;
        titleStyle.normal.textColor = new Color(1f, 0.92f, 0.6f);

        GUI.Label(new Rect(posX - 40, posY - 22, barWidth + 80, 20), "ความคืบหน้าการท่องคาถา", titleStyle);

        // ปีกซ้ายและขวา <[  ]>
        GUIStyle wingStyle = new GUIStyle(GUI.skin.label);
        wingStyle.fontSize = 14;
        wingStyle.fontStyle = FontStyle.Bold;
        wingStyle.normal.textColor = new Color(1f, 0.75f, 0.2f);
        GUI.Label(new Rect(posX - 35, posY - 6, 30, 20), "⬖⬗", wingStyle);
        GUI.Label(new Rect(posX + barWidth + 8, posY - 6, 30, 20), "⬖⬗", wingStyle);

        // เปอร์เซ็นต์ตัวเลขด้านขวา
        int displayPercent = Mathf.RoundToInt(chantingProgress * 100f);
        GUIStyle pctStyle = new GUIStyle(GUI.skin.label);
        pctStyle.fontSize = 16;
        pctStyle.fontStyle = FontStyle.Bold;
        pctStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(posX + barWidth + 38, posY - 7, 60, 24), $"{displayPercent}%", pctStyle);

        // รางแถบความคืบหน้า (Background)
        GUI.color = new Color(0.2f, 0.15f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(posX, posY, barWidth, barHeight), Texture2D.whiteTexture);

        // เส้นแถบความคืบหน้า (Golden Fill)
        float fillW = barWidth * chantingProgress;
        GUI.color = new Color(1f, 0.75f, 0.1f, 0.95f);
        GUI.DrawTexture(new Rect(posX, posY, fillW, barHeight), Texture2D.whiteTexture);

        // หัวเข็มประกายสีทองตรงปลายแถบ
        if (fillW > 4)
        {
            GUI.color = new Color(1f, 1f, 0.7f, 1f);
            GUI.DrawTexture(new Rect(posX + fillW - 3, posY - 3, 6, barHeight + 6), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }

    /// <summary>
    /// วาด 4 เลนแนวตั้ง (W A S D), โน้ตที่เลื่อนลงมา และโซนกดด้านล่าง
    /// </summary>
    private void DrawRhythmBoard(float elapsedTime)
    {
        float boardWidth = Mathf.Min(440f, Screen.width * 0.42f);
        float laneSpacing = boardWidth / 4f;
        float boardLeft = (Screen.width - boardWidth) / 2f;
        float topY = 120f;
        float targetY = Screen.height - 200f; // ตำแหน่งโซนกดด้านล่าง
        float travelDist = targetY - topY;
        float noteSize = 64f;

        // 1. วาดเส้นแสงแนวตั้งทั้ง 4 เลน
        for (int lane = 0; lane < 4; lane++)
        {
            float laneCenterX = boardLeft + (lane * laneSpacing) + (laneSpacing / 2f);

            // เส้นแสงบางๆ
            GUI.color = new Color(1f, 0.85f, 0.4f, 0.25f);
            GUI.DrawTexture(new Rect(laneCenterX - 1f, topY, 2f, travelDist + 50f), Texture2D.whiteTexture);

            // แสงสว่างเมื่อกดโดน
            if (lanePressFeedback[lane] > 0)
            {
                GUI.color = new Color(1f, 0.9f, 0.4f, lanePressFeedback[lane] * 0.45f);
                GUI.DrawTexture(new Rect(laneCenterX - 12f, topY, 24f, travelDist + 50f), Texture2D.whiteTexture);
            }
        }
        GUI.color = Color.white;

        // 2. วาดภาพมือพนมทำพิธีตรงกลางด้านล่าง
        DrawPrayingHands(boardLeft + boardWidth / 2f, targetY + 60f);

        // 3. วาดโซนรับจังหวะ (Target Receptors) ด้านล่างทั้ง 4 เลน
        for (int lane = 0; lane < 4; lane++)
        {
            float laneCenterX = boardLeft + (lane * laneSpacing) + (laneSpacing / 2f);
            DrawTargetReceptor(lane, laneCenterX, targetY, noteSize);
        }

        // 4. วาดตัวโน้ตที่กำลังเลื่อนลงมา
        foreach (var note in activeNotes)
        {
            if (note.isHit || note.isMissed) continue;

            float timeDiff = note.targetTime - elapsedTime;

            // แสดงเฉพาะโน้ตที่อยู่ในระยะการเดินทาง
            if (timeDiff <= noteTravelTime && timeDiff >= -0.3f)
            {
                float progress = 1f - (timeDiff / noteTravelTime);
                float noteY = topY + (progress * travelDist);
                float laneCenterX = boardLeft + (note.lane * laneSpacing) + (laneSpacing / 2f);

                DrawFallingNote(note.lane, laneCenterX, noteY, noteSize);
            }
        }
    }

    /// <summary>
    /// วาดวงแหวนเป้าหมายด้านล่าง (Target Receptor)
    /// </summary>
    private void DrawTargetReceptor(int lane, float centerX, float centerY, float size)
    {
        float feedback = lanePressFeedback[lane];
        float currentSize = size + (feedback * 8f);
        Rect r = new Rect(centerX - currentSize / 2f, centerY - currentSize / 2f, currentSize, currentSize);

        // แสงออร่ารอบโซน
        GUI.color = new Color(1f, 0.85f, 0.4f, 0.7f + feedback * 0.3f);
        if (targetReceptorTex != null)
        {
            GUI.DrawTexture(r, targetReceptorTex);
        }
        else
        {
            GUI.DrawTexture(r, circleTalismanTex != null ? circleTalismanTex : Texture2D.whiteTexture);
        }

        // ตัวอักษร W A S D ตรงกลางเป้าหมาย
        GUIStyle keyStyle = new GUIStyle(GUI.skin.label);
        keyStyle.fontSize = Mathf.RoundToInt(22 + feedback * 4);
        keyStyle.fontStyle = FontStyle.Bold;
        keyStyle.alignment = TextAnchor.MiddleCenter;
        keyStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(centerX - size / 2f, centerY - size / 2f, size, size), LaneLabels[lane], keyStyle);
        GUI.color = Color.white;
    }

    /// <summary>
    /// วาดตัวโน้ตยันต์คาถาที่เลื่อนลงมา
    /// </summary>
    private void DrawFallingNote(int lane, float centerX, float centerY, float size)
    {
        Rect r = new Rect(centerX - size / 2f, centerY - size / 2f, size, size);

        // วาดภาพวงกลมยันต์
        GUI.color = new Color(0.95f, 0.9f, 0.8f, 0.95f);
        if (circleTalismanTex != null)
        {
            GUI.DrawTexture(r, circleTalismanTex);
        }
        else
        {
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }

        // ตัวอักษร W, A, S, D ภายในโน้ต
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 22;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = new Color(0.12f, 0.1f, 0.08f);

        GUI.Label(r, LaneLabels[lane], textStyle);
        GUI.color = Color.white;
    }

    /// <summary>
    /// วาดมือพนมทำพิธีตรงกลางล่าง
    /// </summary>
    private void DrawPrayingHands(float centerX, float centerY)
    {
        if (prayingHandsTex == null) return;

        float handW = 200f;
        float handH = 140f;
        Rect handRect = new Rect(centerX - handW / 2f, centerY, handW, handH);

        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.DrawTexture(handRect, prayingHandsTex);
        GUI.color = Color.white;
    }

    /// <summary>
    /// แสดงตัวหนังสือผลการกด (Perfect/Good/Miss) และ Combo
    /// </summary>
    private void DrawJudgementAndCombo()
    {
        float centerX = Screen.width / 2f;
        float posY = Screen.height - 290f;

        // 1. ผลการกด (Perfect, Good, Miss)
        if (lastJudgmentTimer > 0 && !string.IsNullOrEmpty(lastJudgmentText))
        {
            float alpha = Mathf.Clamp01(lastJudgmentTimer / 0.4f);
            GUIStyle judgeStyle = new GUIStyle(GUI.skin.label);
            judgeStyle.fontSize = 26;
            judgeStyle.fontStyle = FontStyle.Bold;
            judgeStyle.alignment = TextAnchor.MiddleCenter;
            judgeStyle.normal.textColor = new Color(lastJudgmentColor.r, lastJudgmentColor.g, lastJudgmentColor.b, alpha);

            GUI.Label(new Rect(centerX - 200f, posY, 400f, 40f), lastJudgmentText, judgeStyle);
        }

        // 2. แสดงคอมโบ (COMBO)
        if (comboCount > 1)
        {
            GUIStyle comboStyle = new GUIStyle(GUI.skin.label);
            comboStyle.fontSize = 18;
            comboStyle.fontStyle = FontStyle.Bold;
            comboStyle.alignment = TextAnchor.MiddleCenter;
            comboStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            GUI.Label(new Rect(centerX - 150f, posY - 28f, 300f, 30f), $"COMBO x{comboCount}", comboStyle);
        }
    }

    /// <summary>
    /// วาดคำอธิบายสัญลักษณ์ด้านล่าง (Perfect, Good, Miss Legend) ตามภาพตัวอย่าง
    /// </summary>
    private void DrawLegendBottom()
    {
        float centerX = Screen.width / 2f;
        float posY = Screen.height - 45f;
        float spacing = 120f;

        GUIStyle legendStyle = new GUIStyle(GUI.skin.label);
        legendStyle.fontSize = 13;
        legendStyle.fontStyle = FontStyle.Bold;
        legendStyle.alignment = TextAnchor.MiddleLeft;

        // 1. Perfect Legend (สีทอง/ส้ม)
        float pX = centerX - spacing * 1.3f;
        GUI.color = new Color(1f, 0.75f, 0.2f);
        GUI.DrawTexture(new Rect(pX, posY + 2, 14, 14), circleTalismanTex != null ? circleTalismanTex : Texture2D.whiteTexture);
        legendStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(pX + 20, posY, 80, 20), "Perfect", legendStyle);

        // 2. Good Legend (สีครีม/ขาว)
        float gX = centerX - spacing * 0.1f;
        GUI.color = new Color(0.95f, 0.95f, 0.85f);
        GUI.DrawTexture(new Rect(gX, posY + 2, 14, 14), circleTalismanTex != null ? circleTalismanTex : Texture2D.whiteTexture);
        legendStyle.normal.textColor = new Color(0.95f, 0.95f, 0.85f);
        GUI.Label(new Rect(gX + 20, posY, 80, 20), "Good", legendStyle);

        // 3. Miss Legend (สีดำ/ส้มมืด)
        float mX = centerX + spacing * 1.1f;
        GUI.color = new Color(0.35f, 0.12f, 0.12f);
        GUI.DrawTexture(new Rect(mX, posY + 2, 14, 14), circleTalismanTex != null ? circleTalismanTex : Texture2D.whiteTexture);
        legendStyle.normal.textColor = new Color(0.85f, 0.4f, 0.4f);
        GUI.Label(new Rect(mX + 20, posY, 80, 20), "Miss", legendStyle);

        GUI.color = Color.white;
    }

    // ==========================================
    // Procedural Graphics & Sound Generation
    // ==========================================
    private void CreateProceduralTextures()
    {
        int size = 128;

        // 1. Circle Talisman (ยันต์กลมโน้ตวิ่ง)
        circleTalismanTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] talismanPixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // ลวดลายวงแหวนมณฑล (Talisman Pattern)
                    float ringPattern = Mathf.Sin(dist * 0.8f) * 0.15f;
                    float edgeGlow = (dist > radius - 8f) ? 0.35f : 0f;
                    float baseVal = 0.85f + ringPattern + edgeGlow;

                    // สีกระดาษยันต์โบราณ (Parchment Gold)
                    talismanPixels[y * size + x] = new Color(baseVal * 0.96f, baseVal * 0.88f, baseVal * 0.72f, 0.98f);
                }
                else
                {
                    talismanPixels[y * size + x] = Color.clear;
                }
            }
        }
        circleTalismanTex.SetPixels(talismanPixels);
        circleTalismanTex.Apply();

        // 2. Target Receptor (วงแหวนเป้าหมายเรืองแสง)
        targetReceptorTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] receptorPixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius && dist >= radius - 16f)
                {
                    // ขอบวงแหวนเรืองแสง
                    float factor = 1f - Mathf.Abs(dist - (radius - 8f)) / 8f;
                    receptorPixels[y * size + x] = new Color(1f, 0.88f, 0.5f, factor * 0.95f);
                }
                else if (dist < radius - 16f)
                {
                    // ด้านในโปร่งแสงเล็กน้อย
                    receptorPixels[y * size + x] = new Color(0.1f, 0.08f, 0.05f, 0.6f);
                }
                else
                {
                    receptorPixels[y * size + x] = Color.clear;
                }
            }
        }
        targetReceptorTex.SetPixels(receptorPixels);
        targetReceptorTex.Apply();

        // 3. Praying Hands Texture (ภาพจำลองมือพนม)
        int hW = 200;
        int hH = 140;
        prayingHandsTex = new Texture2D(hW, hH, TextureFormat.RGBA32, false);
        Color[] handPixels = new Color[hW * hH];
        Vector2 handCenter = new Vector2(hW / 2f, hH * 0.4f);

        for (int y = 0; y < hH; y++)
        {
            for (int x = 0; x < hW; x++)
            {
                float dx = Mathf.Abs(x - handCenter.x);
                float dy = y;

                // ทรงรูปมือพนมยอดแหลม
                float maxDx = (dy / (float)hH) * (hW * 0.45f);
                if (dx <= maxDx && dy >= 15)
                {
                    float shade = 1f - (dx / (maxDx + 0.1f)) * 0.35f;
                    // สีผิวมือ + แสงไฟเทียนสีส้มอบอุ่น
                    handPixels[y * hW + x] = new Color(0.85f * shade, 0.62f * shade, 0.48f * shade, 0.88f);
                }
                else
                {
                    handPixels[y * hW + x] = Color.clear;
                }
            }
        }
        prayingHandsTex.SetPixels(handPixels);
        prayingHandsTex.Apply();
    }

    private void SetupAudioSource()
    {
        synthAudioSource = gameObject.GetComponent<AudioSource>();
        if (synthAudioSource == null)
        {
            synthAudioSource = gameObject.AddComponent<AudioSource>();
        }
        synthAudioSource.playOnAwake = false;
        synthAudioSource.spatialBlend = 0f; // 2D Sound
    }

    /// <summary>
    /// สังเคราะห์เสียงระฆัง/ขันสวดมนต์ (Tibetan Singing Bowl / Bell Tone)
    /// </summary>
    private void PlaySynthChime(float frequency, float duration)
    {
        if (chantHitSound != null)
        {
            synthAudioSource.PlayOneShot(chantHitSound, 0.7f);
            return;
        }

        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-5.0f * (t / duration)); // Exponential decay
            // Sine wave + harmonic
            samples[i] = (Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.7f +
                          Mathf.Sin(2 * Mathf.PI * (frequency * 2.02f) * t) * 0.3f) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("ChantChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        synthAudioSource.PlayOneShot(clip, 0.6f);
    }

    private void PlaySynthMissTone()
    {
        if (chantMissSound != null)
        {
            synthAudioSource.PlayOneShot(chantMissSound, 0.7f);
            return;
        }

        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1.0f - (t / duration);
            // Low sawtooth dissonance
            samples[i] = (Mathf.PingPong(t * 130f, 1.0f) - 0.5f) * env * 0.35f;
        }

        AudioClip clip = AudioClip.Create("ChantMiss", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        synthAudioSource.PlayOneShot(clip, 0.5f);
    }

    private void PlaySynthFailTone()
    {
        if (chantFailSound != null)
        {
            synthAudioSource.PlayOneShot(chantFailSound, 0.8f);
            return;
        }

        PlaySynthMissTone();
    }
}
