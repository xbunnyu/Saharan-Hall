using TMPro;
using UnityEngine;

/// <summary>
/// วางบน TextMeshProUGUI ที่ต้องการแสดงเงิน
/// จะอัพเดทอัตโนมัติเมื่อ PlayerWalletManager เปลี่ยนค่า
/// </summary>
public class MoneyDisplay : MonoBehaviour
{
    private TextMeshProUGUI label;

    void Start()
    {
        label = GetComponent<TextMeshProUGUI>();

        if (PlayerWalletManager.Instance != null)
        {
            // แสดงค่าเริ่มต้น
            UpdateUI(PlayerWalletManager.Instance.money);
            // Subscribe Event — อัปเดทอัตโนมัติเมื่อเงินเปลี่ยน
            PlayerWalletManager.Instance.onMoneyChanged.AddListener(UpdateUI);
        }
        else
        {
            label.text = "฿ 0 บาท";
            Debug.LogWarning("[MoneyDisplay] ไม่พบ PlayerWalletManager ในฉาก");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe เมื่อ UI ถูกทำลาย
        if (PlayerWalletManager.Instance != null)
        {
            PlayerWalletManager.Instance.onMoneyChanged.RemoveListener(UpdateUI);
        }
    }

    private void UpdateUI(int amount)
    {
        label.text = $"฿ {amount:N0} บาท";
    }
}
