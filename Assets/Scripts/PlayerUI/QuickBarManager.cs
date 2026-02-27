using UnityEngine;
using UnityEngine.InputSystem;

public class QuickbarManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private QuickbarSlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int slotCount = 8;

    [Header("Items (debug)")]
    [SerializeField] private QuickItem[] startingItems = new QuickItem[8];

    [Header("Use")]
    [SerializeField] private GameObject user; // игрок (или оставь пустым и подставь сам)

    private QuickItem[] _items;
    private QuickbarSlotView[] _views;

    public int SelectedIndex { get; private set; } = 0; // 0..7

    private void Awake()
    {
        if (slotParent == null) slotParent = transform;
        //if (user == null) user = GameObject.FindWithTag("Player");

        _items = new QuickItem[slotCount];
        _views = new QuickbarSlotView[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            _items[i] = (startingItems != null && i < startingItems.Length) ? startingItems[i] : null;

            var view = Instantiate(slotPrefab, slotParent);
            view.SetIndex(i + 1);
            view.SetItem(_items[i]);
            _views[i] = view;
        }

        RefreshSelection();
    }
    
    public void Select(int index)
    {
        if (index < 0 || index >= slotCount) return;
        if (SelectedIndex == index) return;

        SelectedIndex = index;
        RefreshSelection();
    }

    public void UseSelected()
    {
        var item = _items[SelectedIndex];
        if (item == null) return;

        item.Use(user != null ? user : gameObject);
    }

    public void SetItem(int index, QuickItem item)
    {
        if (index < 0 || index >= slotCount) return;

        _items[index] = item;
        _views[index].SetItem(item);
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slotCount; i++)
            _views[i].SetSelected(i == SelectedIndex);
    }
}
