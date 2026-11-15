using UnityEngine;

/// <summary>
/// กองดินเรืองแสงสีทองตลอด (มองเห็นจากไกล) จนกว่าจะถูกขุด → ขุดแล้วแสงดับ
/// ใส่ให้อัตโนมัติจากเมนูจัดฉากป่า (ForestSceneSetupTool) คู่กับ ItemHighlighter
/// </summary>
[RequireComponent(typeof(DigSpotInteractable))]
public class DigSpotGlowSync : MonoBehaviour
{
    DigSpotInteractable spot;
    ItemHighlighter glow;
    bool lastDug;

    void Awake()
    {
        spot = GetComponent<DigSpotInteractable>();
        glow = GetComponent<ItemHighlighter>();
    }

    void Start()
    {
        if (glow != null) glow.SetHighlightEnabled(!spot.isDug);
        lastDug = spot.isDug;
    }

    void Update()
    {
        if (spot == null || glow == null || spot.isDug == lastDug) return;
        lastDug = spot.isDug;
        glow.SetHighlightEnabled(!lastDug); // ขุดแล้ว → ดับ
    }
}
