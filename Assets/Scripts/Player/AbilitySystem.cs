using UnityEngine;
using System.Collections.Generic;

public class AbilitySystem : MonoBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] private List<AbilityDataSO> allAbilities = new List<AbilityDataSO>(); // 모든 능력 목록 (Inspector에서 할당)

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

        foreach (StatModifier modifier in ability.statModifiers)
        {
            float value = modifier.GetValue(level);
            ApplyStatModifier(modifier.statType, modifier.operation, value);
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
                if (operation == ModifierOperation.Multiply)
                    playerStats.ModifyMaxHP(value);
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
        }
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

            case StatType.VampireChance:
                playerStats.SetVampireChance(0f);
                break;

            case StatType.ShadowCooldown:
                playerStats.SetShadowCooldown(0f);
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
        abilityLevels.Clear();
    }
}
