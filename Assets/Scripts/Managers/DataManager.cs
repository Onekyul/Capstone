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
    
    // 게임 데이터 관련 API 주소
    private string baseUrl = "http://localhost:7200/api/Game"; 
    //강화 관련 API 주소
    private string upgradeUrl = "http://localhost:7200/api/Upgrade/attempt";
    
    public int MyUserId => SessionManager.Instance != null ? SessionManager.Instance.UserId : 0;

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
    // [server] 초기화: 로그인(Session) -> 데이터로드(Data) 순차 실행
    // ==================================================================================
    public void InitializeNetwork(Action onComplete)
    {
        // 1. SessionManager가 있는지 확인
        if (SessionManager.Instance == null)
        {
            Debug.LogError(" No SessionManager");
            return;
        }

        // 2. 로그인 요청 위임
        SessionManager.Instance.Login((isSuccess) => 
        {
            if (isSuccess)
            {
                // 3. 로그인 성공 시 내 데이터 로드 시작
                StartCoroutine(CoLoadGame(onComplete));
            }
            else
            {
                Debug.LogError("로그인 실패로 인해 게임 데이터를 로드하지 못했습니다.");
            }
        });
    }

    IEnumerator CoLoadGame(Action onComplete)
    {
        // SessionManager를 통해 얻은 ID 사용
        string json = $"{{\"userId\":{MyUserId}}}";

        using (UnityWebRequest req = CreatePostRequest(baseUrl + "/load", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"📥 [Server] 데이터 로드 완료");
                
                // 1. 서버 DTO 받기
                GameDataDto serverData = JsonUtility.FromJson<GameDataDto>(req.downloadHandler.text);
                
                // 2. 로컬 PlayerData로 변환
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

        using (UnityWebRequest req = CreatePostRequest(baseUrl + "/save", json))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                // 실패 처리
                Debug.LogWarning($"저장 실패: {req.error}");
            }
        }
    }

    // ==================================================================================
    // [Mapping] 데이터 변환 및 헬퍼 함수들 
    // ==================================================================================
    
    private UnityWebRequest CreatePostRequest(string url, string json)
    {
        var req = UnityWebRequest.PostWwwForm(url, json);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }
    

    private void ApplyServerDataToLocal(GameDataDto serverData)
    {
        currentPlayer.Inventory.Clear();
        foreach (var itemDto in serverData.inventory)
        {
            ItemData itemSO = GetMaterialData(itemDto.id);
            if (itemSO != null) currentPlayer.Inventory.Add(new InventorySlot(itemDto.id, itemDto.count));
        }

        currentPlayer.unlockedEnchants.Clear();
        foreach (var enchantDto in serverData.enchants)
        {
            EnchantData enchantSO = GetEnchantData(enchantDto.id);
            if (enchantSO != null) currentPlayer.unlockedEnchants.Add(new EnchantState(enchantDto.id, enchantDto.level));
        }

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
        
        currentPlayer.ownedWeapons.Clear();
        currentPlayer.ownedArmors.Clear();
        
        foreach(var equipDto in serverData.equipments)
        {
            WeaponData wData = GetWeaponData(equipDto.id);
            if(wData != null)
            {
                currentPlayer.ownedWeapons.Add(new EquipmentState(equipDto.id, equipDto.level));
                continue;
            }
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
            data.equipments.Add(new EquipItemDto(w.itemId, w.reinforcementLevel));
            
        foreach(var a in currentPlayer.ownedArmors)
            data.equipments.Add(new EquipItemDto(a.itemId, a.reinforcementLevel));
            
        return data;
    }

    // --- 인벤토리/강화 로직 등 기존 메서드 유지 ---
    public void AddInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);
        if (slot != null) slot.count += amount; 
        else currentPlayer.Inventory.Add(new InventorySlot(id, amount)); 
        SaveGame();
    }
    
    public bool HasInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);
        return slot != null && slot.count >= amount;
    }
    
    public bool UseInventory(string id, int amount)
    {
        var slot = currentPlayer.Inventory.Find(x => x.itemId == id);
        if (slot == null || slot.count < amount) return false; 
        slot.count -= amount;
        if (slot.count <= 0) currentPlayer.Inventory.Remove(slot);
        SaveGame();
        return true;
    }
    
    public void EquipWeapon(string weaponId)
    {
        if (currentPlayer.ownedWeapons.Exists(w => w.itemId == weaponId))
        {
            currentPlayer.equippedWeaponId = weaponId;
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
        currentPlayer.equippedWeaponId = weaponId;
        SaveGame();
        Debug.Log($"[DataManager] 무기 장착: {weaponId} (타입: {weaponType})");
    }
    
    public string GetEquippedWeaponId(int weaponType)
    {
        switch (weaponType)
        {
            case 0: return currentPlayer.equippedSwordId;
            case 1: return currentPlayer.equippedSpearId;
            case 2: return currentPlayer.equippedBowId;
            default: return "";
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
    
    // SO Getter Methods
    public WeaponData GetWeaponData(string id)
    {
        WeaponData data = Resources.Load<WeaponData>($"Data/Items/Weapon/Sword/{id}");
        if (data != null) return data;
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Bow/{id}");
        if (data != null) return data;
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Spear/{id}");
        return data;
    }
    
    public int GetWeaponTypeFromId(string weaponId)
    {
        WeaponData data = Resources.Load<WeaponData>($"Data/Items/Weapon/Sword/{weaponId}");
        if (data != null) return 0; 
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Spear/{weaponId}");
        if (data != null) return 1; 
        data = Resources.Load<WeaponData>($"Data/Items/Weapon/Bow/{weaponId}");
        if (data != null) return 2; 
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
        ItemData data = Resources.Load<ItemData>($"Data/Items/Material/Enchant/{id}");
        if (data != null) return data;
        data = Resources.Load<ItemData>($"Data/Items/Material/Equip/{id}");
        if (data != null) return data;
        data = Resources.Load<ItemData>($"Data/Items/Material/{id}");
        if (data == null) Debug.LogWarning($"[DataManager] 재료 아이템 없음: {id}");
        return data;
    }

    public EnchantData GetEnchantData(string id)
    {
        return Resources.Load<EnchantData>($"Data/Enchant/{id}");
    }
    
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

    public void SetEnchantLevel(string id, int level)
    {
        var enchant = currentPlayer.unlockedEnchants.Find(e => e.enchantId == id);
        if (enchant != null) enchant.level = Mathf.Max(0, level);
        else if (level > 0) currentPlayer.unlockedEnchants.Add(new EnchantState(id, level));
        SaveGame();
    }
    
    // ==================================================================================
    // 서버 통신 공통 함수 
    // ==================================================================================
    private IEnumerator CoSendUpgradeRequest(string targetId, string matInfo, float successRate, Action<bool, string> onComplete)
    {
        // 서버로 보낼 DTO
        var reqDto = new UpgradeReqDto
        {
            userId = MyUserId,
            targetId = targetId,
            materialInfo = matInfo,
            successRate = successRate
        };

        string json = JsonUtility.ToJson(reqDto);

        using (UnityWebRequest req = CreatePostRequest(upgradeUrl, json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var resDto = JsonUtility.FromJson<UpgradeResDto>(req.downloadHandler.text);
                onComplete?.Invoke(resDto.success, resDto.message);
            }
            else
            {
                Debug.LogError($"[통신 에러] {req.error}");
                onComplete?.Invoke(false, "서버 통신 실패");
            }
        }
    }
    
    
    

    public void TryUpgradeItem(string id, Action<bool, string> onComplete)
    {
        UpgradeTable table = null;
        string itemName = "";
        WeaponData wData = GetWeaponData(id);
        
        if (wData != null) { table = wData.upgradeTable; itemName = wData.weaponName; }
        else {
            ArmorData aData = GetArmorData(id);
            if (aData != null) { table = aData.upgradeTable; itemName = aData.armorName; }
        }
        
        if (table == null) { onComplete?.Invoke(false, "강화 데이터를 찾을 수 없습니다."); return; }
        
        int currentLevel = GetItemLevel(id);
        var nextStep = table.GetNextStep(currentLevel);
        if (nextStep == null) { onComplete?.Invoke(false, "최대 레벨입니다."); return; }
        
        string matId = nextStep.requiredMaterial.itemId;
        int matCount = nextStep.materialCount;

        // 1. 로컬 인벤토리 검증
        if (!HasInventory(matId, matCount)) { onComplete?.Invoke(false, "재료가 부족합니다."); return; }

        // 2. 서버 통신 (확률을 0.0 ~ 1.0 형태로 변환해서 보냄)
        float rate = nextStep.successRate / 100f; 
        string matInfo = $"{matId} {matCount}개";

        StartCoroutine(CoSendUpgradeRequest(id, matInfo, rate, (isSuccess, msg) => 
        {
            // 통신이 끝난 후 무조건 재료 깎음
            UseInventory(matId, matCount);

            if (isSuccess) 
            {
                ApplyLevelUpInternal(id);
                Debug.Log($"[강화 성공] {itemName} (+{currentLevel + 1})");
            }
            else 
            {
                Debug.Log($"[강화 실패] {itemName}...");
            }

            // ★ 결과 적용 후 한 번만 SaveGame 호출 (인벤토리 깎인 거 + 레벨업 덮어쓰기)
            SaveGame(); 
            onComplete?.Invoke(isSuccess, msg);
        }));
    }
    
    private void ApplyLevelUpInternal(string id)
    {
        var weapon = currentPlayer.ownedWeapons.Find(w => w.itemId == id);
        if (weapon != null) { weapon.reinforcementLevel++; return; }
        var armor = currentPlayer.ownedArmors.Find(a => a.itemId == id);
        if (armor != null) { armor.reinforcementLevel++; return; }
    }
    
    public int GetInventoryCount(string itemId)
    {
        if (currentPlayer == null || currentPlayer.Inventory == null) return 0;
        var slot = currentPlayer.Inventory.Find(x => x.itemId == itemId);
        return slot != null ? slot.count : 0;
    }
    
    public void TryEnhanceEnchant(string enchantId, Action<bool, string> onComplete)
    {
        EnchantData data = GetEnchantData(enchantId);
        if (data == null) { onComplete?.Invoke(false, "인챈트 정보 없음"); return; }
        
        int currentLevel = GetEnchantLevel(enchantId);
        var nextStep = data.GetNextLevelInfo(currentLevel);
        if (nextStep == null) { onComplete?.Invoke(false, "최대 레벨"); return; }
        
        // 1. 로컬 인벤토리 검증 (필요한 재료가 여러 개일 수 있으니 모두 검사)
        foreach (var matCost in nextStep.requiredMaterials)
        {
            if (!HasInventory(matCost.material.itemId, matCost.count)) 
            {
                onComplete?.Invoke(false, "재료가 부족합니다."); 
                return;
            }
        }
        
        // 2. 서버 통신 세팅
        float rate = nextStep.successRate / 100f;
        string matInfo = "인챈트 재료 묶음"; // 로그용 텍스트
        
        StartCoroutine(CoSendUpgradeRequest(enchantId, matInfo, rate, (isSuccess, msg) => 
        {
            // 통신 끝난 후 모든 재료 차감
            foreach (var matCost in nextStep.requiredMaterials)
            {
                UseInventory(matCost.material.itemId, matCost.count);
            }

            if (isSuccess)
            {
                ApplyEnchantLevelUp(enchantId);
                Debug.Log($"[인챈트 성공] {data.enchantName}");
            }
            else
            {
                Debug.Log($"[인챈트 실패] {data.enchantName}");
            }

            // ★ 통신 끝난 후 최종 저장
            SaveGame();
            onComplete?.Invoke(isSuccess, msg);
        }));
    }

    private void ApplyEnchantLevelUp(string id)
    {
        var enchant = currentPlayer.unlockedEnchants.Find(e => e.enchantId == id);
        if (enchant != null) enchant.level++;
        else currentPlayer.unlockedEnchants.Add(new EnchantState(id, 1));
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