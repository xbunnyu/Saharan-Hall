using System;
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
    None,               // ไม่มีมินิเกม (เช่นเควสทั่วไปหรือส่งของ)
    RhythmChantWASD,    // มินิเกมท่องคาถา Rhythm Game W A S D
    DeadByDaylightQTE,  // มินิเกม Skill Check QTE แบบ Dead by Daylight (ปั่นไฟ/กด Spacebar)
    TalismanDrawing,    // สำหรับรองรับมินิเกมวาดผ้ายันต์ในอนาคต
    ExorcismRitual      // สำหรับรองรับมินิเกมทำพิธีขับไล่ผีในอนาคต
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

    // สถานะของเควส
    [HideInInspector]
    public bool isAccepted = false;
    [HideInInspector]
    public bool isCompleted = false;

    public QuestData Clone()
    {
        return (QuestData)this.MemberwiseClone();
    }
}
