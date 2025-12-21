using UnityEngine;
using System.Collections;


public class BowWeapon : WeaponBase
{
    [Header("Bow Specific")]
    [SerializeField] private GameObject arrowPrefab;
    private Vector2 lastAttackDir;

    
    public override void Attack(Vector2 direction)
    {
        if (Time.time - lastAttackTime < attackCooldown || bIsAttacking)
        {
            return;
        }

        lastAttackDir = direction;
        StartCoroutine(PerformBowAttack(direction));
    }

    private IEnumerator PerformBowAttack(Vector2 direction)
    {
        bIsAttacking = true;
        lastAttackTime = Time.time;

        // 공격 횟수 확인 (2연격 등)
        int attackCount = playerStats != null ? playerStats.GetAttackCount() : 1;
        
        for (int i = 0; i < attackCount; i++)
        {
            // 1. 화살 프리펩을 attackPoint 위치에 생성
            GameObject arrowObject = Instantiate(arrowPrefab, attackPoint.position, 
                Quaternion.LookRotation(Vector3.forward, direction));
                
            // 2. 생성된 화살의 Arrow.cs 스크립트를 가져옴
            Arrow arrow = arrowObject.GetComponent<Arrow>();
            if (arrow != null)
            {
                // 3. 활의 데미지(damage) 값을 화살에게 전달
                arrow.setDamage(GetTotalDamage());
            }

            // 공격 애니메이션이나 사운드 재생 등을 여기서 처리
            CreateAttackEffect(direction);
            
            // 연속 공격 사이에 짧은 딜레이 (2번째 화살부터)
            if (i < attackCount - 1)
            {
                yield return new WaitForSeconds(0.15f);
            }
        }
        
        yield return new WaitForSeconds(attackDuration); // 공격 후 짧은 딜레이
        bIsAttacking = false;
    }
    
    // 활은 투사체가 적을 감지하므로, 이 함수는 비워둬도 괜찮습니다.
    public override void DetectEnemies()
    {
        enemiesInRange.Clear();
    }

    protected override void CreateAttackEffect(Vector2 direction)
    {
        // 활 시위를 당기는 소리 재생 등의 효과를 여기에 구현할 수 있습니다.
    }
}
