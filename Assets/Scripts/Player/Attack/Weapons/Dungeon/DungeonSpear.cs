using UnityEngine;
using Fusion;

public class DungeonSpear : DungeonWeaponBase
{
    [Header("Spear Specific")]
    [SerializeField] private float spearRange = 3f;   
    [SerializeField] private float attackWidth = 0.5f;
    [SerializeField] private LayerMask enemyLayer; 
    [SerializeField] private GameObject attackEffectPrefab; // 시각 이펙트 프리팹

    // ★ 오직 서버(데디케이티드)에서만 실행되는 진짜 타격 판정
    protected override void ExecuteServerAttack(Vector2 direction)
    {
        // 1. 박스 중심점 및 각도 계산
        Vector2 boxCenter = (Vector2)transform.position + direction * (spearRange / 2);
        Vector2 boxSize = new Vector2(spearRange, attackWidth);
        float angle = Vector2.SignedAngle(Vector2.right, direction);

        // 2. 범위 내 적 찾기 (서버 물리엔진)
        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, enemyLayer);

        foreach (Collider2D col in colliders)
        {
            MonsterController monster = col.GetComponent<MonsterController>();
            if (monster != null)
            {
                float finalDamage = GetTotalDamage();
                int[] appliedEnchants = CalculateAppliedEnchants();
                
                // 엘리트 킬러 같은 특수 능력은 여기서 playerStats.GetMonsterTypeDamageMultiplier() 등으로 추가 계산 가능
                
                // 서버가 몬스터 체력을 깎음
                monster.TakeDamage(finalDamage);
                monster.TakeElement(appliedEnchants);

                Debug.Log($"[서버] 창 찌르기 적중! {col.name}에게 {finalDamage} 데미지!");
            }
        }
    }

    // ★ 모든 클라이언트가 자기 화면에 그리는 시각 효과 (RPC)
    protected override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // 이펙트 생성 후 attackDuration 뒤에 알아서 삭제됨
            GameObject effect = Instantiate(attackEffectPrefab, attackPoint.position, rotation);
            Destroy(effect, attackDuration);
        }
    }
}