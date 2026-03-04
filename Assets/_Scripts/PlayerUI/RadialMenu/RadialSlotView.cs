using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadialSlotView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    public int SlotIndex { get; private set; }

    public void Setup(int index, Sprite sprite, string text = "")
    {
        SlotIndex = index;
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (label != null) label.text = text;
    }

    public void SetHighlighted(bool on)
    {
        if (background != null)
            background.color = on ? Color.white : Color.gray; // поменяй под стиль
        if (icon != null)
            icon.color = on ? Color.white : new Color(0.9f, 0.9f, 0.9f, 0.9f);
        transform.localScale = on ? Vector3.one * 1.2f : Vector3.one;
    }
}
