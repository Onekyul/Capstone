using UnityEngine;
using System;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    // 싱글톤 인스턴스 (비활성화 상태에서도 접근 가능)
    public static PlayerStats Instance { get; private set; }

    [Header("Visual Effects")]
    [SerializeField] private SpriteRenderer playerSprite; // 플레이어 렌더러
    [SerializeField] private Material flashMaterial;      // 흰색 점멸 머티리얼 (M_WhiteFlash)
    [SerializeField] private float flashDuration = 0.08f;  // 반짝이는 시간
    
    private Material originalMaterial; // 원래 Material을 저장할 변수
    private bool isFlashing = false;

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
    private bool hasCriticalStrike = false; // 급소 공격 보유 여부
    private float criticalStrikeChance = 0.1f; // 급소 공격 확률 (10%)
    private float criticalStrikeMultiplier = 1.5f; // 급소 공격 배율 (150%)
    private bool hasLastStand = false; // 불굴의 의지 보유 여부
    private bool lastStandUsed = false; // 불굴의 의지 사용 여부 (1회용)
    private float lastStandInvincibilityDuration = 3f; // 불굴의 의지 무적 시간 (3초)
    private float lastStandInvincibilityEndTime = 0f; // 불굴의 의지 무적 종료 시간
    private bool hasStealth = false; // 잠입의 달인 보유 여부
    private bool stealthAttackReady = false; // 잠입 공격 준비 완료 여부
    private float stealthSafeTime = 5f; // 피해 받지 않아야 하는 시간 (5초)
    private float lastDamageTime = 0f; // 마지막으로 피해를 받은 시간
    private float stealthAttackMultiplier = 3.0f; // 잠입 공격 배율 (300%)
    private bool hasEliteKiller = false; // 엘리트/속성 킬러 보유 여부
    private bool hasElementalMastery = false; // 속성 공격 보유 여부
    private int elementalMasteryAttackCount = 0; // 속성 공격 카운터 (10번째마다 발동)
    private const int ELEMENTAL_MASTERY_TRIGGER = 10; // 10번째 공격마다 발동

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
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // AbilitySystem 참조 가져오기
        abilitySystem = GetComponent<AbilitySystem>();
        if (abilitySystem == null)
        {
            Debug.LogWarning("PlayerStats: AbilitySystem을 찾을 수 없습니다. 능력 시스템이 작동하지 않습니다.");
        }
    }

    void Start()
    {
        // SpriteRenderer 자동 할당
        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
        }
        
        // 게임 시작 시 원래 Material 저장
        if (playerSprite != null)
        {
            originalMaterial = playerSprite.material;
            Debug.Log($"[PlayerStats] 원래 Material 저장 완료: {originalMaterial.name}");
        }
        else
        {
            Debug.LogWarning("[PlayerStats] SpriteRenderer를 찾을 수 없습니다! 흰색 점멸 효과가 작동하지 않습니다.");
        }
        
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
        
        // 잠입의 달인 체크: 5초간 피해를 받지 않으면 잠입 공격 준비
        if (hasStealth && !stealthAttackReady)
        {
            if (Time.time - lastDamageTime >= stealthSafeTime)
            {
                stealthAttackReady = true;
                Debug.Log("★ [잠입의 달인] 잠입 공격 준비 완료! 다음 공격 시 공격력 300%");
            }
        }
        
        // ===== 치트키: 응축된 공격 테스트 (F5) =====
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("===== [치트키] 응축된 공격 습득 =====");
            AcquireAbility(3); // 응축된 공격 ID = 3
        }

        // ===== 치트키: 흡혈 테스트 (F6) =====
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SetVampireChance(1.0f); // 100% 확률 흡혈
            Debug.Log("===== [치트키] 흡혈 100% 적용 (확률: 100%, 회복량: 공격력의 50%) =====");
        }

        // ===== 치트키: 방패 소환 테스트 (F7) =====
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log("===== [치트키] 방패 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(13); // 방패 능력 ID = 13
        }

        // ===== 치트키: 자석 능력 테스트 (F9) =====
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("===== [치트키] 자석 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(16); // 자석 능력 ID = 17
        }

        // ===== 치트키: 급소 공격 테스트 (F10) =====
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Debug.Log("===== [치트키] 급소 공격 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(8); // 급소 공격 능력 ID = 8
        }

        // ===== 치트키: 불굴의 의지 테스트 (F11) =====
        if (Input.GetKeyDown(KeyCode.F11))
        {
            Debug.Log("===== [치트키] 불굴의 의지 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(10); // 불굴의 의지 능력 ID = 10
        }

        // ===== 치트키: 강철피부 테스트 (F12) =====
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Debug.Log("===== [치트키] 강철피부 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(22); // 강철피부 능력 ID = 22
        }

        // ===== 치트키: 잠입의 달인 테스트 (Insert) =====
        if (Input.GetKeyDown(KeyCode.Insert))
        {
            Debug.Log("===== [치트키] 잠입의 달인 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(23); // 잠입의 달인 능력 ID = 23
        }

        // ===== 치트키: 추가 체력 테스트 (Delete) =====
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Debug.Log("===== [치트키] 추가 체력 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(29); // 추가 체력 능력 ID = 29
        }

        // ===== 치트키: 속성 공격 테스트 (Home) =====
        if (Input.GetKeyDown(KeyCode.Home))
        {
            Debug.Log("===== [치트키] 속성 공격 능력 습득 시도 =====");
            
            // AbilitySystem이 있는지 확인
            if (abilitySystem == null)
            {
                Debug.LogError("[치트키] AbilitySystem이 없습니다!");
                return;
            }
            
            AcquireAbility(9); // 속성 공격 능력 ID = 9
        }

        // ===== 치트키: 인챈트 레벨 조정 =====
        // Numpad 1: 불 인챈트 레벨 +1
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log("★★★ [치트키] Numpad 1 눌림! 불 인챈트 레벨 증가 시도 ★★★");
            
            if (DataManager.instance != null)
            {
                int currentLevel = DataManager.instance.GetEnchantLevel("ent_fire");
                DataManager.instance.SetEnchantLevel("ent_fire", currentLevel + 1);
                
                // 무기에 인챈트 레벨 재적용
                RefreshWeaponEnchants();
                
                Debug.Log($"★★★ [치트키] 불 인챈트 레벨 증가: {currentLevel} → {currentLevel + 1} ★★★");
            }
            else
            {
                Debug.LogError("[치트키] DataManager.instance가 null입니다!");
            }
        }

        // Numpad 2: 얼음 인챈트 레벨 +1
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            Debug.Log("★★★ [치트키] Numpad 2 눌림! 얼음 인챈트 레벨 증가 시도 ★★★");
            
            if (DataManager.instance != null)
            {
                int currentLevel = DataManager.instance.GetEnchantLevel("ent_ice");
                DataManager.instance.SetEnchantLevel("ent_ice", currentLevel + 1);
                
                // 무기에 인챈트 레벨 재적용
                RefreshWeaponEnchants();
                
                Debug.Log($"★★★ [치트키] 얼음 인챈트 레벨 증가: {currentLevel} → {currentLevel + 1} ★★★");
            }
            else
            {
                Debug.LogError("[치트키] DataManager.instance가 null입니다!");
            }
        }

        // Numpad 3: 번개 인챈트 레벨 +1 (번개 필드 테스트용!)
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            Debug.Log("★★★★★ [치트키] Numpad 3 눌림! 번개 인챈트 레벨 증가 시도 ★★★★★");
            
            if (DataManager.instance != null)
            {
                int currentLevel = DataManager.instance.GetEnchantLevel("ent_lightning");
                DataManager.instance.SetEnchantLevel("ent_lightning", currentLevel + 1);
                
                // 무기에 인챈트 레벨 재적용
                RefreshWeaponEnchants();
                
                Debug.Log($"★★★★★ [치트키] 번개 인챈트 레벨 증가: {currentLevel} → {currentLevel + 1} ★★★★★");
                Debug.Log("⚡⚡⚡ 번개 필드 테스트 가능! 적을 공격하세요! ⚡⚡⚡");
            }
            else
            {
                Debug.LogError("[치트키] DataManager.instance가 null입니다!");
            }
        }

        // Numpad 4: 독 인챈트 레벨 +1
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            Debug.Log("★★★ [치트키] Numpad 4 눌림! 독 인챈트 레벨 증가 시도 ★★★");
            
            if (DataManager.instance != null)
            {
                int currentLevel = DataManager.instance.GetEnchantLevel("ent_poison");
                DataManager.instance.SetEnchantLevel("ent_poison", currentLevel + 1);
                
                // 무기에 인챈트 레벨 재적용
                RefreshWeaponEnchants();
                
                Debug.Log($"★★★ [치트키] 독 인챈트 레벨 증가: {currentLevel} → {currentLevel + 1} ★★★");
            }
            else
            {
                Debug.LogError("[치트키] DataManager.instance가 null입니다!");
            }
        }

        // Numpad 0: 모든 인챈트 레벨 리셋
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            Debug.Log("★★★ [치트키] Numpad 0 눌림! 모든 인챈트 리셋 시도 ★★★");
            
            if (DataManager.instance != null)
            {
                DataManager.instance.SetEnchantLevel("ent_fire", 0);
                DataManager.instance.SetEnchantLevel("ent_ice", 0);
                DataManager.instance.SetEnchantLevel("ent_lightning", 0);
                DataManager.instance.SetEnchantLevel("ent_poison", 0);
                
                // 무기에 인챈트 레벨 재적용
                RefreshWeaponEnchants();
                
                Debug.Log("★★★ [치트키] 모든 인챈트 레벨 리셋 완료 (0으로 초기화) ★★★");
            }
            else
            {
                Debug.LogError("[치트키] DataManager.instance가 null입니다!");
            }
        }
    }

    public void TakeDamage(float damage) // 데미지 받는 함수
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, Collider2D attackerCollider) // 반사 데미지 지원
    {
        if (playerCurHP <= 0) return;

        // 불굴의 의지 무적 체크 (최우선)
        if (Time.time < lastStandInvincibilityEndTime)
        {
            Debug.Log("불굴의 의지 무적 상태! 공격 무효화");
            return;
        }

        // 그림자 은신 무적 체크
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
        lastHitTime = Time.time; // 피격 시간 기록
        
        // 잠입의 달인: 피해를 받으면 잠입 상태 초기화
        if (hasStealth)
        {
            lastDamageTime = Time.time;
            if (stealthAttackReady)
            {
                stealthAttackReady = false;
                Debug.Log("[잠입의 달인] 피해를 받아 잠입 공격 취소됨");
            }
        }

        Debug.Log($"받은 데미지: {damage:F1} → 방어 후: {finalDamage:F1} (방어력: {totalDefense}, 감소율: {damageReduction * 100:F1}%)");

        // 불굴의 의지 체크 (체력이 0 이하가 되었을 때)
        if (playerCurHP <= 0 && hasLastStand && !lastStandUsed)
        {
            // 불굴의 의지 발동
            playerCurHP = playerMaxHP * 0.01f; // 체력 1%로 회복
            lastStandUsed = true; // 1회용 사용 완료
            lastStandInvincibilityEndTime = Time.time + lastStandInvincibilityDuration; // 3초 무적
            Debug.Log("불굴의 의지 발동! 체력 1%로 회복 및 3초 무적!");
            OnHealthChanged?.Invoke(playerCurHP);
            return; // 사망 처리 방지
        }

        playerCurHP = Mathf.Max(0, playerCurHP); // 체력이 음수가 되지 않도록 함

        // 반사 데미지 처리 (능력 ID 6: 저주받은 방패)
        if (HasCursedShield() && attackerCollider != null)
        {
            float reflectDamage = finalDamage * GetReflectPercent();
            ReflectDamageToAttacker(attackerCollider, reflectDamage);
        }

        // 피격 시 흰색 점멸 실행
        if (!isFlashing)
        {
            StartCoroutine(WhiteFlashRoutine());
        }

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
            gameObject.SetActive(false);

        }

    }

    //저주받은 방패 능력 보유 확인 (능력 ID 6)
    private bool HasCursedShield()
    {
        if (abilitySystem != null)
        {
            return abilitySystem.HasAbility(6);
        }
        return false;
    }

    //반사 비율 계산
    private float GetReflectPercent()
    {
        if (abilitySystem == null) return 0.3f;
        
        // 저주받은 방패는 1회만 습득 가능 (레벨업 불가)
        // 고정 반사율: 30%
        if (abilitySystem.HasAbility(6))
        {
            return 0.3f;
        }
        
        return 0f;
    }

    //공격자에게 반사 데미지 전달
    private void ReflectDamageToAttacker(Collider2D attackerCollider, float reflectDamage)
    {
        MonsterController monster = attackerCollider.GetComponent<MonsterController>();
        if (monster != null)
        {
            monster.TakeDamage(reflectDamage);
            Debug.Log($"[반사 데미지] {attackerCollider.name}에게 {reflectDamage:F1} 데미지 반사!");
            
            // TODO: 반사 이펙트 추가 (번개, 빛나는 효과 등)
        }
        else
        {
            Debug.LogWarning($"[반사 데미지] {attackerCollider.name}에 MonsterController가 없습니다!");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 방패와 충돌 중인 적은 플레이어 데미지로 계산하지 않음
            // 방패는 레이어 2 (Ignore Raycast)로 설정되어 있음
            Collider2D[] shieldColliders = GetComponentsInChildren<Collider2D>();
            bool isTouchingShield = false;
            
            foreach (Collider2D shieldCollider in shieldColliders)
            {
                // Shield 컴포넌트가 있는 Collider인지 확인
                if (shieldCollider.GetComponent<Shield>() != null)
                {
                    if (shieldCollider.IsTouching(other))
                    {
                        isTouchingShield = true;
                        break;
                    }
                }
            }
            
            // 방패와 닿아있으면 플레이어는 데미지를 받지 않음
            if (isTouchingShield)
            {
                return;
            }
            
            // 2. 마지막 데미지를 입은 후 'damageTickCooldown'이 지났는지 확인
            if (Time.time - lastDamageTickTime > damageTickCooldown)
            {
                // 3. 시간이 지났다면, 데미지를 입고 마지막 시간을 지금 시간으로 갱신
                lastDamageTickTime = Time.time;

                float damage = other.GetComponent<MonsterController>()?.normalDamage ?? 10f;
                // 공격자 정보 전달하여 반사 데미지 작동
                TakeDamage(damage, other);
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

        // 쿨타임 예시 계산 (1초 기준)
        float exampleOldCooldown = 1.0f / oldValue;
        float exampleNewCooldown = 1.0f / attackSpeedMultiplier;
        float cooldownChange = exampleNewCooldown - exampleOldCooldown;
        string changeType = cooldownChange > 0 ? "증가(느려짐)" : "감소(빨라짐)";

        Debug.Log($"[공격속도 변경] {oldValue:F2} → {attackSpeedMultiplier:F2} (변화량: {value:F2}, 타입: {(isAdditive ? "덧셈" : "곱셈")})");
        Debug.Log($"  → 쿨타임 영향 (1초 기준): {exampleOldCooldown:F2}초 → {exampleNewCooldown:F2}초 ({changeType} {Mathf.Abs(cooldownChange):F2}초)");
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
    
    // 최대 체력을 고정값으로 증가 (추가 체력 능력용)
    public void AddMaxHP(float amount)
    {
        float oldMaxHP = playerMaxHP;
        playerMaxHP += amount;
        
        // 현재 체력도 증가량만큼 증가 (체력을 채워주는 효과)
        playerCurHP += amount;
        
        OnHealthChanged?.Invoke(playerCurHP);
        Debug.Log($"최대 체력 증가: {oldMaxHP} → {playerMaxHP} (+{amount}, 현재 체력: {playerCurHP})");
    }

    public void ModifyDefense(float value, bool isAdditive)
    {
        float oldValue = totalDefense;
        if (isAdditive)
            totalDefense += value;
        else
            totalDefense *= value;
        
        Debug.Log($"[방어력 변경] {oldValue:F2} → {totalDefense:F2} (변화량: {value:F2}, 타입: {(isAdditive ? "덧셈" : "곱셈")})");
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

    public void SetCriticalStrike(bool enabled)
    {
        hasCriticalStrike = enabled;
        if (enabled)
        {
            Debug.Log($"급소 공격 활성화! 확률: {criticalStrikeChance * 100}%, 배율: {criticalStrikeMultiplier * 100}%");
        }
    }

    public void SetLastStand(bool enabled)
    {
        hasLastStand = enabled;
        lastStandUsed = false; // 능력 습득 시 사용 여부 초기화
        if (enabled)
        {
            Debug.Log("불굴의 의지 활성화! 치명타 방어 시 체력 1%로 회복 및 3초 무적 (1회용)");
        }
    }

    public void SetStealth(bool enabled)
    {
        hasStealth = enabled;
        stealthAttackReady = false;
        lastDamageTime = Time.time; // 능력 습득 시점부터 타이머 시작
        if (enabled)
        {
            Debug.Log($"잠입의 달인 활성화! {stealthSafeTime}초간 피해받지 않으면 다음 공격 {stealthAttackMultiplier * 100}% 적용");
        }
    }

    public void SetEliteKiller(bool enabled)
    {
        hasEliteKiller = enabled;
        if (enabled)
        {
            Debug.Log("엘리트 킬러 활성화! 엘리트/속성 몬스터 공격력 150%, 일반 몬스터 공격력 75%");
        }
    }

    // 급소 공격 체크 (무기에서 호출)
    public bool CheckCriticalStrike()
    {
        if (!hasCriticalStrike) return false;
        
        float randomValue = UnityEngine.Random.value;
        bool isCritical = randomValue < criticalStrikeChance;
        
        if (isCritical)
        {
            Debug.Log($"★ 급소 공격 발동! (확률: {criticalStrikeChance * 100}%, 랜덤값: {randomValue:F3})");
        }
        
        return isCritical;
    }

    public float GetCriticalStrikeMultiplier()
    {
        return criticalStrikeMultiplier;
    }

    // 잠입 공격 체크 (무기에서 호출)
    public bool CheckStealthAttack()
    {
        if (!hasStealth || !stealthAttackReady) return false;
        
        // 잠입 공격 사용 (1회용)
        stealthAttackReady = false;
        lastDamageTime = Time.time; // 타이머 리셋
        Debug.Log($"★ 잠입 공격 발동! 공격력 {stealthAttackMultiplier * 100}% 적용");
        
        return true;
    }

    public float GetStealthAttackMultiplier()
    {
        return stealthAttackMultiplier;
    }

    // 엘리트 킬러 능력: 몬스터 타입에 따른 데미지 배율
    public float GetMonsterTypeDamageMultiplier(MonsterType monsterType)
    {
        if (!hasEliteKiller) return 1.0f; // 능력이 없으면 배율 없음

        switch (monsterType)
        {
            case MonsterType.Elite:
            case MonsterType.Element:
                return 1.5f; // 엘리트/속성 몬스터: 150%
            case MonsterType.Normal:
                return 0.75f; // 일반 몬스터: 75%
            default:
                return 1.0f;
        }
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
        Debug.Log($"[흡혈 체크] 공격력: {attackDamage:F1}, 흡혈 확률: {vampireChance * 100:F1}%, 현재 체력: {playerCurHP:F1}/{playerMaxHP:F1}");

        if (vampireChance > 0)
        {
            float randomValue = UnityEngine.Random.value;
            Debug.Log($"[흡혈 확률 체크] 랜덤값: {randomValue:F3}, 필요값: {vampireChance:F3} → {(randomValue < vampireChance ? "성공" : "실패")}");

            if (randomValue < vampireChance)
            {
                float healAmount = attackDamage * vampireHealPercent;
                float oldHP = playerCurHP;
                playerCurHP = Mathf.Min(playerCurHP + healAmount, playerMaxHP);
                float actualHeal = playerCurHP - oldHP;
                OnHealthChanged?.Invoke(playerCurHP);
                Debug.Log($"★ 흡혈 발동! 회복량: {healAmount:F1} → 실제 회복: {actualHeal:F1} (체력: {oldHP:F1} → {playerCurHP:F1})");
            }
        }
        else
        {
            Debug.Log("[흡혈] 흡혈 확률이 0이므로 발동하지 않음");
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

        // 신발 보너스 적용 (공격 속도만)
        string bootsId = DataManager.instance.GetEquippedItemId(EquipmentType.Boots);
        Debug.Log($"장착된 신발 ID: {bootsId}");

        if (!string.IsNullOrEmpty(bootsId))
        {
            ArmorData boots = DataManager.instance.GetArmorData(bootsId);
            if (boots != null)
            {
                int bootsLevel = DataManager.instance.GetItemLevel(bootsId);
                float bootsAttackSpeedBonus = boots.bonusAttackSpeed + (boots.attackSpeedPerLevel * bootsLevel);
                attackSpeedMultiplier += bootsAttackSpeedBonus;
                Debug.Log($"신발 장착: {boots.armorName} +{bootsLevel} (공격 속도 +{bootsAttackSpeedBonus} = 기본 {boots.bonusAttackSpeed} + 강화 {boots.attackSpeedPerLevel * bootsLevel})");
            }
            else
            {
                Debug.LogError($"신발 데이터를 찾을 수 없습니다: {bootsId}");
            }
        }

        Debug.Log($"=== 최종 스탯 - 최대 체력: {playerMaxHP}, 방어력: {totalDefense}, 이동 속도 배율: {moveSpeedMultiplier}, 공격 속도 배율: {attackSpeedMultiplier} ===");
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

    // 현재 장착된 무기의 인챈트 레벨을 새로고침 (치트키용)
    private void RefreshWeaponEnchants()
    {
        Debug.Log("→→→ [RefreshWeaponEnchants] 무기 인챈트 새로고침 시작 ←←←");
        
        // AttackManager를 통해 현재 무기 찾기
        AttackManager attackManager = GetComponent<AttackManager>();
        if (attackManager != null)
        {
            Debug.Log($"→ AttackManager 찾음: {attackManager.name}");
            
            WeaponBase currentWeapon = attackManager.GetComponentInChildren<WeaponBase>();
            if (currentWeapon != null)
            {
                Debug.Log($"→ 현재 무기 찾음: {currentWeapon.GetType().Name} (WeaponID: {currentWeapon.GetWeaponId()})");
                
                currentWeapon.UpgradeEnchantLevels();
                
                Debug.Log("✓✓✓ [RefreshWeaponEnchants] 무기 인챈트 레벨 새로고침 완료! ✓✓✓");
            }
            else
            {
                Debug.LogWarning("✗ [RefreshWeaponEnchants] 현재 장착된 무기를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("✗✗✗ [RefreshWeaponEnchants] AttackManager를 찾을 수 없습니다! ✗✗✗");
        }
    }

    // 랜덤 능력 초기화 (던전 종료 시 호출)
    // 씬 전환 시 플레이어가 새로 생성되므로 Start()에서 자동으로 초기화됩니다.
    public void ResetAbilities()
    {
        Debug.Log("[PlayerStats] 랜덤 능력 초기화 시작");
        
        // 1. 전투 스탯 배율 초기화
        attackDamageMultiplier = 1.0f;
        attackSpeedMultiplier = 1.0f;
        moveSpeedMultiplier = 1.0f; // 이동속도도 초기화
        attackCount = 1;
        
        // 2. 특수 능력 초기화
        vampireChance = 0f;
        dodgeChance = 0f;
        shadowCooldown = 0f;
        nextShadowTime = 0f;
        shadowInvincibilityEndTime = 0f;
        hasRage = false;
        hasRevenge = false;
        revengeEndTime = 0f;
        hasCriticalStrike = false;
        hasLastStand = false;
        lastStandUsed = false;
        lastStandInvincibilityEndTime = 0f;
        hasStealth = false;
        stealthAttackReady = false;
        lastDamageTime = 0f;
        hasEliteKiller = false;
        hasElementalMastery = false;
        elementalMasteryAttackCount = 0;
        
        // 3. 디버그 플래그 초기화
        debugShowRage = false;
        debugShowRevenge = false;
        finalAttackMultiplier = 1.0f;
        
        // 4. AbilitySystem에도 초기화 요청
        if (abilitySystem != null)
        {
            abilitySystem.ResetAbilities();
        }
        
        // 5. 장비 보너스 재적용 (랜덤 능력으로 변경된 최대 체력/방어력/이동속도를 장비 스탯으로 복원)
        ApplyEquipmentBonuses();
        
        // 6. 체력 풀로 회복
        playerCurHP = playerMaxHP;
        OnHealthChanged?.Invoke(playerCurHP);
        
        Debug.Log($"[PlayerStats] 랜덤 능력 초기화 완료 - 체력: {playerCurHP}/{playerMaxHP}, 방어력: {totalDefense}");
    }
    
    // 플레이어 부활 (주로 내부 사용 - 필요시 외부에서도 호출 가능)
    public void RevivePlayer()
    {
        Debug.Log("[PlayerStats] 플레이어 부활 시작");
        
        // 1. 랜덤 능력 초기화 (장비 보너스 재적용 + 체력 풀 회복 포함)
        ResetAbilities();
        
        // 2. 무적 시간 초기화
        lastHitTime = -10f;
        lastDamageTickTime = 0f;
        
        // 3. 오브젝트 활성화
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        Debug.Log("[PlayerStats] 플레이어 부활 완료");
    }

    // 흰색 점멸 효과 코루틴
    private IEnumerator WhiteFlashRoutine()
    {
        isFlashing = true;

        // 1. 머티리얼을 흰색 전용으로 교체
        if (playerSprite != null && flashMaterial != null)
        {
            playerSprite.material = flashMaterial;

            // 2. 아주 짧은 시간 대기
            yield return new WaitForSeconds(flashDuration);

            // 3. 원래 머티리얼로 복구
            playerSprite.material = originalMaterial;
        }

        isFlashing = false;
    }
    
    // ===== 속성 공격 (Elemental Mastery) 관련 함수 =====
    
    // 속성 공격 능력 활성화
    public void SetElementalMastery(bool active)
    {
        hasElementalMastery = active;
        if (active)
        {
            elementalMasteryAttackCount = 0; // 카운터 초기화
            Debug.Log("★ [속성 공격] 능력 활성화! 10번째 공격마다 모든 인챈트가 100% 발동합니다.");
        }
        else
        {
            Debug.Log("[속성 공격] 능력 비활성화");
        }
    }
    
    // 공격할 때마다 카운터 증가 (무기에서 호출)
    public void IncrementAttackCounter()
    {
        if (!hasElementalMastery) return;
        
        elementalMasteryAttackCount++;
        Debug.Log($"[속성 공격] 공격 카운터: {elementalMasteryAttackCount}/{ELEMENTAL_MASTERY_TRIGGER}");
    }
    
    // 현재 공격이 10번째 공격인지 확인 (무기에서 호출)
    public bool ShouldTriggerElementalMastery()
    {
        if (!hasElementalMastery) return false;
        
        // 카운터 증가
        elementalMasteryAttackCount++;
        
        if (elementalMasteryAttackCount >= ELEMENTAL_MASTERY_TRIGGER)
        {
            elementalMasteryAttackCount = 0; // 카운터 리셋
            Debug.Log($"★★★ [속성 공격] 10번째 공격 발동! 모든 인챈트가 100% 확률로 적용됩니다! ★★★");
            return true;
        }
        
        Debug.Log($"[속성 공격] 공격 카운터: {elementalMasteryAttackCount}/{ELEMENTAL_MASTERY_TRIGGER}");
        return false;
    }
    
    // 속성 공격 보유 여부 확인
    public bool HasElementalMastery() => hasElementalMastery;

}


