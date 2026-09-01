using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class InventoryItem
{
    [Header("ข้อมูลไอเทม")]
    public string itemName = "ไอเทม";
    public Sprite icon;
    [TextArea(2, 5)]
    public string description = "คำอธิบายไอเทม...";

    [Header("การใช้งาน")]
    public bool isUsable = true;
    public AudioClip useSound;
    
    [Header("เหตุการณ์เมื่อกดใช้ไอเทม")]
    public UnityEvent onUse;

    [Header("3D Model สำหรับถือและวางในโลก")]
    public GameObject worldPrefab;
    public Vector3 holdOffset = new Vector3(0.3f, -0.25f, 0.5f);
    public Vector3 holdRotation = Vector3.zero;
    public Vector3 holdScale = Vector3.one;

    public InventoryItem() { }

    public InventoryItem(string name, Sprite icon, string desc, bool usable, AudioClip sound, UnityEvent useEvent, GameObject prefab = null, Vector3? offset = null, Vector3? rotation = null, Vector3? scale = null)
    {
        this.itemName = name;
        this.icon = icon;
        this.description = desc;
        this.isUsable = usable;
        this.useSound = sound;
        this.onUse = useEvent;
        this.worldPrefab = prefab;
        if (offset.HasValue) this.holdOffset = offset.Value;
        if (rotation.HasValue) this.holdRotation = rotation.Value;
        if (scale.HasValue) this.holdScale = scale.Value;
    }
}
