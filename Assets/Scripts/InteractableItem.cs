using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "ไอเทม";
    public Sprite itemIcon;
    [TextArea(2, 4)]
    public string itemDescription = "รายละเอียดไอเทมเมื่ออยู่ในกระเป๋า...";

    [Header("ระบบอ่านข้อมูล (Press E)")]
    public bool canRead = true;
    public string customReadPromptText = ""; // ข้อความกำหนดเอง เช่น "เปิด/ปิดตำหนัก" หรือ "คุยรับเควส"
    public string readTitle = "ข้อมูลไอเทม";
    [TextArea(3, 8)]
    public string readDescription = "รายละเอียดหรือข้อความที่ต้องการให้อ่าน...";

    [Header("ระบบเก็บไอเทม (Press F)")]
    public bool canCollect = true;
    public bool destroyOnCollect = true;

    [Header("ระบบการใช้งานไอเทม (Inventory Use)")]
    public bool isUsable = true;
    public AudioClip useSound;
    public UnityEvent onUse = new UnityEvent();

    [Header("โมเดล 3D สำหรับถือและวาง (3D Model / Prefab)")]
    [Tooltip("Prefab 3D ของไอเทมนี้เมื่อนำออกมาถือที่มือ หรือวางลงพื้น (ถ้าเว้นว่างจะค้นหา Prefab ของตัวเอง)")]
    public GameObject worldPrefab;
    public Vector3 holdOffset = new Vector3(0.3f, -0.25f, 0.5f);
    public Vector3 holdRotation = Vector3.zero;
    public Vector3 holdScale = Vector3.one;

    [Header("ระบบแท่นวาง/กลไกที่ต้องใช้ไอเทม (Item Socket / Key Lock)")]
    [Tooltip("ต้องถือไอเทมที่ระบุมาด้วยหรือไม่ เพื่อจะโต้ตอบกับวัตถุนี้")]
    public bool requireHeldItem = false;
    [Tooltip("ชื่อของไอเทมที่ต้องถือมาใช้ เช่น 'กุญแจทอง' หรือ 'นมบูด'")]
    public string requiredItemName = "";
    [Tooltip("เมื่อใช้สำเร็จ ให้ลบ/สูญเสียไอเทมที่ถืออยู่หรือไม่")]
    public bool consumeHeldItemOnUse = true;
    [Tooltip("วัตถุนี้เป็นแท่นสำหรับวางไอเทม (Item Socket) หรือไม่")]
    public bool isPlacementSocket = false;
    [Tooltip("ตำแหน่งที่จะนำไอเทมมาวางติดไว้บนแท่น (ถ้าเว้นว่างจะวางที่จุดศูนย์กลาง)")]
    public Transform socketAttachPoint;
    [Tooltip("เสียงเมื่อนำไอเทมมาใช้สำเร็จ")]
    public AudioClip useWithHeldItemSound;
    [Tooltip("Event เมื่อนำไอเทมที่ถือมาใช้สำเร็จ (เช่น เปิดประตู, ปลดล็อคกลไก)")]
    public UnityEvent onUsedWithHeldItem = new UnityEvent();

    [Header("เสียงประกอบ (Optional)")]
    public AudioClip readSound;
    public AudioClip collectSound;

    [Header("Events เพิ่มเติม")]
    public UnityEvent onRead = new UnityEvent();
    public UnityEvent onCollect = new UnityEvent();

    /// <summary>
    /// แปลงข้อมูลของไอเทมชิ้นนี้เป็นข้อมูลสำหรับใส่ในกระเป๋า (InventoryItem)
    /// </summary>
    public virtual InventoryItem GetInventoryData()
    {
        return new InventoryItem(itemName, itemIcon, itemDescription, isUsable, useSound, onUse, worldPrefab, holdOffset, holdRotation, holdScale);
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นนำไอเทมที่ถืออยู่ในมือมาใช้กับวัตถุนี้
    /// </summary>
    public virtual bool TryUseWithHeldItem(PlayerInteraction interactor, InventoryItem heldItem)
    {
        if (heldItem == null) return false;

        // หากวัตถุนี้ไม่ได้เป็นแท่นวาง (isPlacementSocket) และไม่ได้ต้องการไอเทมเฉพาะ (requireHeldItem) ให้คืนค่า false เพื่อให้การกด E ทำงานตามปกติ
        if (!isPlacementSocket && !requireHeldItem)
        {
            return false;
        }

        // ตรวจสอบว่าไอเทมตรงกับที่ต้องการหรือไม่
        bool isMatch = string.IsNullOrEmpty(requiredItemName) || heldItem.itemName.Equals(requiredItemName, System.StringComparison.OrdinalIgnoreCase);

        if (requireHeldItem && !isMatch)
        {
            interactor.ShowNotification($"ต้องใช้ [{requiredItemName}] เพื่อโต้ตอบกับสิ่งนี้!");
            return false;
        }

        if (isPlacementSocket || (requireHeldItem && isMatch))
        {
            if (useWithHeldItemSound != null)
            {
                AudioSource.PlayClipAtPoint(useWithHeldItemSound, transform.position);
            }

            // ถ้าเป็นแท่นวางไอเทม ให้นำโมเดล 3D มาวางที่แท่น
            if (isPlacementSocket && heldItem.worldPrefab != null)
            {
                Transform targetParent = socketAttachPoint != null ? socketAttachPoint : transform;
                GameObject placedObj = Instantiate(heldItem.worldPrefab, targetParent.position, targetParent.rotation);
                placedObj.transform.SetParent(targetParent);
                placedObj.SetActive(true);
            }

            onUsedWithHeldItem?.Invoke();
            interactor.ShowNotification($"ใช้ [{heldItem.itemName}] กับ [{itemName}] สำเร็จ!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นกดอ่าน (E)
    /// </summary>
    public virtual void OnRead(PlayerInteraction interactor)
    {
        if (!canRead) return;

        if (readSound != null)
        {
            AudioSource.PlayClipAtPoint(readSound, transform.position);
        }

        onRead?.Invoke();
    }

    /// <summary>
    /// ทำงานเมื่อผู้เล่นกดเก็บ (F)
    /// </summary>
    public virtual void OnCollect(PlayerInteraction interactor)
    {
        if (!canCollect) return;

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        onCollect?.Invoke();

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
