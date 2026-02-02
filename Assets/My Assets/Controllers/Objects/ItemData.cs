using UnityEngine;

public enum ItemEffectType { HealHealth, RestoreHunger }

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    public ItemEffectType effectType;
    public float amount = 25f;

    public bool consumeOnUse = true;
}
