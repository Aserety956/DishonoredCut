using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class QuickbarManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private QuickbarSlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int slotCount = 8;

    [Header("Items (debug)")]
    [SerializeField] private QuickItem[] startingItems = new QuickItem[8];

    [Header("Use")]
    [SerializeField] private GameObject user; // игрок
    
    [Header("Animation")]
    [SerializeField] private RectTransform quickbarRoot;
    [SerializeField] private float showY = 50f;
    [SerializeField] private float hideY = -200f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float visibleTime = 2f;
    
    [SerializeField] private CanvasGroup canvasGroup;

    private Tween _tween;
    private Tween _hideDelay;

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
        
        if (quickbarRoot != null)
        {
            quickbarRoot.anchoredPosition = new Vector2(0, hideY);
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }
    
    public void Select(int index)
    {
        if (index < 0 || index >= slotCount) return;

        // даже если тот же слот — всё равно показываем UI
        SelectedIndex = index;
        RefreshSelection();

        ShowQuickbar();
    }
    
    private void ShowQuickbar()
    {
        if (quickbarRoot == null) return;

        _tween?.Kill();
        _hideDelay?.Kill();
        
        _tween = quickbarRoot
            .DOAnchorPosY(showY, duration)
            .SetEase(Ease.OutBack);
        
        canvasGroup?.DOFade(1f, duration);

        _hideDelay = DOVirtual.DelayedCall(visibleTime, HideQuickbar);
    }

    private void HideQuickbar()
    {
        if (quickbarRoot == null) return;

        _tween?.Kill();
        
        _tween = quickbarRoot
            .DOAnchorPosY(hideY, duration)
            .SetEase(Ease.InBack);
        
        canvasGroup?.DOFade(0f, duration);
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
