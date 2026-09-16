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
/// - มีระบบวาด UI อัตโนมัติ (Canvas UI + Fallback OnGUI)
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

    [Header("เสียงประกอบ")]
    public AudioClip buySuccessSound;
    public AudioClip buyFailSound;

    [Header("Runtime State")]
    public bool isShopOpen = false;

    private PlayerInteraction activePlayer;
    private PlayerController playerController;
    private AudioSource audioSource;

    // UI References
    private GameObject shopCanvas;
    private GameObject shopPanel;
    private TextMeshProUGUI walletText;
    private List<TextMeshProUGUI> itemPriceTexts = new List<TextMeshProUGUI>();

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

        LockPlayerControls(true);
        EnsureUIExists();

        if (shopPanel != null) shopPanel.SetActive(true);
        if (shopCanvas != null) shopCanvas.SetActive(true);

        RefreshUI();
        Debug.Log("[ShopController] 🛒 เปิดหน้าต่างร้านค้าแล้ว");
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
        if (index < 0 || index >= shopItems.Count) return;

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
        if (walletText != null && PlayerWalletManager.Instance != null)
        {
            walletText.text = $"💰 เงินของคุณ: <color=#FFD700>{PlayerWalletManager.Instance.money:N0} บาท</color>";
        }
    }

    private void LockPlayerControls(bool lockState)
    {
        if (playerController != null) playerController.enabled = !lockState;

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

    // ==========================================
    // Dynamic Canvas UI Creation
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
            shopPanel = new GameObject("ShopPanel");
            shopPanel.transform.SetParent(shopCanvas.transform, false);

            RectTransform panelRect = shopPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800, 560);

            // BG Card
            Image bg = shopPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "<color=#FFD700>🏪 ร้านค้าวัตถุมงคล (SHOP)</color>";
            titleTMP.fontSize = 26;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.rectTransform.anchoredPosition = new Vector2(0, 230);
            titleTMP.rectTransform.sizeDelta = new Vector2(760, 50);

            // Wallet Display
            GameObject walletObj = new GameObject("WalletText");
            walletObj.transform.SetParent(shopPanel.transform, false);
            walletText = walletObj.AddComponent<TextMeshProUGUI>();
            walletText.fontSize = 20;
            walletText.alignment = TextAlignmentOptions.Center;
            walletText.rectTransform.anchoredPosition = new Vector2(0, 185);
            walletText.rectTransform.sizeDelta = new Vector2(760, 40);

            // Close Guide
            GameObject guideObj = new GameObject("GuideText");
            guideObj.transform.SetParent(shopPanel.transform, false);
            TextMeshProUGUI guideTMP = guideObj.AddComponent<TextMeshProUGUI>();
            guideTMP.text = "กด <color=#00FF7F>[1 - 6]</color> หรือคลิกปุ่มเพื่อซื้อ  |  กด <color=#FF6347>[E / Esc]</color> เพื่อปิด";
            guideTMP.fontSize = 16;
            guideTMP.alignment = TextAlignmentOptions.Center;
            guideTMP.rectTransform.anchoredPosition = new Vector2(0, -240);
            guideTMP.rectTransform.sizeDelta = new Vector2(760, 30);
        }
    }

    // ==========================================
    // OnGUI Fallback Rendering (การันตีการวาด 100%)
    // ==========================================
    void OnGUI()
    {
        if (!isShopOpen) return;

        float w = 720f;
        float h = 500f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        // Background Window Box
        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Box(new Rect(x + 5, y + 5, w - 10, h - 10), "");

        // Header Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(x, y + 15, w, 32), "🏪 ร้านค้าวัตถุมงคล (SHOP)", titleStyle);

        // Wallet Balance
        int curMoney = PlayerWalletManager.Instance != null ? PlayerWalletManager.Instance.money : 0;
        GUIStyle walletStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter
        };
        walletStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y + 50, w, 28), $"💰 เงินของคุณ: {curMoney:N0} บาท", walletStyle);

        // Render 6 Items Grid (2 Columns x 3 Rows)
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

            // Item Name & Key
            GUI.Label(new Rect(itemX + 12, itemY + 8, itemW - 24, 25), $"[{i + 1}] {item.itemName}", itemTitleStyle);
            // Price
            GUI.Label(new Rect(itemX + 12, itemY + 33, itemW - 24, 25), $"ราคา: {item.price:N0} บาท", priceStyle);

            // Buy Button
            string btnLabel = canAfford ? $"ซื้อ  [กด {i + 1}]" : "เงินไม่พอ";
            GUI.enabled = canAfford;
            if (GUI.Button(new Rect(itemX + 12, itemY + 62, itemW - 24, 32), btnLabel))
            {
                BuyItem(i);
            }
            GUI.enabled = true;
        }

        // Footer Guidance
        GUIStyle footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        footerStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x, y + h - 35, w, 25), "กด [1 - 6] หรือคลิกปุ่มเพื่อซื้อ  |  กด [E / Esc] เพื่อปิดร้านค้า", footerStyle);
    }
}
