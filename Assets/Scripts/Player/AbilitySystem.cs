using UnityEngine;
using System.Collections.Generic;

public class AbilitySystem : MonoBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] private List<AbilityDataSO> allAbilities = new List<AbilityDataSO>(); // 모든 능력 목록 (Inspector에서 할당)
    [SerializeField] private GameObject shieldAbilityPrefab; // 인스펙터에서 위에서 만든 프리팹 할당
    
    private List<GameObject> activeAbilityObjects = new List<GameObject>(); // 관리용 리스트
    private Dictionary<int, int> abilityLevels = new Dictionary<int, int>(); // Key: 능력ID, Value: 현재레벨
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("AbilitySystem: PlayerStats를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        // ===== 치트키: 방패 능력 초기화 (F8) =====
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log("===== [치트키] 랜덤 능력 초기화 (방패 파괴) =====");
            ResetAbilities();
        }
    }

  
    // 능력을 습득하거나 레벨업
    public void AcquireAbility(int abilityID)
    {
        AbilityDataSO ability = GetAbilityByID(abilityID);
        if (ability == null)
        {
            Debug.LogError($"[AbilitySystem] 능력 ID {abilityID}를 찾을 수 없습니다!");
            return;
        }

        // 이미 보유한 능력인지 확인
        if (abilityLevels.ContainsKey(abilityID))
        {
            HandleDuplicateAbility(ability);
        }
        else
        {
            // 새로운 능력 습득 (레벨 1로 시작)
            abilityLevels[abilityID] = 1;
            ApplyAbility(ability, 1);
            Debug.Log($"[AbilitySystem] {ability.abilityName} 레벨 1 습득!");
        }
    }

   
    // 중복된 능력 처리
    private void HandleDuplicateAbility(AbilityDataSO ability)
    {
        int currentLevel = abilityLevels[ability.abilityID];

        // 레벨업 가능한 능력
        if (ability.hasLevels)
        {
            if (currentLevel < ability.maxLevel)
            {
                // 기존 효과 제거 후 새 레벨 적용
                RemoveAbility(ability, currentLevel);
                int nextLevel = currentLevel + 1; // 현재 레벨에서 1 증가
                abilityLevels[ability.abilityID] = nextLevel;
                ApplyAbility(ability, nextLevel);
                Debug.Log($"[AbilitySystem] {ability.abilityName} 레벨업: {currentLevel} → {nextLevel}");
            }
            else
            {
                Debug.LogWarning($"[AbilitySystem] {ability.abilityName}는 최대 레벨입니다! (레벨 {ability.maxLevel})");
            }
        }
        // 누적 가능한 능력
        else if (ability.canStack)
        {
            abilityLevels[ability.abilityID]++;
            ApplyAbility(ability, 1); // 매번 1레벨 효과를 누적
            Debug.Log($"[AbilitySystem] {ability.abilityName} 추가 습득! (총 {abilityLevels[ability.abilityID]}회)");
        }
        // 1회만 습득 가능
        else
        {
            Debug.LogWarning($"[AbilitySystem] {ability.abilityName}는 이미 습득했습니다!");
        }
    }

 
    // 능력 효과 적용
    private void ApplyAbility(AbilityDataSO ability, int level)
    {
        if (playerStats == null) return;

        // ===== 특수 능력: "운빨" (ID 28) =====
        if (ability.abilityID == 28)
        {
            ApplyLuckAbility();
            return; // 일반 로직 건너뛰기
        }

        foreach (StatModifier modifier in ability.statModifiers)
        {
            float value = modifier.GetValue(level);
            ApplyStatModifier(modifier.statType, modifier.operation, value);
        }
        
        // 능력 ID 13 (방패 소환)인 경우 특수 처리
        if (ability.abilityID == 13)
        {
            SpawnShieldAbility();
        }
        
        // 능력 ID 17 (자석)인 경우 특수 처리
        if (ability.abilityID == 16)
        {
            Debug.Log("[AbilitySystem] 자석 능력 활성화! 경험치가 플레이어에게 끌려옵니다.");
        }
        
        // 능력 ID 8 (급소 공격)인 경우 특수 처리
        if (ability.abilityID == 8)
        {
            playerStats.SetCriticalStrike(true);
            Debug.Log("[AbilitySystem] 급소 공격 활성화! 10% 확률로 공격력 150% 적용");
        }
        
        // 능력 ID 10 (불굴의 의지)인 경우 특수 처리
        if (ability.abilityID == 10)
        {
            playerStats.SetLastStand(true);
            Debug.Log("[AbilitySystem] 불굴의 의지 활성화! 사망 시 체력 1%로 회복 및 3초 무적 (1회용)");
        }
        
        // 능력 ID 23 (잠입의 달인)인 경우 특수 처리
        if (ability.abilityID == 23)
        {
            playerStats.SetStealth(true);
            Debug.Log("[AbilitySystem] 잠입의 달인 활성화! 5초간 피해받지 않으면 다음 공격 300% 적용");
        }
        
        // 능력 ID 9 (속성 공격)인 경우 특수 처리
        if (ability.abilityID == 9)
        {
            playerStats.SetElementalMastery(true);
            Debug.Log("[AbilitySystem] 속성 공격 활성화! 10번째 공격마다 모든 인챈트가 100% 발동합니다.");
        }
        
        // 능력 ID 14 (탐지)인 경우 특수 처리
        if (ability.abilityID == 14)
        {
            DetectionIndicator detector = FindFirstObjectByType<DetectionIndicator>();
            if (detector != null)
            {
                detector.ActivateDetection();
            }
            else
            {
                Debug.LogWarning("[AbilitySystem] DetectionIndicator를 찾을 수 없습니다! Player에 DetectionIndicator 컴포넌트를 추가하세요.");
            }
        }
        
        // 능력 ID 26 (죽음의 숨결)인 경우 특수 처리
        if (ability.abilityID == 26)
        {
            playerStats.SetDeathBreath(true);
            Debug.Log("[AbilitySystem] 죽음의 숨결 활성화! 적 처치 시 10% 확률로 공격속도/이동속도 +30% (3초)");
        }
        
        // 능력 ID 27 (빙결폭발)인 경우 특수 처리
        if (ability.abilityID == 27)
        {
            playerStats.SetFrozenExplosion(true);
            Debug.Log("[AbilitySystem] 빙결폭발 활성화! 빙결 상태 몬스터 사망 시 주변에 공격력 200% 폭발 데미지");
        }
        
        // 능력 ID 25 (전염)인 경우 특수 처리
        if (ability.abilityID == 25)
        {
            playerStats.SetContagion(true);
            Debug.Log("[AbilitySystem] 전염 활성화! 인챈트 공격 피격 시 5% 확률로 주변 적 1명에게 인챈트 전염");
        }
    }


    // 능력 효과 제거 (레벨업 시 기존 효과 제거용)
    private void RemoveAbility(AbilityDataSO ability, int level)
    {
        if (playerStats == null) return;

        foreach (StatModifier modifier in ability.statModifiers)
        {
            float value = modifier.GetValue(level);
            RemoveStatModifier(modifier.statType, modifier.operation, value);
        }
    }


    // 스탯에 변화 적용
    private void ApplyStatModifier(StatType statType, ModifierOperation operation, float value)
    {
        switch (statType)
        {
            case StatType.AttackDamageMultiplier:
                if (operation == ModifierOperation.Add)
                    playerStats.ModifyAttackDamage(value, true);
                else if (operation == ModifierOperation.Multiply)
                    playerStats.ModifyAttackDamage(value, false);
                break;

            case StatType.AttackSpeedMultiplier:
                if (operation == ModifierOperation.Add)
                    playerStats.ModifyAttackSpeed(value, true);
                else if (operation == ModifierOperation.Multiply)
                    playerStats.ModifyAttackSpeed(value, false);
                break;

            case StatType.MoveSpeedMultiplier:
                if (operation == ModifierOperation.Add)
                    playerStats.ModifyMoveSpeed(value, true);
                else if (operation == ModifierOperation.Multiply)
                    playerStats.ModifyMoveSpeed(value, false);
                break;

            case StatType.AttackCount:
                playerStats.SetAttackCount((int)value);
                break;

            case StatType.MaxHP:
                if (operation == ModifierOperation.Add)
                    playerStats.AddMaxHP(value);
                else if (operation == ModifierOperation.Multiply)
                    playerStats.ModifyMaxHP(value);
                break;

            case StatType.Defense:
                if (operation == ModifierOperation.Add)
                    playerStats.ModifyDefense(value, true);
                else if (operation == ModifierOperation.Multiply)
                    playerStats.ModifyDefense(value, false);
                break;

            case StatType.VampireChance:
                playerStats.SetVampireChance(value);
                break;

            case StatType.DodgeChance:
                playerStats.SetDodgeChance(value);
                break;

            case StatType.ShadowCooldown:
                playerStats.SetShadowCooldown(value);
                break;

            case StatType.HasRage:
                playerStats.SetRage(value > 0);
                break;

            case StatType.HasRevenge:
                playerStats.SetRevenge(value > 0);
                break;

            case StatType.HasCriticalStrike:
                playerStats.SetCriticalStrike(value > 0);
                break;

            case StatType.HasLastStand:
                playerStats.SetLastStand(value > 0);
                break;

            case StatType.HasStealth:
                playerStats.SetStealth(value > 0);
                break;

            case StatType.ExpMultiplier:
                if (operation == ModifierOperation.Add)
                    LevelManager.instance.AddExpMultiplier(value);
                break;

            case StatType.HasEliteKiller:
                playerStats.SetEliteKiller(value > 0);
                break;

            case StatType.HasElementalMastery:
                playerStats.SetElementalMastery(value > 0);
                break;
                
            case StatType.HasDeathBreath:
                playerStats.SetDeathBreath(value > 0);
                break;
                
            case StatType.HasFrozenExplosion:
                playerStats.SetFrozenExplosion(value > 0);
                break;
        }
    }

    public void SpawnShieldAbility()
    {
        // 이미 방패가 소환되어 있으면 중복 소환 방지
        if (activeAbilityObjects.Count > 0)
        {
            Debug.LogWarning("[AbilitySystem] 방패는 이미 소환되어 있습니다!");
            return;
        }

        if (shieldAbilityPrefab == null)
        {
            Debug.LogError("[AbilitySystem] 방패 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 프리팹 생성 및 플레이어 자식으로 설정
        GameObject shieldObj = Instantiate(shieldAbilityPrefab, transform.position, Quaternion.identity);
        shieldObj.transform.SetParent(this.transform);
        shieldObj.transform.localPosition = Vector3.zero; // 플레이어 중심에 배치
    
        // 리스트에 추가하여 나중에 초기화할 때 사용
        activeAbilityObjects.Add(shieldObj);
        Debug.Log("[AbilitySystem] 방패 3개 소환 완료!");
    }

   
    // 스탯 변화 제거 (레벨업 시 기존 효과 제거용)
    private void RemoveStatModifier(StatType statType, ModifierOperation operation, float value)
    {
        switch (statType)
        {
            case StatType.AttackSpeedMultiplier:
                if (operation == ModifierOperation.Add)
                    playerStats.ModifyAttackSpeed(-value, true);
                break;

            case StatType.Defense:
                if (operation == ModifierOperation.Add)
                    playerStats.ModifyDefense(-value, true);
                break;

            case StatType.VampireChance:
                playerStats.SetVampireChance(0f);
                break;

            case StatType.ShadowCooldown:
                playerStats.SetShadowCooldown(0f);
                break;

            case StatType.ExpMultiplier:
                if (operation == ModifierOperation.Add)
                    LevelManager.instance.AddExpMultiplier(-value);
                break;

            case StatType.HasEliteKiller:
                playerStats.SetEliteKiller(false);
                break;

            // 다른 스탯들도 필요시 추가
        }
    }

 
    // ID로 능력 데이터 찾기
    private AbilityDataSO GetAbilityByID(int abilityID)
    {
        return allAbilities.Find(ability => ability.abilityID == abilityID);
    }
    
    /// 현재 보유한 능력 레벨 가져오기
    public int GetAbilityLevel(int abilityID)
    {
        return abilityLevels.ContainsKey(abilityID) ? abilityLevels[abilityID] : 0;
    }

    // 능력 보유 여부 확인
    public bool HasAbility(int abilityID)
    {
        return abilityLevels.ContainsKey(abilityID);
    }
    
    // ===== 랜덤 능력 초기화 (던전 종료 시 호출) =====
    public void ResetAbilities()
    {
        Debug.Log("[AbilitySystem] 모든 능력 초기화");
        
        // 생성된 능력 오브젝트 파괴 (방패 등)
        foreach (GameObject obj in activeAbilityObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
                Debug.Log($"[AbilitySystem] 능력 오브젝트 파괴: {obj.name}");
            }
        }
        activeAbilityObjects.Clear();
        
        // 능력 레벨 딕셔너리 초기화
        abilityLevels.Clear();
    }

    // ===== "운빨" 능력 전용 메서드 =====
    private void ApplyLuckAbility()
    {
        // 1. 랜덤 스탯 선택 (5개 중 1개)
        int randomStatIndex = Random.Range(0, 5);
        string selectedStat = "";
        
        switch (randomStatIndex)
        {
            case 0: selectedStat = "최대 체력"; break;
            case 1: selectedStat = "방어력"; break;
            case 2: selectedStat = "공격력"; break;
            case 3: selectedStat = "공격속도"; break;
            case 4: selectedStat = "이동속도"; break;
        }
        
        // 2. 50% 확률로 증가/감소 결정
        bool isPositive = Random.value < 0.5f; // 50% 확률
        float multiplier = isPositive ? 2.0f : 0.5f; // 100% 증가 or 50% 감소
        string result = isPositive ? "100% 증가!" : "50% 감소...";
        
        Debug.Log($"★★★ [운빨 능력 발동] ★★★");
        Debug.Log($"선택된 스탯: {selectedStat}");
        Debug.Log($"결과: {result}");
        
        // 3. 선택된 스탯에 효과 적용
        switch (randomStatIndex)
        {
            case 0: // 최대 체력
                playerStats.ModifyMaxHP(multiplier);
                break;
            case 1: // 방어력
                playerStats.ModifyDefense(multiplier, false);
                break;
            case 2: // 공격력
                playerStats.ModifyAttackDamage(multiplier, false);
                break;
            case 3: // 공격속도
                playerStats.ModifyAttackSpeed(multiplier, false);
                break;
            case 4: // 이동속도
                playerStats.ModifyMoveSpeed(multiplier, false);
                break;
        }
        
        Debug.Log($"★★★ {selectedStat}이(가) {result} ★★★");
    }
}
