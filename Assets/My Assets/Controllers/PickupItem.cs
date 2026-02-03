using UnityEngine;

public enum ItemEffectType { HealHealth, RestoreHunger }
public class PickupItem : MonoBehaviour
{
    public string itemName;
    public Sprite icon;

    public ItemEffectType effectType;
    public float amount = 25f;

    public bool consumeOnUse = true;
    public string displayName = "Item";

    [Header("Prompt (World Space)")]
    public GameObject promptGO;

    public void ShowPrompt(bool show, string customText = null)
    {
        if (customText != null && promptGO)
        {
            var textComp = promptGO.GetComponentInChildren<TMPro.TMP_Text>();
            var textAux = textComp ? textComp.text : null;
            if (textComp)
            { 
                textComp.text = customText;
                promptGO.SetActive(show);
                textComp.text = textAux;
            }
        } else
        {
            if (promptGO) promptGO.SetActive(show);
        }

    }

    public void OnPickedUp()
    {
        gameObject.SetActive(false);
    }
}
