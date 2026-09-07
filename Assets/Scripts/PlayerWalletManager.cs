using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton จัดการเงิน (money) ของผู้เล่น
/// วางบน GameObject เดิมหรือ GameManager ในฉาก
/// BuddhistAltarManager และ NPCController ใช้ไฟล์นี้หักและให้รางวัลเงิน
/// </summary>
public class PlayerWalletManager : MonoBehaviour
{
    public static PlayerWalletManager Instance { get; private set; }

    [Header("กระเป๋าเงิน (Wallet)")]
    [Tooltip("จำนวนเงินเริ่มต้น")]
    public int startingMoney = 500;

    [HideInInspector]
    public int money;

    [Header("Events")]
    public UnityEvent<int> onMoneyChanged; // ส่งยอดเงินใหม่ออกไป

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        money = startingMoney;
    }

    /// <summary>เพิ่มเงิน (รับรางวัล)</summary>
    public void EarnMoney(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        onMoneyChanged?.Invoke(money);

        if (InteractionUIManager.Instance != null)
        {
            InteractionUIManager.Instance.ShowNotification(
                $"<color=#FFD700>+{amount:N0} บาท</color>  (รวม: {money:N0} บาท)", 2.5f);
        }

        Debug.Log($"[Wallet] +{amount} บาท → รวม {money} บาท");
    }

    /// <summary>หักเงิน — คืน false หากเงินไม่พอ</summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true;
        if (money < amount) return false;

        money -= amount;
        onMoneyChanged?.Invoke(money);
        Debug.Log($"[Wallet] -{amount} บาท → รวม {money} บาท");
        return true;
    }
}
