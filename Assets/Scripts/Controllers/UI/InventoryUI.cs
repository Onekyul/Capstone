using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InventoryUI : MonoBehaviour
{
  [Header("1. 장비 슬롯")]
    public InventorySlotUI slotWeapon;
    public InventorySlotUI slotHelmet;
    public InventorySlotUI slotArmor;
    public InventorySlotUI slotBoots;

    [Header("2. 인챈트 슬롯 (5개)")]
    public InventorySlotUI slotFire;
    public InventorySlotUI slotIce;
    public InventorySlotUI slotLightning;
    public InventorySlotUI slotPoison;
    public InventorySlotUI slotWater;

    [Header("3. 재료 슬롯 (Grid)")]
    public Transform materialGridParent;
    private InventorySlotUI[] materialSlots;

    
    public string hubSceneName = "BaseArea";
    
    void Awake()
    {
        Debug.Log("awake");
        materialSlots = materialGridParent.GetComponentsInChildren<InventorySlotUI>();
    }

    void OnEnable()
    {
        Debug.Log("OnEnable");
        UpdateAllInventory();
    }
    
    public void UpdateAllInventory()
    {
        if (DataManager.instance == null) return;
        UpdateEquipment();
        UpdateEnchants();
        UpdateMaterialGrid();
    }

    //장비 
    void UpdateEquipment()
    {
        UpdateEquipSlot(slotWeapon, EquipmentType.Weapon);
        UpdateEquipSlot(slotHelmet, EquipmentType.Helmet);
        UpdateEquipSlot(slotArmor, EquipmentType.Armor);
        UpdateEquipSlot(slotBoots, EquipmentType.Boots);
    }

    void UpdateEquipSlot(InventorySlotUI slot, EquipmentType type)
    {
        string id = DataManager.instance.GetEquippedItemId(type);
        Sprite iconToDisplay = null;
        
        if (type == EquipmentType.Weapon) 
        {
            WeaponData wData = DataManager.instance.GetWeaponData(id);
            if (wData != null) iconToDisplay = wData.icon;
        }
        else 
        {
            ArmorData aData = DataManager.instance.GetArmorData(id);
            if (aData != null) iconToDisplay = aData.icon;
        }
        
        slot.SetEquipment(iconToDisplay);
    }

    //인챈트
    void UpdateEnchants()
    {
        UpdateSingleEnchant(slotFire, "ent_fire");
        UpdateSingleEnchant(slotIce, "ent_ice");
        UpdateSingleEnchant(slotLightning, "ent_lightning");
        UpdateSingleEnchant(slotPoison, "ent_poison");
        UpdateSingleEnchant(slotWater, "ent_water");
    }

    void UpdateSingleEnchant(InventorySlotUI slot, string id)
    {
        int level = DataManager.instance.GetEnchantLevel(id);
        if (level > 0)
        {
            EnchantData data = DataManager.instance.GetEnchantData(id);
            slot.SetEnchant(data, level);
        }
        else
        {
            slot.ClearSlot();
        }
    }

    //재료
    void UpdateMaterialGrid()
    {
        List<InventorySlot> invList = DataManager.instance.currentPlayer.Inventory;
        Debug.Log($"[InventoryUI] 찾은 재료 슬롯 개수: {materialSlots.Length}");
        for (int i = 0; i < materialSlots.Length; i++)
        {
            if (i < invList.Count)
            {
                string itemId = invList[i].itemId;
                int count = invList[i].count;
                
                ItemData data = DataManager.instance.GetMaterialData(itemId);
            
                materialSlots[i].SetMaterial(data, count);
            }
            else
            {
                materialSlots[i].ClearSlot();
            }
        }
    }
    
}
