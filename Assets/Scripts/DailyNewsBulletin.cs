using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ข้อมูลข่าวสารหรืออีเวนต์ประจำวัน
/// </summary>
[System.Serializable]
public class DailyNewsEntry
{
    [Tooltip("วันที่ข่าวนี้มีผล (เช่น วันที่ 1, 2, 3...) หรือใส่ 0 เพื่อใช้เป็นข่าวเริ่มต้น/ทุกวัน")]
    public int targetDay = 1;

    [Tooltip("ชื่อหัวฉบับหรือประเภทเอกสาร เช่น 'หนังสือพิมพ์สาฮารันเดลี่', 'ประกาศข่าวสารหมู่บ้าน', 'สมุดบันทึกเหตุการณ์'")]
    public string publicationTitle = "หนังสือพิมพ์ท้องถิ่น";

    [Tooltip("พาดหัวข่าวประจำวัน")]
    public string headline = "เหตุการณ์ลี้ลับเกิดขึ้นเมื่อคืนนี้!";

    [TextArea(3, 8)]
    [Tooltip("เนื้อหาข่าวสารหลัก / เรื่องราวที่เกิดขึ้น")]
    public string newsStory = "ชาวบ้านพบเห็นเงาประหลาดบริเวณท้ายหมู่บ้าน ผู้นำชุมชนขอความร่วมมือทุกคนให้อยู่ในเคหสถานหลังพลบค่ำ...";

    [TextArea(2, 4)]
    [Tooltip("การแจ้งเตือนอีเวนต์หรือคำเตือนพิเศษ (จะมีกรอบสีเตือนเด่นชัดในหน้าต่างอ่าน)")]
    public string eventAlert = "⚠️ [แจ้งเตือนอีเวนต์]: คืนนี้จะมีลมกรรโชกแรง หมอกลงหนาจัด ห้ามออกนอกตำหนัก!";

    [Tooltip("แหล่งที่มา ผู้ประกาศ หรือหมายเหตุท้ายข่าว")]
    public string authorOrSource = "— ข่าวสารชุมชนสาฮารันฮอลล์";

    [Header("เหตุการณ์เมื่ออ่านข่าวนี้ครั้งแรก")]
    public UnityEvent onFirstRead = new UnityEvent();

    [HideInInspector]
    public bool hasBeenRead = false;
}

/// <summary>
/// รูปแบบการแสดงผลข่าวสาร
/// </summary>
public enum NewsDisplayMode
{
    MatchCurrentDay,         // แสดงผลอัตโนมัติตามวันปัจจุบันของเกม (DayManager.Instance.currentDay)
    AllPastAndCurrentDays,   // แสดงข่าวของวันนี้เป็นหลัก และต่อด้วยประวัติข่าวของวันก่อนหน้า
    SpecificFixedDay         // ล็อคให้แสดงเฉพาะวันที่ระบุ (เหมาะกับจดหมายหรือข่าวฉบับเก่าเฉพาะวัน)
}

/// <summary>
/// รูปแบบการสุ่มรูปภาพเบาะแสข่าวสาร
/// </summary>
public enum ClueRandomMode
{
    RandomPerDay,       // สุ่มวันละ 1 ภาพ (ตลอดทั้งวันนั้นผู้เล่นจะได้ภาพเดิม)
    RandomEveryRead,    // สุ่มใหม่ทุกครั้งที่กดอ่าน [E]
    SequenceByDay       // เรียงตามลำดับในรายการตามวัน (วันที่ 1 ภาพที่ 1, วันที่ 2 ภาพที่ 2...)
}

/// <summary>
/// วัตถุหนังสือพิมพ์, กระดานข่าวสาร, สมุดบันทึก หรือสิ่งของแจ้งเตือนอีเวนต์ในฉาก
/// - รองรับการกด [E] อ่านข้อมูล
/// - ปรับเปลี่ยนเนื้อหาข่าวสารและคำเตือนอีเวนต์ตามวันในเกมอัตโนมัติ (DayManager)
/// - มีระบบเรืองแสงเตือนผู้เล่นอัตโนมัติเมื่อมีข่าวใหม่ที่ยังไม่ได้อ่าน
/// </summary>
[SelectionBase]
public class DailyNewsBulletin : InteractableItem
{
    [Header("═════ ข่าวสารแบบรูปภาพ / สุ่มเบาะแส (Image Clues) ═════")]
    [Tooltip("เปิดใช้งานโหมดรูปภาพ (สุ่มภาพเบาะแสที่ทำไว้แสดงแทนข้อความล้วน)")]
    public bool useImageNews = true;

    [Tooltip("รายการรูปภาพเบาะแสหรือข่าวสารที่ทำไว้ทั้งหมด (ลากไฟล์รูปภาพ Sprite มาใส่ในลิสต์นี้ได้เลย)")]
    public List<Sprite> clueImages = new List<Sprite>();

    [Tooltip("รูปแบบการสุ่มรูปภาพ:\n- RandomPerDay: สุ่มวันละ 1 ภาพ (ตลอดวันนั้นจะได้ภาพเดิม)\n- RandomEveryRead: สุ่มใหม่ทุกครั้งที่กดอ่าน\n- SequenceByDay: เรียงตามลำดับตามวัน")]
    public ClueRandomMode randomMode = ClueRandomMode.RandomPerDay;

    [Tooltip("หัวข้อด้านบน (ปล่อยว่างได้ถ้าไม่ต้องการให้มีข้อความบังภาพ)")]
    public string imageNewsTitle = "📰 ข่าวสารและเบาะแส";

    [Tooltip("ข้อความคำอธิบายใต้ภาพ (ปล่อยว่างได้ถ้าต้องการดูภาพเพียวๆ)")]
    public string imageNewsCaption = "";

    [Tooltip("ขนาดของรูปภาพเบาะแสบนหน้าจอ (กว้าง x สูง)")]
    public Vector2 clueDisplaySize = new Vector2(1000f, 650f);

    [Header("═════ ตั้งค่าระบบข่าวสารแบบข้อความเดิม (Text Fallback) ═════")]
    [Tooltip("รูปแบบการเลือกข่าวสารมาแสดง")]
    public NewsDisplayMode displayMode = NewsDisplayMode.MatchCurrentDay;

    [Tooltip("วันที่ต้องการล็อคแสดง (ใช้เมื่อเลือกโหมด SpecificFixedDay)")]
    public int fixedTargetDay = 1;

    [Tooltip("ข้อความที่แสดงหากวันปัจจุบันยังไม่มีข่าวสารบันทึกไว้")]
    public string fallbackNoNewsText = "ไม่มีข่าวสารหรือประกาศใหม่สำหรับวันนี้...";

    [Header("รายการข่าวสารและอีเวนต์แต่ละวัน (News Entries)")]
    public List<DailyNewsEntry> dailyNewsList = new List<DailyNewsEntry>()
    {
        new DailyNewsEntry()
        {
            targetDay = 1,
            publicationTitle = "หนังสือพิมพ์ท้องถิ่น - ฉบับวันที่ 1",
            headline = "ยินดีต้อนรับสู่ตำหนักสาฮารัน",
            newsStory = "ตำหนักเปิดรับผู้มาเยือนเพื่อสะสมบุญบารมีและขจัดปัดเป่าเคราะห์ภัย ขอให้ผู้ดูแลตำหนักเตรียมเครื่องบูชาให้พร้อม...",
            eventAlert = "✨ [คำแนะนำ]: หมั่นสังเกตสิ่งของเรืองแสงในตำหนัก และช่วยแก้ปัญหาให้ผู้มาเยือนให้สำเร็จก่อนเข้านอน",
            authorOrSource = "— ผู้ใหญ่บ้าน"
        },
        new DailyNewsEntry()
        {
            targetDay = 2,
            publicationTitle = "หนังสือพิมพ์ท้องถิ่น - ฉบับวันที่ 2",
            headline = "สัญญาณเตือนอาถรรพ์ทวีความรุนแรง!",
            newsStory = "เกิดเหตุการณ์ไม่สงบ มีเสียงสวดและควันประหลาดลอยมาจากทิศเหนือ ชาวบ้านบางคนเริ่มล้มป่วยด้วยสาเหตุที่ไม่ทราบแน่ชัด...",
            eventAlert = "⚠️ [อีเวนต์สำคัญ]: คืนนี้จะมีผู้มาเยือนจากต่างถิ่นและวิญญาณเร่ร่อน โปรดระมัดระวังในการทำพิธี!",
            authorOrSource = "— สำนักข่าวสารสาฮารัน"
        }
    };

    [Header("ระบบแจ้งเตือนและการเรืองแสง (Alert & Glow on New Event)")]
    [Tooltip("ให้ไอเทมเรืองแสงเตือนผู้เล่นอัตโนมัติ เมื่อมีข่าวสารใหม่ประจำวันที่ยังไม่ได้อ่าน")]
    public bool glowWhenHasUnreadNews = true;

    [Tooltip("เมื่อผู้เล่นกดอ่านข่าววันนี้แล้ว ให้ดับแสงเรืองแสงหรือไม่")]
    public bool stopGlowAfterRead = true;

    [Tooltip("เมื่อขึ้นวันใหม่ ให้แสดงแถบแจ้งเตือนสั้นๆ บนหน้าจอ")]
    public bool notifyBannerOnNewDay = true;

    // ข้อมูลข่าวปัจจุบันที่เลือกมาแสดง
    private DailyNewsEntry currentActiveEntry;
    private int lastObservedDay = -1;
    private int cachedClueIndex = -1;
    private int cachedClueDay = -1;
    private HashSet<int> readDays = new HashSet<int>();

    /// <summary>
    /// สุ่มเลือกภาพเบาะแสตามโหมดที่ตั้งค่าไว้
    /// </summary>
    public Sprite GetClueImage(int day)
    {
        if (clueImages == null || clueImages.Count == 0) return null;

        if (randomMode == ClueRandomMode.RandomEveryRead)
        {
            int rnd = Random.Range(0, clueImages.Count);
            return clueImages[rnd];
        }
        else if (randomMode == ClueRandomMode.SequenceByDay)
        {
            int index = Mathf.Clamp(day - 1, 0, clueImages.Count - 1);
            return clueImages[index];
        }

        // RandomPerDay: สุ่มวันละ 1 ภาพ (ตลอดทั้งวันนั้นจะได้ภาพเดิม)
        if (cachedClueDay != day || cachedClueIndex < 0 || cachedClueIndex >= clueImages.Count)
        {
            cachedClueDay = day;
            System.Random prng = new System.Random(day * 1337 + 42);
            cachedClueIndex = prng.Next(0, clueImages.Count);
        }

        return clueImages[cachedClueIndex];
    }

    protected override void Awake()
    {
        // กำหนดค่าเริ่มต้นของ InteractableItem ให้เหมาะกับการอ่านข่าว
        canRead = true;
        if (string.IsNullOrEmpty(customReadPromptText))
        {
            customReadPromptText = "อ่านข่าวสาร";
        }

        base.Awake();
    }

    private void OnEnable()
    {
        DayManager.OnDayChanged += HandleDayChanged;
        RefreshContent();
    }

    private void OnDisable()
    {
        DayManager.OnDayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        RefreshContent();
    }

    private void Update()
    {
        // ตรวจสอบการเปลี่ยนวันแบบ Real-time (ในกรณีที่ DayManager เปลี่ยนค่า currentDay)
        int currentDay = GetCurrentGameDay();
        if (currentDay != lastObservedDay)
        {
            HandleDayChanged(currentDay);
        }
    }

    /// <summary>
    /// ทำงานเมื่อระบบวันของเกมเปลี่ยนขึ้นวันใหม่
    /// </summary>
    private void HandleDayChanged(int newDay)
    {
        lastObservedDay = newDay;
        RefreshContent();

        // แจ้งเตือน Popup สั้นๆ เมื่อขึ้นวันใหม่
        if (notifyBannerOnNewDay && currentActiveEntry != null && !currentActiveEntry.hasBeenRead)
        {
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification($"📰 มีข่าวสารและอีเวนต์ใหม่ประจำวันที่ {newDay}! (ตรวจดูที่ {itemName})", 4.0f);
            }
        }
    }

    /// <summary>
    /// ค้นหาและอัปเดตเนื้อหาข่าวสารตามวันปัจจุบัน
    /// </summary>
    public void RefreshContent()
    {
        int day = GetCurrentGameDay();

        if (useImageNews && clueImages != null && clueImages.Count > 0)
        {
            readImage = GetClueImage(day);
            readTitle = string.IsNullOrEmpty(imageNewsTitle) ? "" : $"{imageNewsTitle} (วันที่ {day})";
            readDescription = imageNewsCaption;
        }
        else
        {
            readImage = null;
            currentActiveEntry = FindEntryForDay(day);

            // จัดเตรียมหัวข้อและเนื้อหาสำหรับ Reading Dialog
            if (currentActiveEntry != null)
            {
                readTitle = string.IsNullOrEmpty(currentActiveEntry.publicationTitle) ? itemName : currentActiveEntry.publicationTitle;
                readDescription = FormatReadingDescription(currentActiveEntry, day);
            }
            else
            {
                readTitle = $"{itemName} (วันที่ {day})";
                readDescription = fallbackNoNewsText;
            }
        }

        // จัดการระบบเรืองแสงเตือนผู้เล่น
        UpdateGlowState();
    }

    /// <summary>
    /// ตรวจสอบและเปิด/ปิดระบบเรืองแสงตามสถานะการอ่าน
    /// </summary>
    private void UpdateGlowState()
    {
        if (!glowWhenHasUnreadNews) return;

        int day = GetCurrentGameDay();
        bool hasUnread = false;

        if (useImageNews && clueImages != null && clueImages.Count > 0)
        {
            hasUnread = !readDays.Contains(day);
        }
        else
        {
            hasUnread = currentActiveEntry != null && !currentActiveEntry.hasBeenRead;
        }

        isImportantItem = hasUnread;

        var highlighter = GetComponent<ItemHighlighter>();
        if (highlighter != null)
        {
            highlighter.SetHighlightEnabled(hasUnread);
        }
    }

    /// <summary>
    /// ได้รับวันปัจจุบันของเกม
    /// </summary>
    public int GetCurrentGameDay()
    {
        if (displayMode == NewsDisplayMode.SpecificFixedDay)
        {
            return fixedTargetDay;
        }

        if (DayManager.Instance != null)
        {
            return DayManager.Instance.currentDay;
        }

        return 1;
    }

    /// <summary>
    /// ค้นหารายการข่าวสารที่ตรงกับวันที่ระบุ
    /// </summary>
    private DailyNewsEntry FindEntryForDay(int day)
    {
        if (dailyNewsList == null || dailyNewsList.Count == 0) return null;

        // 1. หาข่าวที่ระบุ targetDay ตรงกับวันนี้พอดี
        foreach (var entry in dailyNewsList)
        {
            if (entry != null && entry.targetDay == day)
            {
                return entry;
            }
        }

        // 2. ถ้าไม่พบ ให้ลองหาข่าวทั่วไป (targetDay == 0)
        foreach (var entry in dailyNewsList)
        {
            if (entry != null && entry.targetDay == 0)
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// จัดรูปแบบข้อความให้อ่านง่าย สวยงาม มีกรอบแจ้งเตือนอีเวนต์
    /// </summary>
    private string FormatReadingDescription(DailyNewsEntry entry, int currentDay)
    {
        string text = "";

        // พาดหัวข่าว
        if (!string.IsNullOrEmpty(entry.headline))
        {
            text += $"<size=120%><color=#FFD700><b>📰 {entry.headline}</b></color></size>\n";
            text += "<color=#888888>────────────────────────────</color>\n\n";
        }

        // เนื้อหาข่าวสารหลัก
        if (!string.IsNullOrEmpty(entry.newsStory))
        {
            text += $"{entry.newsStory}\n\n";
        }

        // กรอบแจ้งเตือนอีเวนต์สำคัญ (ถ้ามี)
        if (!string.IsNullOrEmpty(entry.eventAlert))
        {
            text += $"<color=#00FFFF><b>[ แจ้งเตือนเหตุการณ์ / อีเวนต์ ]</b></color>\n";
            text += $"<color=#FFE4B5>{entry.eventAlert}</color>\n\n";
        }

        // แหล่งที่มา / หมายเหตุ
        if (!string.IsNullOrEmpty(entry.authorOrSource))
        {
            text += $"<size=85%><color=#AAAAAA><i>{entry.authorOrSource}</i></color></size>";
        }

        // หากเปิดโหมดแสดงข่าวย้อนหลัง ให้ต่อท้ายด้วยข่าวของวันก่อนหน้า
        if (displayMode == NewsDisplayMode.AllPastAndCurrentDays && dailyNewsList != null)
        {
            bool hasPastNews = false;
            string pastNewsText = "";

            foreach (var pastEntry in dailyNewsList)
            {
                if (pastEntry != null && pastEntry.targetDay < currentDay && pastEntry.targetDay > 0)
                {
                    hasPastNews = true;
                    pastNewsText += $"\n\n<color=#FFA500><b>📜 ข่าวสารย้อนหลัง (วันที่ {pastEntry.targetDay})</b></color>\n";
                    pastNewsText += $"<b>{pastEntry.headline}</b>\n";
                    pastNewsText += $"<size=90%>{pastEntry.newsStory}</size>";
                }
            }

            if (hasPastNews)
            {
                text += "\n\n<color=#555555>════════════════════════════</color>";
                text += pastNewsText;
            }
        }

        return text;
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นกด [E] อ่านข่าวสาร
    /// </summary>
    public override void OnRead(PlayerInteraction interactor)
    {
        int day = GetCurrentGameDay();

        RefreshContent();

        // บันทึกสถานะว่าอ่านแล้ว
        readDays.Add(day);

        // สั่งให้ PlayerInteraction ทำการ Freeze ผู้เล่นและปลดล็อคเมาส์
        if (interactor != null && !interactor.IsReading())
        {
            interactor.OpenReading(this);
        }
        else if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowReadingDialog(readTitle, readDescription, null, readImage, clueDisplaySize);
        }

        if (currentActiveEntry != null)
        {
            if (!currentActiveEntry.hasBeenRead)
            {
                currentActiveEntry.hasBeenRead = true;
                currentActiveEntry.onFirstRead?.Invoke();
                Debug.Log($"[DailyNewsBulletin] 📖 ผู้เล่นเปิดอ่านข่าวสารวันที่ {day} เป็นครั้งแรก!");
            }
        }

        // จัดการปิดแสงเรืองหลังอ่าน
        if (stopGlowAfterRead)
        {
            UpdateGlowState();
        }

        base.OnRead(interactor);
    }

    /// <summary>
    /// เพิ่มหรือตั้งค่าข่าวสารสำหรับวันที่ระบุผ่านสคริปต์
    /// </summary>
    public void SetNewsForDay(int day, string title, string headline, string story, string alert = "")
    {
        var existing = FindEntryForDay(day);
        if (existing != null && existing.targetDay == day)
        {
            existing.publicationTitle = title;
            existing.headline = headline;
            existing.newsStory = story;
            existing.eventAlert = alert;
            existing.hasBeenRead = false;
        }
        else
        {
            dailyNewsList.Add(new DailyNewsEntry()
            {
                targetDay = day,
                publicationTitle = title,
                headline = headline,
                newsStory = story,
                eventAlert = alert,
                hasBeenRead = false
            });
        }

        RefreshContent();
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Find Clue Images From Assets")]
    public void AutoFindClueImages()
    {
        string[] searchFolders = new string[] { "Assets/UI.V1", "Assets/Image" };
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", searchFolders);
        if (guids == null || guids.Length == 0)
        {
            guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new string[] { "Assets" });
        }

        if (guids != null && guids.Length > 0)
        {
            if (clueImages == null) clueImages = new List<Sprite>();
            int added = 0;
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null && !clueImages.Contains(sp))
                {
                    clueImages.Add(sp);
                    added++;
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[DailyNewsBulletin] ✅ ค้นหาและดึงรูปภาพ Sprite เข้ามาในลิสต์เพิ่ม {added} ภาพ (รวมทั้งหมด {clueImages.Count} ภาพ)");
        }
        else
        {
            Debug.LogWarning("[DailyNewsBulletin] ⚠️ ไม่พบไฟล์ประเภท Sprite ใน Assets (ตรวจสอบ Texture Type ให้เป็น 'Sprite (2D and UI)')");
        }
    }
#endif
}
