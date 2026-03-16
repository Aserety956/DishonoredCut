using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerController player);
    void SetHighlight(bool enabled);
    string GetInteractText();
}
