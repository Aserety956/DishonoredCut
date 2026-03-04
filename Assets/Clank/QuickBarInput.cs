using UnityEngine;
using UnityEngine.InputSystem;

public class QuickbarInput : MonoBehaviour
{
    [SerializeField] private QuickbarManager quickbar; // ссылка на UI-Quickbar на Canvas

    private void Awake()
    {
        // Если не назначил в инспекторе — попробуем найти на сцене
        if (quickbar == null)
            quickbar = FindAnyObjectByType<QuickbarManager>();
    }

    public void OnSlot1(InputValue v) { if (v.isPressed) quickbar.Select(0); }
    public void OnSlot2(InputValue v) { if (v.isPressed) quickbar.Select(1); }
    public void OnSlot3(InputValue v) { if (v.isPressed) quickbar.Select(2); }
    public void OnSlot4(InputValue v) { if (v.isPressed) quickbar.Select(3); }
    public void OnSlot5(InputValue v) { if (v.isPressed) quickbar.Select(4); }
    public void OnSlot6(InputValue v) { if (v.isPressed) quickbar.Select(5); }
    public void OnSlot7(InputValue v) { if (v.isPressed) quickbar.Select(6); }
    public void OnSlot8(InputValue v) { if (v.isPressed) quickbar.Select(7); }

    public void OnUseQuick(InputValue v) { if (v.isPressed) quickbar.UseSelected(); }
}
