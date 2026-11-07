using UnityEngine;

/// <summary>
/// ไอเทมเครื่องรางสำหรับกดเก็บเข้าตัว (Amulet Pickup Item)
/// วางไว้ในฉากเพื่อให้ผู้เล่นเดินไปกด [F] เพื่อเก็บเข้ากระเป๋า
/// แล้วนำไปทำพิธีฝังที่หลุมพิธีกรรม
/// </summary>
public class AmuletPickup : InteractableItem
{
    protected override void Awake()
    {
        base.Awake();

        if (string.IsNullOrEmpty(itemName) || itemName == "ไอเทม")
        {
            itemName = "เครื่องราง";
        }

        canCollect = true;
        canRead = false;
        destroyOnCollect = true;
        isImportantItem = true;
        glowColor = new Color(0.9f, 0.4f, 0.9f, 1f); // แสงเรืองสีม่วงมนตรา

        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, 0.3f, 0.5f);
        }
    }
}
