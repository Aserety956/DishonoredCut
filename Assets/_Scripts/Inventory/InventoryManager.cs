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
    public List<Slot> slots = new List<Slot>(15);
    public bool isOpened;

    private void Awake()
    {
        uiPanel.SetActive(true);
    }

    private void Start()
    {
        for (int i = 0; i < inventoryPanel.childCount; i++)
        {
            slots.Add(inventoryPanel.GetChild(i).GetComponent<Slot>());
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

    public void AddItem(ItemSO _item, int _amount)
    {
        foreach (Slot slot in slots)
        {
            if (slot.item == _item && (slot.amount + _amount) <= _item.maxAmount)
            {
                slot.amount += _amount;
                slot.itemAmountText.text = _amount.ToString();
                return;
            }
            
        }
        
        foreach(Slot slot in slots)
        {
            if (slot.isEmpty)
            {
                slot.item = _item;
                slot.amount = _amount;
                slot.isEmpty = false;
                slot.SetIcon(_item.icon);
                slot.itemAmountText.text = _amount.ToString();
                return;
            }
        }
    }
}
