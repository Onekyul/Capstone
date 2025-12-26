using UnityEngine;

/// 게임 시작 시 저장된 강화 데이터를 실제 장비에 반영하는 클래스
/// Player GameObject에 붙여서 사용합니다.
public class EquipmentInitializer : MonoBehaviour
{
    private bool isInitialized = false;

    private void Start()
    {
        // DataManager가 로드될 때까지 약간 대기
        Invoke(nameof(InitializeEquipmentStats), 0.5f);
    }

    private void InitializeEquipmentStats()
    {
        if (isInitialized)
        {
            Debug.LogWarning("장비는 이미 초기화되었습니다.");
            return;
        }

        // DataManager 확인
        if (DataManager.instance == null || DataManager.instance.currentPlayer == null)
        {
            Debug.LogError("DataManager가 준비되지 않았습니다!");
            return;
        }

        PlayerData data = DataManager.instance.currentPlayer;
        
        // 1. 무기 초기화
        InitializeWeapons(data);
        
        // 2. 방어구 초기화
        InitializeArmors(data);
        
        isInitialized = true;
        Debug.Log("[장비 초기화 완료] 저장된 강화 수치가 적용되었습니다.");
    }


    /// 보유 중인 모든 무기의 강화 수치를 반영합니다.
    private void InitializeWeapons(PlayerData data)
    {
        AttackManager attackManager = GetComponent<AttackManager>();
        if (attackManager == null)
        {
            Debug.LogError("AttackManager를 찾을 수 없습니다!");
            return;
        }

        // 모든 보유 무기를 순회
        foreach (var weaponState in data.ownedWeapons)
        {
            string weaponId = weaponState.itemId;
            int reinforcementLevel = weaponState.reinforcementLevel;
            
            if (reinforcementLevel <= 0) continue; // 강화되지 않은 무기는 스킵
            
            // 해당 무기 오브젝트 찾기
            WeaponBase weapon = FindWeaponByName(attackManager, weaponId);
            
            if (weapon != null)
            {
                // 강화 레벨만큼 공격력 증가
                // 주의: UpgradeAttackDamage()를 여러 번 호출하면 로그가 많이 찍힐 수 있음
                float totalBonus = reinforcementLevel * 1f; // 레벨당 +1 공격력
                weapon.UpgradeAttackDamage(totalBonus);
                
                Debug.Log($"[무기 초기화] {weaponId} +{reinforcementLevel} | 공격력: {weapon.GetBaseDamage()}");
            }
            else
            {
                Debug.LogWarning($"무기를 찾을 수 없습니다: {weaponId}");
            }
        }
    }


    /// 보유 중인 방어구의 강화 수치를 PlayerStats에 반영합니다.
    private void InitializeArmors(PlayerData data)
    {
        PlayerStats playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats를 찾을 수 없습니다!");
            return;
        }

        // 현재 장착 중인 방어구만 적용
        ApplyArmorBonus(data.equippedHelmetId, playerStats);
        ApplyArmorBonus(data.equippedArmorId, playerStats);
        ApplyArmorBonus(data.equippedBootsId, playerStats);
    }

   
    /// 특정 방어구의 보너스를 PlayerStats에 적용
    private void ApplyArmorBonus(string armorId, PlayerStats playerStats)
    {
        if (string.IsNullOrEmpty(armorId)) return;
        
        // 강화 레벨 가져오기
        int reinforcementLevel = DataManager.instance.GetItemLevel(armorId);
        if (reinforcementLevel <= 0) return; // 강화되지 않은 방어구는 스킵
        
        // ArmorData 로드
        ArmorData armorData = DataManager.instance.GetArmorData(armorId);
        if (armorData == null)
        {
            Debug.LogWarning($"방어구 데이터를 찾을 수 없습니다: {armorId}");
            return;
        }
        
        // 방어구 타입에 따라 다른 스탯 적용
        switch (armorData.armorType)
        {
            case ArmorType.Helmet:
                // 투구: 체력 보너스 (레벨당 +5)
                float helmetHpBonus = reinforcementLevel * 5f;
                float helmetMultiplier = 1 + (helmetHpBonus * 0.01f); // 퍼센트로 변환
                playerStats.ModifyMaxHP(helmetMultiplier);
                Debug.Log($"[투구 초기화] {armorId} +{reinforcementLevel} | 체력 +{helmetHpBonus}%");
                break;
                
            case ArmorType.Armor:
                // 갑옷: 체력 보너스 (레벨당 +10)
                float armorHpBonus = reinforcementLevel * 10f;
                float armorMultiplier = 1 + (armorHpBonus * 0.01f);
                playerStats.ModifyMaxHP(armorMultiplier);
                Debug.Log($"[갑옷 초기화] {armorId} +{reinforcementLevel} | 체력 +{armorHpBonus}%");
                break;
                
            case ArmorType.Boots:
                // 신발: 속도 보너스 (레벨당 +0.05)
                float speedBonus = reinforcementLevel * 0.05f;
                playerStats.ModifyMoveSpeed(speedBonus, true);
                Debug.Log($"[신발 초기화] {armorId} +{reinforcementLevel} | 속도 +{speedBonus}");
                break;
        }
    }
    
    /// 무기 ID로 무기 오브젝트를 찾습니다.
    private WeaponBase FindWeaponByName(AttackManager attackManager, string weaponId)
    {
        WeaponBase[] allWeapons = attackManager.GetAllWeapons();
        
        // weaponId에 따라 무기 선택
        // 예: "sword_wood" → Sword, "spear_iron" → Spear
        if (weaponId.Contains("sword"))
        {
            return allWeapons[0]; // Sword (인덱스 0)
        }
        else if (weaponId.Contains("spear"))
        {
            return allWeapons[1]; // Spear (인덱스 1)
        }
        else if (weaponId.Contains("bow"))
        {
            return allWeapons[2]; // Bow (인덱스 2)
        }
        
        return null;
    }
}

