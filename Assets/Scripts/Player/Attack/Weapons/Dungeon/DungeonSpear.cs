using UnityEngine;
using Fusion;

public class DungeonSpear : DungeonWeaponBase
{
    [Header("Spear Specific")]
    private float spearRange = 7.0f;
    private float attackWidth = 2.0f;
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileLifeTime = 0.15f;

    // ★ 오직 서버(데디케이티드)에서만 실행되는 진짜 타격 판정
    protected override void ExecuteServerAttack(Vector2 direction)
    {
        // Physics2D 대신 transform.position 직접 비교 → 이동 중 위치 desync 방지
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Vector2 attackOrigin = transform.position;
        Vector2 perpDir = new Vector2(-direction.y, direction.x); // 공격 방향 수직

        foreach (var enemy in enemies)
        {
            if (!enemy.activeInHierarchy) continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - attackOrigin;

            // 전방 거리 체크 (공격 방향 기준)
            float forwardDist = Vector2.Dot(toEnemy, direction);
            if (forwardDist < 0f || forwardDist > spearRange) continue;

            // 측면 거리 체크 (폭 기준)
            float lateralDist = Mathf.Abs(Vector2.Dot(toEnemy, perpDir));
            if (lateralDist > attackWidth / 2f) continue;

            MonsterController monster = enemy.GetComponentInParent<MonsterController>();
            if (monster != null)
            {
                float finalDamage = GetTotalDamage();
                int[] appliedEnchants = CalculateAppliedEnchants();
                monster.TakeDamage(finalDamage);
                monster.TakeElement(appliedEnchants);
                Debug.Log($"[서버] 창 찌르기 적중! {enemy.name}에게 {finalDamage} 데미지!");
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

        SpearProjectile proj = effect.GetComponent<SpearProjectile>();
        if (proj == null)
            proj = effect.AddComponent<SpearProjectile>();
        proj.Initialize(direction, projectileSpeed, 0.3f, 0f, null);
    }
}
