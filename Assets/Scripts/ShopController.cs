using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class ShopItemData
{
    public string itemName;
    public int price;
    public string description;
    [Tooltip("รูปสินค้าในโฟลเดอร์ Resources (ไม่ต้องใส่ .png) เช่น ShopIcons/Offering_RedWater")]
    public string iconResource;

    [System.NonSerialized] private Sprite cachedIcon;

    public ShopItemData(string name, int itemPrice, string desc = "", string icon = "")
    {
        itemName = name;
        price = itemPrice;
        description = desc;
        iconResource = icon;
    }

    /// <summary>โหลดรูปสินค้าจาก Resources (รูป PNG ธรรมดา ไม่ต้องตั้งเป็น Sprite)</summary>
    public Sprite GetIcon()
    {
        if (cachedIcon != null || string.IsNullOrEmpty(iconResource)) return cachedIcon;
        var tex = Resources.Load<Texture2D>(iconResource);
        if (tex != null) cachedIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return cachedIcon;
    }
}

/// <summary>
/// ตัวจัดการระบบร้านค้า (Shop System Manager)
/// - จัดการสินค้า 6 รายการ พร้อมราคาเรียงลำดับ
/// - ซื้อขายหักเงินผ่าน PlayerWalletManager และเพิ่มของเข้ากระเป๋าผู้เล่น
/// - มีระบบ UI สวยงาม ไม่ซ้อนทับ และสามารถใช้เมาส์คลิกซื้อได้อย่างอิสระ
/// </summary>
public class ShopController : MonoBehaviour
{
    public static ShopController Instance { get; private set; }

    [Header("สินค้าในร้าน (2 รายการ: ของถวายกุมาร / ของถวายศาลตายาย)")]
    [Tooltip("ชื่อต้องตรงกับ offeringItemName ของศาล: 'น้ำแดง' = ศาลกุมารทอง (KumanThongInteractable), 'ขนมไทย' = ศาลตายาย (ForestShrineInteractable)")]
    public List<ShopItemData> shopItems = new List<ShopItemData>()
    {
        new ShopItemData("น้ำแดง", 50, "ของถวายกุมารทอง", "ShopIcons/Offering_RedWater"),
        new ShopItemData("ขนมไทย", 150, "ห่อใบตอง ของถวายศาลตายาย", "ShopIcons/Offering_Khanom")
    };

    [Header("Runtime State")]
    public bool isShopOpen = false;
    public System.Action onShopClosed;

    private PlayerInteraction activePlayer;
    private PlayerController playerController;
    private AudioSource audioSource;

    // uGUI Canvas References
    private GameObject shopCanvas;
    private GameObject shopPanel;
    private TextMeshProUGUI walletText;
    private List<Button> itemBuyButtons = new List<Button>();
    private List<TextMeshProUGUI> itemBuyButtonTexts = new List<TextMeshProUGUI>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (!isShopOpen) return;

        // หาก UI Panel ถูกปิดโดยตรงจากปุ่ม CloseButton หรือ UI Event ให้สั่งเรียก CloseShop()
        if (shopPanel != null && !shopPanel.activeInHierarchy)
        {
            CloseShop();
            return;
        }

        // กด Esc หรือ E หรือ Tab เพื่อปิดร้านค้า
        if (InputCompat.Down(KeyCode.Escape) || InputCompat.Down(KeyCode.E) || InputCompat.Down(KeyCode.Tab))
        {
            CloseShop();
            return;
        }

        // คีย์ลัดกดเลข 1-6 เพื่อซื้อสินค้า
        for (int i = 0; i < 6 && i < shopItems.Count; i++)
        {
            if (InputCompat.Down(KeyCode.Alpha1 + i) || InputCompat.Down(KeyCode.Keypad1 + i))
            {
                BuyItem(i);
            }
        }
    }

    /// <summary>
    /// เปิดหน้าต่างร้านค้า
    /// </summary>
    public void OpenShop(PlayerInteraction player)
    {
        if (isShopOpen) return;

        activePlayer = player != null ? player : FindFirstObjectByType<PlayerInteraction>();
        isShopOpen = true;

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.HideReadingDialog();
        }

        EnsureUIExists();
        LockPlayerControls(true);

        if (shopPanel != null) shopPanel.SetActive(true);
        if (shopCanvas != null) shopCanvas.SetActive(true);

        RefreshUI();
        Debug.Log("[ShopController] 🛒 เปิดหน้าต่างร้านค้าแล้ว (ปลดล็อคเมาส์อิสระ)");
    }

    /// <summary>
    /// ปิดหน้าต่างร้านค้า
    /// </summary>
    public void CloseShop()
    {
        if (!isShopOpen) return;

        isShopOpen = false;

        if (shopPanel != null) shopPanel.SetActive(false);
        if (shopCanvas != null) shopCanvas.SetActive(false);

        LockPlayerControls(false);
        Debug.Log("[ShopController] 🚪 ปิดหน้าต่างร้านค้าเรียบร้อย");

        onShopClosed?.Invoke();
    }

    /// <summary>
    /// ซื้อสินค้าตาม Index (0 ถึง 5)
    /// </summary>
    public void BuyItem(int index)
    {
        if (!isShopOpen || index < 0 || index >= shopItems.Count) return;

        ShopItemData item = shopItems[index];

        // 1. ตรวจสอบกระเป๋าเงิน
        if (PlayerWalletManager.Instance != null)
        {
            if (PlayerWalletManager.Instance.money < item.price)
            {
                // เงินไม่พอ
                PlaySynthBeep(180f, 0.3f);
                if (InteractionUIManager.Instance != null)
                {
                    InteractionUIManager.Instance.ShowNotification($"<color=#FF4500>❌ เงินไม่พอ!</color> ต้องการ <color=#FFD700>{item.price:N0} บาท</color> (คุณมี {PlayerWalletManager.Instance.money:N0} บาท)", 2.5f);
                }
                return;
            }

            // 2. หักเงิน
            bool success = PlayerWalletManager.Instance.SpendMoney(item.price);
            if (!success) return;
        }

        // 3. เพิ่มไอเทมเข้ากระเป๋าผู้เล่น
        if (activePlayer != null)
        {
            InventoryItem newItem = new InventoryItem(
                item.itemName,
                item.GetIcon(), // รูปเดียวกับในร้าน โชว์ในกระเป๋าด้วย
                $"{item.description} (ราคาซื้อ: {item.price:N0} บาท)",
                true,
                null,
                null
            );
            activePlayer.AddItem(newItem);
        }

        // 4. เล่นเสียงและแจ้งเตือน
        PlaySynthBeep(880f, 0.25f);
        int remainMoney = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;
        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification($"<color=#00FF7F>🎉 ซื้อ [{item.itemName}] สำเร็จ!</color> (คงเหลือ: {remainMoney:N0} บาท)", 3.0f);
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        int curMoney = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;

        if (walletText != null)
        {
            walletText.text = $"เงินที่มี  <color=#FFB35C>{curMoney:N0} บาท</color>";
        }

        for (int i = 0; i < itemBuyButtons.Count && i < shopItems.Count; i++)
        {
            bool canAfford = curMoney >= shopItems[i].price;
            if (itemBuyButtons[i] != null)
            {
                itemBuyButtons[i].interactable = canAfford;
            }
            if (itemBuyButtonTexts[i] != null)
            {
                itemBuyButtonTexts[i].text = canAfford ? $"[{i + 1}]  ซื้อ" : "เงินไม่พอ";
                itemBuyButtonTexts[i].color = canAfford ? new Color32(0xFF, 0xB3, 0x5C, 0xFF) : new Color32(0x7A, 0x70, 0x62, 0xFF);
            }
        }
    }

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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void PlaySynthBeep(float freq, float duration)
    {
        if (audioSource == null) return;
        int sampleRate = 44100;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * 0.35f;
        }
        AudioClip clip = AudioClip.Create("ShopBeepSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }

    private static Sprite radialGlow;
    /// <summary>วงแสงจางจากกลางออกขอบ (วางหลังรูปสินค้า)</summary>
    private static Sprite RadialGlowSprite()
    {
        if (radialGlow != null) return radialGlow;
        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        radialGlow = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        return radialGlow;
    }

    private Sprite CreateRoundedRectSprite(int width = 64, int height = 64)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    // ==========================================
    // Dynamic Canvas UI Creation (uGUI Sharp Vector)
    // ==========================================
    private void EnsureUIExists()
    {
        if (shopPanel != null && shopCanvas != null) return;

        shopCanvas = GameObject.Find("Shop_Canvas");
        if (shopCanvas == null)
        {
            shopCanvas = new GameObject("Shop_Canvas");
            Canvas canvas = shopCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

            CanvasScaler scaler = shopCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            shopCanvas.AddComponent<GraphicRaycaster>();
        }

        shopPanel = shopCanvas.transform.Find("ShopPanel")?.gameObject;
        if (shopPanel == null)
        {
            Sprite boxSprite = CreateRoundedRectSprite(64, 64);

            shopPanel = new GameObject("ShopPanel");
            shopPanel.transform.SetParent(shopCanvas.transform, false);

            // สีเดียวกับ UI ใหม่ของเกม (ครีม / ส้มเทียน / เทาหม่น) ไม่มีอีโมจิ (ฟอนต์ไทยไม่มี)
            Color cream = new Color32(0xE8, 0xDC, 0xC4, 0xFF);
            Color muted = new Color32(0xA8, 0x97, 0x7A, 0xFF);
            Color candle = new Color32(0xFF, 0xB3, 0x5C, 0xFF);

            int count = Mathf.Min(shopItems.Count, 6);
            int rows = Mathf.Max(1, (count + 1) / 2);
            // การ์ดสูงขึ้นเพื่อใส่รูปสินค้าด้านบน
            float cardW = 340f, cardH = 470f, gapX = 36f, gapY = 24f;
            float panelW = 2 * cardW + gapX + 80f;
            float panelH = 200f + rows * cardH + (rows - 1) * gapY;

            RectTransform panelRect = shopPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(panelW, panelH);

            // พื้นดำโปร่ง + เส้นขอบบาง
            Image bg = shopPanel.AddComponent<Image>();
            bg.sprite = boxSprite;
            bg.color = new Color(0.04f, 0.03f, 0.02f, 0.88f);
            var bgOutline = shopPanel.AddComponent<Outline>();
            bgOutline.effectColor = new Color(cream.r, cream.g, cream.b, 0.25f);
            bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // ปุ่มปิด: ตัวหนังสือ "ปิด [E]" มุมขวาบน ไม่มีกล่องแดง
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(shopPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20, -16);
            closeRect.sizeDelta = new Vector2(110, 36);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0, 0, 0, 0); // โปร่งใส แต่ยังคลิกได้

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseShop);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI closeTMP = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "ปิด  [E]";
            closeTMP.fontSize = 20;
            closeTMP.color = muted;
            closeTMP.alignment = TextAlignmentOptions.Right;
            closeTMP.rectTransform.anchoredPosition = Vector2.zero;
            closeTMP.rectTransform.sizeDelta = new Vector2(110, 36);

            // หัวข้อ
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "ร้านค้าวัตถุมงคล";
            titleTMP.fontSize = 34;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = cream;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.rectTransform.anchoredPosition = new Vector2(0, panelH / 2f - 52f);
            titleTMP.rectTransform.sizeDelta = new Vector2(panelW - 200f, 50);

            // เงินที่มี
            GameObject walletObj = new GameObject("WalletText");
            walletObj.transform.SetParent(shopPanel.transform, false);
            walletText = walletObj.AddComponent<TextMeshProUGUI>();
            walletText.fontSize = 22;
            walletText.color = muted;
            walletText.alignment = TextAlignmentOptions.Center;
            walletText.rectTransform.anchoredPosition = new Vector2(0, panelH / 2f - 96f);
            walletText.rectTransform.sizeDelta = new Vector2(panelW - 80f, 36);

            // การ์ดสินค้า 2 คอลัมน์
            itemBuyButtons.Clear();
            itemBuyButtonTexts.Clear();

            float firstY = panelH / 2f - 130f - cardH / 2f;
            for (int i = 0; i < count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int itemIdx = i;
                ShopItemData item = shopItems[i];
                // แถวสุดท้ายมีชิ้นเดียว → วางกลาง
                bool alone = (i == count - 1) && (count % 2 == 1);
                float x = alone ? 0f : (col == 0 ? -(cardW + gapX) / 2f : (cardW + gapX) / 2f);

                GameObject cardObj = new GameObject($"ItemCard_{i}");
                cardObj.transform.SetParent(shopPanel.transform, false);
                RectTransform cardRect = cardObj.AddComponent<RectTransform>();
                cardRect.anchoredPosition = new Vector2(x, firstY - row * (cardH + gapY));
                cardRect.sizeDelta = new Vector2(cardW, cardH);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = boxSprite;
                cardBg.color = new Color(1f, 1f, 1f, 0.04f);
                var cardOutline = cardObj.AddComponent<Outline>();
                cardOutline.effectColor = new Color(cream.r, cream.g, cream.b, 0.15f);
                cardOutline.effectDistance = new Vector2(1f, -1f);

                // รูปสินค้า: แสงเทียนสลัวๆ ด้านหลัง + รูป (รักษาสัดส่วน)
                float iconY = cardH / 2f - 150f;
                GameObject glowObj = new GameObject("IconGlow");
                glowObj.transform.SetParent(cardObj.transform, false);
                Image glowImg = glowObj.AddComponent<Image>();
                glowImg.sprite = RadialGlowSprite();
                glowImg.color = new Color(candle.r, candle.g, candle.b, 0.22f);
                glowImg.raycastTarget = false;
                glowImg.rectTransform.anchoredPosition = new Vector2(0, iconY);
                glowImg.rectTransform.sizeDelta = new Vector2(280, 280);

                Sprite icon = item.GetIcon();
                if (icon != null)
                {
                    GameObject iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(cardObj.transform, false);
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.sprite = icon;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                    iconImg.rectTransform.anchoredPosition = new Vector2(0, iconY);
                    iconImg.rectTransform.sizeDelta = new Vector2(230, 230);
                    var shadow = iconObj.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, 0.6f);
                    shadow.effectDistance = new Vector2(4, -6);
                }

                // ชื่อ / ใช้ทำอะไร / ราคา (ใต้รูป)
                GameObject infoObj = new GameObject("InfoText");
                infoObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI infoTMP = infoObj.AddComponent<TextMeshProUGUI>();
                infoTMP.text = $"<size=30><b>{item.itemName}</b></size>\n" +
                               $"<color=#A8977A>{item.description}</color>\n" +
                               $"<size=26><color=#FFB35C>{item.price:N0} บาท</color></size>";
                infoTMP.fontSize = 20;
                infoTMP.color = cream;
                infoTMP.alignment = TextAlignmentOptions.Center;
                infoTMP.raycastTarget = false;
                infoTMP.rectTransform.anchoredPosition = new Vector2(0, -cardH / 2f + 140f);
                infoTMP.rectTransform.sizeDelta = new Vector2(cardW - 30f, 120);

                // ปุ่มซื้อ: พื้นดำโปร่ง ตัวหนังสือสีเทียน
                GameObject buyBtnObj = new GameObject("BuyButton");
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                RectTransform buyBtnRect = buyBtnObj.AddComponent<RectTransform>();
                buyBtnRect.anchoredPosition = new Vector2(0, -cardH / 2f + 40f);
                buyBtnRect.sizeDelta = new Vector2(cardW - 60f, 48);

                Image buyBtnImg = buyBtnObj.AddComponent<Image>();
                buyBtnImg.sprite = boxSprite;
                buyBtnImg.color = new Color(0f, 0f, 0f, 0.45f);
                var buyOutline = buyBtnObj.AddComponent<Outline>();
                buyOutline.effectColor = new Color(candle.r, candle.g, candle.b, 0.5f);
                buyOutline.effectDistance = new Vector2(1f, -1f);

                Button buyBtn = buyBtnObj.AddComponent<Button>();
                var colors = buyBtn.colors;
                colors.highlightedColor = new Color(1f, 0.85f, 0.6f, 1f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                buyBtn.colors = colors;
                buyBtn.onClick.AddListener(() => BuyItem(itemIdx));
                itemBuyButtons.Add(buyBtn);

                GameObject buyTxtObj = new GameObject("Text");
                buyTxtObj.transform.SetParent(buyBtnObj.transform, false);
                TextMeshProUGUI buyTMP = buyTxtObj.AddComponent<TextMeshProUGUI>();
                buyTMP.text = $"[{i + 1}]  ซื้อ";
                buyTMP.fontSize = 22;
                buyTMP.fontStyle = FontStyles.Bold;
                buyTMP.alignment = TextAlignmentOptions.Center;
                buyTMP.rectTransform.anchoredPosition = Vector2.zero;
                buyTMP.rectTransform.sizeDelta = new Vector2(cardW - 60f, 48);
                itemBuyButtonTexts.Add(buyTMP);
            }

            // คำแนะนำด้านล่าง
            GameObject guideObj = new GameObject("GuideText");
            guideObj.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI guideTMP = guideObj.AddComponent<TextMeshProUGUI>();
            guideTMP.text = count > 1
                ? $"กด [1 - {count}] หรือคลิกปุ่มเพื่อซื้อ    ·    [E / Esc] ปิดร้าน"
                : "กด [1] หรือคลิกปุ่มเพื่อซื้อ    ·    [E / Esc] ปิดร้าน";
            guideTMP.fontSize = 17;
            guideTMP.color = muted;
            guideTMP.alignment = TextAlignmentOptions.Center;
            guideTMP.rectTransform.anchoredPosition = new Vector2(0, -panelH / 2f + 30f);
            guideTMP.rectTransform.sizeDelta = new Vector2(panelW - 60f, 30);
        }
    }

    // ==========================================
    // OnGUI Fallback Rendering (เฉพาะกรณีไม่มี uGUI Canvas เท่านั้น)
    // ==========================================
    void OnGUI()
    {
        if (!isShopOpen) return;

        // หากมี uGUI Panel ทำงานอยู่แล้ว ให้ข้ามการวาดด้วย OnGUI เพื่อไม่ให้ UI ซ้อนทับกัน
        if (shopPanel != null && shopPanel.activeSelf) return;

        float w = 720f;
        float h = 500f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Box(new Rect(x + 5, y + 5, w - 10, h - 10), "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(x, y + 15, w, 32), "🏪 ร้านค้าวัตถุมงคล (SHOP)", titleStyle);

        int curMoney = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;
        GUIStyle walletStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter
        };
        walletStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y + 50, w, 28), $"💰 เงินของคุณ: {curMoney:N0} บาท", walletStyle);

        float itemW = 330f;
        float itemH = 105f;
        float startX = x + 20f;
        float startY = y + 90f;
        float spacingX = 20f;
        float spacingY = 15f;

        GUIStyle itemTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        itemTitleStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

        GUIStyle priceStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        priceStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

        for (int i = 0; i < 6 && i < shopItems.Count; i++)
        {
            int col = i % 2;
            int row = i / 2;
            float itemX = startX + col * (itemW + spacingX);
            float itemY = startY + row * (itemH + spacingY);

            ShopItemData item = shopItems[i];
            bool canAfford = curMoney >= item.price;

            GUI.Box(new Rect(itemX, itemY, itemW, itemH), "");

            GUI.Label(new Rect(itemX + 12, itemY + 8, itemW - 24, 25), $"[{i + 1}] {item.itemName}", itemTitleStyle);
            GUI.Label(new Rect(itemX + 12, itemY + 33, itemW - 24, 25), $"ราคา: {item.price:N0} บาท", priceStyle);

            string btnLabel = canAfford ? $"ซื้อ  [กด {i + 1}]" : "เงินไม่พอ";
            GUI.enabled = canAfford;
            if (GUI.Button(new Rect(itemX + 12, itemY + 62, itemW - 24, 32), btnLabel))
            {
                BuyItem(i);
            }
            GUI.enabled = true;
        }

        GUIStyle footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        footerStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x, y + h - 35, w, 25), "กด [1 - 6] หรือคลิกปุ่มเพื่อซื้อ  |  กด [E / Esc] เพื่อปิดร้านค้า", footerStyle);
    }
}
