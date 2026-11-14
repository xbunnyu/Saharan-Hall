using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI แบบใหม่ (ตามภาพจำลองรอบ 3) — จัดหน้าตอนเกมรัน ไม่แก้ไฟล์ฉาก ระบบเดิมทำงานเหมือนเดิม
/// - เอา UI แบบเก่าออก: แผงไม้ กระดาษ โบว์ ป้ายห้อยโซ่ ถุงเงิน
/// - กล่องภารกิจมุมซ้ายบนแบบรายการเช็ก (ช่องติ๊ก + คำอธิบาย)
/// - บทพูดเป็นซับไตเติลแบบหนัง "ชื่อ :  บทพูด" ไม่มีพื้นหลัง
/// - ปุ่มเรียบ: พื้นดำโปร่ง กรอบเส้นบาง ชี้แล้วเป็นสีเทียน
/// - วันที่/เงิน/ชื่อเสียง โผล่เฉพาะตอนค่าเปลี่ยน แล้วจางหาย
/// - ฟอนต์: Angsana New (หัวข้อ) + Leelawadee UI (บทพูด/ปุ่ม) ดึงจาก Windows ตอนรัน (ไม่ได้ใส่ไฟล์ฟอนต์ในโปรเจกต์)
/// ปิด: วาง component นี้ในฉากแล้วเอาติ๊ก Use Horror UI ออก
/// </summary>
[DefaultExecutionOrder(9000)]
public class HorrorUIStyle : MonoBehaviour
{
    [Tooltip("เอาติ๊กออก = ใช้ UI แบบเก่า")]
    public bool useHorrorUI = true;

    [Header("ฟอนต์ (ดึงจาก Windows)")]
    public string titleFontFamily = "Angsana New";
    public string bodyFontFamily = "Leelawadee UI";

    [Header("ขนาด")]
    // ขนาดตั้งให้เข้ากับฟอนต์ K2D ในเกม (ตัวใหญ่กว่า Angsana)
    public float subtitleSize = 34f; // เล็กลง 20% จาก 42
    public float trackerTitleSize = 30f;
    public float trackerItemSize = 27f;
    public float trackerDescSize = 17f;
    public float buttonTextSize = 28f;
    [Tooltip("ป้ายวันที่/เงิน/ชื่อเสียง โชว์กี่วินาทีหลังค่าเปลี่ยน")]
    public float hudShowSeconds = 3.5f;
    [Tooltip("การ์ด 'วันที่ X' ค้างกลางจอกี่วินาที ก่อนเริ่มจาง")]
    public float dayCardSeconds = 2.5f;
    [Tooltip("ใช้เวลาจางหายกี่วินาที")]
    public float dayCardFadeSeconds = 1.5f;

    static readonly Color Cream = Color.white; // ตัวหนังสือหลักสีขาว (เดิมสีครีม)
    static readonly Color Muted = new Color32(0xA8, 0x97, 0x7A, 0xFF);
    static readonly Color Dim = new Color32(0x7A, 0x70, 0x62, 0xFF);
    static readonly Color Candle = new Color32(0xFF, 0xB3, 0x5C, 0xFF);
    static readonly Color CheckRed = new Color32(0xC0, 0x39, 0x2B, 0xFF);

    TMP_FontAsset titleFont, bodyFont, createFont;
    TMP_FontAsset NewTextFont(TMP_FontAsset preferred) { return preferred != null ? preferred : createFont; }

    /// <summary>หา component ถ้าไม่มีให้เพิ่ม (ห้ามใช้ ?? กับ component ของ Unity — ใน Editor มันคืนค่าว่างปลอม)</summary>
    static T GetOrAdd<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); if (c == null) c = go.AddComponent<T>(); return c; }
    static T GetOrAdd<T>(Component comp) where T : Component { return GetOrAdd<T>(comp.gameObject); }
    bool applied;

    // ── bootstrap: สร้างตัวเองอัตโนมัติทุกฉาก ──
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }
    static void OnSceneLoaded(Scene s, LoadSceneMode m) { Ensure(); }
    static void Ensure()
    {
        // หน้าเมนูหลักไม่ต้องมี HUD วันที่/เงิน
        if (SceneManager.GetActiveScene().name == "MainMenu") return;
        if (FindFirstObjectByType<HorrorUIStyle>(FindObjectsInactive.Include) != null) return;
        new GameObject("[HorrorUIStyle]").AddComponent<HorrorUIStyle>();
    }

    IEnumerator Start()
    {
        if (!useHorrorUI) yield break;
        yield return null; // ให้ UI เดิม Start ครบก่อน
        yield return null;
        LoadFonts();
        ApplyAll();
        applied = true;
    }

    // ══════════ ฟอนต์ ══════════
    [Tooltip("ดึงฟอนต์จาก Windows (ปิดไว้ — ตัวอักษรไทยเพี้ยน) / ปิด = ใช้ฟอนต์ที่มีในเกม")]
    public bool useWindowsFonts = false;

    void LoadFonts()
    {
        // ฟอนต์ในเกม (K2D) จากตัวหนังสือที่มีอยู่ในฉาก
        TMP_FontAsset gameFont = null;
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.questDescriptionText != null)
            gameFont = QuestUIManager.Instance.questDescriptionText.font;
        if (gameFont == null)
        {
            var anyText = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            if (anyText != null) gameFont = anyText.font;
        }
        if (gameFont == null) gameFont = TMP_Settings.defaultFontAsset;

        // ข้อความเดิม: ไม่เปลี่ยนฟอนต์ (ฟอนต์ของแต่ละกล่องมีตัวอักษรต่างกัน — บางตัวไม่มีตัวอังกฤษ/สัญลักษณ์)
        // ข้อความที่สร้างใหม่ (กล่องภารกิจ/ป้ายวันที่/เงิน) ใช้ createFont
        createFont = gameFont;
        titleFont = bodyFont = null;
        if (!useWindowsFonts) return;

        var t = MakeOSFont(titleFontFamily, gameFont); if (t != null) titleFont = t;
        var b = MakeOSFont(bodyFontFamily, gameFont); if (b != null) bodyFont = b;
    }

    static TMP_FontAsset MakeOSFont(string family, TMP_FontAsset fallback)
    {
        try
        {
            var fa = TMP_FontAsset.CreateFontAsset(family, "Regular");
            if (fa == null) { Debug.LogWarning($"[HorrorUI] ไม่เจอฟอนต์ '{family}' ในเครื่อง → ใช้ฟอนต์เดิม"); return null; }
            fa.isMultiAtlasTexturesEnabled = true;
            if (fallback != null)
            {
                if (fa.fallbackFontAssetTable == null) fa.fallbackFontAssetTable = new List<TMP_FontAsset>();
                fa.fallbackFontAssetTable.Add(fallback); // ตัวอักษรที่ฟอนต์ใหม่ไม่มี (เช่น ฿) ใช้ฟอนต์เดิมแทน
            }
            return fa;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HorrorUI] โหลดฟอนต์ '{family}' ไม่ได้: {e.Message}");
            return null;
        }
    }

    /// <summary>กล่องแม่ถูกย่อ/ขยายเทียบกับ Canvas หลักกี่เท่า (1 = ปกติ)</summary>
    static float ParentScale(RectTransform rt)
    {
        if (rt == null || rt.parent == null) return 1f;
        Canvas c = rt.GetComponentInParent<Canvas>(true);
        if (c != null) c = c.rootCanvas;
        float canvasScale = c != null ? Mathf.Abs(c.transform.lossyScale.x) : 1f;
        float parentScale = Mathf.Abs(rt.parent.lossyScale.x);
        if (canvasScale < 1e-5f || parentScale < 1e-5f) return 1f;
        return Mathf.Clamp(parentScale / canvasScale, 0.05f, 20f);
    }

    void Style(TMP_Text t, TMP_FontAsset font, float size, Color color, float outline = 0.22f)
    {
        if (t == null) return;
        if (font != null) t.font = font;
        t.enableAutoSizing = false;
        t.color = color;
        t.fontStyle |= FontStyles.Bold; // ตัวหนาทั้งหมด
        // ค่าที่ตั้งไว้ในฉากเดิมทำให้ตัวหนังสือเบี้ยว/เล็กจิ๋ว: ระยะขอบ (margin) และการย่อขนาด (scale)
        t.margin = Vector4.zero;
        t.rectTransform.localScale = Vector3.one;
        // ขนาดบนจอจริง: ชดเชยกล่องแม่ที่ถูกย่อ/ขยายไว้ในฉาก
        t.fontSize = size / ParentScale(t.rectTransform);
        t.textWrappingMode = TextWrappingModes.Normal;
        try { t.outlineWidth = outline; t.outlineColor = new Color32(0, 0, 0, 220); } catch { }
    }

    // ══════════ จัดหน้าครั้งแรก ══════════
    QuestUIManager q;
    InteractionUIManager ui;
    readonly List<Button> choiceButtons = new List<Button>();
    readonly List<GameObject> oldButtonFrames = new List<GameObject>();
    string lastPromptRaw, lastPromptClean;
    TMP_Text acceptLabel;

    /// <summary>ล้างป้าย [E]: เอาแท็กสี อีโมจิ ลูกศร ออก เหลือ "[E]  ข้อความ" ตัวเดียวสีครีม</summary>
    static string CleanPrompt(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "");
        s = s.Replace("→", " - ").Replace("➔", " - ").Replace("★", "");
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsSurrogate(c) || c == '️' || c == '‍') continue;
            if (c >= '←' && c <= '⇿') continue;
            if (c >= '⌀' && c <= '➿') continue;
            sb.Append(c);
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s{2,}", "  ").Trim();
    }

    void ApplyAll()
    {
        q = QuestUIManager.Instance != null ? QuestUIManager.Instance : FindFirstObjectByType<QuestUIManager>(FindObjectsInactive.Include);
        ui = InteractionUIManager.Instance != null ? InteractionUIManager.Instance : FindFirstObjectByType<InteractionUIManager>(FindObjectsInactive.Include);

        if (q != null) ApplyDialog();
        if (ui != null) ApplyPromptAndNotification();
        SetupHUD();
        BuildTrackerCanvas();
    }

    void HideImages(GameObject root, ICollection<Graphic> keep = null)
    {
        if (root == null) return;
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (keep != null && keep.Contains(img)) continue;
            img.enabled = false;
        }
        foreach (var raw in root.GetComponentsInChildren<RawImage>(true)) raw.enabled = false;
    }

    // ── กล่องบทสนทนา → ซับไตเติล + ปุ่มเรียบ ──
    void ApplyDialog()
    {
        GameObject panel = q.questDialogPanel;
        if (panel == null) return;

        var keep = new HashSet<Graphic>();
        foreach (var b in new[] { q.acceptButton, q.declineButton, q.inquiryButton })
            if (b != null && b.targetGraphic != null) keep.Add(b.targetGraphic);
        HideImages(panel, keep);

        // แผงเต็มจอ → ตำแหน่งลูกๆ อิงขอบจอ
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        prt.localScale = Vector3.one;

        if (q.npcNameText != null) q.npcNameText.alpha = 0f; // ชื่อไปอยู่หน้าบทพูดแทน
        if (q.questTitleText != null) q.questTitleText.alpha = 0f;

        foreach (var t in new TMP_Text[] { q.questDescriptionText, q.declineDialogueText, q.waitingDialogueText, q.responseDialogueText })
        {
            if (t == null) continue;
            Style(t, bodyFont, subtitleSize, Cream, 0.25f);
            t.fontStyle &= ~FontStyles.Bold; // ซับไตเติลไม่ใช้ตัวหนา
            t.alignment = TextAlignmentOptions.Bottom;
            var rt = t.rectTransform;
            if (rt.parent == panel.transform || rt.IsChildOf(panel.transform))
            {
                rt.SetParent(panel.transform, false);
                rt.anchorMin = new Vector2(0.10f, 0.20f); rt.anchorMax = new Vector2(0.90f, 0.38f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
            }
        }
        if (q.responsePanel != null) HideImages(q.responsePanel);

        if (q.questRequirementText != null)
        {
            Style(q.questRequirementText, bodyFont, 22f, Muted, 0.2f);
            var rt = q.questRequirementText.rectTransform;
            rt.SetParent(panel.transform, false);
            rt.anchorMin = new Vector2(0.2f, 0.135f); rt.anchorMax = new Vector2(0.8f, 0.165f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.localScale = Vector3.one;
            q.questRequirementText.alignment = TextAlignmentOptions.Center;
        }
        if (q.questRewardText != null) q.questRewardText.alpha = 0f; // ค่าตอบแทนไปอยู่บนปุ่มรับงาน

        // ปุ่ม: สอบถาม | รับงาน | ปฏิเสธ เรียงกลางจอด้านล่าง
        PlaceButton(q.inquiryButton, panel.transform, -370f);
        PlaceButton(q.acceptButton, panel.transform, 0f);
        PlaceButton(q.declineButton, panel.transform, 370f);
    }

    void PlaceButton(Button b, Transform panel, float x)
    {
        if (b == null) return;
        choiceButtons.Add(b);
        var rt = b.GetComponent<RectTransform>();
        // กรอบเก่าของปุ่ม (เช่น Accept/Accept) มีป้าย "รับงาน" กับข้อความรางวัลเก่าค้างอยู่ → จำไว้แล้วซ่อนตลอด
        if (rt.parent != null && rt.parent != panel) oldButtonFrames.Add(rt.parent.gameObject);
        rt.SetParent(panel, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(330f, 70f);
        rt.anchoredPosition = new Vector2(x, 105f);
        rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;

        // ปิดรูปไม้/ลวดลายของลูกๆ ในปุ่ม
        foreach (var img in b.GetComponentsInChildren<Image>(true)) if (img != b.targetGraphic) img.enabled = false;

        var bg = b.targetGraphic as Image;
        if (bg == null) bg = GetOrAdd<Image>(b.gameObject);
        bg.enabled = true;
        bg.sprite = null;
        bg.type = Image.Type.Simple;
        bg.color = new Color(0.04f, 0.03f, 0.02f, 0.5f);
        b.targetGraphic = bg;
        b.transition = Selectable.Transition.None;

        // กรอบเส้นบาง 4 ด้าน (ไม่ใช้ Outline effect เพราะจะย้อมสีทั้งปุ่ม)
        var borders = new List<Image>();
        var holder = new GameObject("HorrorBorder", typeof(RectTransform));
        holder.transform.SetParent(rt, false);
        holder.transform.SetAsFirstSibling();
        var hrt = holder.GetComponent<RectTransform>();
        hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one; hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
        Vector2[] amin = { new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 0) };
        Vector2[] amax = { new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        Vector2[] size = { new Vector2(0, 1.5f), new Vector2(0, 1.5f), new Vector2(1.5f, 0), new Vector2(1.5f, 0) };
        for (int i = 0; i < 4; i++)
        {
            var e = new GameObject("Edge", typeof(RectTransform));
            e.transform.SetParent(holder.transform, false);
            var img = e.AddComponent<Image>();
            img.raycastTarget = false;
            var ert = img.rectTransform;
            ert.anchorMin = amin[i]; ert.anchorMax = amax[i];
            ert.pivot = new Vector2(amin[i].x == 1 ? 1 : 0, amin[i].y == 1 ? 1 : 0);
            ert.sizeDelta = size[i]; ert.anchoredPosition = Vector2.zero;
            borders.Add(img);
        }

        foreach (var t in b.GetComponentsInChildren<TMP_Text>(true))
        {
            var trt = t.rectTransform;
            // ย้ายตัวหนังสือมาเป็นลูกตรงของปุ่ม (ตัวกลางในฉากเดิมถูกย่อขนาดไว้ → ตัวหนังสือเล็กจิ๋ว)
            trt.SetParent(rt, false);
            trt.localRotation = Quaternion.identity;
            Style(t, bodyFont, buttonTextSize, Cream, 0.18f);
            t.alignment = TextAlignmentOptions.Center;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

        var hover = GetOrAdd<HorrorButtonHover>(b.gameObject);
        hover.Init(b, borders.ToArray());
    }

    // ── ป้าย [E] / ข้อความแจ้งเตือน ──
    void ApplyPromptAndNotification()
    {
        if (ui.promptPanel != null) HideImages(ui.promptPanel);
        if (ui.promptItemNameText != null) ui.promptItemNameText.alpha = 0f;
        if (ui.promptActionText != null)
        {
            Style(ui.promptActionText, bodyFont, 34f, Cream, 0.25f);
            ui.promptActionText.alignment = TextAlignmentOptions.Center;
            ui.promptActionText.textWrappingMode = TextWrappingModes.NoWrap; // บรรทัดเดียวตามตัวอย่าง
            // ย้ายป้าย [E] ลงมาด้านล่างจอ (ประมาณ 22% จากขอบล่าง) ตามภาพตัวอย่าง ไม่บังกลางจอ
            if (ui.promptPanel != null)
            {
                var ppr = ui.promptPanel.GetComponent<RectTransform>();
                if (ppr != null)
                {
                    ppr.anchorMin = ppr.anchorMax = new Vector2(0.5f, 0.22f);
                    ppr.pivot = new Vector2(0.5f, 0.5f);
                    ppr.anchoredPosition = Vector2.zero;
                }
            }
            var prt = ui.promptActionText.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            float s = ParentScale(prt);
            prt.sizeDelta = new Vector2(1200f / s, 70f / s);
        }
        if (ui.notificationText != null) Style(ui.notificationText, bodyFont, 34f, Cream, 0.25f);
    }

    // ══════════ HUD แบบใหม่ (ตามภาพตัวอย่าง) ══════════
    // "คืนที่ ๑" กลางบน + "+600 บาท" สีเทียนมุมขวาบน + ชื่อเสียงตัวเล็กใต้วันที่
    // ซ่อนไว้ โผล่ตอนค่าเปลี่ยนแล้วจางหาย / ป้ายเก่า (ไม้ โซ่ ถุงเงิน) ซ่อนทั้งหมด
    readonly List<CanvasGroup> oldHud = new List<CanvasGroup>();
    TMP_Text moneySrc, repSrc;
    TMP_Text dayLabel, moneyLabel, repLabel, topDayLabel, moneyTotal;
    Image repFill;
    CanvasGroup dayGroup, moneyGroup;
    float dayUntil, moneyUntil;
    int lastDay = -1, lastMoney = int.MinValue; string lastRep;

    void SetupHUD()
    {
        var olds = new List<TMP_Text>();
        if (DayManager.Instance != null && DayManager.Instance.dayHUDText != null) olds.Add(DayManager.Instance.dayHUDText);
        foreach (var m in FindObjectsByType<MoneyDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { var t = m.GetComponentInChildren<TMP_Text>(true); if (t != null) { olds.Add(t); moneySrc = t; } }
        foreach (var r in FindObjectsByType<ReputationDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { var t = r.GetComponentInChildren<TMP_Text>(true); if (t != null) { olds.Add(t); repSrc = t; } }

        foreach (var t in olds)
        {
            // ซ่อนทั้งแผงของป้ายเก่า (แผงบนสุดใต้ Canvas หลัก)
            Transform root = t.transform;
            var canvas = t.canvas != null ? t.canvas.rootCanvas : null;
            while (root.parent != null && canvas != null && root.parent != canvas.transform) root = root.parent;
            var g = GetOrAdd<CanvasGroup>(root.gameObject);
            g.alpha = 0f; g.blocksRaycasts = false;
            if (!oldHud.Contains(g)) oldHud.Add(g);
        }
    }

    TMP_Text MakeHudText(string name, Vector2 anchor, Vector2 pos, float size, Color color, TextAlignmentOptions align, out CanvasGroup group)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(trackerRoot, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        Style(t, NewTextFont(titleFont), size, color, 0.25f);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.alignment = align;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(600f, size * 1.6f);
        rt.anchoredPosition = pos;
        group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        return t;
    }

    static string ThaiDigits(int n)
    {
        var s = n.ToString();
        var sb = new StringBuilder();
        foreach (char ch in s) sb.Append(ch >= '0' && ch <= '9' ? (char)('๐' + (ch - '0')) : ch);
        return sb.ToString();
    }

    static int ParseNumber(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        var digits = new StringBuilder();
        foreach (char ch in s) if (char.IsDigit(ch) && ch < 128) digits.Append(ch); else if (digits.Length > 0 && ch != ',') break;
        int v; return int.TryParse(digits.ToString(), out v) ? v : 0;
    }

    void UpdateHUD()
    {
        if (trackerRoot == null) return;
        if (dayLabel == null)
        {
            // การ์ดขึ้นวันใหม่: "วันที่ 1" ตัวใหญ่กลางจอ บนพื้นดำจางๆ ทั้งจอ แล้วค่อยๆ จางหาย
            var card = new GameObject("HUD_DayCard", typeof(RectTransform));
            card.transform.SetParent(trackerRoot, false);
            card.transform.SetAsFirstSibling(); // อยู่หลังกล่องภารกิจ
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var dim = card.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = false;
            dayGroup = card.AddComponent<CanvasGroup>();
            dayGroup.alpha = 0f; dayGroup.blocksRaycasts = false; dayGroup.interactable = false;

            var tgo = new GameObject("DayText", typeof(RectTransform));
            tgo.transform.SetParent(card.transform, false);
            dayLabel = tgo.AddComponent<TextMeshProUGUI>();
            Style(dayLabel, NewTextFont(titleFont), 72f, Cream, 0.2f);
            dayLabel.textWrappingMode = TextWrappingModes.NoWrap;
            dayLabel.alignment = TextAlignmentOptions.Center;
            dayLabel.raycastTarget = false;
            var trt2 = dayLabel.rectTransform;
            trt2.anchorMin = trt2.anchorMax = new Vector2(0.5f, 0.5f);
            trt2.pivot = new Vector2(0.5f, 0.5f);
            trt2.sizeDelta = new Vector2(1200f, 160f);
            trt2.anchoredPosition = Vector2.zero;
            // แถบบนแสดงตลอด: "วันที่ ๑" กลางบน + ชื่อเสียงใต้วันที่ + กระเป๋าเงินมุมขวาบน
            CanvasGroup g0;
            topDayLabel = MakeHudText("HUD_Day", new Vector2(0.5f, 1f), new Vector2(0f, -18f), 40f, Cream, TextAlignmentOptions.Top, out g0);
            g0.alpha = 1f;
            repLabel = MakeHudText("HUD_Rep", new Vector2(0.5f, 1f), new Vector2(0f, -72f), 22f, Muted, TextAlignmentOptions.Top, out g0);
            g0.alpha = 1f;
            repFill = MakeRepBar(new Vector2(0f, -110f));
            moneyTotal = MakeHudText("HUD_MoneyTotal", new Vector2(1f, 1f), new Vector2(-48f, -24f), 34f, Cream, TextAlignmentOptions.TopRight, out g0);
            g0.alpha = 1f;
            // ตัวเลข +600 / -100 ลอยใต้ยอดเงินตอนเงินเปลี่ยน แล้วจางหาย
            moneyLabel = MakeHudText("HUD_Money", new Vector2(1f, 1f), new Vector2(-48f, -76f), 26f, Candle, TextAlignmentOptions.TopRight, out moneyGroup);
        }

        // ป้ายเก่าอาจถูกเปิดกลับโดยระบบเดิม → กดซ่อนไว้ตลอด
        foreach (var g in oldHud) if (g != null && g.alpha != 0f) g.alpha = 0f;

        // วันที่ (เลขไทย)
        int day = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        if (day != lastDay)
        {
            lastDay = day;
            dayLabel.text = "วันที่ " + ThaiDigits(day);
            topDayLabel.text = "วันที่ " + ThaiDigits(day);
            dayUntil = Time.time + dayCardSeconds;
        }

        // เงิน: ยอดรวมโชว์ตลอด + ส่วนที่ได้/เสีย เช่น "+600"
        int money = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money
                  : (moneySrc != null ? ParseNumber(moneySrc.text) : 0);
        if (lastMoney == int.MinValue || money != lastMoney)
        {
            if (lastMoney != int.MinValue)
            {
                int delta = money - lastMoney;
                moneyLabel.text = (delta >= 0 ? "+" : "-") + Mathf.Abs(delta).ToString("N0");
                moneyLabel.color = delta >= 0 ? Candle : new Color32(0xB0, 0x47, 0x3A, 0xFF);
                moneyUntil = Time.time + hudShowSeconds;
            }
            lastMoney = money;
            moneyTotal.text = money.ToString("N0") + " <size=70%><color=#A8977A>บาท</color></size>";
        }

        // ชื่อเสียง: คะแนน/คะแนนที่ต้องได้เพื่อขึ้นระดับถัดไป + แถบ
        UpdateReputation();

        // การ์ดวันใหม่: โผล่เร็ว ค่อยๆ จางออกช้าๆ
        if (dayGroup != null)
        {
            bool showDay = Time.time < dayUntil;
            dayGroup.alpha = Mathf.MoveTowards(dayGroup.alpha, showDay ? 1f : 0f, Time.deltaTime * (showDay ? 3f : 1f / Mathf.Max(0.1f, dayCardFadeSeconds)));
        }
        Fade(moneyGroup, Time.time < moneyUntil);
    }

    void UpdateReputation()
    {
        var rm = ReputationManager.Instance;
        string text;
        float fill;
        if (rm != null)
        {
            int score = rm.GetScore(), level = rm.GetLevel();
            int[] th = { 0, rm.thresholdLevel1, rm.thresholdLevel2, rm.thresholdLevel3 };
            if (level >= 3)
            {
                text = $"ชื่อเสียง  {score}  ·  ระดับ {level} (สูงสุด)";
                fill = 1f;
            }
            else
            {
                int lo = th[level], hi = th[level + 1];
                text = $"ชื่อเสียง  {score}/{hi}  ·  ระดับ {level}";
                fill = hi > lo ? Mathf.Clamp01((score - lo) / (float)(hi - lo)) : 0f;
            }
        }
        else
        {
            text = repSrc != null ? repSrc.text : "ชื่อเสียง  0  ·  ระดับ 0";
            fill = 0f;
        }
        if (text != lastRep) { lastRep = text; repLabel.text = text; }
        if (repFill != null) repFill.fillAmount = fill;
    }

    Image MakeRepBar(Vector2 pos)
    {
        var bgGo = new GameObject("HUD_RepBar", typeof(RectTransform));
        bgGo.transform.SetParent(trackerRoot, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(Cream.r, Cream.g, Cream.b, 0.18f);
        bg.raycastTarget = false;
        var rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(180f, 4f);
        rt.anchoredPosition = pos;

        var fGo = new GameObject("Fill", typeof(RectTransform));
        fGo.transform.SetParent(bgGo.transform, false);
        var f = fGo.AddComponent<Image>();
        f.sprite = WhiteSprite();
        f.type = Image.Type.Filled;
        f.fillMethod = Image.FillMethod.Horizontal;
        f.color = Candle;
        f.raycastTarget = false;
        var frt = f.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        return f;
    }

    static Sprite whiteSprite;
    static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }


    static void Fade(CanvasGroup g, bool show)
    {
        if (g == null) return;
        g.alpha = Mathf.MoveTowards(g.alpha, show ? 1f : 0f, Time.deltaTime * (show ? 4f : 1.2f));
    }

    // ══════════ กล่องภารกิจแบบรายการเช็ก ══════════
    RectTransform trackerRoot;
    string trackerSignature = "";

    void BuildTrackerCanvas()
    {
        var go = new GameObject("[HorrorTracker]", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 40;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        trackerRoot = go.GetComponent<RectTransform>();
    }

    TMP_Text AddText(Transform parent, string s, TMP_FontAsset font, float size, Color color, float x, float y, float w = 700f)
    {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = s;
        Style(t, NewTextFont(font), size, color, 0.22f);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, size * 1.6f);
        rt.anchoredPosition = new Vector2(x, y);
        return t;
    }

    void AddBar(Transform parent, Vector2 pos, Vector2 size, Color c, float rot = 0f, Vector2? pivot = null)
    {
        var go = new GameObject("B", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = pivot ?? new Vector2(0f, 1f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0, 0, rot);
        var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.7f); sh.effectDistance = new Vector2(1.5f, -1.5f);
    }

    void AddCheckBox(Transform parent, float x, float y, bool done)
    {
        const float s = 21f, w = 2f;
        AddBar(parent, new Vector2(x, y), new Vector2(s, w), Cream);
        AddBar(parent, new Vector2(x, y - s + w), new Vector2(s, w), Cream);
        AddBar(parent, new Vector2(x, y), new Vector2(w, s), Cream);
        AddBar(parent, new Vector2(x + s - w, y), new Vector2(w, s), Cream);
        if (done)
        {
            // เครื่องหมายถูกสีแดง 2 ขีด
            AddBar(parent, new Vector2(x + 3f, y - 11f), new Vector2(11f, 4f), CheckRed, -45f, new Vector2(0f, 0.5f));
            AddBar(parent, new Vector2(x + 10f, y - 19f), new Vector2(30f, 4f), CheckRed, 58f, new Vector2(0f, 0.5f));
        }
    }

    static string StepDesc(MinigameType m, string npc)
    {
        switch (m)
        {
            case MinigameType.RhythmChantWASD: return "(ท่องคาถาให้ตรงจังหวะ)";
            case MinigameType.TalismanDrawing:
            case MinigameType.DeadByDaylightQTE: return "(ลงอักขระยันต์ให้ครบ)";
            case MinigameType.TimingBarQTE: return "(โปรยให้ตรงจังหวะเปลวเทียน)";
            case MinigameType.SequentialSlashQTE: return $"(ตวัดดาบตามรอยชาดใส่หลัง{npc})";
            default: return "";
        }
    }

    static string StepName(MinigameType m)
    {
        switch (m)
        {
            case MinigameType.SequentialSlashQTE: return "ตวัดดาบฟันอาคม";
            case MinigameType.TimingBarQTE: return "โปรยข้าวสาร";
            default: return QuestData.GetMinigameNameThai(m);
        }
    }

    void UpdateTracker()
    {
        if (trackerRoot == null || q == null) return;

        // ซ่อนกล่องภารกิจแบบเก่า
        if (q.questTrackerPanel != null)
        {
            var g = GetOrAdd<CanvasGroup>(q.questTrackerPanel);
            g.alpha = 0f; g.blocksRaycasts = false;
        }

        var sb = new StringBuilder();
        foreach (var quest in q.activeQuests)
        {
            if (quest == null) continue;
            sb.Append(quest.questId).Append(quest.npcName).Append(quest.completedMinigameSequence != null ? quest.completedMinigameSequence.Count : 0)
              .Append(quest.isTaskCompleted).Append(quest.requiredItemName).Append('|');
        }
        string sig = sb.ToString();
        if (sig == trackerSignature) return;
        trackerSignature = sig;

        // ลบเฉพาะของกล่องภารกิจ (ไม่ลบป้ายวันที่/เงิน/ชื่อเสียงที่อยู่ใน Canvas เดียวกัน)
        for (int i = trackerRoot.childCount - 1; i >= 0; i--)
        {
            var child = trackerRoot.GetChild(i).gameObject;
            if (!child.name.StartsWith("HUD_")) Destroy(child);
        }

        float y = -26f;
        foreach (var quest in q.activeQuests)
        {
            if (quest == null) continue;
            string npc = quest.npcName;
            string problem = !string.IsNullOrEmpty(quest.problemType) ? quest.problemType : quest.questTitle;
            // ระยะบรรทัดตั้งให้เข้ากับ K2D (สระบน/ล่างของไทยสูง)
            AddText(trackerRoot, "วัตถุประสงค์หลัก:", titleFont, trackerTitleSize * 0.85f, Muted, 40f, y); y -= trackerTitleSize * 1.45f;
            AddText(trackerRoot, $"ช่วย{npc} '{problem}'", titleFont, trackerTitleSize, Cream, 40f, y); y -= trackerTitleSize * 2.0f;

            var steps = new List<(string name, string desc, bool done)>();
            if (!string.IsNullOrEmpty(quest.requiredItemName))
            {
                int have = q.GetPlayerItemCount(quest.requiredItemName);
                steps.Add(($"หา{quest.requiredItemName}", $"(มีแล้ว {have}/{quest.requiredQuantity})", have >= quest.requiredQuantity));
            }
            else if (quest.requiredMinigameSequence != null)
            {
                int done = quest.completedMinigameSequence != null ? quest.completedMinigameSequence.Count : 0;
                for (int i = 0; i < quest.requiredMinigameSequence.Count; i++)
                {
                    var m = quest.requiredMinigameSequence[i];
                    steps.Add((StepName(m), StepDesc(m, npc), i < done));
                }
            }
            steps.Add(($"กลับไปบอก{npc}", $"(ส่งงานเพื่อรับค่าตอบแทน {quest.rewardMoney:N0} บาท)", false));

            foreach (var s in steps)
            {
                AddCheckBox(trackerRoot, 44f, y - 8f, s.done);
                AddText(trackerRoot, s.name, titleFont, trackerItemSize, s.done ? Muted : Cream, 76f, y);
                if (!string.IsNullOrEmpty(s.desc)) AddText(trackerRoot, s.desc, bodyFont, trackerDescSize, Muted, 76f, y - trackerItemSize * 1.45f);
                y -= trackerItemSize * 2.6f;
            }
            y -= 18f;
        }
    }

    // ══════════ ซับไตเติลพิมพ์ทีละตัว ══════════
    [Header("ซับไตเติล")]
    [Tooltip("ความเร็วพิมพ์บทพูด (ตัวอักษรต่อวินาที) — 0 = ขึ้นทั้งประโยคทันที")]
    public float typeCharsPerSecond = 32f;

    readonly Dictionary<TMP_Text, string> typedText = new Dictionary<TMP_Text, string>();
    readonly Dictionary<TMP_Text, float> typedCount = new Dictionary<TMP_Text, float>();

    void Typewriter(TMP_Text t, int startVisible)
    {
        if (typeCharsPerSecond <= 0f) { t.maxVisibleCharacters = 99999; return; }

        string last;
        if (!typedText.TryGetValue(t, out last) || last != t.text)
        {
            // ข้อความใหม่ (หน้าใหม่) → เริ่มพิมพ์ใหม่ โดยชื่อ NPC ขึ้นทันที
            typedText[t] = t.text;
            typedCount[t] = startVisible;
        }

        t.ForceMeshUpdate();
        int total = t.textInfo.characterCount;
        float n = typedCount[t];
        if (n < total)
        {
            n += typeCharsPerSecond * Time.unscaledDeltaTime;
            typedCount[t] = n;
            t.maxVisibleCharacters = Mathf.Min((int)n, total);
        }
        else
        {
            t.maxVisibleCharacters = 99999;
        }
    }

    // ══════════ ป้ายลอยเหนือของที่เล็งอยู่ ══════════
    // บรรทัดเล็กบนสุด = ชื่อพิธี / ชื่อของ / เคสไหน ขั้นที่เท่าไร / [E] หรือ "ไม่ได้อยู่ในขั้นตอน"
    [Header("ป้ายเหนือของที่เล็ง")]
    [Tooltip("ปิด = ใช้ข้อความ [E] ล่างจอแบบเดิม")]
    public bool showItemLabel = true;
    [Tooltip("ระยะเหนือขอบบนของวัตถุ (เมตร)")]
    public float itemLabelLift = 0.08f;

    TMP_Text itemLabel;
    Image itemStem;
    CanvasGroup itemGroup;
    InteractableItem labelItem;
    Vector3 labelLocalTop;
    string labelSig;

    static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

    bool UpdateItemLabel()
    {
        if (!showItemLabel || trackerRoot == null || ui == null) return false;
        if (itemLabel == null)
        {
            var go = new GameObject("HUD_ItemLabel", typeof(RectTransform));
            go.transform.SetParent(trackerRoot, false);
            itemGroup = go.AddComponent<CanvasGroup>();
            itemGroup.alpha = 0f; itemGroup.blocksRaycasts = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(900f, 10f);

            var stemGo = new GameObject("Stem", typeof(RectTransform));
            stemGo.transform.SetParent(go.transform, false);
            itemStem = stemGo.AddComponent<Image>();
            itemStem.color = new Color(Cream.r, Cream.g, Cream.b, 0.55f);
            itemStem.raycastTarget = false;
            var srt = itemStem.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(1.5f, 34f);
            srt.anchoredPosition = Vector2.zero;

            var tGo = new GameObject("Text", typeof(RectTransform));
            tGo.transform.SetParent(go.transform, false);
            itemLabel = tGo.AddComponent<TextMeshProUGUI>();
            Style(itemLabel, NewTextFont(titleFont), 34f, Cream, 0.25f);
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
            itemLabel.alignment = TextAlignmentOptions.Bottom;
            itemLabel.raycastTarget = false;
            itemLabel.lineSpacing = -10f;
            var trt = itemLabel.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
            trt.pivot = new Vector2(0.5f, 0f);
            trt.sizeDelta = new Vector2(900f, 220f);
            trt.anchoredPosition = new Vector2(0f, 40f);
        }

        var item = ui.CurrentPromptItem;
        var cam = Camera.main;
        // เมาส์ปลดล็อก = มีหน้าต่างเปิดอยู่ (ร้านค้า ศาลกุมาร ฯลฯ) → ไม่ต้องโชว์ป้าย
        bool want = item != null && cam != null && ui.promptPanel != null && ui.promptPanel.activeInHierarchy
                    && Cursor.lockState == CursorLockMode.Locked
                    && (q == null || q.questDialogPanel == null || !q.questDialogPanel.activeInHierarchy);

        if (want)
        {
            // จุดบนสุดของวัตถุ (คำนวณครั้งเดียวต่อชิ้น เก็บเป็นตำแหน่งในพิกัดของวัตถุ)
            if (item != labelItem)
            {
                labelItem = item;
                var rends = item.GetComponentsInChildren<Renderer>();
                Vector3 top = item.transform.position;
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    top = new Vector3(b.center.x, b.max.y, b.center.z);
                }
                labelLocalTop = item.transform.InverseTransformPoint(top);
            }
            Vector3 world = item.transform.TransformPoint(labelLocalTop) + Vector3.up * itemLabelLift;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) want = false;
            else
            {
                sp.x = Mathf.Clamp(sp.x, Screen.width * 0.15f, Screen.width * 0.85f);
                sp.y = Mathf.Clamp(sp.y, Screen.height * 0.10f, Screen.height * 0.62f); // ไม่ชนแถบวันที่ด้านบน
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(trackerRoot, sp, null, out local);
                ((RectTransform)itemGroup.transform).anchoredPosition = local;

                string text = BuildItemLabel(item);
                if (text != labelSig) { labelSig = text; itemLabel.text = text; }
            }
        }

        itemGroup.alpha = Mathf.MoveTowards(itemGroup.alpha, want ? 1f : 0f, Time.deltaTime * 8f);
        return want;
    }

    string BuildItemLabel(InteractableItem item)
    {
        string muted = Hex(Muted), candle = Hex(Candle);
        string key = !string.IsNullOrEmpty(item.interactionKeyText) ? item.interactionKeyText.ToUpper() : "E";
        string action = !string.IsNullOrEmpty(item.customReadPromptText) ? item.customReadPromptText : "อ่านข้อมูล";
        string name = CleanPrompt(item.itemName);

        MinigameType mg = MinigameType.None;
        if (item is QTEInteractable) mg = MinigameType.TalismanDrawing;
        else if (item is RhythmInteractable) mg = MinigameType.RhythmChantWASD;
        else if (item is SlashInteractable) mg = MinigameType.SequentialSlashQTE;
        else if (item is TimingBarInteractable) mg = MinigameType.TimingBarQTE;

        var sb = new StringBuilder();
        if (mg != MinigameType.None)
        {
            QuestData req = null;
            if (q != null && q.activeQuests != null)
                foreach (var quest in q.activeQuests) if (quest != null && quest.IsMinigameRequired(mg)) { req = quest; break; }

            sb.Append($"<size=60%><color=#{muted}>พิธี{StepName(mg)}</color></size>\n");
            if (req != null)
            {
                int step = req.completedMinigameSequence.Count + 1, total = req.requiredMinigameSequence.Count;
                sb.Append($"<color=#{candle}>{name}</color>\n");
                sb.Append($"<size=65%>เคส{req.npcName}  ·  ขั้นตอนที่ {step}/{total}</size>\n");
                if (item.canRead) sb.Append($"<size=70%><color=#{candle}>[{key}]  {action}</color></size>");
            }
            else
            {
                sb.Append($"{name}\n");
                sb.Append($"<size=65%><color=#{muted}>ไม่ได้อยู่ในขั้นตอนพิธีของเคสนี้</color></size>");
            }
        }
        else
        {
            sb.Append($"{name}\n");
            var acts = new List<string>();
            if (item.canRead) acts.Add($"[{key}]  {action}");
            if (item.canCollect) acts.Add("[F]  เก็บไอเทม");
            if (acts.Count > 0) sb.Append($"<size=70%><color=#{candle}>{string.Join("    ", acts)}</color></size>");
        }
        return sb.ToString().TrimEnd('\n');
    }

    // ══════════ ทุกเฟรม: คงหน้าตาใหม่ไว้ (ระบบเดิมเปลี่ยนข้อความ/เปิดปิดของตลอด) ══════════
    void LateUpdate()
    {
        if (!useHorrorUI || !applied) return;
        UpdateHUD();
        UpdateTracker();
        bool labelShown = UpdateItemLabel();

        // ป้าย [E]: ล้างอีโมจิ/ลูกศร/สี (ฟอนต์ไม่มีตัวพวกนี้ → ขึ้นเป็นตัวประหลาด)
        if (ui != null && ui.promptActionText != null)
        {
            string raw = ui.promptActionText.text;
            if (raw != lastPromptRaw && raw != lastPromptClean)
            {
                lastPromptRaw = raw;
                lastPromptClean = CleanPrompt(raw);
                ui.promptActionText.text = lastPromptClean;
            }
            // ป้ายลอยเหนือของแสดงข้อมูลครบแล้ว → ซ่อนข้อความล่างจอ
            ui.promptActionText.alpha = labelShown ? 0f : 1f;
        }

        if (q == null) return;

        // กรอบปุ่มเก่า + ข้อความรางวัลเก่า ซ่อนตลอด
        foreach (var f in oldButtonFrames) if (f != null && f.activeSelf) f.SetActive(false);
        if (q.questRewardText != null && q.questRewardText.gameObject.activeSelf) q.questRewardText.gameObject.SetActive(false);

        GameObject panel = q.questDialogPanel;
        if (panel == null || !panel.activeInHierarchy) return;

        // ชื่อ NPC ขึ้นหน้าบทพูดแบบซับไตเติล + บทพูดค่อยๆ พิมพ์ออกมาทีละตัว
        string npcName = q.npcNameText != null ? q.npcNameText.text : "";
        foreach (var t in new TMP_Text[] { q.questDescriptionText, q.declineDialogueText })
        {
            if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
            string prefix = $"{npcName} :  ";
            if (!string.IsNullOrEmpty(npcName) && !t.text.StartsWith(prefix))
                t.text = prefix + t.text.Trim().Trim('"', '“', '”');
            Typewriter(t, string.IsNullOrEmpty(npcName) ? 0 : prefix.Length);
        }

        // ปุ่มรับงาน: "รับงาน · 600 บาท" (ใช้ป้ายตัวแรกในปุ่มที่ไม่ใช่ข้อความรางวัล)
        var quest = q.CurrentQuest;
        if (quest != null && q.acceptButton != null && q.acceptButton.gameObject.activeInHierarchy)
        {
            if (acceptLabel == null)
            {
                foreach (var t in q.acceptButton.GetComponentsInChildren<TMP_Text>(true))
                    if (t != q.questRewardText) { acceptLabel = t; break; }
            }
            string want = $"รับงาน  ·  {quest.rewardMoney:N0} บาท";
            if (acceptLabel != null && acceptLabel.text != want)
            {
                acceptLabel.gameObject.SetActive(true);
                acceptLabel.alpha = 1f;
                acceptLabel.text = want;
            }
        }

        // ปุ่ม "คลิกเพื่อไปต่อ" → ตัวหนังสือเล็กมุมขวาล่าง ไม่มีกรอบ
        var cont = panel.transform.Find("ContinueButton");
        if (cont != null)
        {
            foreach (var img in cont.GetComponentsInChildren<Image>(true)) img.enabled = false;
            var hv = cont.GetComponent<HorrorButtonHover>(); if (hv != null) hv.enabled = false;
            var rt = cont as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-48f, 40f); rt.sizeDelta = new Vector2(420f, 50f);
            foreach (var t in cont.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.fontSize != 24f)
                {
                    var trt = t.rectTransform;
                    trt.SetParent(cont, false);
                    Style(t, bodyFont, 24f, Muted, 0.2f);
                    trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                }
                t.color = Muted;
                t.alignment = TextAlignmentOptions.BottomRight;
            }
        }
    }
}

/// <summary>ปุ่มแบบเรียบ: ชี้แล้วกรอบ+ตัวหนังสือเป็นสีเทียน / กดไม่ได้ = จาง</summary>
public class HorrorButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button button; Image[] borders; TMP_Text[] texts; bool hovered;
    static readonly Color Cream = Color.white;
    static readonly Color Dim = new Color32(0x7A, 0x70, 0x62, 0xFF);
    static readonly Color Candle = new Color32(0xFF, 0xB3, 0x5C, 0xFF);

    public void Init(Button b, Image[] edges) { button = b; borders = edges; texts = b.GetComponentsInChildren<TMP_Text>(true); }
    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; }
    void OnDisable() { hovered = false; }

    void LateUpdate()
    {
        if (button == null) return;
        bool on = button.interactable;
        Color c = !on ? Dim : (hovered ? Candle : Cream);
        Color edge = !on ? new Color(Dim.r, Dim.g, Dim.b, 0.5f) : (hovered ? Candle : new Color(Cream.r, Cream.g, Cream.b, 0.6f));
        if (borders != null) foreach (var e in borders) if (e != null) e.color = edge;
        if (texts != null) foreach (var t in texts) if (t != null && t.alpha > 0.01f) t.color = new Color(c.r, c.g, c.b, t.color.a);
    }
}
