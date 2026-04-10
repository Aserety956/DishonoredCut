using UnityEngine;

public class PickableItem : MonoBehaviour, IInteractable
{
    public ItemSO pickedItem;
    public int amount;
    
    [Header("Highlight")]
    [SerializeField] private Behaviour outlineBehaviour;
    
    /*
    [SerializeField] private PickableItem pickableItem;
    [SerializeField] private InventoryManager inventoryManager;*/
    
    public void Interact(PlayerController player)
    {
        //inventoryManager.AddItem(pickedItem., pickableItem.amount);
    }

    public void SetHighlight(bool enabled)
    {
        if (outlineBehaviour != null)
            outlineBehaviour.enabled = enabled;
    }

    public string GetInteractText()
    {
        return "E — Pick up heal potion";
    }
}
