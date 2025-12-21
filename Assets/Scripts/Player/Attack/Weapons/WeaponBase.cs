using UnityEngine;
using System.Collections.Generic;

public abstract class WeaponBase : MonoBehaviour // 모든 무기들의 설계도 역할을 하는 추상 클래스 
{
    [Header("Weapon Stats")]
    [SerializeField] protected float baseDamage = 10f;
    public Enchantment curEnchantment; // 무기에 적용된 인챈트

    [SerializeField] protected float attackCooldown = 1f;   // 공격 쿨타임
    [SerializeField] protected float attackDuration = 0.3f; // 검 휘두르는 동작이 0.3초 동안 유지 

    [Header("Visual Effects")]
    [SerializeField] protected GameObject attackEffectPrefab; // 공격 효과 프리펩
    [SerializeField] protected Transform attackPoint;         // 효과 생성 기준 위치치

    [Header("Enchantment Settings")]
    [SerializeField] protected float enchantChancePerLevel = 10f; // 강화 레벨당 적용 확률 (10 = 10%)

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
    }
    
    public float GetTotalDamage()
    {
        float totalDamage = baseDamage;
        
        // 인챈트 보너스 적용
        if (curEnchantment != null)
        {
            totalDamage += curEnchantment.floatDamageBonus;
        }
        
        // PlayerStats의 공격력 배율 적용
        if (playerStats != null)
        {
            totalDamage *= playerStats.GetAttackDamageMultiplier();
        }
        
        return totalDamage;
    }
    
    public float GetAttackCooldown()
    {
        float cooldown = attackCooldown;
        
        // PlayerStats의 공격속도 배율 적용 (배율이 높을수록 쿨타임 감소)
        if (playerStats != null)
        {
            cooldown /= playerStats.GetAttackSpeedMultiplier();
        }
        
        return cooldown;
    }

    // 인챈트 적용 여부를 확률로 계산하는 메서드
    protected virtual int[] CalculateAppliedEnchants()
    {
        int[] result = new int[4]; // [불, 얼음, 번개, 독]
        
        if (playerStats == null)
        {
            return result; // PlayerStats가 없으면 인챈트 없음
        }

        int[] enchantLevels = playerStats.GetEnchantmentLevels();
        
        for (int i = 0; i < 4; i++)
        {
            if (enchantLevels[i] > 0)
            {
                // 강화 레벨에 따른 확률 계산 (레벨 * 확률)
                float chance = enchantLevels[i] * enchantChancePerLevel;
                float randomValue = Random.Range(0f, 100f);
                
                if (randomValue < chance)
                {
                    // 확률에 성공하면 해당 인챈트의 강화 수치를 적용
                    result[i] = enchantLevels[i];
                }
            }
        }
        
        return result;
    }
}