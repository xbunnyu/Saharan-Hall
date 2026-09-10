using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระดับความยากของเควส — ใช้กำหนดรางวัล, น้ำหนักการสุ่ม และสี Badge บน UI
/// </summary>
public enum QuestDifficulty
{
    Easy,       // ง่าย     🟢
    Medium,     // ปานกลาง 🟡
    Hard,       // ยาก      🟠
    VeryHard    // ยากมาก  🔴
}

public enum MinigameType
{
    None,               // 0. ไม่มีมินิเกม (เควสส่งของทั่วไป)
    RhythmChantWASD,    // 1. มินิเกมท่องคาถา (Rhythm Game W A S D)
    DeadByDaylightQTE,  // 2. มินิเกม Skill Check QTE (กด Spacebar หยุดเข็มในวงล้อ)
    TalismanDrawing,    // 3. มินิเกมเขียนยันต์ ( Skill Check QTE เดียวกับ DeadByDaylightQTE)
    SequentialSlashQTE, // 4. มินิเกมตวัดดาบฟันยันต์ (Sequential Slash QTE)
    TimingBarQTE        // 5. มินิเกมแถบจังหวะโปรยข้าวสาร (Horizontal Timing Bar QTE)
}

[Serializable]
public class QuestData
{
    [Header("Quest Info (ข้อมูลเควส)")]
    public string questId = "quest_01";
    public string questTitle = "ขอความช่วยเหลือ";
    [TextArea(3, 6)]
    public string questDescription = "ท่านผู้ดูแลตำหนัก ช่วยท่องคาถาทำพิธีสะเดาะเคราะห์ให้ข้าหน่อยได้หรือไม่?";

    [Header("Difficulty (ระดับความยาก)")]
    [Tooltip("ระดับความยากของเควส — ส่งผลต่อสี Badge บน UI, น้ำหนักการสุ่ม และความเร็ว/จำนวนโน้ตในมินิเกม")]
    public QuestDifficulty difficulty = QuestDifficulty.Easy;

    [Header("Minigame Settings (การตั้งค่ามินิเกม)")]
    [Tooltip("ประเภทมินิเกมที่ต้องเล่นเมื่อรับเควสนี้")]
    public MinigameType minigameType = MinigameType.RhythmChantWASD;
    [Tooltip("ความเร็วในการเลื่อนของโน้ต (0 = คำนวณอัตโนมัติตามความยาก)")]
    public float customNoteSpeed = 0f;
    [Tooltip("จำนวนโน้ตทั้งหมดในรอบ (0 = คำนวณอัตโนมัติตามความยาก)")]
    public int customNoteCount = 0;
    [Tooltip("เปอร์เซ็นต์คะแนนขั้นต่ำในการผ่าน (ค่าเริ่มต้น 60%)")]
    [Range(30, 100)]
    public int passPercentage = 60;

    [Header("NPC Info (ข้อมูล NPC)")]
    public string npcName = "ชาวบ้าน";
    public Sprite npcPortrait;

    [Header("Dialogues (บทสนทนา)")]
    [TextArea(2, 4)]
    public string greetingDialogue = "สวัสดีท่านผู้ดูแลตำหนัก ข้ามีเรื่องเดือดร้อนใจ อยากให้ท่านช่วยท่องคาถาทำพิธีให้...";
    [TextArea(2, 4)]
    public string acceptDialogue = "ขอบพระคุณท่านมาก! ข้าขอฝากความหวังไว้กับพิธีกรรมของท่านนะ";
    [TextArea(2, 4)]
    public string completeDialogue = "ยอดเยี่ยมมาก! พิธีสำเร็จลุล่วงด้วยดี ขอบพระคุณท่านจริง ๆ ข้าขอตัวลาก่อน";
    [TextArea(2, 4)]
    public string failDialogue = "อ๊ากก! ทำไมมีสิ่งชั่วร้ายเข้าครอบงำ... พิธีล้มเหลวแล้ว!";
    [TextArea(2, 4)]
    public string declineDialogue = "งั้นหรือ... น่าเสียดายจัง ไม่เป็นไร โอกาสหน้าข้าจะมาใหม่";

    [Header("Requirements (สิ่งที่ต้องการสำหรับส่งเควส - กรณีเควสส่งของ)")]
    [Tooltip("ชื่อไอเทมที่ต้องการ (หากเป็นเควสส่งของ)")]
    public string requiredItemName = "";
    [Tooltip("จำนวนไอเทมที่ต้องการ")]
    public int requiredQuantity = 1;
    [Tooltip("สำหรับเควสที่ไม่ใช่การส่งของ (เช่น ทำพิธีกรรม/มินิเกม) กำหนดว่าทำภารกิจสำเร็จแล้วหรือไม่")]
    public bool isTaskCompleted = false;

    [Header("Reward (รางวัลตอบแทน)")]
    public string rewardDescription = "เงินรางวัล 150 เหรียญ";
    public int rewardMoney = 150;
    public string rewardItemName = "";

    [Header("Karma (ค่าความดี/ความชั่ว — ผู้เล่นไม่เห็น)")]
    [Tooltip("ค่า Karma ที่ได้รับเมื่อส่งเควสสำเร็จ  บวก = ความดี | ลบ = ความชั่ว")]
    public int karmaReward = 15;

    [Header("Minigame Sequence (ลำดับมินิเกมที่ต้องเล่นเพื่อส่งเควส)")]
    public List<MinigameType> requiredMinigameSequence = new List<MinigameType>();
    public List<MinigameType> completedMinigameSequence = new List<MinigameType>();

    // สถานะของเควส
    [HideInInspector]
    public bool isAccepted = false;
    [HideInInspector]
    public bool isCompleted = false;

    /// <summary>
    /// สุ่มชุดมินิเกมที่ต้องทำ 2 ถึง 4 มินิเกมสำหรับเควสนี้
    /// </summary>
    public void GenerateRandomMinigameSequence(int minCount = 2, int maxCount = 4)
    {
        requiredMinigameSequence.Clear();
        completedMinigameSequence.Clear();

        MinigameType[] pool = new MinigameType[]
        {
            MinigameType.RhythmChantWASD,
            MinigameType.TalismanDrawing,
            MinigameType.SequentialSlashQTE,
            MinigameType.TimingBarQTE
        };

        // สับเปลี่ยนรายการมินิเกมที่มี (Fisher-Yates Shuffle)
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            var temp = pool[i];
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        int count = UnityEngine.Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count && i < pool.Length; i++)
        {
            requiredMinigameSequence.Add(pool[i]);
        }

        if (requiredMinigameSequence.Count > 0)
        {
            minigameType = requiredMinigameSequence[0];
        }
    }

    /// <summary>
    /// ตรวจสอบว่ามินิเกมประเภทนี้จำเป็นต้องเล่นในขั้นตอนปัจจุบันของเควสหรือไม่
    /// </summary>
    public bool IsMinigameRequired(MinigameType type)
    {
        if (requiredMinigameSequence == null || requiredMinigameSequence.Count == 0)
        {
            return (type == minigameType || (type == MinigameType.DeadByDaylightQTE && minigameType == MinigameType.TalismanDrawing) || (type == MinigameType.TalismanDrawing && minigameType == MinigameType.DeadByDaylightQTE)) && !isTaskCompleted;
        }

        if (completedMinigameSequence.Count >= requiredMinigameSequence.Count) return false;

        MinigameType currentRequired = requiredMinigameSequence[completedMinigameSequence.Count];

        bool match = (type == currentRequired) || 
                     (type == MinigameType.DeadByDaylightQTE && currentRequired == MinigameType.TalismanDrawing) ||
                     (type == MinigameType.TalismanDrawing && currentRequired == MinigameType.DeadByDaylightQTE);

        return match;
    }

    /// <summary>
    /// ทำเครื่องหมายว่าเล่นมินิเกมประเภทนี้สำเร็จไปแล้ว 1 ขั้นตอน
    /// </summary>
    public bool MarkMinigameCompleted(MinigameType type)
    {
        if (requiredMinigameSequence == null || requiredMinigameSequence.Count == 0)
        {
            isTaskCompleted = true;
            return true;
        }

        if (!completedMinigameSequence.Contains(type))
        {
            completedMinigameSequence.Add(type);
        }

        if (completedMinigameSequence.Count >= requiredMinigameSequence.Count)
        {
            isTaskCompleted = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// คืนค่าข้อความชื่อมินิเกมภาษาไทย
    /// </summary>
    public static string GetMinigameNameThai(MinigameType type)
    {
        switch (type)
        {
            case MinigameType.RhythmChantWASD: return "🥁 ท่องคาถา (WASD)";
            case MinigameType.DeadByDaylightQTE:
            case MinigameType.TalismanDrawing: return "📜 เขียนยันต์มหาเวทย์";
            case MinigameType.SequentialSlashQTE: return "⚔️ ตวัดดาบฟันยันต์";
            case MinigameType.TimingBarQTE: return "🌾 โปรยข้าวสารไล่ผี";
            default: return "พิธีกรรม";
        }
    }

    public QuestData Clone()
    {
        QuestData clone = (QuestData)this.MemberwiseClone();
        clone.requiredMinigameSequence = new List<MinigameType>(this.requiredMinigameSequence);
        clone.completedMinigameSequence = new List<MinigameType>(this.completedMinigameSequence);
        return clone;
    }
}
