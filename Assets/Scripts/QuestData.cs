using System;
using UnityEngine;

[Serializable]
public class QuestData
{
    [Header("Quest Info (ข้อมูลเควส)")]
    public string questId = "quest_01";
    public string questTitle = "ขอความช่วยเหลือ";
    [TextArea(3, 6)]
    public string questDescription = "ท่านผู้ดูแลตำหนัก ช่วยหาของสิ่งนี้ให้ข้าหน่อยได้หรือไม่?";

    [Header("NPC Info (ข้อมูล NPC)")]
    public string npcName = "ชาวบ้าน";
    public Sprite npcPortrait;

    [Header("Dialogues (บทสนทนา)")]
    [TextArea(2, 4)]
    public string greetingDialogue = "สวัสดีท่านผู้ดูแลตำหนัก ข้ามีเรื่องเดือดร้อนใจอยากให้ท่านช่วย...";
    [TextArea(2, 4)]
    public string acceptDialogue = "ขอบพระคุณท่านมาก! ข้าจะยืนรอของอยู่ตรงนี้นะ";
    [TextArea(2, 4)]
    public string waitingDialogue = "ข้ายังรอของชิ้นนี้จากท่านอยู่นะ...";
    [TextArea(2, 4)]
    public string completeDialogue = "ยอดเยี่ยมมาก! ได้ของครบถ้วนแล้ว ขอบพระคุณท่านจริง ๆ ข้าขอตัวลาก่อน";
    [TextArea(2, 4)]
    public string declineDialogue = "งั้นหรือ... น่าเสียดายจัง ไม่เป็นไร โอกาสหน้าข้าจะมาใหม่";

    [Header("Requirements (สิ่งที่ต้องการสำหรับส่งเควส)")]
    public string requiredItemName = "";
    public int requiredQuantity = 1;

    [Header("Reward (รางวัลตอบแทน)")]
    public string rewardDescription = "เงินรางวัล 100 เหรียญ";
    public int rewardMoney = 100;
    public string rewardItemName = "";

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
