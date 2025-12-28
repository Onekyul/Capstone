using UnityEngine;
using System.Collections.Generic;

public abstract class WeaponBase : MonoBehaviour // 모든 무기들의 설계도 역할을 하는 추상 클래스 
{
    [Header("Weapon Identity")]
    [SerializeField] protected string weaponId = ""; // 이 무기의 고유 ID (예: "sword_basic", "spear_01")
    
    [Header("Weapon Stats")]
    [SerializeField] protected float baseDamage = 10f; // 기본 공격력 (DataManager에서 로드됨)

    [SerializeField] protected float attackCooldown = 1f;   // 공격 쿨타임
    [SerializeField] protected float attackDuration = 0.3f; // 검 휘두르는 동작이 0.3초 동안 유지
    
    [Header("Final Stats (Debug View)")]
    [SerializeField] private float finalTotalDamage; // 최종 공격력 (읽기 전용 - 디버그용)
    [SerializeField] private float finalCooldown; // 최종 쿨타임 (읽기 전용 - 디버그용) 

    [Header("Visual Effects")]
    [SerializeField] protected GameObject attackEffectPrefab; // 공격 효과 프리펩
    [SerializeField] protected Transform attackPoint;         // 효과 생성 기준 위치치

    [Header("Enchantment Settings")]
    [SerializeField] protected float enchantChancePerLevel = 10f; // 강화 레벨당 적용 확률 (10 = 10%)
    [SerializeField] protected int[] enchantmentLevels = new int[4]; // [불, 얼음, 번개, 독] 인챈트 강화 수치
    
    [Header("Enchantment Debug Info (Read Only)")]
    [SerializeField] private int debugFireLevel = 0; // 불 인챈트 레벨 (읽기 전용)
    [SerializeField] private int debugIceLevel = 0; // 얼음 인챈트 레벨 (읽기 전용)
    [SerializeField] private int debugLightningLevel = 0; // 번개 인챈트 레벨 (읽기 전용)
    [SerializeField] private int debugPoisonLevel = 0; // 독 인챈트 레벨 (읽기 전용)

    protected float lastAttackTime;
    protected bool bIsAttacking = false;
    protected List<GameObject> enemiesInRange = new List<GameObject>();
    protected PlayerStats playerStats; // PlayerStats 참조



    public abstract void Attack(Vector2 direction);
    public abstract void DetectEnemies();
    protected abstract void CreateAttackEffect(Vector2 direction);
    
    protected virtual void Start()
    {
        // PlayerStats 참조 가져오기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }
        
        // DataManager에서 무기 데이터 로드 및 공격력 적용
        LoadWeaponData();
        
        // DataManager에서 인챈트 레벨 로드
        LoadEnchantData();
    }
    
    // DataManager에서 무기 데이터를 불러와서 공격력 적용
    protected virtual void LoadWeaponData()
    {
        if (DataManager.instance == null)
        {
            Debug.LogWarning($"{gameObject.name}: DataManager가 없습니다. 기본 공격력 사용.");
            return;
        }
        
        // 이 무기의 고유 ID가 설정되지 않았다면 경고
        if (string.IsNullOrEmpty(weaponId))
        {
            Debug.LogWarning($"{gameObject.name}: weaponId가 설정되지 않았습니다. Inspector에서 weaponId를 설정하세요. 기본 공격력 사용.");
            return;
        }
        
        // 이 무기의 고유 ID로 데이터 로드
        WeaponData weaponData = DataManager.instance.GetWeaponData(weaponId);
        
        if (weaponData != null)
        {
            // 무기의 기본 공격력 + 강화 레벨 적용
            int reinforcementLevel = DataManager.instance.GetItemLevel(weaponId);
            baseDamage = (float)weaponData.baseAtk + (weaponData.atkPerLevel * reinforcementLevel);
            
            Debug.Log($"=== 무기 데이터 로드 ===");
            Debug.Log($"무기: {weaponData.weaponName} +{reinforcementLevel} (ID: {weaponId})");
            Debug.Log($"기본 공격력: {weaponData.baseAtk}");
            Debug.Log($"강화 보너스: +{weaponData.atkPerLevel * reinforcementLevel} ({weaponData.atkPerLevel} × {reinforcementLevel})");
            Debug.Log($"최종 공격력: {baseDamage}");
            Debug.Log($"======================");
        }
        else
        {
            Debug.LogError($"{gameObject.name}: 무기 데이터를 찾을 수 없습니다 (ID: {weaponId}). 기본 공격력 사용.");
        }
    }
    
    // NPC가 무기를 강화할 때 호출하는 함수
    public void UpgradeWeaponDamage()
    {
        LoadWeaponData(); // 무기 데이터를 다시 로드하여 강화된 공격력 적용
        Debug.Log($"{gameObject.name}: 무기 강화 완료! 새로운 공격력: {baseDamage}");
    }
    
    // DataManager에서 인챈트 레벨을 불러와서 적용
    protected virtual void LoadEnchantData()
    {
        if (DataManager.instance == null)
        {
            Debug.LogWarning($"{gameObject.name}: DataManager가 없습니다. 인챈트를 로드할 수 없습니다.");
            return;
        }
        
        // 4가지 인챈트 레벨 로드 (불, 얼음, 번개, 독)
        enchantmentLevels[0] = DataManager.instance.GetEnchantLevel("ent_fire");
        enchantmentLevels[1] = DataManager.instance.GetEnchantLevel("ent_ice");
        enchantmentLevels[2] = DataManager.instance.GetEnchantLevel("ent_lightning");
        enchantmentLevels[3] = DataManager.instance.GetEnchantLevel("ent_poison");
        
        // 디버그 필드에도 즉시 반영
        debugFireLevel = enchantmentLevels[0];
        debugIceLevel = enchantmentLevels[1];
        debugLightningLevel = enchantmentLevels[2];
        debugPoisonLevel = enchantmentLevels[3];
        
        Debug.Log($"=== 인챈트 데이터 로드 ===");
        Debug.Log($"불 인챈트: Lv.{enchantmentLevels[0]} (발동 확률: {enchantmentLevels[0] * enchantChancePerLevel}%)");
        Debug.Log($"얼음 인챈트: Lv.{enchantmentLevels[1]} (발동 확률: {enchantmentLevels[1] * enchantChancePerLevel}%)");
        Debug.Log($"번개 인챈트: Lv.{enchantmentLevels[2]} (발동 확률: {enchantmentLevels[2] * enchantChancePerLevel}%)");
        Debug.Log($"독 인챈트: Lv.{enchantmentLevels[3]} (발동 확률: {enchantmentLevels[3] * enchantChancePerLevel}%)");
        Debug.Log($"======================");
    }
    
    // NPC가 인챈트를 강화할 때 호출하는 함수
    public void UpgradeEnchantLevels()
    {
        LoadEnchantData(); // 인챈트 데이터를 다시 로드하여 강화된 인챈트 적용
        Debug.Log($"{gameObject.name}: 인챈트 강화 완료!");
    }
    
    protected virtual void Update()
    {
        // 디버그용 최종 스탯 표시 (에디터에서 실시간 확인 가능)
        finalTotalDamage = GetTotalDamage();
        finalCooldown = GetAttackCooldown();
        
        // 디버그용 인챈트 레벨 표시 (에디터에서 실시간 확인 가능)
        if (DataManager.instance != null)
        {
            debugFireLevel = enchantmentLevels[0];
            debugIceLevel = enchantmentLevels[1];
            debugLightningLevel = enchantmentLevels[2];
            debugPoisonLevel = enchantmentLevels[3];
        }
    }
    
    public float GetTotalDamage()
    {
        float totalDamage = baseDamage;
        

        // PlayerStats의 공격력 배율 적용
        if (playerStats != null)
        {
            float multiplier = playerStats.GetAttackDamageMultiplier();
            totalDamage *= multiplier;
        }
        
        return totalDamage;
    }
    
    public float GetAttackCooldown()
    {
        float cooldown = attackCooldown;
        
        // PlayerStats의 공격속도 배율 적용 (배율이 높을수록 쿨타임 감소)
        if (playerStats != null)
        {
            float speedMultiplier = playerStats.GetAttackSpeedMultiplier();
            cooldown /= speedMultiplier;
        }
        
        return cooldown;
    }
    
    // 디버그용: 공격 시 한 번만 로그 출력
    protected void LogAttackStats()
    {
        if (playerStats != null)
        {
            float attackMultiplier = playerStats.GetAttackDamageMultiplier();
            float speedMultiplier = playerStats.GetAttackSpeedMultiplier();
            float finalDamage = baseDamage * attackMultiplier;
            float calculatedCooldown = attackCooldown / speedMultiplier;
            
            Debug.Log($"=== [{weaponId}] 공격 스탯 ===");
            Debug.Log($"공격력: {baseDamage} × {attackMultiplier:F2} = {finalDamage:F2}");
            Debug.Log($"쿨타임: {attackCooldown:F2}초 ÷ {speedMultiplier:F2} = {calculatedCooldown:F2}초");
            Debug.Log($"========================");
        }
    }

    // 인챈트 적용 여부를 확률로 계산하는 메서드
    protected virtual int[] CalculateAppliedEnchants()
    {
        int[] result = new int[4]; // [불, 얼음, 번개, 독]
        
        // 무기 자체의 인챈트 레벨을 사용 (PlayerStats가 아닌 this.enchantmentLevels 사용)
        for (int i = 0; i < 4; i++)
        {
            if (enchantmentLevels[i] > 0)
            {
                // 강화 레벨에 따른 확률 계산 (레벨 * 확률)
                float chance = enchantmentLevels[i] * enchantChancePerLevel;
                float randomValue = Random.Range(0f, 100f);
                
                if (randomValue < chance)
                {
                    // 확률에 성공하면 해당 인챈트의 강화 수치를 적용
                    result[i] = enchantmentLevels[i];
                }
            }
        }
        
        return result;
    }

    // ===== 무기별 인챈트 관리 메서드 =====
    
    // 특정 인챈트의 레벨을 설정
    // 인챈트 인덱스 (0: 불, 1: 얼음, 2: 번개, 3: 독)
    // 설정할 레벨
    public void SetEnchantmentLevel(int enchantIndex, int level)
    {
        if (enchantIndex >= 0 && enchantIndex < 4)
        {
            enchantmentLevels[enchantIndex] = Mathf.Max(0, level);
            Debug.Log($"{gameObject.name}: {GetEnchantmentName(enchantIndex)} 인챈트 레벨 {level}로 설정");
        }
        else
        {
            Debug.LogError($"잘못된 인챈트 인덱스: {enchantIndex}");
        }
    }

 
    /// 특정 인챈트의 레벨을 증가
  
    //인챈트 인덱스 (0: 불, 1: 얼음, 2: 번개, 3: 독)
    //증가량 (기본값 1)
    public void IncreaseEnchantmentLevel(int enchantIndex, int amount = 1)
    {
        if (enchantIndex >= 0 && enchantIndex < 4)
        {
            enchantmentLevels[enchantIndex] += amount;
            Debug.Log($"{gameObject.name}: {GetEnchantmentName(enchantIndex)} 인챈트 레벨 {amount} 증가 (현재: {enchantmentLevels[enchantIndex]})");
        }
        else
        {
            Debug.LogError($"잘못된 인챈트 인덱스: {enchantIndex}");
        }
    }
    
    // 모든 인챈트 레벨을 한 번에 설정합니다.
    // 4개의 인챈트 레벨 배열 [불, 얼음, 번개, 독]
    public void SetAllEnchantmentLevels(int[] levels)
    {
        if (levels == null || levels.Length != 4)
        {
            Debug.LogError("인챈트 레벨 배열은 4개의 요소를 가져야 합니다!");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            enchantmentLevels[i] = Mathf.Max(0, levels[i]);
        }
        
        Debug.Log($"{gameObject.name}: 모든 인챈트 레벨 설정 완료");
    }

   
    /// 특정 인챈트의 현재 레벨을 반환
    /// 인챈트 인덱스 (0: 불, 1: 얼음, 2: 번개, 3: 독)
    public int GetEnchantmentLevel(int enchantIndex)
    {
        if (enchantIndex >= 0 && enchantIndex < 4)
        {
            return enchantmentLevels[enchantIndex];
        }
        return 0;
    }
    
    /// 모든 인챈트 레벨을 반환
    /// 4개의 인챈트 레벨 배열 [불, 얼음, 번개, 독]
    public int[] GetEnchantmentLevels()
    {
        return (int[])enchantmentLevels.Clone();
    }
    
    /// 인챈트 인덱스에 해당하는 이름을 반환
    private string GetEnchantmentName(int index)
    {
        switch (index)
        {
            case 0: return "불";
            case 1: return "얼음";
            case 2: return "번개";
            case 3: return "독";
            default: return "알 수 없음";
        }
    }

    // ===== 무기 업그레이드 메서드 (NPC용) =====
    
    /// 무기의 기본 공격력을 영구히 증가
    public void UpgradeAttackDamage(float amount = 1f)
    {
        baseDamage += amount;
        Debug.Log($"{gameObject.name} 공격력 업그레이드! 현재 공격력: {baseDamage}");
    }

    
    /// 현재 기본 공격력을 반환합니다.
    public float GetBaseDamage()
    {
        return baseDamage;
    }
    
    /// 무기의 고유 ID를 반환합니다.
    public string GetWeaponId()
    {
        return weaponId;
    }
    
    /// 무기의 고유 ID를 설정합니다. (런타임에서 동적으로 변경 가능)
    public void SetWeaponId(string newWeaponId)
    {
        weaponId = newWeaponId;
        LoadWeaponData(); // ID 변경 시 데이터를 다시 로드
        LoadEnchantData();
    }
}