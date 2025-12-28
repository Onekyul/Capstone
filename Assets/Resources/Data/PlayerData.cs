using UnityEngine;
using System.Collections.Generic;

public enum EquipmentType
{
    Weapon,
    Helmet,
    Armor,
    Boots
}

public class PlayerData
{
    //장착 장비
    public string equippedWeaponId = "bow_wood";
    public string equippedHelmetId = "helmet_wood";
    public string equippedArmorId = "armor_wood";
    public string equippedBootsId = "boots_wood";
    
    //인벤토리
    public List<InventorySlot> Inventory = new List<InventorySlot>();
    
    //보유 장비
    public List<EquipmentState> ownedWeapons = new List<EquipmentState>();
    public List<EquipmentState> ownedArmors = new List<EquipmentState>();

    //보유 인챈트
    public List<EnchantState> unlockedEnchants = new List<EnchantState>();

    public PlayerData()
    {
        ownedWeapons.Add(new EquipmentState("bow_wood",0));
        ownedArmors.Add(new EquipmentState("helmet_wood",0));
        ownedArmors.Add(new EquipmentState("armor_wood",0));
        ownedArmors.Add(new EquipmentState("boots_wood",0));
    }
    
}

[System.Serializable]
public class InventorySlot
{
    public string itemId;
    public int count;

    public InventorySlot(string itemId, int count)
    {
        this.itemId = itemId;
        this.count = count;
    }
}
[System.Serializable]
public class EquipmentState
{
    public string itemId;
    public int reinforcementLevel;

    public EquipmentState(string itemId, int reinforcementLevel)
    {
        this.itemId = itemId;
        this.reinforcementLevel = reinforcementLevel;
    }
       
}

[System.Serializable]
public class EnchantState
{
    public string enchantId;
    public int level;
    public EnchantState(string enchantId, int level)
    {
        this.enchantId = enchantId;
        this.level = level;
    }
}


