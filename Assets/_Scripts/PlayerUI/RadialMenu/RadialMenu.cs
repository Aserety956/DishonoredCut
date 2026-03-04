using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class RadialMenu : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private GameObject root; // RadialMenuRoot
    [SerializeField] private RectTransform radialContainer; // центр (например 600x600)
    [SerializeField] private GameObject slotPrefab; // RadialSlot prefab
    [SerializeField] private int slotCount = 8; //todo: динамическое кол во зависящее от открытых умений/предметов
    [SerializeField] private float radius = 200f; // радиус расположения слотов внутри container

    [Header("Behavior")]
    //[SerializeField] private bool chooseOnRelease = true;
    [SerializeField] private float slowTimeScale = 0.2f;
    private float _previousTimeScale = 1f;

    // internal
    private readonly List<RadialSlotView> _slots = new List<RadialSlotView>();
    private bool _isOpen;
    private int _highlighted = -1;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        
        _highlighted = -1;
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].SetHighlighted(false);

        // Создадим слоты заранее, чтобы не лагать при открытии
        for (int i = 0; i < slotCount; i++)
        {
            var go = Instantiate(slotPrefab, radialContainer);
            var view = go.GetComponent<RadialSlotView>();
            view.Setup(i, null, (i+1).ToString()); // иконку назначим позже
            _slots.Add(view);
        }

        LayoutSlots();
        
    }

    private void LayoutSlots()
    {
        // Располагаем по кругу в local coordinates radialContainer
        float angleStep = 360f / slotCount;
        for (int i = 0; i < _slots.Count; i++)
        {
            float angle = Mathf.Deg2Rad * (90f - i * angleStep); // 90deg = вверх; поменяй если нужно стартовый угол
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            var rt = _slots[i].GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
        }
    }

    private void Update()
    {
        if (!_isOpen) return;

        // Подсветка слота под курсором
        UpdateHighlight();
        
        // При клике левой — выбрать
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_highlighted >= 0)
                OnSelectSlot(_highlighted);
            Close();
        }
    }

    private void UpdateHighlight()
    {
        // Получаем позицию курсора в локальных координатах radialContainer
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            radialContainer, 
            Mouse.current.position.ReadValue(), 
            null, // camera null for Screen Space - Overlay
            out localPos);

        int bestIndex = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _slots.Count; i++)
        {
            var rt = _slots[i].GetComponent<RectTransform>();
            float d = Vector2.SqrMagnitude(localPos - rt.anchoredPosition);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        // Обновим визуал
        if (bestIndex != _highlighted)
        {
            if (_highlighted >= 0) _slots[_highlighted].SetHighlighted(false);
            _highlighted = bestIndex;
            if (_highlighted >= 0) _slots[_highlighted].SetHighlighted(true);
        }
    }

    private void OnSelectSlot(int index)
    {
        Debug.Log($"Radial selected slot {index}");
        // TODO: вызови логику equip/use: Quickbar.Instance.UseSlot(index) или через событие
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        ClearHighlight();
        root.SetActive(true);
        _highlighted = -1;
        
        _previousTimeScale = Time.timeScale; // юзать для UI unscaled delta time из за анимаций

        // замедлить игру
        Time.timeScale = slowTimeScale;
        
        // Unlock cursor for selection:
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        ClearHighlight();
        root.SetActive(false);
        
        Time.timeScale = _previousTimeScale; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void ClearHighlight()
    {
        if (_highlighted >= 0 && _highlighted < _slots.Count)
            _slots[_highlighted].SetHighlighted(false);

        _highlighted = -1;
    }
    // ==== Input system hook (Send Messages style) ====
    // В PlayerInput в карте Player создайте Action "Radial" (Button, <Mouse>/rightButton)
    // Behavior = Press (включить send messages)
    public void OnRadial(InputValue val)
    {
        if (val.isPressed)
            Open();
        else
            Close();
    }

    // Если хочешь, можно отдельным action повесить выбор по клавишам 1..8 (и вызывать OnSelectSlot)
    public void OnSlot1(InputValue v) { if (v.isPressed) OnSelectSlot(0); }
    public void OnSlot2(InputValue v) { if (v.isPressed) OnSelectSlot(1); }
    // ... до OnSlot8
}

