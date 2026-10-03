using TMPro;
using UnityEngine;

/// <summary>
/// วางบน TextMeshProUGUI ใน Canvas เพื่อแสดงค่าชื่อเสียง
/// ทำงานเหมือน MoneyDisplay — อัปเดตอัตโนมัติเมื่อชื่อเสียงเปลี่ยน
/// </summary>
public class ReputationDisplay : MonoBehaviour
{
    private TextMeshProUGUI label;

    void Start()
    {
        label = GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (ReputationManager.Instance != null)
        {
            UpdateUI(ReputationManager.Instance.GetScore(), ReputationManager.Instance.GetLevel());
            ReputationManager.Instance.onReputationChanged.AddListener(UpdateUI);
        }
        else
        {
            if (label != null) label.text = "ชื่อเสียง: 0 (ระดับ 0)";
        }
    }

    void OnDestroy()
    {
        if (ReputationManager.Instance != null)
        {
            ReputationManager.Instance.onReputationChanged.RemoveListener(UpdateUI);
        }
    }

    private void UpdateUI(int score, int level)
    {
        if (label != null)
        {
            string levelName = ReputationManager.Instance != null ? ReputationManager.Instance.GetLevelName() : "";
            label.text = $"ชื่อเสียง: {score} (ระดับ {level})";
        }
    }
}
