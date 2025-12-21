using UnityEngine;
using System.Collections;


public class SpearWeapon : WeaponBase
{
    [Header("Spear Specific")]
    [SerializeField] private float spearRange = 3f;   // 공격 각도
    [SerializeField] private LayerMask enemyLayer = 128; // Layer 7 (Enemy) - 2^7 = 128
    [SerializeField] private float attackWidth = 0.5f;
    
    private Vector2 lastAttackDir;

    public override void Attack(Vector2 direction) // WeaponBase.cs 의 Attack 추상 메소드 재정의
    {
        if (Time.time - lastAttackTime < GetAttackCooldown() || bIsAttacking) //쿨타임 체크 || 공격 중인치 체크
        {
            return;
        }
        
        lastAttackDir=direction;
        StartCoroutine(PerformSpearAttack()); // 체크해서 맞으면 코루틴 시작
    }
    
    private IEnumerator PerformSpearAttack()
    {
        bIsAttacking = true;
        lastAttackTime = Time.time;
        
        // 공격 횟수 확인 (2연격 등)
        int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;
        
        for (int i = 0; i < attackCount; i++)
        {
            // 직선 범위 내 적들 감지
            DetectEnemies();
            
            // 공격 이펙트 생성
            CreateAttackEffect(lastAttackDir);
            
            // 범위 내 모든 적에게 데미지
            foreach (GameObject enemy in enemiesInRange)
            {
                if (enemy != null)
                {
                    enemy.GetComponent<MonsterController>()?.TakeDamage(GetTotalDamage());
                }
            }
            
            // 연속 공격 사이에 짧은 딜레이 (2번째 공격부터)
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
            GameObject effect = Instantiate(attackEffectPrefab, attackPoint.position, 
                Quaternion.LookRotation(Vector3.forward, direction));
            Destroy(effect, attackDuration);
        }
    }



    public override void DetectEnemies()
    {
        enemiesInRange.Clear();

        Vector2 boxCenter = (Vector2)transform.position + lastAttackDir * (spearRange / 2);
        Vector2 boxSize = new Vector2(spearRange, attackWidth);

        float angle = Vector2.SignedAngle(Vector2.right, lastAttackDir);

        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, enemyLayer);

        foreach (Collider2D col in colliders)
        {
            enemiesInRange.Add(col.gameObject);
        }
    }
    
    
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && Camera.main != null)
        {
            // 1. 마우스 위치를 실시간으로 계산합니다. (SwordWeapon과 동일한 로직)
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
        
            // 2. 계산된 마우스 방향으로 기즈모를 그립니다.
            DrawBoxGizmo(direction, Color.red);
            }
        else
        {
            // 게임 실행 중이 아닐 때는 기본 방향(오른쪽)으로 그립니다.
            DrawBoxGizmo(Vector2.right, Color.yellow);
        }
    }
    
    private void DrawBoxGizmo(Vector2 direction, Color color)
    {
       if (direction == Vector2.zero) direction = Vector2.right; // 방향이 없을 때 기본값

        Vector2 boxCenter = (Vector2)transform.position + direction * (spearRange / 2);
        Vector2 boxSize = new Vector2(spearRange, attackWidth);
        float angle = Vector2.SignedAngle(Vector2.right, direction);

        // Gizmos의 위치, 회전, 크기를 설정
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, angle), Vector3.one);
        Gizmos.matrix = rotationMatrix;

        Gizmos.color = color;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Gizmos 설정을 원래대로 되돌림
        Gizmos.matrix = Matrix4x4.identity;
    }
}
