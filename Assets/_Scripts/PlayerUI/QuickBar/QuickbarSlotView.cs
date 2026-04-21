using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickbarSlotView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] public TMP_Text nameText;
    

    [Header("Visuals")]
    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color emptyColor = new Color(1,1,1,0.25f);

    public void SetIndex(int oneBasedIndex)
    {
        if (indexText != null)
            indexText.text = oneBasedIndex.ToString();
    }

    public void SetItem(Slot slot)
    {
        //if (iconImage == null) return;

        if (slot == null)
        {
            iconImage.enabled = false;
            nameText.text = null;
            iconImage.sprite = null;
            amountText.text = null;
            if (background != null) background.color = emptyColor;
        }
        else
        {
            iconImage.enabled = true;
            nameText.text = slot.item.itemName;
            iconImage.sprite = slot.item.icon;
            amountText.text = slot.amount.ToString();
            if (background != null) background.color = normalColor;
        }
    }

    public void SetSelected(bool selected)
    {
        if (background == null) return;
        background.color = selected ? selectedColor : normalColor;
    }
}