using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class QuickbarManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private QuickbarSlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] public int slotCount;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Use")]
    [SerializeField] private GameObject user;
    
    [Header("Animation")]
    [SerializeField] private RectTransform quickbarRoot;
    [SerializeField] private float showY = 50f;
    [SerializeField] private float hideY = -200f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float visibleTime = 2f;
    
    [SerializeField] private CanvasGroup canvasGroup;

    private Tween _tween;
    private Tween _hideDelay;

    private Slot[] _assignedSlots;
    public QuickbarSlotView[] _views;
    
    private class QuickbarEntry
    {
        public Slot inventorySlot;
        public QuickbarSlotView view;
    }

    private readonly List<QuickbarEntry> _entriesQuickBar = new();

    public int SelectedIndex { get; private set; } = 0; // 0..7

    private void Awake()
    {
        if (slotParent == null) slotParent = transform;
        
        _assignedSlots = new Slot[slotCount];
        _views = new QuickbarSlotView[slotCount];

        BuildUI();
    }

    public void BuildUI()
    {
        for (int i = 0; i < slotCount; i++)
        {
            Slot slot = _assignedSlots[i];
            
            _assignedSlots[i] = (inventoryManager.filledSlots != null && i < inventoryManager.filledSlots.Count) ? inventoryManager.filledSlots[i] : null;

            var view = Instantiate(slotPrefab, slotParent);
            view.SetIndex(i + 1);
            view.SetItem(slot);
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
    
    public void ShowQuickbar()
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

    public void HideQuickbar()
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
        var item = _assignedSlots[SelectedIndex];
        if (item == null) return;

        //item.Use(user != null ? user : gameObject);
    }

    public void SetItemQuickBar(int index, Slot slot)
    {
        if (index < 0 || index >= slotCount) return;

        _assignedSlots[index] = slot;
        _views[index].SetItem(slot);
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slotCount; i++)
            _views[i].SetSelected(i == SelectedIndex);
    }

    public void AssignSlot(int quickbarIndex, Slot inventorySlot)
    {
        //inventorySlot = _assignedSlots[quickbarIndex];
        SetItemQuickBar(quickbarIndex,inventorySlot); 
    }
    
    public void UnassignSlot(int quickbarIndex, Slot inventorySlot)
    {
        //inventorySlot = _assignedSlots[quickbarIndex];
        SetItemQuickBar(quickbarIndex,inventorySlot); 
    }
    
    public void RefreshSlot(int quickbarIndex)
    {
        var currentSlot = _assignedSlots[quickbarIndex];
        _views[quickbarIndex].SetItem(currentSlot);
        // и передать его в _views[quickbarIndex]
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            var slots = _assignedSlots[i];
            _views[i].SetItem(slots);
        }
    }
    
    public Slot GetAssignedSlot(int quickbarIndex)
    {
        if (quickbarIndex >= 0)
        {
            return _assignedSlots[quickbarIndex];
        }
        return null;
    }
}
