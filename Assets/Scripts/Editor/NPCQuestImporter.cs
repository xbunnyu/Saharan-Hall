using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// นำเข้า NPC + บทพูดจากชีต "ตัวใช้ NPC DIALOGUE" (ไฟล์ Assets/Data/NPCQuests.json) ลงใน HallManager.questList
/// แต่ละคนได้โมเดลของตัวเอง (หรือยืมโมเดลตัวอื่นตามช่อง "model" ในไฟล์ JSON)
/// HallManager สุ่มเควสจากรายการนี้อยู่แล้ว → ใครจะมาเยี่ยมก็สุ่มตามน้ำหนักความยาก
/// เมนู: Tools > Saharan > นำเข้า NPC + บทพูดจากชีต
/// ฉากยังไม่ถูกเซฟ — ชอบกด Ctrl+S, ไม่ชอบกด Ctrl+Z
/// </summary>
public static class NPCQuestImporter
{
    const string JsonPath = "Assets/Data/NPCQuests.json";
    const string PrefabFolder = "Assets/Prefab/walk";

    [System.Serializable]
    class Row
    {
        public string name, region, job, alignment, problem, reward, tier;
        public string greeting, waiting, complete, decline, inquiry;
        public string difficulty, ritualCount, mainQte, sequence, model;
    }

    [System.Serializable]
    class Table { public Row[] npcs; }

    [MenuItem("Tools/Saharan/นำเข้า NPC + บทพูดจากชีต (สุ่มมาเยี่ยม)")]
    public static void Import()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("นำเข้า NPC", "หยุดเกม (ปุ่ม Play) ก่อน แล้วค่อยกดเมนูนี้", "ตกลง");
            return;
        }

        var hm = Object.FindFirstObjectByType<HallManager>(FindObjectsInactive.Include);
        if (hm == null)
        {
            EditorUtility.DisplayDialog("นำเข้า NPC", "ไม่เจอ HallManager — เปิดฉาก GamePlay ก่อน", "ตกลง");
            return;
        }

        if (!File.Exists(JsonPath))
        {
            EditorUtility.DisplayDialog("นำเข้า NPC", "ไม่เจอไฟล์ " + JsonPath, "ตกลง");
            return;
        }

        var table = JsonUtility.FromJson<Table>(File.ReadAllText(JsonPath, Encoding.UTF8));
        if (table == null || table.npcs == null || table.npcs.Length == 0)
        {
            EditorUtility.DisplayDialog("นำเข้า NPC", "อ่านไฟล์ " + JsonPath + " ไม่ได้", "ตกลง");
            return;
        }

        bool go = EditorUtility.DisplayDialog(
            "นำเข้า NPC + บทพูดจากชีต",
            "จะแทนที่รายการเควสใน HallManager (ตอนนี้ " + (hm.questList != null ? hm.questList.Count : 0) + " เควส)\n" +
            "ด้วย NPC จากชีต " + table.npcs.Length + " คน พร้อมบทพูด ถามเพิ่มเติม พิธี และโมเดลของแต่ละคน\n\n" +
            "ฉากยังไม่ถูกเซฟ — ชอบกด Ctrl+S, ไม่ชอบกด Ctrl+Z",
            "เริ่มเลย", "ยกเลิก");
        if (!go) return;

        // เตรียม prefab ทุกตัวให้ขนาดเท่ากัน + ใส่ texture + controller
        EditorUtility.DisplayProgressBar("นำเข้า NPC", "เตรียมโมเดลตัวละคร...", 0.2f);
        string prepReport;
        try { prepReport = NPCWalkSetupTool.PrepareAllPrefabs(); }
        finally { EditorUtility.ClearProgressBar(); }

        string storyNote = AssignStoryPrefabs();

        Sprite portrait = hm.questList == null ? null :
            hm.questList.Select(q => q != null ? q.npcPortrait : null).FirstOrDefault(p => p != null);

        var report = new StringBuilder();
        var quests = new List<QuestData>();
        int index = 1;
        foreach (var row in table.npcs)
        {
            if (row == null || string.IsNullOrEmpty(row.name)) continue;
            var q = BuildQuest(row, index++, portrait, report);
            quests.Add(q);
        }

        Undo.RecordObject(hm, "นำเข้า NPC จากชีต");
        hm.questList = quests;
        EditorUtility.SetDirty(hm);
        EditorSceneManager.MarkSceneDirty(hm.gameObject.scene);

        report.Insert(0, "นำเข้า " + quests.Count + " เควสแล้ว (ยังไม่เซฟฉาก — กด Ctrl+S ถ้าชอบ)\n" + storyNote + "\n\n");
        Debug.Log("[NPCQuestImporter]\n" + report + "\n--- prefab ---\n" + prepReport);
        EditorUtility.DisplayDialog("นำเข้า NPC เสร็จแล้ว", report.ToString(), "ตกลง");
    }

    /// <summary>NPC เคสต่อเนื่อง (NPCStoryData) → ใส่โมเดลจริงแทนแคปซูล</summary>
    static readonly Dictionary<string, string> StoryModels = new Dictionary<string, string>
    {
        { "ป้าติ๋ม", "patim_walk01" },
    };

    static string AssignStoryPrefabs()
    {
        var notes = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:NPCStoryData"))
        {
            var story = AssetDatabase.LoadAssetAtPath<NPCStoryData>(AssetDatabase.GUIDToAssetPath(guid));
            if (story == null || string.IsNullOrEmpty(story.npcName)) continue;
            string model;
            if (!StoryModels.TryGetValue(story.npcName.Trim(), out model)) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + model + ".prefab");
            if (prefab == null) { notes.Add("⚠️ ไม่เจอ prefab ของ " + story.npcName); continue; }
            Undo.RecordObject(story, "ใส่โมเดล NPC เคสต่อเนื่อง");
            story.npcPrefab = prefab;
            EditorUtility.SetDirty(story);
            notes.Add(story.npcName + " ใช้โมเดลจริงแล้ว (แทนแคปซูล)");
        }
        AssetDatabase.SaveAssets();
        return string.Join("\n", notes);
    }

    static QuestData BuildQuest(Row r, int index, Sprite portrait, StringBuilder report)
    {
        var q = new QuestData();
        q.questId = "npc_" + index.ToString("00");
        q.problemType = r.problem ?? "";
        q.questTitle = "";
        q.questDescription = r.greeting ?? "";
        q.greetingDialogue = "";
        q.waitingDialogue = r.waiting ?? "";
        q.completeDialogue = r.complete ?? "";
        q.declineDialogue = r.decline ?? "";

        q.npcName = r.name;
        q.npcPortrait = portrait;

        // ถามเพิ่มเติม: ระดับชื่อเสียง 0 ยังไม่บอก, ระดับ 1 ขึ้นไปบอกข้อมูลจากชีต
        string inquiry = string.IsNullOrEmpty(r.inquiry) ? "..." : r.inquiry;
        q.inquiryDialogues = new string[4] { "...(ยังไม่ไว้ใจพอที่จะเล่าให้ฟัง)", inquiry, inquiry, inquiry };

        q.difficulty = ParseDifficulty(r.difficulty);
        int money;
        float moneyF;
        money = float.TryParse(r.reward, out moneyF) ? Mathf.RoundToInt(moneyF) : 150;
        q.rewardMoney = money;
        q.rewardDescription = money.ToString();

        bool evil = (r.alignment ?? "").Contains("ชั่ว");
        q.karmaReward = evil ? -(15 + 15 * (int)q.difficulty) : 30;

        q.requiredMinigameSequence = ParseSequence(r.sequence);
        q.completedMinigameSequence = new List<MinigameType>();

        string modelNote = "";
        if (!string.IsNullOrEmpty(r.model))
        {
            q.npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + r.model + ".prefab");
            if (q.npcPrefab == null) modelNote = " ⚠️ ไม่เจอ prefab " + r.model;
        }

        report.AppendLine((evil ? "😈 " : "🙏 ") + r.name + " — " + q.rewardMoney + " บาท, " +
                          string.Join(" → ", q.requiredMinigameSequence.Select(QuestData.GetMinigameNameThai)) +
                          ", โมเดล " + (q.npcPrefab != null ? q.npcPrefab.name : "(Default)") + modelNote);
        return q;
    }

    static QuestDifficulty ParseDifficulty(string s)
    {
        s = s ?? "";
        if (s.Contains("ยากมาก")) return QuestDifficulty.VeryHard;
        if (s.Contains("ยาก")) return QuestDifficulty.Hard;
        if (s.Contains("ปานกลาง")) return QuestDifficulty.Medium;
        return QuestDifficulty.Easy;
    }

    /// <summary>
    /// "1) ท่องคาถา → 2) เขียนยันต์" → [RhythmChantWASD, TalismanDrawing]
    /// ตัดพิธีที่ซ้ำออก เพราะระบบนับพิธีชนิดเดียวกันได้ครั้งเดียว (ถ้าซ้ำ เควสจะไม่มีวันจบ)
    /// </summary>
    static List<MinigameType> ParseSequence(string s)
    {
        var result = new List<MinigameType>();
        foreach (var step in (s ?? "").Split('→'))
        {
            MinigameType t;
            if (step.Contains("ท่องคาถา")) t = MinigameType.RhythmChantWASD;
            else if (step.Contains("เขียนยันต์")) t = MinigameType.TalismanDrawing;
            else if (step.Contains("โปรยข้าวสาร")) t = MinigameType.TimingBarQTE;
            else if (step.Contains("ปาดมีด") || step.Contains("ตวัด")) t = MinigameType.SequentialSlashQTE;
            else continue;

            bool dup = result.Contains(t) ||
                       (t == MinigameType.TalismanDrawing && result.Contains(MinigameType.DeadByDaylightQTE));
            if (!dup) result.Add(t);
        }
        if (result.Count == 0) result.Add(MinigameType.RhythmChantWASD);
        return result;
    }
}
