// using UnityEngine;
// using System.Collections;


// public class SpearWeapon : WeaponBase
// {
//     [Header("Spear Specific")]
//     [SerializeField] private float spearRange = 3f;   // 공격 각도
//     [SerializeField] private LayerMask enemyLayer = 128; // Layer 7 (Enemy) - 2^7 = 128
//     [SerializeField] private float attackWidth = 0.5f;

//     [Header("Effect Settings")]
//     // 플레이어 중심에서 검기가 얼마나 떨어져서 나타날지 (0.5 ~ 1.0 추천)
//     [SerializeField] private float effectOffsetDistance = 0.8f;
    
//     private Vector2 lastAttackDir;

//     public override void Attack(Vector2 direction) // WeaponBase.cs 의 Attack 추상 메소드 재정의
//     {
//         if (Time.time - lastAttackTime < GetAttackCooldown() || bIsAttacking) //쿨타임 체크 || 공격 중인치 체크
//         {
//             return;
//         }
        
//         lastAttackDir=direction;
//         StartCoroutine(PerformSpearAttack()); // 체크해서 맞으면 코루틴 시작
//     }
    
//     private IEnumerator PerformSpearAttack()
//     {
//         bIsAttacking = true;
//         lastAttackTime = Time.time;
        
//         // 디버그: 공격 스탯 로그 출력
//         LogAttackStats();
        
//         // 공격 횟수 확인 (2연격 등)
//         int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;
        
//         for (int i = 0; i < attackCount; i++)
//         {
//             // 직선 범위 내 적들 감지
//             DetectEnemies();
            
//             // 공격 이펙트 생성
//             CreateAttackEffect(lastAttackDir);
            
//             // 범위 내 모든 적에게 데미지
//             foreach (GameObject enemy in enemiesInRange)
//             {
//                 if (enemy != null)
//                 {
//                     // 최종 데미지 계산
//                     float finalDamage = GetTotalDamage();
                    
//                     // 엘리트 킬러 능력 적용
//                     MonsterController monster = enemy.GetComponent<MonsterController>();
//                     if (monster != null && playerStats != null)
//                     {
//                         MonsterType monsterType = monster.GetMonsterType();
//                         float monsterTypeMultiplier = playerStats.GetMonsterTypeDamageMultiplier(monsterType);
//                         finalDamage *= monsterTypeMultiplier;
                        
//                         if (monsterTypeMultiplier != 1.0f)
//                         {
//                             Debug.Log($"[엘리트 킬러] {monsterType} 몬스터에게 배율 {monsterTypeMultiplier * 100}% 적용! 최종 데미지: {finalDamage:F1}");
//                         }
//                     }
                    
//                     enemy.GetComponent<MonsterController>()?.TakeDamage(finalDamage);
//                 }
//             }
            
//             // 연속 공격 사이에 짧은 딜레이 (2번째 공격부터)
//             if (i < attackCount - 1)
//             {
//                 yield return new WaitForSeconds(0.12f);
//             }
//         }
        
//         yield return new WaitForSeconds(attackDuration);
//         bIsAttacking = false;
//     }
    
    
//     protected override void CreateAttackEffect(Vector2 direction)
//     {
//         if (attackEffectPrefab != null)
//         {
//             // 1. 회전값 계산 (마우스 방향대로 이미지를 회전시킴)
//             // *주의: 검기 스프라이트 원본은 반드시 '오른쪽(->)'을 보고 있어야 함
//             float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
//             Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

//             // 2. 위치 계산 (플레이어 위치 + 방향 * 거리)
//             Vector3 spawnPosition = transform.position + (Vector3)(direction * effectOffsetDistance);

//             // 3. 검기 생성 (위치와 회전 적용)
//             GameObject effect = Instantiate(attackEffectPrefab, spawnPosition, rotation);
//         }
//     }



//     public override void DetectEnemies()
//     {
//         enemiesInRange.Clear();

//         Vector2 boxCenter = (Vector2)transform.position + lastAttackDir * (spearRange / 2);
//         Vector2 boxSize = new Vector2(spearRange, attackWidth);

//         float angle = Vector2.SignedAngle(Vector2.right, lastAttackDir);

//         Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle);

//         foreach (Collider2D col in colliders)
//         {
//             if (col.CompareTag("Enemy"))
//                 enemiesInRange.Add(col.gameObject);
//         }
//     }
    
    
//     private void OnDrawGizmosSelected()
//     {
//         if (Application.isPlaying && Camera.main != null)
//         {
//             // 1. 마우스 위치를 실시간으로 계산합니다. (SwordWeapon과 동일한 로직)
//             Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//             Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
        
//             // 2. 계산된 마우스 방향으로 기즈모를 그립니다.
//             DrawBoxGizmo(direction, Color.red);
//         }
//         else
//         {
//             // 게임 실행 중이 아닐 때는 기본 방향(오른쪽)으로 그립니다.
//             DrawBoxGizmo(Vector2.right, Color.yellow);
//         }
//     }
    
//     private void DrawBoxGizmo(Vector2 direction, Color color)
//     {
//        if (direction == Vector2.zero) direction = Vector2.right; // 방향이 없을 때 기본값

//         Vector2 boxCenter = (Vector2)transform.position + direction * (spearRange / 2);
//         Vector2 boxSize = new Vector2(spearRange, attackWidth);
//         float angle = Vector2.SignedAngle(Vector2.right, direction);

//         // Gizmos의 위치, 회전, 크기를 설정
//         Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, angle), Vector3.one);
//         Gizmos.matrix = rotationMatrix;

//         Gizmos.color = color;
//         Gizmos.DrawWireCube(Vector3.zero, boxSize);

//         // Gizmos 설정을 원래대로 되돌림
//         Gizmos.matrix = Matrix4x4.identity;
//     }
// }

using UnityEngine;
using System.Collections;

public class SpearWeapon : WeaponBase
{
    [Header("Spear Projectile Settings")]
    [SerializeField] private float projectileSpeed = 15f;     // 창이 날아가는 속도 (빠르게 설정)
    [SerializeField] private float projectileLifeTime = 1.0f; // 창이 유지되는 시간 (사거리 역할)

    [Header("Spear Specific")]
    [SerializeField] private float spearRange = 3f;   
    [SerializeField] private float attackWidth = 0.5f;

    [Header("Effect Settings")]
    [SerializeField] private float effectOffsetDistance = 0.8f;
    
    private Vector2 lastAttackDir;

    public override void Attack(Vector2 direction) 
    {
        if (Time.time - lastAttackTime < GetAttackCooldown() || bIsAttacking) 
        {
            return;
        }
        
        lastAttackDir = direction;
        StartCoroutine(PerformSpearAttack()); 
    }
    
    private IEnumerator PerformSpearAttack()
    {
        bIsAttacking = true;
        lastAttackTime = Time.time;
        
        LogAttackStats();
        
        int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;
        
        for (int i = 0; i < attackCount; i++)
        {
            // 투사체 발사
            CreateAttackEffect(lastAttackDir);
            
            if (i < attackCount - 1)
            {
                yield return new WaitForSeconds(0.12f);
            }
        }
        
        yield return new WaitForSeconds(attackDuration);
        bIsAttacking = false;
    }
    
    protected override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            Vector3 spawnPosition = transform.position + (Vector3)(direction * effectOffsetDistance);

            // 1. 창 투사체 생성
            GameObject projectile = Instantiate(attackEffectPrefab, spawnPosition, rotation);

            // 2. 투사체 스크립트 확인 및 추가
            SpearProjectile spearProj = projectile.GetComponent<SpearProjectile>();
            if (spearProj == null)
            {
                spearProj = projectile.AddComponent<SpearProjectile>();
            }

            // 3. 투사체에 데미지, 방향, 속도 등 정보 전달
            float finalDamage = GetTotalDamage();
            spearProj.Initialize(direction, projectileSpeed, projectileLifeTime, finalDamage, playerStats);
        }
    }

    public override void DetectEnemies()
    {
        // 투사체가 직접 충돌을 체크하므로 비워둡니다.
        enemiesInRange.Clear();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && Camera.main != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
            DrawBoxGizmo(direction, Color.red);
        }
        else
        {
            DrawBoxGizmo(Vector2.right, Color.yellow);
        }
    }
    
    private void DrawBoxGizmo(Vector2 direction, Color color)
    {
        if (direction == Vector2.zero) direction = Vector2.right;

        Vector2 boxCenter = (Vector2)transform.position + direction * (spearRange / 2);
        Vector2 boxSize = new Vector2(spearRange, attackWidth);
        float angle = Vector2.SignedAngle(Vector2.right, direction);

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, angle), Vector3.one);
        Gizmos.matrix = rotationMatrix;

        Gizmos.color = color;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        Gizmos.matrix = Matrix4x4.identity;
    }
}