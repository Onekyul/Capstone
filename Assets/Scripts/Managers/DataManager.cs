using System;
using UnityEngine;
using System.IO;
using System.Text;

using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public PlayerData currentPlayer;
    private string savePath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        savePath = Path.Combine(Application.persistentDataPath, "save.dat");
        LoadGame();
    }


    //데이터 세이브 및 로드 
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentPlayer);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string code = Convert.ToBase64String(bytes);

        File.WriteAllText(savePath, code);
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string code = File.ReadAllText(savePath);
            byte[] bytes = Convert.FromBase64String(code);
            string json = Encoding.UTF8.GetString(bytes);
            currentPlayer = JsonUtility.FromJson<PlayerData>(json);
        }
        else
        {
            currentPlayer = new PlayerData();
            SaveGame();
        }
    }

    //인벤토리 추가 및 사용
    public void AddInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);

        if (slot != null)
        {
            slot.count += amount; // 이미 있으면 개수 증가
        }
        else
        {
            currentPlayer.Inventory.Add(new InventorySlot(id, amount)); // 없으면 새로 추가
        }
        
        // 데이터가 변했으니 저장
        SaveGame();
    }
    public bool HasInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);
        return slot != null && slot.count >= amount;// 사용가능인지 판별
    }
    
    public bool UseInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);
        
        if (slot == null || slot.count < amount)
        {
            return false; // 재료 부족
        }

        slot.count -= amount;
        SaveGame();
        return true;
    }
    
    //무기 및 방어구 장착
    public void EquipWeapon(string weaponId)
    {
        if (currentPlayer.ownedWeapons.Exists(w => w.itemId == weaponId))
        {
            currentPlayer.equippedWeaponId = weaponId;
            SaveGame();
        }
    }
    
    public void EquipArmor(string armorId, ArmorType type)
    {
        if (currentPlayer.ownedArmors.Exists(a => a.itemId == armorId))
        {
            switch (type)
            {
                case ArmorType.Helmet: currentPlayer.equippedHelmetId = armorId; break;
                case ArmorType.Armor:  currentPlayer.equippedArmorId = armorId; break;
                case ArmorType.Boots:  currentPlayer.equippedBootsId = armorId; break;
            }
            SaveGame();
        }
    }
    
    
    //SO getter
    public WeaponData GetWeaponData(string id)
    {
        // Resources/Data/Items/Weapons 폴더 안에 있는 파일 로드
        return Resources.Load<WeaponData>($"Data/Items/Weapon/Sword/{id}");
    }
    public ArmorData GetArmorData(string id)
    {
        ArmorData data = Resources.Load<ArmorData>($"Data/Items/Armor/Helmet/{id}");
        if (data != null) return data;
        
        data = Resources.Load<ArmorData>($"Data/Items/Armor/Armor/{id}");
        if (data != null) return data;
        
        data = Resources.Load<ArmorData>($"Data/Items/Armor/Boots/{id}");
        if (data != null) return data;
        
        Debug.LogError($"[GetArmorData] 방어구 데이터를 찾을 수 없습니다. ID: {id}\n(검색 경로: Data/Items/Armor/ 하위의 Helmet, Armor, Boots 폴더)");
        return null;
    }
    public ItemData GetMaterialData(string id)
    {
        ItemData data = null;
        data = Resources.Load<ItemData>($"Data/Items/Material/Enchant/{id}");
        if (data != null) return data;
        
        data = Resources.Load<ItemData>($"Data/Items/Material/Equip/{id}");
        if (data != null) return data;
        
        data = Resources.Load<ItemData>($"Data/Items/Material/{id}");
        
        if (data == null)
        {
            Debug.LogWarning($"[DataManager] 재료 아이템을 찾을 수 없습니다. ID: {id}, 경로들을 확인해보세요.");
        }

        return data;
    }
    public EnchantData GetEnchantData(string id)
    {
        return Resources.Load<EnchantData>($"Data/Enchant/{id}");
    }
    
    //현재 레벨 조회
    public int GetItemLevel(string id)
    {
        var weapon = currentPlayer.ownedWeapons.Find(w => w.itemId == id);
        if (weapon != null) return weapon.reinforcementLevel;

        var armor = currentPlayer.ownedArmors.Find(a => a.itemId == id);
        if (armor != null) return armor.reinforcementLevel;

        return 0;
    }

    public int GetEnchantLevel(string id)
    {
        var enchant = currentPlayer.unlockedEnchants.Find(e => e.enchantId == id);
        return (enchant != null) ? enchant.level : 0;
    }

    
    //강화
    public bool TryUpgradeItem(string id)
    {
        UpgradeTable table = null;
        string itemName = "";

       
        WeaponData wData = GetWeaponData(id);
        if (wData != null)
        {
            table = wData.upgradeTable;
            itemName = wData.weaponName;
        }
        else
        {
           
            ArmorData aData = GetArmorData(id);
            if (aData != null)
            {
                table = aData.upgradeTable;
                itemName = aData.armorName;
            }
        }
        
        if (table == null)
        {
            Debug.LogError($"강화 테이블을 찾을 수 없습니다: {id}");
            return false;
        }
        
        int currentLevel = GetItemLevel(id);
        var nextStep = table.GetNextStep(currentLevel);

        if (nextStep == null)
        {
            Debug.Log("이미 최고 레벨입니다.");
            return false; // 더 이상 강화 불가
        }
        
        string matId = nextStep.requiredMaterial.itemId;
        int matCount = nextStep.materialCount;

        if (!HasInventory(matId, matCount))
        {
            return false;
        }
        
        UseInventory(matId, matCount);
        
        int randomVal = UnityEngine.Random.Range(0, 100);

        if (randomVal < nextStep.successRate)
        {
            ApplyLevelUpInternal(id);
            Debug.Log($"[강화 성공] {itemName} (+{currentLevel + 1})");
        }
        else
        {
            Debug.Log($"[강화 실패] {itemName}...");
        }

        SaveGame(); 
        return true;
    }
    
    private void ApplyLevelUpInternal(string id)
    {
        var weapon = currentPlayer.ownedWeapons.Find(w => w.itemId == id);
        if (weapon != null)
        {
            weapon.reinforcementLevel++;
            return;
        }

        var armor = currentPlayer.ownedArmors.Find(a => a.itemId == id);
        if (armor != null)
        {
            armor.reinforcementLevel++;
            return;
        }
    }
    
    public int GetInventoryCount(string itemId)
    {
        if (currentPlayer == null || currentPlayer.Inventory == null) return 0;
    
        var slot = currentPlayer.Inventory.Find(x => x.itemId == itemId);
        return slot != null ? slot.count : 0;
    }
    
    
    //인챈트
    public bool TryEnhanceEnchant(string enchantId)
    {
        // 1. 데이터 로드
        EnchantData data = GetEnchantData(enchantId);
        if (data == null)
        {
            Debug.LogError($"[DataManager] 인챈트 데이터를 찾을 수 없음: {enchantId}");
            return false;
        }

        // 2. 현재 레벨 확인 (0 = 미해금)
        int currentLevel = GetEnchantLevel(enchantId);

        // 3. 다음 레벨(목표 레벨) 정보 가져오기
        // 예: 현재 0레벨 -> 1레벨(해금) 정보를 가져옴
        var nextStep = data.GetNextLevelInfo(currentLevel);

        // 다음 단계가 없으면 이미 만렙
        if (nextStep == null)
        {
            Debug.Log("이미 최고 레벨입니다.");
            return false;
        }

        // 4. 재료 부족 여부 확인 (중요: 소모하기 전에 먼저 검사!)
        foreach (var matCost in nextStep.requiredMaterials)
        {
            // 재료 ID와 개수 체크
            if (!HasInventory(matCost.material.itemId, matCost.count))
            {
                Debug.Log($"재료 부족: {matCost.material.itemName} ({matCost.count}개 필요)");
                return false;
            }
        }

        // =========================================================
        // [실행] 재료가 모두 있으므로 소모 시작
        // =========================================================
        
        // 5. 재료 소모
        foreach (var matCost in nextStep.requiredMaterials)
        {
            UseInventory(matCost.material.itemId, matCost.count);
        }

        // 6. 확률 계산 (0 ~ 99)
        int randomVal = UnityEngine.Random.Range(0, 100);

        if (randomVal < nextStep.successRate)
        {
            // [성공] 레벨 업 (또는 해금) 적용
            ApplyEnchantLevelUp(enchantId);
            
            // 로그 메시지 분기 (해금 vs 강화)
            if (currentLevel == 0)
                Debug.Log($"[인챈트 해금 성공!] {data.enchantName} 습득!");
            else
                Debug.Log($"[인챈트 강화 성공!] {data.enchantName} (+{currentLevel} -> +{currentLevel + 1})");
        }
        else
        {
            // [실패] (재료만 소모되고 끝)
            Debug.Log($"[인챈트 실패...] {data.enchantName} 강화에 실패했습니다.");
        }

        // 7. 결과 저장
        SaveGame();
        
        // 시도는 성공했음 (결과가 성공/실패인지는 별개)
        return true; 
    }
    private void ApplyEnchantLevelUp(string id)
    {
        // 이미 보유 중인 인챈트인지 확인
        var enchant = currentPlayer.unlockedEnchants.Find(e => e.enchantId == id);
        
        if (enchant != null)
        {
            // 있으면 레벨 증가
            enchant.level++;
        }
        else
        {
            // 없으면 새로 추가 (해금, 1레벨 시작)
            currentPlayer.unlockedEnchants.Add(new EnchantState(id, 1));
        }
    }
    public string GetEquippedItemId(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Weapon: return currentPlayer.equippedWeaponId;
            case EquipmentType.Helmet: return currentPlayer.equippedHelmetId;
            case EquipmentType.Armor:  return currentPlayer.equippedArmorId;
            case EquipmentType.Boots:  return currentPlayer.equippedBootsId;
            default: return "";
        }
    }

    public void ClearALlData()
    {
        currentPlayer.ownedArmors.Clear();
        currentPlayer.ownedWeapons.Clear();
        currentPlayer.unlockedEnchants.Clear();
    }
    
    
}
