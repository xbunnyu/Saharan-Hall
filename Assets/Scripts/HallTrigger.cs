using UnityEngine;

public class HallTrigger : InteractableItem
{
    [Header("Hall Trigger Settings")]
    [Tooltip("เธเนเธญเธเธงเธฒเธกเน€เธกเธทเนเธญเธ•เธณเธซเธเธฑเธเธขเธฑเธเธเธดเธ”เธญเธขเธนเน")]
    public string openPromptText = "เธเธ”เน€เธเธทเนเธญเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ (เน€เธฃเธดเนเธกเธ•เนเธญเธเธฃเธฑเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธ)";
    [Tooltip("เธเนเธญเธเธงเธฒเธกเน€เธกเธทเนเธญเธ•เธณเธซเธเธฑเธเน€เธเธดเธ”เธญเธขเธนเน")]
    public string closePromptText = "เธเธ”เน€เธเธทเนเธญเธเธดเธ”เธ•เธณเธซเธเธฑเธ (เธซเธขเธธเธ”เธฃเธฑเธเธเธนเนเธกเธฒเน€เธขเธทเธญเธ)";

    public AudioClip bellSound;

    void Start()
    {
        canRead = true;
        canCollect = false;
        UpdateTriggerInfo();
    }

    void Update()
    {
        UpdateTriggerInfo();
    }

    private void UpdateTriggerInfo()
    {
        if (HallManager.Instance != null)
        {
            int target = HallManager.Instance.maxNpcPerSession > 0 ? HallManager.Instance.maxNpcPerSession : 3;
            int current = HallManager.Instance.npcsServedThisSession;

            if (HallManager.Instance.isHallOpen)
            {
                bool canClose = current >= target;
                itemName = $"เนเธ—เนเธเธเธงเธเธเธธเธกเธ•เธณเธซเธเธฑเธ [เน€เธเธดเธ”เธญเธขเธนเน: {current}/{target} เธเธ]";
                customReadPromptText = canClose ? "เธเธ”เน€เธเธทเนเธญเธเธดเธ”เธ•เธณเธซเธเธฑเธ" : $"เธ•เนเธญเธเธฃเธฑเธเนเธเธเนเธซเนเธเธฃเธ ({current}/{target} เธเธ)";
                readTitle = $"เธชเธ–เธฒเธเธฐเธ•เธณเธซเธเธฑเธ: เน€เธเธดเธ”เธ—เธณเธเธฒเธฃ ({current}/{target} เธเธ)";
                readDescription = canClose ? closePromptText : $"เธขเธฑเธเธเธดเธ”เนเธกเนเนเธ”เน! เธ•เนเธญเธเนเธซเนเธเธฃเธดเธเธฒเธฃเธเธนเนเธกเธฒเน€เธขเธทเธญเธเนเธซเนเธเธฃเธ {target} เธเธเธเนเธญเธ (เธเธ“เธฐเธเธตเน {current}/{target})";
            }
            else
            {
                itemName = "เนเธ—เนเธเธเธงเธเธเธธเธกเธ•เธณเธซเธเธฑเธ [เธเธดเธ”เธญเธขเธนเน]";
                customReadPromptText = "เธเธ”เน€เธเธทเนเธญเน€เธเธดเธ”เธ•เธณเธซเธเธฑเธ";
                readTitle = "เธชเธ–เธฒเธเธฐเธ•เธณเธซเธเธฑเธ: เธเธดเธ”เธ—เธณเธเธฒเธฃ";
                readDescription = openPromptText;
            }
        }
        else
        {
            itemName = "เนเธ—เนเธเธเธงเธเธเธธเธกเธ•เธณเธซเธเธฑเธ";
            customReadPromptText = "เน€เธเธดเธ”/เธเธดเธ”เธ•เธณเธซเธเธฑเธ";
        }
    }

    public override void OnRead(PlayerInteraction interactor)
    {
        // เน€เธกเธทเนเธญเธเธ” E เธ—เธตเนเนเธ—เนเธ เนเธซเนเธชเธฅเธฑเธเธชเธ–เธฒเธเธฐเน€เธเธดเธ”/เธเธดเธ”เธ•เธณเธซเธเธฑเธ
        if (bellSound != null)
        {
            AudioSource.PlayClipAtPoint(bellSound, transform.position);
        }

        if (HallManager.Instance != null)
        {
            HallManager.Instance.ToggleHall();
        }
        else
        {
            Debug.LogWarning("[HallTrigger] โ ๏ธ เนเธกเนเธเธ HallManager เนเธเธเธฒเธ!");
        }

        interactor.CloseReading();
    }
}
