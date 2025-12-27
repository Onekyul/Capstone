using UnityEngine;
using System;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Health System")]
    [SerializeField] private float baseMaxHP = 100f;// 기본 최대 체력
    [SerializeField] private float playerMaxHP = 100f;// 최종 최대 체력 (기본 + 장비 보너스)
    [SerializeField] private float playerCurHP; // 현재 체력

    [Header("Defense System")]
    [SerializeField] private float baseDefense = 0f; // 기본 방어력
    [SerializeField] private float totalDefense = 0f; // 최종 방어력 (기본 + 장비 보너스)

    [Header("Combat Stats Modifiers")]
    [SerializeField] private float attackDamageMultiplier = 1.0f; // 공격력 배율
    [SerializeField] private float attackSpeedMultiplier = 1.0f; // 공격속도 배율
    [SerializeField] private float moveSpeedMultiplier = 1.0f; // 이동속도 배율
    [SerializeField] private int attackCount = 1; // 공격 횟수 (2연격용)
    
    [Header("Final Stats (Debug View)")]
    [SerializeField] private float finalAttackMultiplier = 1.0f; // 최종 공격력 배율 (읽기 전용)
    [SerializeField] private bool debugShowRage; // 분노 활성화 여부
    [SerializeField] private bool debugShowRevenge; // 복수심 활성화 여부
    
    [Header("Special Abilities")]
    private float vampireChance = 0f; // 흡혈 확률 (0~1)
    private float vampireHealPercent = 0.5f; // 흡혈 회복량 (공격력의 %)
    private float dodgeChance = 0f; // 회피 확률 (0~1)
    private float shadowCooldown = 0f; // 그림자 은신 쿨타임 (레벨별: 10/9/7/5/2초)
    private float shadowInvincibilityDuration = 0.2f; // 그림자 은신 무적 지속 시간
    private float nextShadowTime = 0f; // 다음 그림자 은신 발동 시간
    private float shadowInvincibilityEndTime = 0f; // 그림자 은신 무적 종료 시간
    private bool hasRage = false; // 분노 보유 여부
    private bool hasRevenge = false; // 복수심 보유 여부
    private float revengeEndTime = 0f; // 복수심 종료 시간

    [Header("Collision Damage")]
    [SerializeField] private float damageTickCooldown = 1.0f; // 1초에 한 번씩만 겹침 데미지를 받음
    private float lastDamageTickTime; // 마지막으로 데미지를 받은 시간

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f; // 피격 후 무적 시간 (초)
    private float lastHitTime = -10f; // 마지막 피격 시간 (초기값은 충분히 과거)

    // 체력 변화 이벤트
    public event Action<float> OnHealthChanged;
    //플레이어 사망 이벤트
    public event Action OnPlayerDied;
    // 레벨업 이벤트 - LevelUpManager가 구독
    public event Action OnLevelUp;

    private AbilitySystem abilitySystem; // 능력 시스템 참조

    void Awake()
    {
        // AbilitySystem 참조 가져오기
        abilitySystem = GetComponent<AbilitySystem>();
        if (abilitySystem == null)
        {
            Debug.LogWarning("PlayerStats: AbilitySystem을 찾을 수 없습니다. 능력 시스템이 작동하지 않습니다.");
        }
    }

    void Start()
    {
        // DataManager가 준비될 때까지 대기
        if (DataManager.instance == null)
        {
            Debug.LogError("DataManager가 없습니다! 장비 스탯을 적용할 수 없습니다.");
            // 기본값으로 초기화
            playerMaxHP = baseMaxHP;
            totalDefense = baseDefense;
        }
        else
        {
            // 장비 보너스를 먼저 적용
            ApplyEquipmentBonuses();
        }
        
        playerCurHP = playerMaxHP; // 게임 시작 시 체력을 최대치로 설정
        OnHealthChanged?.Invoke(playerCurHP); // 초기 체력 UI 업데이트
    }

    void Update()
    {
        // 그림자 은신 주기적 발동
        if (shadowCooldown > 0 && Time.time >= nextShadowTime)
        {
            ActivateShadowInvincibility();
            nextShadowTime = Time.time + shadowCooldown;
        }
        
        // 디버그용 최종 스탯 표시 (에디터에서 확인 가능)
        finalAttackMultiplier = GetAttackDamageMultiplier();
        debugShowRage = hasRage;
        debugShowRevenge = hasRevenge && Time.time < revengeEndTime;
        
    }

    public void TakeDamage(float damage) // 데미지 받는 함수
    {
        if (playerCurHP <= 0) return;

        // 그림자 은신 무적 체크 (최우선)
        if (Time.time < shadowInvincibilityEndTime)
        {
            Debug.Log("그림자 은신 무적 상태! 공격 무효화");
            return;
        }

        // 무적 시간 체크
        if (Time.time - lastHitTime < invincibilityDuration)
        {
            return; // 무적 시간 중에는 데미지를 받지 않음
        }

        // 회피 체크
        if (dodgeChance > 0 && UnityEngine.Random.value < dodgeChance)
        {
            Debug.Log("공격 회피!");
            return;
        }

        // 방어력 적용 (퍼센트 감소 방식)
        // 공식: 데미지 감소율 = 방어력 / (방어력 + 100)
        float damageReduction = totalDefense / (totalDefense + 100f);
        float finalDamage = damage * (1f - damageReduction);

        playerCurHP -= finalDamage;
        playerCurHP = Mathf.Max(0, playerCurHP); // 체력이 음수가 되지 않도록 함
        lastHitTime = Time.time; // 피격 시간 기록

        Debug.Log($"받은 데미지: {damage:F1} → 방어 후: {finalDamage:F1} (방어력: {totalDefense}, 감소율: {damageReduction * 100:F1}%)");

        OnHealthChanged?.Invoke(playerCurHP);

        // 복수심 발동
        if (hasRevenge && playerCurHP > 0)
        {
            revengeEndTime = Time.time + 5f; // 5초간 유지
            Debug.Log("복수심 발동! 5초간 공격력 200%");
        }

        if (playerCurHP <= 0)
        {
            OnPlayerDied?.Invoke();
            Debug.Log("플레이어 사망!");
            StageManager.instance.FinishGame(false);
            Destroy(gameObject); // 플레이어 사망 처리

        }

    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 2. 마지막 데미지를 입은 후 'damageTickCooldown'이 지났는지 확인
            if (Time.time - lastDamageTickTime > damageTickCooldown)
            {
                // 3. 시간이 지났다면, 데미지를 입고 마지막 시간을 지금 시간으로 갱신
                lastDamageTickTime = Time.time;

                float damage = other.GetComponent<MonsterController>()?.normalDamage ?? 10f;
                TakeDamage(damage);
            }
        }
    }

    public void AcquireAbility(int abilityID)
    {
        // 레벨업 매니저가 선택한 능력 ID를 전달하면 AbilitySystem에 전달
        if (abilitySystem == null)
        {
            Debug.LogError("AbilitySystem이 없습니다!");
            return;
        }

        abilitySystem.AcquireAbility(abilityID);
        Debug.Log($"[PlayerStats] 능력 ID {abilityID} 습득 요청");
    }


    // ===== AbilitySystem에서 호출할 스탯 수정 메서드들 =====
    public void ModifyAttackDamage(float value, bool isAdditive)
    {
        float oldValue = attackDamageMultiplier;
        if (isAdditive)
            attackDamageMultiplier += value;
        else
            attackDamageMultiplier *= value;
        
        Debug.Log($"[공격력 변경] {oldValue:F2} → {attackDamageMultiplier:F2} (변화량: {value:F2}, 타입: {(isAdditive ? "덧셈" : "곱셈")})");
    }

    public void ModifyAttackSpeed(float value, bool isAdditive)
    {
        float oldValue = attackSpeedMultiplier;
        if (isAdditive)
            attackSpeedMultiplier += value;
        else
            attackSpeedMultiplier *= value;
        
        Debug.Log($"[공격속도 변경] {oldValue:F2} → {attackSpeedMultiplier:F2} (변화량: {value:F2}, 타입: {(isAdditive ? "덧셈" : "곱셈")})");
    }

    public void ModifyMoveSpeed(float value, bool isAdditive)
    {
        if (isAdditive)
            moveSpeedMultiplier += value;
        else
            moveSpeedMultiplier *= value;
    }

    public void SetAttackCount(int count)
    {
        attackCount = count;
    }

    public void ModifyMaxHP(float multiplier)
    {
        float oldMaxHP = playerMaxHP;
        float healthRatio = playerCurHP / oldMaxHP; // 현재 체력 비율 저장
        
        playerMaxHP *= multiplier;
        
        // 체력 비율 유지 (죽창 같은 대폭 감소 능력 대응)
        playerCurHP = playerMaxHP * healthRatio;
        playerCurHP = Mathf.Max(playerCurHP, 1f); // 최소 1의 체력 보장
        
        OnHealthChanged?.Invoke(playerCurHP);
        Debug.Log($"최대 체력 변경: {oldMaxHP} → {playerMaxHP} (배율: {multiplier}x, 현재 체력: {playerCurHP})");
    }

    public void SetVampireChance(float chance)
    {
        vampireChance = chance;
    }

    public void SetDodgeChance(float chance)
    {
        dodgeChance = chance;
    }

    public void SetShadowCooldown(float cooldown)
    {
        shadowCooldown = cooldown;
        if (cooldown > 0)
        {
            // 그림자 은신 활성화 시 즉시 첫 발동
            nextShadowTime = Time.time + cooldown;
            Debug.Log($"그림자 은신 활성화! {cooldown}초마다 0.2초 무적");
        }
    }

    private void ActivateShadowInvincibility()
    {
        shadowInvincibilityEndTime = Time.time + shadowInvincibilityDuration;
        Debug.Log($"그림자 은신 발동! {shadowInvincibilityDuration}초간 무적");
    }

    public void SetRage(bool enabled)
    {
        hasRage = enabled;
    }

    public void SetRevenge(bool enabled)
    {
        hasRevenge = enabled;
    }

    // ===== 스탯 게터 메서드들 (다른 클래스에서 참조용) =====
    
    public float GetAttackDamageMultiplier()
    {
        float multiplier = attackDamageMultiplier;

        // 분노 효과: 체력 1% 감소당 공격력 1% 증가
        if (hasRage)
        {
            float healthPercent = playerCurHP / playerMaxHP;
            float rageBuff = (1.0f - healthPercent); // 잃은 체력 비율
            multiplier += rageBuff;
        }

        // 복수심 효과: 5초간 공격력 200%
        if (hasRevenge && Time.time < revengeEndTime)
        {
            multiplier *= 2.0f;
        }

        return multiplier;
    }

    public float GetAttackSpeedMultiplier() => attackSpeedMultiplier;
    public float GetMoveSpeedMultiplier() => moveSpeedMultiplier;
    public int GetAttackCount() => attackCount;
    public float GetVampireChance() => vampireChance;
    public float GetVampireHealPercent() => vampireHealPercent;

    // 흡혈 회복 처리 (적 처치 시 호출)
    public void OnEnemyKilled(float attackDamage)
    {
        if (vampireChance > 0 && UnityEngine.Random.value < vampireChance)
        {
            float healAmount = attackDamage * vampireHealPercent;
            playerCurHP = Mathf.Min(playerCurHP + healAmount, playerMaxHP);
            OnHealthChanged?.Invoke(playerCurHP);
            Debug.Log($"흡혈 발동! {healAmount} 체력 회복");
        }
    }

    // 장비 보너스 적용 시스템 
    private void ApplyEquipmentBonuses()
    {
        if (DataManager.instance == null)
        {
            Debug.LogWarning("DataManager가 없습니다. 장비 보너스를 적용할 수 없습니다.");
            return;
        }

        Debug.Log("=== 장비 보너스 적용 시작 ===");

        // 기본 스탯으로 초기화
        playerMaxHP = baseMaxHP;
        totalDefense = baseDefense;
        moveSpeedMultiplier = 1.0f; // 이동속도 배율 초기화

        Debug.Log($"기본 스탯 - 체력: {baseMaxHP}, 방어력: {baseDefense}");

        // 헬멧 보너스 적용 (방어력만)
        string helmetId = DataManager.instance.GetEquippedItemId(EquipmentType.Helmet);
        Debug.Log($"장착된 헬멧 ID: {helmetId}");
        
        if (!string.IsNullOrEmpty(helmetId))
        {
            ArmorData helmet = DataManager.instance.GetArmorData(helmetId);
            if (helmet != null)
            {
                int helmetLevel = DataManager.instance.GetItemLevel(helmetId);
                float helmetDefBonus = helmet.bonusDef + (helmet.defPerLevel * helmetLevel);
                totalDefense += helmetDefBonus;
                Debug.Log($"헬멧 장착: {helmet.armorName} +{helmetLevel} (방어력 +{helmetDefBonus} = 기본 {helmet.bonusDef} + 강화 {helmet.defPerLevel * helmetLevel})");
            }
            else
            {
                Debug.LogError($"헬멧 데이터를 찾을 수 없습니다: {helmetId}");
            }
        }

        // 갑옷 보너스 적용 (최대 체력만)
        string armorId = DataManager.instance.GetEquippedItemId(EquipmentType.Armor);
        Debug.Log($"장착된 갑옷 ID: {armorId}");
        
        if (!string.IsNullOrEmpty(armorId))
        {
            ArmorData armor = DataManager.instance.GetArmorData(armorId);
            if (armor != null)
            {
                int armorLevel = DataManager.instance.GetItemLevel(armorId);
                float armorHpBonus = armor.bonusHp + (armor.hpPerLevel * armorLevel);
                playerMaxHP += armorHpBonus;
                Debug.Log($"갑옷 장착: {armor.armorName} +{armorLevel} (최대 체력 +{armorHpBonus} = 기본 {armor.bonusHp} + 강화 {armor.hpPerLevel * armorLevel})");
            }
            else
            {
                Debug.LogError($"갑옷 데이터를 찾을 수 없습니다: {armorId}");
            }
        }

        // 신발 보너스 적용 (이동 속도만)
        string bootsId = DataManager.instance.GetEquippedItemId(EquipmentType.Boots);
        Debug.Log($"장착된 신발 ID: {bootsId}");
        
        if (!string.IsNullOrEmpty(bootsId))
        {
            ArmorData boots = DataManager.instance.GetArmorData(bootsId);
            if (boots != null)
            {
                int bootsLevel = DataManager.instance.GetItemLevel(bootsId);
                float bootsSpeedBonus = boots.bonusSpeed + (boots.speedPerLevel * bootsLevel);
                moveSpeedMultiplier += bootsSpeedBonus;
                Debug.Log($"신발 장착: {boots.armorName} +{bootsLevel} (이동 속도 +{bootsSpeedBonus} = 기본 {boots.bonusSpeed} + 강화 {boots.speedPerLevel * bootsLevel})");
            }
            else
            {
                Debug.LogError($"신발 데이터를 찾을 수 없습니다: {bootsId}");
            }
        }

        Debug.Log($"=== 최종 스탯 - 최대 체력: {playerMaxHP}, 방어력: {totalDefense}, 이동 속도 배율: {moveSpeedMultiplier} ===");
    }

    // NPC가 호출할 장비 강화 함수
    public void UpgradeEquipmentStats()
    {
        ApplyEquipmentBonuses();
        playerCurHP = Mathf.Min(playerCurHP, playerMaxHP); // 현재 체력이 최대 체력을 넘지 않도록
        OnHealthChanged?.Invoke(playerCurHP);
    }

    // 방어력 게터
    public float GetTotalDefense() => totalDefense;
    public float GetPlayerMaxHP() => playerMaxHP;
    
    // 체력바 UI를 위한 게터 메서드들
    public float GetMaxHP() => playerMaxHP;
    public float GetCurrentHP() => playerCurHP;
    
    // 현재 장착된 무기의 기본 공격력 가져오기
    public float GetEquippedWeaponBaseDamage()
    {
        if (DataManager.instance == null) return 0f;
        
        string weaponId = DataManager.instance.GetEquippedItemId(EquipmentType.Weapon);
        if (string.IsNullOrEmpty(weaponId)) return 0f;
        
        WeaponData weaponData = DataManager.instance.GetWeaponData(weaponId);
        if (weaponData == null) return 0f;
        
        return weaponData.baseAtk;
    }

    // ===== 랜덤 능력 초기화 (던전 종료 시 호출) =====
    public void ResetAbilities()
    {
        Debug.Log("[PlayerStats] 랜덤 능력 초기화 시작");
        
        // 전투 스탯 배율 초기화
        attackDamageMultiplier = 1.0f;
        attackSpeedMultiplier = 1.0f;
        attackCount = 1;
        
        // 특수 능력 초기화
        vampireChance = 0f;
        dodgeChance = 0f;
        shadowCooldown = 0f;
        nextShadowTime = 0f;
        shadowInvincibilityEndTime = 0f;
        hasRage = false;
        hasRevenge = false;
        revengeEndTime = 0f;
        
        // 디버그 플래그 초기화
        debugShowRage = false;
        debugShowRevenge = false;
        finalAttackMultiplier = 1.0f;
        
        // AbilitySystem에도 초기화 요청
        if (abilitySystem != null)
        {
            abilitySystem.ResetAbilities();
        }
        
        Debug.Log("[PlayerStats] 랜덤 능력 초기화 완료");
    }

}

