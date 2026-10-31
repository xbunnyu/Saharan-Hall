using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบควบคุมจุดขุดดินในป่า (Dig Spot Manager)
/// ทำหน้าที่สุ่มเลือกว่ากองดินกองไหนจะมีของรางวัลซ่อนอยู่ โดยสุ่ม 1 กอง (หรือจำนวนตามที่กำหนด) จากกองดินทั้งหมด
/// </summary>
public class DigSpotManager : MonoBehaviour
{
    public static DigSpotManager Instance { get; private set; }

    [Header("การตั้งค่ากองดิน (Dig Spots)")]
    [Tooltip("ลากกองดินที่มีสคริปต์ DigSpotInteractable ทั้งหมดมาใส่ที่นี่ (หากเว้นว่าง ระบบจะค้นหาให้อัตโนมัติในฉาก)")]
    public List<DigSpotInteractable> digSpots = new List<DigSpotInteractable>();

    [Tooltip("ให้ค้นหากองดินในฉากทั้งหมดโดยอัตโนมัติหรือไม่ เมื่อเริ่มเกม")]
    public bool autoFindSpotsInScene = true;

    [Tooltip("จำนวนกองดินที่จะมีของรางวัลซ่อนอยู่ (ค่าเริ่มต้นคือ 1 กองจากทั้งหมด)")]
    [Range(1, 10)]
    public int winningSpotCount = 1;

    [Tooltip("สุ่มจุดที่มีของทันทีเมื่อเริ่มเกม")]
    public bool randomizeOnStart = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    private void Start()
    {
        if (autoFindSpotsInScene && (digSpots == null || digSpots.Count == 0))
        {
            FindAllDigSpotsInScene();
        }

        if (randomizeOnStart)
        {
            RandomizeRewardSpots();
        }
    }

    /// <summary>
    /// ค้นหากองดินทั้งหมดที่มีในฉาก
    /// </summary>
    [ContextMenu("Find All Dig Spots In Scene")]
    public void FindAllDigSpotsInScene()
    {
        digSpots.Clear();
        DigSpotInteractable[] foundSpots = FindObjectsByType<DigSpotInteractable>(FindObjectsSortMode.None);
        digSpots.AddRange(foundSpots);
        Debug.Log($"[DigSpotManager] 🔍 พบกองดินในฉากทั้งหมด {digSpots.Count} กอง");
    }

    /// <summary>
    /// สุ่มเลือกกองดินที่จะมีของรางวัลซ่อนอยู่
    /// </summary>
    [ContextMenu("Randomize Reward Spots")]
    public void RandomizeRewardSpots()
    {
        if (digSpots == null || digSpots.Count == 0)
        {
            Debug.LogWarning("[DigSpotManager] ⚠️ ไม่มีรายชื่อกองดินในระบบ ไม่สามารถสุ่มได้!");
            return;
        }

        // 1. รีเซ็ตทุกกองให้ไม่มีของก่อน
        foreach (var spot in digSpots)
        {
            if (spot != null)
            {
                spot.SetHasReward(false);
            }
        }

        // 2. สุ่มเลือก Index ของกองดินตามจำนวนที่กำหนด
        int targetWins = Mathf.Clamp(winningSpotCount, 1, digSpots.Count);
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < digSpots.Count; i++)
        {
            if (digSpots[i] != null) availableIndices.Add(i);
        }

        List<string> chosenNames = new List<string>();

        for (int i = 0; i < targetWins; i++)
        {
            if (availableIndices.Count == 0) break;

            int randListIdx = Random.Range(0, availableIndices.Count);
            int chosenSpotIdx = availableIndices[randListIdx];
            availableIndices.RemoveAt(randListIdx);

            digSpots[chosenSpotIdx].SetHasReward(true);
            chosenNames.Add(digSpots[chosenSpotIdx].gameObject.name);
        }

        Debug.Log($"[DigSpotManager] 🎯 สุ่มสำเร็จ! กองดินที่มีของรางวัลคือ: [{string.Join(", ", chosenNames)}] (จากทั้งหมด {digSpots.Count} กอง)");
    }

    private void OnDrawGizmosSelected()
    {
        if (digSpots == null) return;

        Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.8f);
        foreach (var spot in digSpots)
        {
            if (spot != null)
            {
                Gizmos.DrawWireSphere(spot.transform.position, 0.5f);
                Gizmos.DrawLine(transform.position, spot.transform.position);
            }
        }
    }
}
