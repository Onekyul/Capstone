using UnityEngine;
using System.Collections;

public class SwordWeapon : WeaponBase
{
    [Header("Sword Specific")]
    [SerializeField] private float attackAngle = 90f;   // 부채꼴 각도
    [SerializeField] private LayerMask enemyLayer = 128; // Layer 7 (Enemy) - 2^7 = 128
    [SerializeField] private float swordRange = 1.3f; // 검의 공격 범위
    
    private Vector2 lastAttackDir;
   

    protected override void Start()
    {
        base.Start(); // WeaponBase의 Start 호출 (PlayerStats 초기화)
    }

    public override void Attack(Vector2 direction) // WeaponBase.cs 의 Attack 추상 메소드 재정의
    {
        
        if (Time.time - lastAttackTime < GetAttackCooldown() || bIsAttacking) //쿨타임 체크 || 공격 중인치 체크
        {
            return;
        }
        
        lastAttackDir=direction;
        StartCoroutine(PerformSwordAttack()); // 체크해서 맞으면 코루틴 시작
    }
    
    private IEnumerator PerformSwordAttack()
    {
        bIsAttacking = true;
        lastAttackTime = Time.time;
        
        // 공격 횟수 확인 (2연격 등)
        int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;
        
        for (int i = 0; i < attackCount; i++)
        {
            // 부채꼴 범위 내 적들 감지
            DetectEnemies();
            
            // 공격 이펙트 생성
            CreateAttackEffect(lastAttackDir);
            
            // 범위 내 모든 적에게 데미지
            foreach (GameObject enemy in enemiesInRange)
            {
                if (enemy != null)
                {
                    // 인챈트 적용 계산
                    int[] appliedEnchants = CalculateAppliedEnchants();
                    
                    // 인챈트가 적용되었다면 TakeElement 호출
                    // enemy.GetComponent<MonsterController>()?.TakeElement(appliedEnchants);
                    
                    // 기본 데미지 적용
                    enemy.GetComponent<MonsterController>()?.TakeDamage(GetTotalDamage());
                }
            }
            
            // 연속 공격 사이에 짧은 딜레이 (2번째 공격부터)
            if (i < attackCount - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        yield return new WaitForSeconds(attackDuration);
        bIsAttacking = false;
    }
    
    
    protected override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab != null)
        {
            GameObject effect = Instantiate(attackEffectPrefab, attackPoint.position, 
                Quaternion.LookRotation(Vector3.forward, direction));
            Destroy(effect, attackDuration);
        }
    }
    
  

    public override void DetectEnemies()
    {
        enemiesInRange.Clear();
        
        // 부채꼴 범위 내의 모든 콜라이더 감지 (모든 레이어)
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, swordRange);
        
        // Enemy 레이어만 필터링
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, swordRange, enemyLayer);
        
        foreach (Collider2D col in colliders)
        {
            Vector2 enemyDirection = (col.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(lastAttackDir, enemyDirection);
            
            if (angle <= attackAngle / 2f)
            {
                enemiesInRange.Add(col.gameObject);
        
            }
        }

        // 부채꼴 범위 시각화용 (디버그)
        Debug.DrawRay(transform.position, transform.right * swordRange, Color.red, 0.1f);
    }
    
    // 부채꼴 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // 마우스 방향 계산
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
            
            // 부채꼴 그리기
            DrawSectorGizmo(transform.position, direction, swordRange, attackAngle, Color.red);
        }
        else
        {
            // 에디터에서 기본 방향으로 부채꼴 그리기
            DrawSectorGizmo(transform.position, Vector2.right, swordRange, attackAngle, Color.yellow);
        }
    }
    
    private void DrawSectorGizmo(Vector2 center, Vector2 direction, float radius, float angle, Color color)
    {
        Gizmos.color = color;
        
        // 부채꼴의 중심선
        Gizmos.DrawLine(center, center + direction * radius);
        
        // 부채꼴의 양쪽 경계선
        float halfAngle = angle * 0.5f;
        Vector2 leftBoundary = Quaternion.Euler(0, 0, -halfAngle) * direction;
        Vector2 rightBoundary = Quaternion.Euler(0, 0, halfAngle) * direction;
        
        Gizmos.DrawLine(center, center + leftBoundary * radius);
        Gizmos.DrawLine(center, center + rightBoundary * radius);
        
        // 부채꼴의 호 그리기
        int segments = 20;
        float angleStep = angle / segments;
        Vector2 prevPoint = center + leftBoundary * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -halfAngle + (angleStep * i);
            Vector2 currentDirection = Quaternion.Euler(0, 0, currentAngle) * direction;
            Vector2 currentPoint = center + currentDirection * radius;
            
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}