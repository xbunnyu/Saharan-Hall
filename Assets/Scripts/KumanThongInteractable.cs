using UnityEngine;

/// <summary>
/// วัตถุศาลกุมารทองในฉาก (สืบทอดมาจาก InteractableItem)
/// เมื่อผู้เล่นกด [E] จะเปิดหน้าต่าง UI กุมารทองขึ้นมาถวายเครื่องเซ่นเพื่อลดค่า Fail
/// </summary>
public class KumanThongInteractable : InteractableItem
{
    private void Awake()
    {
        readDescription = "";
        readTitle = "";
    }

    private void Reset()
    {
        itemName = "ศาลกุมารทอง";
        canRead = true;
        canCollect = false;
        interactionKeyText = "E";
        customReadPromptText = "กราบไหว้/ถวายของ";
        readDescription = "";
        readTitle = "";
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        KumanThongUIController ui = KumanThongUIController.Instance;
        if (ui == null)
        {
            ui = FindFirstObjectByType<KumanThongUIController>();
            if (ui == null)
            {
                GameObject uiObj = new GameObject("KumanThongUIController");
                ui = uiObj.AddComponent<KumanThongUIController>();
            }
        }

        ui.OpenUI(interactor);
    }
}
