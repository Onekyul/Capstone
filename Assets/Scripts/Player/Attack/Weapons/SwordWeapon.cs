// using UnityEngine;
// using System.Collections;

// public class SwordWeapon : WeaponBase
// {
//     [Header("Sword Specific")]
//     [SerializeField] private float attackAngle = 90f;   // 부채꼴 각도
//     [SerializeField] private LayerMask enemyLayer = 128; // Layer 7 (Enemy) - 2^7 = 128
//     [SerializeField] private float swordRange = 2.0f; // 검의 공격 범위

//     [Header("Effect Settings")]
//     // 플레이어 중심에서 검기가 얼마나 떨어져서 나타날지 (0.5 ~ 1.0 추천)
//     [SerializeField] private float effectOffsetDistance = 0.8f;

//     private Vector2 lastAttackDir;


//     protected override void Start()
//     {
//         base.Start(); // WeaponBase의 Start 호출 (PlayerStats 초기화)
//     }

//     public override void Attack(Vector2 direction) // WeaponBase.cs 의 Attack 추상 메소드 재정의
//     {

//         if (Time.time - lastAttackTime < GetAttackCooldown() || bIsAttacking) //쿨타임 체크 || 공격 중인치 체크
//         {
//             return;
//         }

//         lastAttackDir = direction;
//         StartCoroutine(PerformSwordAttack()); // 체크해서 맞으면 코루틴 시작
//     }

//     private IEnumerator PerformSwordAttack()
//     {
//         bIsAttacking = true;
//         lastAttackTime = Time.time;
        
//         // 디버그: 공격 스탯 로그 출력
//         LogAttackStats();

//         // 공격 횟수 확인 (2연격 등)
//         int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;

//         for (int i = 0; i < attackCount; i++)
//         {
//             // 부채꼴 범위 내 적들 감지
//             DetectEnemies();

//             // 공격 이펙트 생성
//             CreateAttackEffect(lastAttackDir);

//             // 범위 내 모든 적에게 데미지
//             foreach (GameObject enemy in enemiesInRange)
//             {
//                 if (enemy != null)
//                 {
//                     MonsterController monster = enemy.GetComponent<MonsterController>();
//                     if (monster == null) continue;

//                     // 최종 데미지 계산
//                     float finalDamage = GetTotalDamage();
                    
//                     // 엘리트 킬러 능력 적용
//                     if (playerStats != null)
//                     {
//                         MonsterType monsterType = monster.GetMonsterType();
//                         float monsterTypeMultiplier = playerStats.GetMonsterTypeDamageMultiplier(monsterType);
//                         finalDamage *= monsterTypeMultiplier;
                        
//                         if (monsterTypeMultiplier != 1.0f)
//                         {
//                             Debug.Log($"[엘리트 킬러] {monsterType} 몬스터에게 배율 {monsterTypeMultiplier * 100}% 적용! 최종 데미지: {finalDamage:F1}");
//                         }
//                     }
                    
//                     // 디버그 로그 출력
//                     Debug.Log($"[검 공격] 기본 공격력: {baseDamage}, 최종 공격력: {finalDamage:F1}, 적: {enemy.name}");
                    
//                     // ★ 순서 중요: 먼저 데미지 적용 (storedLastDamage 설정)
//                     monster.TakeDamage(finalDamage);
                    
//                     // ★ 그 다음 인챈트 적용 (storedLastDamage 기반 계산)
//                     int[] appliedEnchants = CalculateAppliedEnchants();
//                     monster.TakeElement(appliedEnchants);
//                 }
//             }

//             // 연속 공격 사이에 짧은 딜레이 (2번째 공격부터)
//             if (i < attackCount - 1)
//             {
//                 yield return new WaitForSeconds(0.1f);
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

//         Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, swordRange);

//         foreach (Collider2D col in colliders)
//         {
//             if (!col.CompareTag("Enemy")) continue;

//             Vector2 enemyDirection = (col.transform.position - transform.position).normalized;
//             float angle = Vector2.Angle(lastAttackDir, enemyDirection);

//             if (angle <= attackAngle / 2f)
//             {
//                 enemiesInRange.Add(col.gameObject);
//             }
//         }

//         // 부채꼴 범위 시각화용 (디버그)
//         Debug.DrawRay(transform.position, transform.right * swordRange, Color.red, 0.1f);
//     }

//     // 부채꼴 공격 범위 시각화
//     private void OnDrawGizmosSelected()
//     {
//         if (Application.isPlaying)
//         {
//             // 마우스 방향 계산
//             Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//             Vector2 direction = (mousePos - (Vector2)transform.position).normalized;

//             // 부채꼴 그리기
//             DrawSectorGizmo(transform.position, direction, swordRange, attackAngle, Color.red);
//         }
//         else
//         {
//             // 에디터에서 기본 방향으로 부채꼴 그리기
//             DrawSectorGizmo(transform.position, Vector2.right, swordRange, attackAngle, Color.yellow);
//         }
//     }

//     private void DrawSectorGizmo(Vector2 center, Vector2 direction, float radius, float angle, Color color)
//     {
//         Gizmos.color = color;

//         // 부채꼴의 중심선
//         Gizmos.DrawLine(center, center + direction * radius);

//         // 부채꼴의 양쪽 경계선
//         float halfAngle = angle * 0.5f;
//         Vector2 leftBoundary = Quaternion.Euler(0, 0, -halfAngle) * direction;
//         Vector2 rightBoundary = Quaternion.Euler(0, 0, halfAngle) * direction;

//         Gizmos.DrawLine(center, center + leftBoundary * radius);
//         Gizmos.DrawLine(center, center + rightBoundary * radius);

//         // 부채꼴의 호 그리기
//         int segments = 20;
//         float angleStep = angle / segments;
//         Vector2 prevPoint = center + leftBoundary * radius;

//         for (int i = 1; i <= segments; i++)
//         {
//             float currentAngle = -halfAngle + (angleStep * i);
//             Vector2 currentDirection = Quaternion.Euler(0, 0, currentAngle) * direction;
//             Vector2 currentPoint = center + currentDirection * radius;

//             Gizmos.DrawLine(prevPoint, currentPoint);
//             prevPoint = currentPoint;
//         }
//     }
    
//     // 장비 강화 시 스탯(공격력, 최대체력) 갱신 함수 필요
// }

using UnityEngine;
using System.Collections;

public class SwordWeapon : WeaponBase
{
    [Header("Sword Projectile Settings")]
    [SerializeField] private float projectileSpeed = 10f;     // 검기 날아가는 속도
    [SerializeField] private float projectileLifeTime = 1.5f; // 검기 유지 시간 (사거리 역할)
    
    [Header("Effect Settings")]
    [SerializeField] private float effectOffsetDistance = 0.8f; // 플레이어 중심에서 떨어져 생성되는 거리

    private Vector2 lastAttackDir;

    protected override void Start()
    {
        base.Start();
    }

    public override void Attack(Vector2 direction)
    {
        if (Time.time - lastAttackTime < GetAttackCooldown() || bIsAttacking)
        {
            return;
        }

        lastAttackDir = direction;
        StartCoroutine(PerformSwordAttack());
    }

    private IEnumerator PerformSwordAttack()
    {
        bIsAttacking = true;
        lastAttackTime = Time.time;
        
        LogAttackStats();

        int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;

        for (int i = 0; i < attackCount; i++)
        {
            // 검기 투사체 발사
            CreateAttackEffect(lastAttackDir);

            // 연속 공격 사이에 짧은 딜레이
            if (i < attackCount - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(attackDuration);
        bIsAttacking = false;
    }

    // 기존의 CreateAttackEffect를 투사체 발사 기능으로 변경
    protected override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab != null)
        {
            // 1. 회전값 및 생성 위치 계산
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 spawnPosition = transform.position + (Vector3)(direction * effectOffsetDistance);

            // 2. 검기 생성
            GameObject projectile = Instantiate(attackEffectPrefab, spawnPosition, rotation);
            
            // 3. 검기 스크립트 부착 확인 (없으면 추가)
            SwordProjectile swordProj = projectile.GetComponent<SwordProjectile>();
            if (swordProj == null)
            {
                swordProj = projectile.AddComponent<SwordProjectile>();
            }

            // 4. 데미지 및 인챈트 계산
            float baseFinalDamage = GetTotalDamage();
            int[] enchants = CalculateAppliedEnchants();

            // 5. 검기에 정보 전달 (방향, 속도, 수명, 데미지, 인챈트, 플레이어스탯)
            swordProj.Initialize(direction, projectileSpeed, projectileLifeTime, baseFinalDamage, enchants, playerStats);
        }
    }

    // 더 이상 자체적으로 적을 탐지하지 않음 (WeaponBase의 추상 메서드라면 빈 상태로 둡니다)
    public override void DetectEnemies()
    {
        enemiesInRange.Clear();
    }

    // 에디터에서 투사체 방식이므로 부채꼴 기즈모는 불필요해져 삭제하거나, 사거리 표시용 원형 기즈모로 변경하는 것을 권장합니다.
}