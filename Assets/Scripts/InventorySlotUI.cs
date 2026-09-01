using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    public Image itemIconImage;
    public TextMeshProUGUI itemNameText;
    public Button selectButton;
    public GameObject selectedHighlight;

    private int slotIndex = -1;
    private System.Action<int> onSelectCallback;
    private InventoryItem currentItem;

    public void Setup(InventoryItem item, int index, System.Action<int> onSelect, bool isSelected = false)
    {
        slotIndex = index;
        currentItem = item;
        onSelectCallback = onSelect;

        if (itemNameText != null)
        {
            itemNameText.text = item != null ? item.itemName : "ไม่มีชื่อ";
        }

        if (itemIconImage != null)
        {
            if (item != null && item.icon != null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.enabled = true;
                // ป้องกันกรณี Alpha ของสีเป็น 0
                Color c = itemIconImage.color;
                if (c.a == 0) c.a = 1f;
                itemIconImage.color = c;
            }
            else
            {
                if (item != null && item.icon == null)
                {
                    Debug.LogWarning($"[InventorySlotUI] ⚠️ ไอเทม '{item.itemName}' ไม่มีรูป Sprite (item.icon เป็น null)! ให้ตรวจดูช่อง Item Icon บน InteractableItem");
                }
                
                if (itemIconImage.sprite == null)
                {
                    itemIconImage.enabled = false;
                }
            }
        }
        else
        {
            // ลองหา Image จากลูกหรือตัวเอง
            itemIconImage = GetComponent<Image>();
        }

        SetSelected(isSelected);

        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
            if (selectButton == null) selectButton = GetComponentInChildren<Button>();
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClick);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(selected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }

    private void OnClick()
    {
        Debug.Log($"[InventorySlotUI] 🖱️ มีการคลิกเลือกช่องไอเทม: '{(currentItem != null ? currentItem.itemName : "Index " + slotIndex)}'");
        onSelectCallback?.Invoke(slotIndex);
    }
}
