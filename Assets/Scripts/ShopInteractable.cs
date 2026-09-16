using UnityEngine;

/// <summary>
/// วัตถุร้านค้าในฉาก (สืบทอดมาจาก InteractableItem)
/// เมื่อผู้เล่นเดินเข้าใกล้แล้วกด [E] จะเปิดหน้าต่างร้านค้าซื้อขายไอเทมทันที
/// </summary>
public class ShopInteractable : InteractableItem
{
    private void Reset()
    {
        itemName = "ร้านค้าวัตถุมงคล";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "เปิดร้านค้า";
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        base.OnRead(interactor);

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
