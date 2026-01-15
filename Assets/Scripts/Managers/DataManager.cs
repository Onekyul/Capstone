using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public PlayerData currentPlayer;
    private string baseUrl = "http://localhost:7001/api";
    public int MyUserId { get; private set; }

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

        if (currentPlayer == null) currentPlayer = new PlayerData();
    }
    
    // ==================================================================================
    // [server] 로그인 & 데이터 로드
    // ==================================================================================
    public void InitializeNetwork(Action onComplete)
    {
        // 디바이스 ID로 게스트 로그인, 게임 시작시 호출
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        StartCoroutine(CoGuestLogin(deviceId, onComplete));
    }

    IEnumerator CoGuestLogin(string deviceId, Action onComplete)
    {
        Debug.Log("[SERVER] GuestLogin..");
        string json = $"{{\"deviceId\":\"{deviceId}\"}}";

        using (UnityWebRequest req = CreatePostRequest(baseUrl + "/Auth/guest-login", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<LoginResponseDto>(req.downloadHandler.text);
                MyUserId = res.userId;
                Debug.Log($"✅ [Server] 로그인 성공 ID: {MyUserId}, 닉네임: {res.nickname}");

                StartCoroutine(CoLoadGame(onComplete));
            }
            else
            {
                Debug.LogError($"[Server] 로그인 실패: {req.error}");
            }
        }
    }
    
    IEnumerator CoLoadGame(Action onComplete)
    {
        string json = $"{{\"userId\":{MyUserId}}}";

        using (UnityWebRequest req = CreatePostRequest(baseUrl + "/Game/load", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"📥 [Server] 데이터 로드 완료");
                
                // 1. 서버 DTO 받기
                GameDataDto serverData = JsonUtility.FromJson<GameDataDto>(req.downloadHandler.text);
                
                // 2. 로컬 PlayerData로 변환 (여기서 내부 함수 사용)
                ApplyServerDataToLocal(serverData);
                
                onComplete?.Invoke();
            }
            else
            {
                Debug.LogError($" [Server] 데이터 로드 실패: {req.error}");
            }
        }
    }
    
    
    // ==================================================================================
    // [Server] 데이터 저장 (Save)
    // ==================================================================================
    
    public void SaveGame()
    {
        if (MyUserId == 0) return;
        StartCoroutine(CoSaveGame());
    }
    
    IEnumerator CoSaveGame()
    {
        GameDataDto dataToSend = ConvertLocalToServerData();
        string json = JsonUtility.ToJson(dataToSend);

        using (UnityWebRequest req = CreatePostRequest(baseUrl + "/Game/save", json))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                // 아직 서버에 Save API가 없으면 실패하는 게 정상
            }
        }
    }

    // ==================================================================================
    // [Mapping] 데이터 변환 (서버 DTO <-> 로컬 PlayerData)
    // ==================================================================================
  // 서버 DTO -> 로컬 데이터 적용
    private void ApplyServerDataToLocal(GameDataDto serverData)
    {
        // 1. 인벤토리 동기화
        currentPlayer.Inventory.Clear();
        foreach (var itemDto in serverData.inventory)
        {
            // ResourceManager 대신 내부에 있는 GetMaterialData 사용
            ItemData itemSO = GetMaterialData(itemDto.id);
            if (itemSO != null)
            {
                currentPlayer.Inventory.Add(new InventorySlot(itemDto.id, itemDto.count));
            }
        }

        // 2. 인챈트 동기화
        currentPlayer.unlockedEnchants.Clear();
        foreach (var enchantDto in serverData.enchants)
        {
            EnchantData enchantSO = GetEnchantData(enchantDto.id);
            if (enchantSO != null)
            {
                currentPlayer.unlockedEnchants.Add(new EnchantState(enchantDto.id, enchantDto.level));
            }
        }

        // 3. 장착 정보 및 장비 리스트 동기화
        if (serverData.equip != null)
        {
            currentPlayer.equippedWeaponId = serverData.equip.weapon;
            currentPlayer.equippedHelmetId = serverData.equip.helmet;
            currentPlayer.equippedArmorId = serverData.equip.armor;
            currentPlayer.equippedBootsId = serverData.equip.boots;

            if (!string.IsNullOrEmpty(currentPlayer.equippedWeaponId))
            {
                int type = GetWeaponTypeFromId(currentPlayer.equippedWeaponId);
                EquipWeaponByType(currentPlayer.equippedWeaponId, type);
            }
        }
        
        // 보유 장비 리스트 처리 (서버에서 받은 목록 -> 로컬 목록)
        currentPlayer.ownedWeapons.Clear();
        currentPlayer.ownedArmors.Clear();
        
        foreach(var equipDto in serverData.equipments)
        {
            // 무기인지 체크
            WeaponData wData = GetWeaponData(equipDto.id);
            if(wData != null)
            {
                currentPlayer.ownedWeapons.Add(new EquipmentState(equipDto.id, equipDto.level));
                continue;
            }
            
            // 방어구인지 체크
            ArmorData aData = GetArmorData(equipDto.id);
            if(aData != null)
            {
                currentPlayer.ownedArmors.Add(new EquipmentState(equipDto.id, equipDto.level));
            }
        }
    }

    private GameDataDto ConvertLocalToServerData()
    {
        GameDataDto data = new GameDataDto();
        data.userId = MyUserId;
        
        foreach (var slot in currentPlayer.Inventory)
            data.inventory.Add(new ItemDto(slot.itemId, slot.count));

        foreach (var enchant in currentPlayer.unlockedEnchants)
            data.enchants.Add(new EnchantDto(enchant.enchantId, enchant.level));
        
        data.equip = new EquipDto();
        data.equip.weapon = currentPlayer.equippedWeaponId;
        data.equip.helmet = currentPlayer.equippedHelmetId;
        data.equip.armor = currentPlayer.equippedArmorId;
        data.equip.boots = currentPlayer.equippedBootsId;

        foreach(var w in currentPlayer.ownedWeapons)
        {
            data.equipments.Add(new EquipItemDto(w.itemId, w.reinforcementLevel));
        }
            
        foreach(var a in currentPlayer.ownedArmors)
        {
            data.equipments.Add(new EquipItemDto(a.itemId, a.reinforcementLevel));
        }
        return data;
    }
    
    private UnityWebRequest CreatePostRequest(string url, string json)
    {
        var req = UnityWebRequest.PostWwwForm(url, json);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
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
        if (slot.count <= 0)
        {
            currentPlayer.Inventory.Remove(slot);
        }
        SaveGame();
        return true;
    }
    
    //무기 및 방어구 장착
    public void EquipWeapon(string weaponId)
    {
        if (currentPlayer.ownedWeapons.Exists(w => w.itemId == weaponId))
        {
            currentPlayer.equippedWeaponId = weaponId;
            
            //무기 타입 판단하여 해당 타입의 equippedId도 업데이트
            int weaponType = GetWeaponTypeFromId(weaponId);
            switch (weaponType)
            {
                case 0: currentPlayer.equippedSwordId = weaponId; break;
                case 1: currentPlayer.equippedSpearId = weaponId; break;
                case 2: currentPlayer.equippedBowId = weaponId; break;
            }
            
            SaveGame();
        }
    }
    
    //무기 타입별 장착 메서드 
    public void EquipWeaponByType(string weaponId, int weaponType)
    {
        if (!currentPlayer.ownedWeapons.Exists(w => w.itemId == weaponId))
        {
            Debug.LogWarning($"[DataManager] 보유하지 않은 무기: {weaponId}");
            return;
        }
        
        switch (weaponType)
        {
            case 0: currentPlayer.equippedSwordId = weaponId; break;
            case 1: currentPlayer.equippedSpearId = weaponId; break;
            case 2: currentPlayer.equippedBowId = weaponId; break;
        }
        
        // 하위 호환성을 위해 equippedWeaponId도 업데이트
        currentPlayer.equippedWeaponId = weaponId;
        SaveGame();
        
        Debug.Log($"[DataManager] 무기 장착: {weaponId} (타입: {weaponType})");
    }
    
    // 무기 타입별 현재 장착 무기 ID 조회
    public string GetEquippedWeaponId(int weaponType)
    {
        switch (weaponType)
        {
            case 0: return currentPlayer.equippedSwordId;
            case 1: return currentPlayer.equippedSpearId;
            case 2: return currentPlayer.equippedBowId;
            default: 
                Debug.LogWarning($"[DataManager] 잘못된 무기 타입: {weaponType}");
                return "";
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
        WeaponData data = Resources.Load<WeaponData>($"Data/Items/Weapon/Sword/{id}");
        if (data != null) return data;
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Bow/{id}");
        if (data != null) return data;
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Spear/{id}");
        return data;
    }
    
    // 무기 ID로 무기 타입 판단 (0=Sword, 1=Spear, 2=Bow)
    public int GetWeaponTypeFromId(string weaponId)
    {
        // Sword 폴더에서 찾기
        WeaponData data = Resources.Load<WeaponData>($"Data/Items/Weapon/Sword/{weaponId}");
        if (data != null) return 0; // Sword
        
        // Spear 폴더에서 찾기
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Spear/{weaponId}");
        if (data != null) return 1; // Spear
        
        // Bow 폴더에서 찾기
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Bow/{weaponId}");
        if (data != null) return 2; // Bow
        
        // 못 찾으면 기본값 0 (Sword) 반환
        Debug.LogWarning($"[DataManager] 무기 타입을 찾을 수 없음: {weaponId}, 기본값(Sword) 반환");
        return 0;
    }
    public ArmorData GetArmorData(string id)
    {
        ArmorData data = Resources.Load<ArmorData>($"Data/Items/Armor/Helmet/{id}");
        if (data != null) return data;
        data = Resources.Load<ArmorData>($"Data/Items/Armor/Armor/{id}");
        if (data != null) return data;
        data = Resources.Load<ArmorData>($"Data/Items/Armor/Boots/{id}");
        return data;
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

    // 인챈트 레벨 직접 설정 (치트키용)
    public void SetEnchantLevel(string id, int level)
    {
        var enchant = currentPlayer.unlockedEnchants.Find(e => e.enchantId == id);
        
        if (enchant != null)
        {
            // 이미 존재하면 레벨 변경
            enchant.level = Mathf.Max(0, level); // 음수 방지
        }
        else if (level > 0)
        {
            // 존재하지 않고 레벨이 0보다 크면 새로 추가
            currentPlayer.unlockedEnchants.Add(new EnchantState(id, level));
        }
        
        SaveGame();
        Debug.Log($"[DataManager] {id} 인챈트 레벨 설정: {level}");
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
        EnchantData data = GetEnchantData(enchantId);
        if (data == null)
        {
            Debug.LogError($"[DataManager] 인챈트 데이터를 찾을 수 없음: {enchantId}");
            return false;
        }
        
        int currentLevel = GetEnchantLevel(enchantId);
        
        var nextStep = data.GetNextLevelInfo(currentLevel);
        
        if (nextStep == null)
        {
            Debug.Log("이미 최고 레벨입니다.");
            return false;
        }
        
        foreach (var matCost in nextStep.requiredMaterials)
        {
            // 재료 ID와 개수 체크
            if (!HasInventory(matCost.material.itemId, matCost.count))
            {
                Debug.Log($"재료 부족: {matCost.material.itemName} ({matCost.count}개 필요)");
                return false;
            }
        }
        
        foreach (var matCost in nextStep.requiredMaterials)
        {
            UseInventory(matCost.material.itemId, matCost.count);
        }
        
        int randomVal = UnityEngine.Random.Range(0, 100);

        if (randomVal < nextStep.successRate)
        {
            ApplyEnchantLevelUp(enchantId);
            
            if (currentLevel == 0)
                Debug.Log($"[인챈트 해금 성공!] {data.enchantName} 습득!");
            else
                Debug.Log($"[인챈트 강화 성공!] {data.enchantName} (+{currentLevel} -> +{currentLevel + 1})");
        }
        else
        {
            Debug.Log($"[인챈트 실패...] {data.enchantName} 강화에 실패했습니다.");
        }
        
        SaveGame();
        
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
