using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickbarSlotView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text indexText;

    [Header("Visuals")]
    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color emptyColor = new Color(1,1,1,0.25f);

    public void SetIndex(int oneBasedIndex)
    {
        if (indexText != null)
            indexText.text = oneBasedIndex.ToString();
    }

    public void SetItem(QuickItem item)
    {
        //if (iconImage == null) return;

        if (item == null)
        {
            iconImage.enabled = false;
            if (background != null) background.color = emptyColor;
        }
        else
        {
            iconImage.enabled = true;
            iconImage.sprite = item.icon;
            if (background != null) background.color = normalColor;
        }
    }

    public void SetSelected(bool selected)
    {
        if (background == null) return;
        background.color = selected ? selectedColor : normalColor;
    }
}