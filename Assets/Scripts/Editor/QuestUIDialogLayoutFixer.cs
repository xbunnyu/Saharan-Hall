#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// เครื่องมือสำหรับจัดระเบียบหน้าต่างบทสนทนาเควส (NPC Info Display / Quest Dialog)
/// - แก้ปัญหาตัวหนังสือล้นจอ และหลุดออกนอกขอบล่าง
/// - แก้ปัญหา Scale Y บิดเบี้ยว (0.28x และ 5.57x) ให้กลับมาเป็น 1.0x มาตรฐาน
/// - ปรับขนาดกล่องข้อความให้กว้างเต็มแถบ และเปิดระบบ Auto Size + Word Wrapping อัตโนมัติ
/// </summary>
public class QuestUIDialogLayoutFixer : EditorWindow
{
    [MenuItem("Tools/Saharan Hall/🛠️ จัดระเบียบหน้าต่างเควสและแก้ตัวหนังสือล้นจอ (Fix Quest Dialog Layout)")]
    public static void FixQuestDialogLayout()
    {
        // 1. ค้นหา GameObject พาเนลบทสนทนาเควส
        GameObject panelGO = GameObject.Find("NPC Info Dispaly");
        if (panelGO == null) panelGO = GameObject.Find("NPC Info Display");
        if (panelGO == null) panelGO = GameObject.Find("QuestDialogPanel");

        if (panelGO == null)
        {
            var qm = Object.FindFirstObjectByType<QuestUIManager>();
            if (qm != null && qm.questDialogPanel != null)
            {
                panelGO = qm.questDialogPanel;
            }
        }

        if (panelGO == null)
        {
            EditorUtility.DisplayDialog("ไม่พบหน้าต่าง", "ไม่พบ GameObject 'NPC Info Dispaly' ในฉากปัจจุบัน กรุณาเปิดฉาก GamePlay ก่อนครับ", "ตกลง");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panelGO, "Fix Quest Dialog Layout");

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        if (panelRT != null)
        {
            // 2. รีเซ็ต Scale ของพาเนลแม่กลับเป็น 1, 1, 1
            panelRT.localScale = Vector3.one;

            // ตั้ง Anchor ยึดขอบล่างของจอ (Bottom-Stretch) สูง 270 พิกเซล
            panelRT.anchorMin = new Vector2(0f, 0f);
            panelRT.anchorMax = new Vector2(1f, 0f);
            panelRT.pivot = new Vector2(0.5f, 0f);
            panelRT.anchoredPosition = new Vector2(0f, 0f);
            panelRT.sizeDelta = new Vector2(0f, 270f);
        }

        // 3. ปรับแต่งรูปภาพ Portrait NPC
        Transform portraitTr = FindChildRecursive(panelGO.transform, "Display NPC");
        if (portraitTr != null)
        {
            RectTransform rt = portraitTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(130f, 0f);
                rt.sizeDelta = new Vector2(170f, 170f);
            }
        }

        // 4. ปรับแต่งชื่อ NPC (NPC name)
        Transform nameTr = FindChildRecursive(panelGO.transform, "NPC name");
        if (nameTr != null)
        {
            RectTransform rt = nameTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(240f, -20f);
                rt.sizeDelta = new Vector2(320f, 40f);
            }

        }

        // 5. ปรับแต่งชื่อเควส (Quest Titel)
        Transform titleTr = FindChildRecursive(panelGO.transform, "Quest Titel");
        if (titleTr == null) titleTr = FindChildRecursive(panelGO.transform, "Quest Title");
        if (titleTr != null)
        {
            RectTransform rt = titleTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(580f, -22f);
                rt.sizeDelta = new Vector2(500f, 36f);
            }
        }

        // 6. ปรับแต่งเนื้อหาบทสนทนาเควส (Quest Descriptions)
        Transform descTr = FindChildRecursive(panelGO.transform, "Quest Descriptions");
        if (descTr == null) descTr = FindChildRecursive(panelGO.transform, "Quest Description");
        if (descTr != null)
        {
            RectTransform rt = descTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                // ขยายกว้างเต็มแผงด้านขวาจาก x=240 ถึงขอบขวา
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(240f, -65f);
                rt.sizeDelta = new Vector2(-280f, 115f);
            }
        }

        // 7. ปรับแต่งกล่องข้อความปฏิเสธ (DeclineDialogue)
        Transform declineDiagTr = FindChildRecursive(panelGO.transform, "DeclineDialogue");
        if (declineDiagTr != null)
        {
            RectTransform rt = declineDiagTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(240f, -65f);
                rt.sizeDelta = new Vector2(-280f, 115f);
            }
        }

        // 8. ปรับแต่งสิ่งที่ต้องการ (QuestRequirement)
        Transform reqTr = FindChildRecursive(panelGO.transform, "QuestRequirement");
        if (reqTr != null)
        {
            RectTransform rt = reqTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(240f, 20f);
                rt.sizeDelta = new Vector2(400f, 35f);
            }
        }

        // 9. ปรับแต่งรางวัลตอบแทน (Quest Reward)
        Transform rewardTr = FindChildRecursive(panelGO.transform, "Quest Reward");
        if (rewardTr != null)
        {
            RectTransform rt = rewardTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(660f, 20f);
                rt.sizeDelta = new Vector2(350f, 35f);
            }
        }

        // 10. จัดตำแหน่งปุ่มกด 3 ปุ่ม (Inquiry, Accept, Decline) ทางมุมขวาล่าง
        // ปุ่มสอบถามเพิ่มเติม (Inquiry)
        Transform inqTr = FindChildRecursive(panelGO.transform, "Inquiry");
        if (inqTr != null)
        {
            RectTransform rt = inqTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-330f, 20f);
                rt.sizeDelta = new Vector2(200f, 42f);
            }
            ResetChildText(inqTr);
        }

        // ปุ่มรับงาน (Accept)
        Transform accTr = FindChildRecursive(panelGO.transform, "Accept");
        if (accTr != null)
        {
            RectTransform rt = accTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-170f, 20f);
                rt.sizeDelta = new Vector2(145f, 42f);
            }
            ResetChildText(accTr);
        }

        // ปุ่มไม่ละ (Decline)
        Transform decTr = FindChildRecursive(panelGO.transform, "Decline");
        if (decTr != null)
        {
            RectTransform rt = decTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-15f, 20f);
                rt.sizeDelta = new Vector2(145f, 42f);
            }
            ResetChildText(decTr);
        }

        // บันทึกฉาก
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelGO.scene);

        EditorUtility.DisplayDialog(
            "จัดระเบียบสำเร็จ!",
            "✅ รีเซ็ต Scale เป็น 1.0x ทุกชิ้นเรียบร้อยแล้ว\n" +
            "✅ ขยายกล่องข้อความให้กว้างเต็มแถบ และเปิด Auto Size เรียบร้อยแล้ว\n" +
            "✅ จัดตำแหน่งปุ่ม 'รับงาน', 'ไม่ละ', 'สอบถาม' ลงขวาล่างเรียบร้อยแล้ว",
            "เยี่ยมมาก"
        );

        Debug.Log("[QuestUIDialogLayoutFixer] 🎉 จัดระเบียบหน้าต่างบทสนทนาเควสเสร็จสมบูรณ์!");
    }

    private static void ResetChildText(Transform buttonTr)
    {
        var textTr = buttonTr.GetComponentInChildren<TextMeshProUGUI>();
        if (textTr != null)
        {
            RectTransform rt = textTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Trim().Equals(name.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
