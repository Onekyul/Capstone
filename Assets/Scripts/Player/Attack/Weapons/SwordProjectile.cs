using UnityEngine;

public class SwordProjectile : MonoBehaviour
{
    private Vector2 moveDirection;
    private float speed;
    private float baseDamage;
    private int[] appliedEnchants;
    private PlayerStats playerStats; // 엘리트 킬러 계산을 위해 참조

    // 검기가 생성될 때 SwordWeapon에서 호출하여 초기 설정값을 전달합니다.
    public void Initialize(Vector2 direction, float speed, float lifeTime, float damage, int[] enchants, PlayerStats stats)
    {
        this.moveDirection = direction.normalized;
        this.speed = speed;
        this.baseDamage = damage;
        this.appliedEnchants = enchants;
        this.playerStats = stats;

        // 지정된 시간이 지나면 검기 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 매 프레임 지정된 방향으로 이동
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    // Trigger 충돌 감지 (적과 부딪혔을 때)
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
                    float monsterTypeMultiplier = playerStats.GetMonsterTypeDamageMultiplier(monsterType);
                    finalDamage *= monsterTypeMultiplier;
                    
                    if (monsterTypeMultiplier != 1.0f)
                    {
                        Debug.Log($"[엘리트 킬러] {monsterType} 몬스터에게 배율 {monsterTypeMultiplier * 100}% 적용! 최종 데미지: {finalDamage:F1}");
                    }
                }

                Debug.Log($"[검기 적중] 기본 데미지: {baseDamage}, 최종 데미지: {finalDamage:F1}, 적: {collision.name}");

                // 데미지 및 인챈트 적용 (damage=0이면 시각 효과 전용이므로 스킵)
                if (baseDamage > 0)
                {
                    monster.TakeDamage(finalDamage);
                    if (appliedEnchants != null && appliedEnchants.Length > 0)
                    {
                        monster.TakeElement(appliedEnchants);
                    }
                }

                // 적중 시 검기를 파괴할지 여부
                // 관통(Penetration) 공격을 원하시면 아래 줄을 주석 처리하세요.
                // Destroy(gameObject); 
            }
        }
    }
}