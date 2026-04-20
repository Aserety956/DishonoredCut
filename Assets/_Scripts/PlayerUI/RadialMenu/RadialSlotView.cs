using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadialSlotView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text amountText;

    public int SlotIndex { get; private set; }

    public void Setup(int index, Slot slot, string text = "")
    {
        SlotIndex = index;

        if (icon != null)
        {
            if (slot != null && slot.item != null && slot.item.icon != null)
            {
                icon.sprite = slot.item.icon;
                icon.enabled = true;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        if (amountText != null)
        {
            if (slot != null && !slot.isEmpty)
                amountText.text = slot.amount.ToString();
            else
                amountText.text = "";
        }

        if (label != null)
            label.text = text;
    }

    public void Refresh(Slot slot)
    {
        if (icon != null)
        {
            if (slot != null && slot.item != null && slot.item.icon != null)
            {
                icon.sprite = slot.item.icon;
                icon.enabled = true;
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        if (amountText != null)
        {
            if (slot != null && !slot.isEmpty)
                amountText.text = slot.amount.ToString();
            else
                amountText.text = "";
        }
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
