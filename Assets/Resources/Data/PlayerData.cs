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
    // 기존 단일 필드는 하위 호환성을 위해 유지 (활 기본값)
    public string equippedWeaponId = "bow_wood";
    
    // ✨ 무기 타입별 장착 ID (각 무기 타입의 마지막 사용 무기 기억)
    public string equippedSwordId = "sword_wood";
    public string equippedSpearId = "spear_wood";
    public string equippedBowId = "bow_wood";
    
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
        // 기본 무기들 추가 (검, 창, 활)
        ownedWeapons.Add(new EquipmentState("sword_wood", 0));
        ownedWeapons.Add(new EquipmentState("spear_wood", 0));
        ownedWeapons.Add(new EquipmentState("bow_wood", 0));
        
        ownedArmors.Add(new EquipmentState("helmet_wood", 0));
        ownedArmors.Add(new EquipmentState("armor_wood", 0));
        ownedArmors.Add(new EquipmentState("boots_wood", 0));
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


