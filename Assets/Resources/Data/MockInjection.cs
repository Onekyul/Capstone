using UnityEngine;
using System.Collections.Generic;

public class MockInjection : MonoBehaviour
{
    void Start()
    {
        if (DataManager.instance == null) return;

        // 데이터 초기화 및 생성
        if (DataManager.instance.currentPlayer == null)
            DataManager.instance.currentPlayer = new PlayerData();

        if (DataManager.instance.currentPlayer.Inventory == null)
        {
            DataManager.instance.currentPlayer.Inventory = new List<InventorySlot>();
        }
        else
        {
            DataManager.instance.ClearALlData();
            DataManager.instance.currentPlayer.Inventory.Clear(); // 기존 비우기
            
        }

        Debug.Log("[Test] 모크 데이터 주입 시작...");
        DataManager.instance.currentPlayer.ownedWeapons.Add(new EquipmentState("bow_wood", 0));
        DataManager.instance.currentPlayer.ownedArmors.Add(new EquipmentState("helmet_wood", 0));
        DataManager.instance.currentPlayer.ownedArmors.Add(new EquipmentState("armor_wood", 0));
        DataManager.instance.currentPlayer.ownedArmors.Add(new EquipmentState("boots_wood", 0));
        
      
        DataManager.instance.currentPlayer.equippedWeaponId = "bow_wood";
        DataManager.instance.currentPlayer.equippedHelmetId = "helmet_wood";
        DataManager.instance.currentPlayer.equippedArmorId = "armor_wood";
        DataManager.instance.currentPlayer.equippedBootsId = "boots_wood";
        
        DataManager.instance.AddInventory("mat_wood", 50);
        DataManager.instance.AddInventory("mat_icestone", 1); 
        DataManager.instance.AddInventory("mat_firestone", 20);
        
        
        
        // 1. WizardUI 갱신
        if (WizardUI.instance != null)
            WizardUI.instance.UpdateUI();

        // 2. InventoryUI 갱신 (추가된 코드)
        InventoryUI inventoryUI = FindAnyObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.UpdateAllInventory();
           
        }
        
    }
}