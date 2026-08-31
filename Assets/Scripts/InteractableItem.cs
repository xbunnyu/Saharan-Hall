using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "ไอเทม";

    [Header("ระบบอ่านข้อมูล (Press E)")]
    public bool canRead = true;
    public string readTitle = "ข้อมูลไอเทม";
    [TextArea(3, 8)]
    public string readDescription = "รายละเอียดหรือข้อความที่ต้องการให้อ่าน...";

    [Header("ระบบเก็บไอเทม (Press F)")]
    public bool canCollect = true;
    public bool destroyOnCollect = true;

    [Header("เสียงประกอบ (Optional)")]
    public AudioClip readSound;
    public AudioClip collectSound;

    [Header("Events เพิ่มเติม")]
    public UnityEvent onRead;
    public UnityEvent onCollect;

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
