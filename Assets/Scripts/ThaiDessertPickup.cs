using UnityEngine;

/// <summary>
/// ไอเทมขนมไทยสำหรับเก็บเข้าตัว (Thai Dessert Pickup Item)
/// วางโมเดลขนมไทยไว้ในฉาก แล้วติดสคริปต์นี้เพื่อกด [F] เก็บเข้ากระเป๋า
/// เพื่อนำไปถวายที่ศาลตายาย
/// </summary>
public class ThaiDessertPickup : InteractableItem
{
    protected override void Awake()
    {
        base.Awake();

        if (string.IsNullOrEmpty(itemName) || itemName == "ไอเทม")
        {
            itemName = "ขนมไทย";
        }

        canCollect = true;
        canRead = false;
        destroyOnCollect = true;
        isImportantItem = true;
        glowColor = new Color(1f, 0.75f, 0.2f, 1f); // แสงเรืองประกายทองอบอุ่น

        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, 0.4f, 0.5f);
        }
    }
}
