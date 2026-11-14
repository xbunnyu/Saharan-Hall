using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ลองหน้าตา UI แบบเกมผีในฉากที่เปิดอยู่ — ไม่เซฟให้เอง
/// ชอบ: Ctrl+S | ไม่ชอบ: Ctrl+Z (ย้อนทั้งหมดในครั้งเดียว) หรือปิดฉากแล้วเลือก Don't Save
/// เมนู: Tools > Saharan > ลองหน้าตา UI แบบผี (ยังไม่เซฟ)
/// </summary>
public static class HorrorUIPreview
{
    static readonly Color PanelTint = new Color(0.36f, 0.33f, 0.31f, 1f);   // ไม้เก่าดำ / ขอบทองหมอง / กระดาษหม่น
    static readonly Color IconTint = new Color(0.72f, 0.68f, 0.62f, 1f);    // ไอคอนเล็กหรี่ลงนิดหน่อย
    static readonly Color TextLight = new Color32(0xE8, 0xDC, 0xC4, 0xFF);  // ครีมหม่น
    static readonly Color TextDim = new Color32(0xA8, 0x97, 0x7A, 0xFF);    // ตัวรอง
    static readonly Color CandleOrange = new Color32(0xFF, 0xB3, 0x5C, 0xFF);
    const float PanelAlpha = 0.88f;
    const float IconMaxSize = 64f;

    [MenuItem("Tools/Saharan/ลองหน้าตา UI แบบผี (ยังไม่เซฟ)")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("UI แบบผี", "หยุดเกม (ปุ่ม Play) ก่อน แล้วค่อยกดเมนูนี้", "ตกลง");
            return;
        }

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.gameObject.scene.IsValid()).ToArray();
        if (canvases.Length == 0)
        {
            EditorUtility.DisplayDialog("UI แบบผี", "ไม่เจอ Canvas ในฉากที่เปิดอยู่", "ตกลง");
            return;
        }

        if (!EditorUtility.DisplayDialog("ลองหน้าตา UI แบบผี",
            "จะปรับ UI ในฉากที่เปิดอยู่:\n\n" +
            "• หรี่แผงไม้ ขอบทอง และกระดาษให้หม่นมืด\n" +
            "• ตัวหนังสือสีเข้มบนกระดาษ → สีครีมหม่น อ่านบนพื้นมืดได้\n" +
            "• ป้าย [E] ไม่มีกรอบ เหลือแค่ข้อความเล็ก\n" +
            "• ปุ่ม: ชี้แล้วเรืองสีเทียน\n\n" +
            "ฉากยังไม่ถูกเซฟ — ชอบกด Ctrl+S, ไม่ชอบกด Ctrl+Z",
            "ลองเลย", "ยกเลิก")) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("ลองหน้าตา UI แบบผี");

        var ui = Object.FindFirstObjectByType<InteractionUIManager>(FindObjectsInactive.Include);
        Image crosshair = ui != null ? ui.crosshairImage : null;
        GameObject prompt = ui != null ? ui.promptPanel : null;

        int images = 0, texts = 0, buttons = 0;
        foreach (var canvas in canvases)
        {
            foreach (var img in canvas.GetComponentsInChildren<Image>(true))
            {
                if (img == crosshair || img.sprite == null) continue;
                Undo.RecordObject(img, "UI แบบผี");
                var rt = img.rectTransform.rect;
                bool icon = rt.width <= IconMaxSize && rt.height <= IconMaxSize;
                Color c = img.color;
                if (icon)
                {
                    img.color = new Color(c.r * IconTint.r, c.g * IconTint.g, c.b * IconTint.b, c.a);
                }
                else
                {
                    img.color = new Color(c.r * PanelTint.r, c.g * PanelTint.g, c.b * PanelTint.b, c.a * PanelAlpha);
                }
                images++;
            }

            foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Color c = tmp.color;
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                if (lum < 0.45f)
                {
                    Undo.RecordObject(tmp, "UI แบบผี");
                    tmp.color = new Color(TextLight.r, TextLight.g, TextLight.b, c.a);
                    texts++;
                }
            }

            foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
            {
                Undo.RecordObject(btn, "UI แบบผี");
                var cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = CandleOrange;
                cb.selectedColor = CandleOrange;
                cb.pressedColor = new Color(CandleOrange.r * 0.8f, CandleOrange.g * 0.8f, CandleOrange.b * 0.8f, 1f);
                btn.colors = cb;
                buttons++;
            }
        }

        // ป้าย [E]: เอากรอบออก เหลือข้อความเล็กๆ
        string promptNote = "ไม่เจอป้าย [E]";
        if (prompt != null)
        {
            foreach (var img in prompt.GetComponentsInChildren<Image>(true))
            {
                Undo.RecordObject(img, "UI แบบผี");
                img.enabled = false;
            }
            Undo.RecordObject(prompt.transform, "UI แบบผี");
            prompt.transform.localScale = prompt.transform.localScale * 0.8f;

            if (ui.promptItemNameText != null)
            {
                Undo.RecordObject(ui.promptItemNameText, "UI แบบผี");
                ui.promptItemNameText.color = TextDim;
            }
            if (ui.promptActionText != null)
            {
                Undo.RecordObject(ui.promptActionText, "UI แบบผี");
                ui.promptActionText.color = TextLight;
            }
            promptNote = "ป้าย [E] เหลือแค่ข้อความ (ย่อ 80%)";
        }

        Undo.CollapseUndoOperations(group);
        foreach (var canvas in canvases) EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        EditorUtility.DisplayDialog("UI แบบผี",
            "ปรับแล้ว: แผง/ไอคอน " + images + " ชิ้น, ตัวหนังสือ " + texts + " ชิ้น, ปุ่ม " + buttons + " ปุ่ม\n" + promptNote +
            "\n\nกด Play ลองดู\nชอบ → หยุดเกมแล้วกด Ctrl+S\nไม่ชอบ → หยุดเกมแล้วกด Ctrl+Z (ย้อนทั้งหมด)",
            "ตกลง");
    }
}
