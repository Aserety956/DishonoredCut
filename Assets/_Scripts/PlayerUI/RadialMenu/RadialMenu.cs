using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class RadialMenu : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform radialContainer; // центр меню
    [SerializeField] private GameObject slotPrefab;
    //[SerializeField] private int slotCount = 8;
    [SerializeField] private float radius = 200f;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private QuickbarManager quickbarManager;

    [Header("Animation")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float openDuration = 0.22f;
    [SerializeField] private float closeDuration = 0.16f;
    [SerializeField] private float startScale = 0.75f;
    [SerializeField] private float slotStartRadiusMultiplier = 0.35f;
    [SerializeField] private float slotStagger = 0.025f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;
    [SerializeField] private Ease slotEase = Ease.OutCubic;

    [Header("Behavior")]
    [SerializeField] private float slowTimeScale = 0.2f;
    private float _previousTimeScale = 1f;
    
    private readonly List<Vector2> _slotTargetPositions = new();

    private bool _isOpen;
    private bool _isAnimating;
    private int _highlighted = -1;

    private Sequence _currentSequence;
    
    private class RadialEntry
    {
        public Slot inventorySlot;
        public RadialSlotView view;
    }
    private readonly List<RadialEntry> _entriesRadialMenu = new();

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (canvasGroup == null)
            canvasGroup = root.GetComponent<CanvasGroup>();

        _highlighted = -1;

        // Создаем слоты заранее
        for (int i = 0; i < inventoryManager.filledSlots.Count; i++)
        {
            Slot slot = inventoryManager.filledSlots[i];

            var go = Instantiate(slotPrefab, radialContainer);
            var view = go.GetComponent<RadialSlotView>();
            view.Setup(i, slot, (i + 1).ToString());
            _entriesRadialMenu.Add(new RadialEntry { inventorySlot = slot, view = view });
        }

        LayoutSlots();
    }

    private void LayoutSlots()
    {
        _slotTargetPositions.Clear();

        if (_entriesRadialMenu.Count == 0)
            return;

        float angleStep = 360f / _entriesRadialMenu.Count;

        for (int i = 0; i < _entriesRadialMenu.Count; i++)
        {
            float angle = Mathf.Deg2Rad * (90f - i * angleStep);
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var rt = _entriesRadialMenu[i].view.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            _slotTargetPositions.Add(pos);
        }
    }

    private void Update()
    {
        if (!_isOpen || _isAnimating)
            return;

        UpdateHighlight();

        HandleQuickbarAssignmentInput();
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_highlighted >= 0)
                OnSelectSlot(_highlighted);

            Close();
        }
        
    }

    private void HandleQuickbarAssignmentInput()
    {
        if (_highlighted < 0)
            return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            AssignHighlightedToQuickbar(0);
        }
        
    }
    
    
    private void AssignHighlightedToQuickbar(int quickbarIndex)
    {
        // взять _entries[_highlighted].inventorySlot
        // передать в quickbarManager.AssignSlot(quickbarIndex, slot)
        GetHighlightedInventorySlot();
        quickbarManager.AssignSlot(quickbarIndex,_entriesRadialMenu[_highlighted].inventorySlot);
        
    }
    
    private Slot GetHighlightedInventorySlot()
    {

        if (_highlighted < 0) 
            return null;

        return _entriesRadialMenu[_highlighted].inventorySlot;
        // если _highlighted некорректен -> return null
        // иначе вернуть _entries[_highlighted].inventorySlot
    }
    
    private void UpdateHighlight()
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            radialContainer,
            Mouse.current.position.ReadValue(),
            null,
            out localPos);

        int bestIndex = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _entriesRadialMenu.Count; i++)
        {
            var rt = _entriesRadialMenu[i].view.GetComponent<RectTransform>();
            float d = Vector2.SqrMagnitude(localPos - rt.anchoredPosition);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        if (bestIndex != _highlighted)
        {
            if (_highlighted >= 0)
                _entriesRadialMenu[_highlighted].view.SetHighlighted(false);

            _highlighted = bestIndex;

            if (_highlighted >= 0)
                _entriesRadialMenu[_highlighted].view.SetHighlighted(true);
        }
    }

    private void OnSelectSlot(int index)
    {
        Debug.Log($"Radial selected slot {index}"); // todo: put item in player hands
    }

    public void Open()
    {
        if (_isOpen && !_isAnimating)
            return;

        _currentSequence?.Kill();

        _isOpen = true;
        _isAnimating = true;

        root.SetActive(true);

        _previousTimeScale = Time.timeScale;
        Time.timeScale = slowTimeScale;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        PlayOpenAnimation();
    }

    public void Close()
    {
        if (!_isOpen && !_isAnimating)
            return;

        _currentSequence?.Kill();
        Time.timeScale = _previousTimeScale;

        _isAnimating = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        ClearHighlight();

        PlayCloseAnimation();
    }

    private void PlayOpenAnimation()
    {
        _currentSequence?.Kill();

        if (canvasGroup.alpha <= 0.001f)
        {
            radialContainer.localScale = Vector3.one * startScale;

            for (int i = 0; i < _entriesRadialMenu.Count; i++)
            {
                var rt = _entriesRadialMenu[i].view.GetComponent<RectTransform>();
                rt.anchoredPosition = _slotTargetPositions[i] * slotStartRadiusMultiplier;
                rt.localScale = Vector3.one * 0.85f;
            }
        }

        _currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        // Общая анимация контейнера
        _currentSequence.Join(
            canvasGroup.DOFade(1f, openDuration)
                .SetEase(Ease.OutQuad)
        );

        _currentSequence.Join(
            radialContainer.DOScale(1f, openDuration)
                .SetEase(openEase)
        );

        // Анимация слотов
        for (int i = 0; i < _entriesRadialMenu.Count; i++)
        {
            int index = i;
            var rt = _entriesRadialMenu[index].view.GetComponent<RectTransform>();

            _currentSequence.Insert(
                index * slotStagger,
                rt.DOAnchorPos(_slotTargetPositions[index], openDuration)
                    .SetEase(slotEase)
            );

            _currentSequence.Insert(
                index * slotStagger,
                rt.DOScale(1f, openDuration)
                    .SetEase(Ease.OutBack)
            );
        }

        _currentSequence.OnComplete(() =>
        {
            _isAnimating = false;
            _isOpen = true;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });
    }

    private void PlayCloseAnimation()
    {
        _currentSequence?.Kill();

        _currentSequence = DOTween.Sequence()
            .SetUpdate(true);

        _currentSequence.Join(
            canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.OutQuad)
        );

        _currentSequence.Join(
            radialContainer.DOScale(startScale, closeDuration).SetEase(closeEase)
        );

        for (int i = 0; i < _entriesRadialMenu.Count; i++)
        {
            int index = i;
            var rt = _entriesRadialMenu[index].view.GetComponent<RectTransform>();

            _currentSequence.Join(
                rt.DOAnchorPos(_slotTargetPositions[index] * slotStartRadiusMultiplier, closeDuration)
                    .SetEase(Ease.InCubic)
            );

            _currentSequence.Join(
                rt.DOScale(0.85f, closeDuration).SetEase(Ease.InCubic)
            );
        }

        _currentSequence.OnComplete(() =>
        {
            _isAnimating = false;
            _isOpen = false;

            root.SetActive(false);

            Time.timeScale = _previousTimeScale;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        });
    }

    private void ClearHighlight()
    {
        if (_highlighted >= 0 && _highlighted < _entriesRadialMenu.Count)
            _entriesRadialMenu[_highlighted].view.SetHighlighted(false);

        _highlighted = -1;
    }
    

    public void OnSlot1(InputValue v) { if (v.isPressed) OnSelectSlot(0); }
    public void OnSlot2(InputValue v) { if (v.isPressed) OnSelectSlot(1); }
    
    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnSlotUpdated += AddOrRefreshSlot;
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnSlotUpdated -= AddOrRefreshSlot;
    }
    
    public void AddOrRefreshSlot(Slot slot)
    {
        foreach (var entry in _entriesRadialMenu)
        {
            if (entry.inventorySlot == slot)
            {
                entry.view.Refresh(slot);
                return;
            }
        }

        var go = Instantiate(slotPrefab, radialContainer);
        var view = go.GetComponent<RadialSlotView>();

        int index = _entriesRadialMenu.Count;
        view.Setup(index, slot, (index + 1).ToString());

        _entriesRadialMenu.Add(new RadialEntry { inventorySlot = slot, view = view });

        LayoutSlots();
    }

}

