using UnityEngine;

/// <summary>
/// วัตถุร้านค้าในฉาก (สืบทอดมาจาก InteractableItem)
/// เมื่อผู้เล่นเดินเข้าใกล้แล้วกด [E] จะเปิดหน้าต่างร้านค้าซื้อขายไอเทมทันที
/// - เปิดให้บริการเฉพาะวันเลขคู่ (วันที่ 2, 4, 6, 8...) เท่านั้น
/// - หากเป็นวันเลขคี่ (วันที่ 1, 3, 5, 7...) ร้านค้าจะถูกซ่อนและไม่สามารถโต้ตอบได้
/// </summary>
public class ShopInteractable : InteractableItem
{
    [Header("Shop Day Settings")]
    [Tooltip("โมเดล/Visual ของร้านค้า หากใส่ไว้จะสั่ง SetActive(true/false) ตามวันคี่/คู่ให้อัตโนมัติ")]
    public GameObject shopVisualModel;

    private bool? lastVisibilityState = null;

    private void Awake()
    {
        readDescription = "";
        readTitle = "";
    }

    private void Start()
    {
        UpdateShopVisibility();
    }

    private void Update()
    {
        UpdateShopVisibility();
    }

    private void Reset()
    {
        itemName = "ร้านค้าวัตถุมงคล";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "เปิดร้านค้า";
        readDescription = "";
        readTitle = "";
    }

    /// <summary>
    /// ตรวจสอบและอัปเดตความมองเห็นของร้านค้าตามวันปัจจุบัน (เปิดเฉพาะวันเลขคู่ 2, 4, 6...)
    /// </summary>
    public void UpdateShopVisibility()
    {
        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        bool isEvenDay = (currentDay % 2 == 0);

        if (!lastVisibilityState.HasValue || lastVisibilityState.Value != isEvenDay)
        {
            lastVisibilityState = isEvenDay;

            // 1. สลับสถานะโต้ตอบได้/ไม่ได้
            canRead = isEvenDay;

            // 2. หากมี shopVisualModel ให้สั่งเปิด/ปิดทั้ง GameObject
            if (shopVisualModel != null)
            {
                shopVisualModel.SetActive(isEvenDay);
            }
            else
            {
                // 3. หากไม่ได้ระบุ shopVisualModel ให้เปิด/ปิด Renderer และ Collider บนวัตถุนี้และลูกๆ
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.enabled = isEvenDay;
                }

                Collider[] colliders = GetComponentsInChildren<Collider>(true);
                foreach (var c in colliders)
                {
                    c.enabled = isEvenDay;
                }
            }

            // หากร้านค้าปิดกะทันหันในวันเลขคี่ ให้สั่งปิด UI หน้าต่างร้านค้าด้วย
            if (!isEvenDay && ShopController.Instance != null && ShopController.Instance.isShopOpen)
            {
                ShopController.Instance.CloseShop();
            }
        }
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        // ป้องกันกรณีแอบกดโต้ตอบในวันเลขคี่
        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        if (currentDay % 2 != 0)
        {
            if (InteractionUIManager.Instance != null)
            {
                InteractionUIManager.Instance.ShowNotification("<color=#FF5555>🔒 ร้านค้าเปิดเฉพาะวันเลขคู่เท่านั้น! (วันที่ 2, 4, 6, 8...)</color>", 3.0f);
            }
            return;
        }

        ShopController shop = ShopController.Instance;
        if (shop == null)
        {
            shop = FindFirstObjectByType<ShopController>();
            if (shop == null)
            {
                GameObject shopObj = new GameObject("ShopController");
                shop = shopObj.AddComponent<ShopController>();
            }
        }

        shop.OpenShop(interactor);
    }
}
