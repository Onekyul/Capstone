using UnityEngine;
using Fusion;

public class DungeonSword : DungeonWeaponBase
{
    [Header("Sword Specific")]
    [SerializeField] private float attackAngle = 90f;
    private float swordRange = 5.0f;

    [Header("Effect Settings")]
    [SerializeField] private float effectOffsetDistance = 0.8f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifeTime = 0.15f;
    [SerializeField] private GameObject attackEffectPrefab;

    // ★ 오직 서버에서만 실행되는 진짜 타격 판정
    protected override void ExecuteServerAttack(Vector2 direction)
    {
        // Physics2D 대신 transform.position 직접 비교 → 이동 중 위치 desync 방지
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var enemy in enemies)
        {
            if (!enemy.activeInHierarchy) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > swordRange) continue;

            Vector2 enemyDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Angle(direction, enemyDir) > attackAngle / 2f) continue;

            MonsterController monster = enemy.GetComponentInParent<MonsterController>();
            if (monster != null)
            {
                float finalDamage = GetTotalDamage();
                int[] appliedEnchants = CalculateAppliedEnchants();
                monster.TakeDamage(finalDamage);
                monster.TakeElement(appliedEnchants);
            }
        }
    }

    // 모든 클라이언트가 자기 화면에 각자 그리는 시각 효과 (RPC로 호출됨)
    public override void CreateAttackEffect(Vector2 direction)
    {
        if (attackEffectPrefab == null)
        {
            Debug.LogWarning($"[DungeonSword] attackEffectPrefab이 null입니다! 오브젝트: {gameObject.name}");
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector3 spawnPosition = transform.position + (Vector3)(direction * effectOffsetDistance);

        GameObject effect = Instantiate(attackEffectPrefab, spawnPosition, rotation);

        SwordProjectile proj = effect.GetComponent<SwordProjectile>();
        if (proj == null)
            proj = effect.AddComponent<SwordProjectile>();
        proj.Initialize(direction, projectileSpeed, 0.3f, 0f, null, null);
    }
}
