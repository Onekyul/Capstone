using UnityEngine;
using System;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Level & XP")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int nextLevelXP = 100;

    [Header("Health System")]
    [SerializeField] private float playerMaxHP = 100f;// 최대 체력
    [SerializeField] private float playerCurHP; // 현재 체력

    [Header("Enchantment Levels")]
    [SerializeField] private int[] enchantmentLevels = new int[4]; // [불, 얼음, 번개, 독] 인챈트 강화 수치

    [Header("Combat Stats Modifiers")]
    [SerializeField] private float attackDamageMultiplier = 1.0f; // 공격력 배율
    [SerializeField] private float attackSpeedMultiplier = 1.0f; // 공격속도 배율
    [SerializeField] private float moveSpeedMultiplier = 1.0f; // 이동속도 배율
    [SerializeField] private int attackCount = 1; // 공격 횟수 (2연격용)
    
    [Header("Special Abilities")]
    private float vampireChance = 0f; // 흡혈 확률 (0~1)
    private float vampireHealPercent = 0.5f; // 흡혈 회복량 (공격력의 %)
    private float dodgeChance = 0f; // 회피 확률 (0~1)
    private float shadowCooldown = 0f; // 그림자 은신 쿨타임
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

    void Start()
    {
        playerCurHP = playerMaxHP; // 게임 시작 시 체력을 최대치로 설정
        OnHealthChanged?.Invoke(playerCurHP); // 초기 체력 UI 업데이트
        
        // AbilitySystem 참조 가져오기
        abilitySystem = GetComponent<AbilitySystem>();
        if (abilitySystem == null)
        {
            Debug.LogWarning("PlayerStats: AbilitySystem을 찾을 수 없습니다. 능력 시스템이 작동하지 않습니다.");
        }
    }

    void Update()
    {
        // 테스트용 치트키 - 인챈트
        if (Input.GetKeyDown(KeyCode.F1))
        {
            enchantmentLevels[0] = 5; // 불 인챈트 5레벨
            Debug.Log("불 인챈트 5레벨 활성화! (50% 확률)");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            enchantmentLevels[1] = 5; // 얼음 인챈트 5레벨
            Debug.Log("얼음 인챈트 5레벨 활성화! (50% 확률)");
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            enchantmentLevels[2] = 5; // 번개 인챈트 5레벨
            Debug.Log("번개 인챈트 5레벨 활성화! (50% 확률)");
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            enchantmentLevels[3] = 5; // 독 인챈트 5레벨
            Debug.Log("독 인챈트 5레벨 활성화! (50% 확률)");
        }
        
        // 테스트용 치트키 - 랜덤 능력
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetRandomAbility(new int[] { 0 }); // 2연격
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            SetRandomAbility(new int[] { 1 }); // 응축된 공격
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            SetRandomAbility(new int[] { 2, 1 }); // 흡혈 레벨 1
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetRandomAbility(new int[] { 3 }); // 회피 기동
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            SetRandomAbility(new int[] { 4 }); // 질주
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            SetRandomAbility(new int[] { 5, 1 }); // 빠른 손놀림 레벨 1
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            SetRandomAbility(new int[] { 6, 1 }); // 그림자 은신 레벨 1
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            SetRandomAbility(new int[] { 7 }); // 분노
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            SetRandomAbility(new int[] { 8 }); // 죽창
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            SetRandomAbility(new int[] { 9 }); // 복수심
        }
    }

    public void TakeDamage(float damage) // 데미지 받는 함수
    {
        if (playerCurHP <= 0) return;

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

        playerCurHP -= damage;
        playerCurHP = Mathf.Max(0, playerCurHP); // 체력이 음수가 되지 않도록 함
        lastHitTime = Time.time; // 피격 시간 기록

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

    public void SetRandomAbility(int[] abilityArray)
    {
        // 이 함수를 레벨업매니저가 호출해서 랜덤 능력 전달함.
        if (abilitySystem == null)
        {
            Debug.LogError("AbilitySystem이 없습니다!");
            return;
        }

        if (abilityArray == null || abilityArray.Length == 0) return;

        int abilityID = abilityArray[0];
        int level = abilityArray.Length > 1 ? abilityArray[1] : 1;

        abilitySystem.AcquireAbility(abilityID, level);
    }

    public int[] GetEnchantmentLevels()
    {
        return enchantmentLevels;
    }

    // ===== AbilitySystem에서 호출할 스탯 수정 메서드들 =====
    
    public void ModifyAttackDamage(float value, bool isAdditive)
    {
        if (isAdditive)
            attackDamageMultiplier += value;
        else
            attackDamageMultiplier *= value;
    }

    public void ModifyAttackSpeed(float value, bool isAdditive)
    {
        if (isAdditive)
            attackSpeedMultiplier += value;
        else
            attackSpeedMultiplier *= value;
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
        playerMaxHP *= multiplier;
        playerCurHP = Mathf.Min(playerCurHP, playerMaxHP);
        OnHealthChanged?.Invoke(playerCurHP);
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
    
}
