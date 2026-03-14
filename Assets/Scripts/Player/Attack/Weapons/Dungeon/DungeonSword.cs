using UnityEngine;
using Fusion;

public class DungeonSword : DungeonWeaponBase
{
    [Header("Sword Specific")]
    [SerializeField] private float attackAngle = 90f;   
    [SerializeField] private LayerMask enemyLayer = 128; // Layer 7 (Enemy) - 2^7 = 128
    [SerializeField] private float swordRange = 2.0f; 

    [Header("Effect Settings")]
    [SerializeField] private float effectOffsetDistance = 0.8f;
    [SerializeField] private GameObject attackEffectPrefab; // [주의] 이건 그냥 GameObject로 둬도 됩니다. 

    // ★ 오직 서버에서만 실행되는 진짜 타격 판정
    protected override void ExecuteServerAttack(Vector2 direction)
    {
        // 1. 범위 내 적 찾기 (물리 엔진)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, swordRange, enemyLayer);

        foreach (Collider2D col in colliders)
        {
            // 2. 각도(부채꼴) 안에 있는지 검사
            Vector2 enemyDirection = (col.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(direction, enemyDirection);

            if (angle <= attackAngle / 2f)
            {
                // TODO: 멀티플레이용 몬스터 스크립트로 변경해야 함! (예: DungeonMonsterController)
                // 현재는 기존 스크립트 이름 사용 (나중에 에러 나면 수정)
                MonsterController monster = col.GetComponent<MonsterController>(); 
                if (monster != null)
                {
                    float finalDamage = GetTotalDamage();
                    int[] appliedEnchants = CalculateAppliedEnchants();
                    
                    // 데미지 입히기 (서버가 몬스터 체력을 깎음)
                    monster.TakeDamage(finalDamage);
                    monster.TakeElement(appliedEnchants);

                    Debug.Log($"[서버] {col.name} 썰어버림! 데미지: {finalDamage}");
                }
            }
        }
    }

    // 모든 클라이언트가 자기 화면에 각자 그리는 시각 효과 (RPC로 호출됨)
    protected override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 spawnPosition = transform.position + (Vector3)(direction * effectOffsetDistance);

            // 퓨전의 Runner.Spawn이 아니라 그냥 Instantiate를 씁니다!
            // 왜냐하면 RPC를 통해 모든 클라이언트가 이 함수를 각자 실행하므로, 알아서 자기 화면에 하나씩 그리기 때문입니다. (최적화)
            GameObject effect = Instantiate(attackEffectPrefab, spawnPosition, rotation);
            Destroy(effect, attackDuration); 
        }
    }
}