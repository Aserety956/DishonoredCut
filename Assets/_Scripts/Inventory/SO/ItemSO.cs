using UnityEngine;


public enum ItemType
{
    Default,
    Food,
    Crossbow,
    Grenade,
    HealPotion,
    ManaPotion
}

public class ItemSO : ScriptableObject
{
    public int maxAmount;
    public string itemName;
    public GameObject itemPrefab;
    public Sprite icon;
    public ItemType itemType;
    
}
