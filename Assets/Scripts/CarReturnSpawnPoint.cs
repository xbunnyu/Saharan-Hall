using UnityEngine;

/// <summary>
/// วางบน Empty GameObject ในซีน GamePlay ตรงจุดที่ต้องการให้ผู้เล่นมายืนเมื่อขับรถกลับมา
/// (สืบทอดจาก SceneSpawnPoint จึงรองรับการแสดง Gizmos และค้นหาจุดเกิดอัตโนมัติ)
/// </summary>
public class CarReturnSpawnPoint : SceneSpawnPoint
{
    public static CarReturnSpawnPoint Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
