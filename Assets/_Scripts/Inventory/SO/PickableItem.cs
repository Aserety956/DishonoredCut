using UnityEngine;

public class PickableItem : MonoBehaviour, IInteractable
{
    public ItemSO pickedItem;
    public int amount;
    
    [Header("Highlight")]
    [SerializeField] private Behaviour outlineBehaviour;
    
    public void Interact(PlayerController player)
    {
    }

    public void SetHighlight(bool enabled)
    {
        if (outlineBehaviour != null)
            outlineBehaviour.enabled = enabled;
    }

    public string GetInteractText()
    {
        return "E — Pick up " + gameObject.name;
    }
}
