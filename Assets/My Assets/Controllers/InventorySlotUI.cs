using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Refs")]
    public Image fillImage;
    public TMP_Text emptyText;

    public void SetEmpty()
    {
        if (fillImage)
        {
            fillImage.enabled = false;
            fillImage.sprite = null;
        }

        if (emptyText)
            emptyText.gameObject.SetActive(true);
    }

    public void SetItem(PickupItem item)
    {
        if (emptyText)
            emptyText.gameObject.SetActive(false);

        if (fillImage)
        {
            fillImage.enabled = true;
            fillImage.sprite = item.icon;
            fillImage.preserveAspect = true;
        }
    }
}
