using UnityEngine;
using Fusion;

public class DungeonSpear : DungeonWeaponBase
{
    [Header("Spear Specific")]
    [SerializeField] private float spearRange = 3f;
    [SerializeField] private float attackWidth = 0.5f;
    [SerializeField] private LayerMask enemyLayer = 128; // Layer 7 (Enemy) - 2^7 = 128
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileLifeTime = 0.15f;

    // ★ 오직 서버(데디케이티드)에서만 실행되는 진짜 타격 판정
    protected override void ExecuteServerAttack(Vector2 direction)
    {
        // 1. 박스 중심점 및 각도 계산
        Vector2 boxCenter = (Vector2)transform.position + direction * (spearRange / 2);
        Vector2 boxSize = new Vector2(spearRange, attackWidth);
        float angle = Vector2.SignedAngle(Vector2.right, direction);

        // 2. 범위 내 적 찾기 (태그 기반 - 레이어 설정 무관하게 동작)
        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle);

        foreach (Collider2D col in colliders)
        {
            if (!col.CompareTag("Enemy")) continue;
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
    public override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab == null)
        {
            Debug.LogWarning($"[DungeonSpear] attackEffectPrefab이 null입니다! 오브젝트: {gameObject.name}");
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector3 spawnPosition = attackPoint != null ? attackPoint.position : transform.position;

        GameObject effect = Instantiate(attackEffectPrefab, spawnPosition, rotation);

        // 시각 효과 전용: damage=0 → 투사체가 날아가지만 데미지는 서버에서 처리
        SpearProjectile proj = effect.GetComponent<SpearProjectile>();
        if (proj == null)
            proj = effect.AddComponent<SpearProjectile>();
        proj.Initialize(direction, projectileSpeed, 0.3f, 0f, null);
    }
}