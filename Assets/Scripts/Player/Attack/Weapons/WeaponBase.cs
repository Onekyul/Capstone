using UnityEngine;
using System.Collections.Generic;

public abstract class WeaponBase : MonoBehaviour // 모든 무기들의 설계도 역할을 하는 추상 클래스 
{
    [Header("Weapon Stats")]
    [SerializeField] protected float damage = 10f;          // 무기 데미지
    
    [SerializeField] protected float attackCooldown = 1f;   // 공격 쿨타임
    [SerializeField] protected float attackDuration = 0.3f; // 검 휘두르는 동작이 0.3초 동안 유지 
    
    [Header("Visual Effects")]
    [SerializeField] protected GameObject attackEffectPrefab; // 공격 효과 프리펩
    [SerializeField] protected Transform attackPoint;         // 효과 생성 기준 위치치
    
    protected float lastAttackTime;
    protected bool bIsAttacking = false;
    protected List<GameObject> enemiesInRange = new List<GameObject>();
    
    public abstract void Attack(Vector2 direction);
    public abstract void DetectEnemies();
    protected abstract void CreateAttackEffect(Vector2 direction);
}