using TMPro;
using UnityEngine;

public class Systems_AppleCountUI : MonoBehaviour
{
    [SerializeField] private string itemTag = "YourItemTag";
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private string prefix = "Apples Collected: ";

    void Update()
    {
        if (Systems_ConsumedItemTracker.Instance == null) return;

        int count = Systems_ConsumedItemTracker.Instance.GetConsumedCount(itemTag);
        countText.text = prefix + count;
    }
}
