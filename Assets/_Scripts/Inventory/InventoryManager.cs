using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class InventoryManager : MonoBehaviour
{
    public GameObject uiPanel;
    public PlayerController playerController;
    public Transform inventoryPanel;
    [SerializeField] private Slot[] _slotsArray = new Slot[15]; 
    public List<Slot> slots = new List<Slot>(15);
    public List<Slot> filledSlots = new List<Slot>(15);
    public bool isOpened;
    [SerializeField] private QuickbarManager quickbarManager;
    
    public event Action<Slot> OnSlotUpdated;
    private void Awake()
    {
        uiPanel.SetActive(true);
    }

    private void Start()
    {
        for (int i = 0; i < inventoryPanel.childCount; i++)
        {
            slots.Add(_slotsArray[i]);
        }
        uiPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (isOpened)
            {
                playerController.isAttacking = false;
                uiPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                playerController.enabled = true;
                playerController.cinCam.enabled = true;
                playerController.mainCam.enabled = true;
                playerController.animator.enabled = true;
                playerController.isAttacking = false;
            }
            else
            {
                playerController.isAttacking = false;
                uiPanel.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                playerController.enabled = false;
                playerController.cinCam.enabled = false;
                playerController.mainCam.enabled = false;
                playerController.animator.enabled = false;
                playerController.isAttacking = false;
            }
            isOpened = !isOpened;
        }
    }

    public bool AddItem(ItemSO item, int amount)
    {
        foreach (Slot slot in slots)
        {
            if (slot.item == item && (slot.amount + amount) <= item.maxAmount)
            {
                slot.amount += amount;
                slot.itemAmountText.text = slot.amount.ToString();
                
                OnSlotUpdated?.Invoke(slot);
                
                quickbarManager.RefreshAllSlots();
                
                return true;
            }

            if (slot.item == item && (slot.amount + amount) > item.maxAmount)
            {
                Debug.Log("You can't have more items of this type");
                return false;
            }
        }
        
        foreach(Slot slot in slots)
        {
            if (slot.isEmpty)
            {
                slot.item = item;
                slot.amount = amount;
                slot.isEmpty = false;
                slot.SetIcon(item.icon);
                slot.itemAmountText.text = amount.ToString();
                
                filledSlots.Add(slot);
                
                OnSlotUpdated?.Invoke(slot);
                
                quickbarManager.RefreshAllSlots();
                
                return true;
            }
        }
        return false;
    }
    
}
