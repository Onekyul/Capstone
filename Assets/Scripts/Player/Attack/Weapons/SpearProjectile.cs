using UnityEngine;

public class SpearProjectile : MonoBehaviour
{
    private Vector2 moveDirection;
    private float speed;
    private float baseDamage;
    private PlayerStats playerStats;

    // 무기 스크립트에서 초기화할 때 호출
    public void Initialize(Vector2 direction, float speed, float lifeTime, float damage, PlayerStats stats)
    {
        this.moveDirection = direction.normalized;
        this.speed = speed;
        this.baseDamage = damage;
        this.playerStats = stats;

        // 지정된 시간(lifeTime)이 지나면 창 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 매 프레임 지정된 방향으로 이동하여 날아가는 효과 구현
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    // 창이 날아가다가 적과 부딪혔을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            MonsterController monster = collision.GetComponent<MonsterController>();
            if (monster != null)
            {
                float finalDamage = baseDamage;

                // 엘리트 킬러 능력 적용
                if (playerStats != null)
                {
                    MonsterType monsterType = monster.GetMonsterType();
                    float multiplier = playerStats.GetMonsterTypeDamageMultiplier(monsterType);
                    finalDamage *= multiplier;
                    
                    if (multiplier != 1.0f)
                    {
                        Debug.Log($"[엘리트 킬러] {monsterType} 몬스터에게 배율 {multiplier * 100}% 적용! 최종 데미지: {finalDamage:F1}");
                    }
                }

                // 몬스터에게 데미지 전달
                monster.TakeDamage(finalDamage);

                // 창이 적을 관통하지 않고 부딪힌 즉시 사라지게 하려면 아래 주석을 해제하세요.
                // Destroy(gameObject); 
            }
        }
    }
}