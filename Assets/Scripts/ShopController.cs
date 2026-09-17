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

    public ShopItemData(string name, int itemPrice, string desc = "")
    {
        itemName = name;
        price = itemPrice;
        description = desc;
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

    [Header("สินค้าในร้าน (Shop Items - 6 รายการ)")]
    public List<ShopItemData> shopItems = new List<ShopItemData>()
    {
        new ShopItemData("ไอเทม 1", 50, "ไอเทมระดับเริ่มต้นราคาประหยัด"),
        new ShopItemData("ไอเทม 2", 150, "ไอเทมระดับพื้นฐานสำหรับพิธีกรรม"),
        new ShopItemData("ไอเทม 3", 350, "ไอเทมระดับกลาง เพิ่มประสิทธิภาพ"),
        new ShopItemData("ไอเทม 4", 700, "ไอเทมระดับสูง สำหรับพิธีใหญ่"),
        new ShopItemData("ไอเทม 5", 1500, "ไอเทมระดับพรีเมียม บารมีสูง"),
        new ShopItemData("ไอเทม 6", 3000, "ไอเทมระดับตำนาน ทรงพลังที่สุด")
    };

    [Header("Runtime State")]
    public bool isShopOpen = false;

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

        // กด Esc หรือ E หรือ Tab เพื่อปิดร้านค้า
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab))
        {
            CloseShop();
            return;
        }

        // คีย์ลัดกดเลข 1-6 เพื่อซื้อสินค้า
        for (int i = 0; i < 6 && i < shopItems.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
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
                null,
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
            walletText.text = $"💰 เงินของคุณ: <color=#FFD700>{curMoney:N0} บาท</color>";
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
                itemBuyButtonTexts[i].text = canAfford ? $"ซื้อ  [กด {i + 1}]" : "เงินไม่พอ";
                itemBuyButtonTexts[i].color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f);
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

            RectTransform panelRect = shopPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860, 600);

            // BG Card Panel
            Image bg = shopPanel.AddComponent<Image>();
            bg.sprite = boxSprite;
            bg.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            // Close Button (Top Right X)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(shopPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-25, -25);
            closeRect.sizeDelta = new Vector2(36, 36);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = boxSprite;
            closeImg.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseShop);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI closeTMP = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 20;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.rectTransform.anchoredPosition = Vector2.zero;
            closeTMP.rectTransform.sizeDelta = new Vector2(36, 36);

            // Header Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "<color=#FFD700>🏪 ร้านค้าวัตถุมงคล (SHOP)</color>";
            titleTMP.fontSize = 28;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.rectTransform.anchoredPosition = new Vector2(0, 250);
            titleTMP.rectTransform.sizeDelta = new Vector2(800, 50);

            // Wallet Display
            GameObject walletObj = new GameObject("WalletText");
            walletObj.transform.SetParent(shopPanel.transform, false);
            walletText = walletObj.AddComponent<TextMeshProUGUI>();
            walletText.fontSize = 22;
            walletText.alignment = TextAlignmentOptions.Center;
            walletText.rectTransform.anchoredPosition = new Vector2(0, 205);
            walletText.rectTransform.sizeDelta = new Vector2(800, 40);

            // Item Cards Grid (2 Columns x 3 Rows)
            itemBuyButtons.Clear();
            itemBuyButtonTexts.Clear();

            float cardW = 380f;
            float cardH = 115f;
            float startX = -200f;
            float startY = 110f;
            float spacingX = 400f;
            float spacingY = 130f;

            for (int i = 0; i < 6 && i < shopItems.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int itemIdx = i;
                ShopItemData item = shopItems[i];

                GameObject cardObj = new GameObject($"ItemCard_{i}");
                cardObj.transform.SetParent(shopPanel.transform, false);
                RectTransform cardRect = cardObj.AddComponent<RectTransform>();
                cardRect.anchoredPosition = new Vector2(startX + col * spacingX, startY - row * spacingY);
                cardRect.sizeDelta = new Vector2(cardW, cardH);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = boxSprite;
                cardBg.color = new Color(0.14f, 0.17f, 0.23f, 0.9f);

                // Name & Price Text
                GameObject infoObj = new GameObject("InfoText");
                infoObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI infoTMP = infoObj.AddComponent<TextMeshProUGUI>();
                infoTMP.text = $"<b><color=#00E5FF>[{i + 1}] {item.itemName}</color></b>\nราคา: <color=#FFD700>{item.price:N0} บาท</color>";
                infoTMP.fontSize = 18;
                infoTMP.alignment = TextAlignmentOptions.Left;
                infoTMP.rectTransform.anchoredPosition = new Vector2(-60, 20);
                infoTMP.rectTransform.sizeDelta = new Vector2(230, 55);

                // Buy Button
                GameObject buyBtnObj = new GameObject("BuyButton");
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                RectTransform buyBtnRect = buyBtnObj.AddComponent<RectTransform>();
                buyBtnRect.anchoredPosition = new Vector2(0, -25);
                buyBtnRect.sizeDelta = new Vector2(340, 38);

                Image buyBtnImg = buyBtnObj.AddComponent<Image>();
                buyBtnImg.sprite = boxSprite;
                buyBtnImg.color = new Color(0.18f, 0.55f, 0.35f, 1f);

                Button buyBtn = buyBtnObj.AddComponent<Button>();
                buyBtn.onClick.AddListener(() => BuyItem(itemIdx));
                itemBuyButtons.Add(buyBtn);

                GameObject buyTxtObj = new GameObject("Text");
                buyTxtObj.transform.SetParent(buyBtnObj.transform, false);
                TextMeshProUGUI buyTMP = buyTxtObj.AddComponent<TextMeshProUGUI>();
                buyTMP.text = $"ซื้อ  [กด {i + 1}]";
                buyTMP.fontSize = 18;
                buyTMP.fontStyle = FontStyles.Bold;
                buyTMP.alignment = TextAlignmentOptions.Center;
                buyTMP.rectTransform.anchoredPosition = Vector2.zero;
                buyTMP.rectTransform.sizeDelta = new Vector2(340, 38);
                itemBuyButtonTexts.Add(buyTMP);
            }

            // Footer Guidance
            GameObject guideObj = new GameObject("GuideText");
            guideObj.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI guideTMP = guideObj.AddComponent<TextMeshProUGUI>();
            guideTMP.text = "กด <color=#00FF7F>[1 - 6]</color> หรือคลิกปุ่มเพื่อซื้อ  |  กด <color=#FF6347>[E / Esc]</color> เพื่อปิดร้านค้า";
            guideTMP.fontSize = 16;
            guideTMP.alignment = TextAlignmentOptions.Center;
            guideTMP.rectTransform.anchoredPosition = new Vector2(0, -265);
            guideTMP.rectTransform.sizeDelta = new Vector2(800, 30);
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
