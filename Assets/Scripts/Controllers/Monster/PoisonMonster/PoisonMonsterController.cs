using UnityEngine;

public class PoisonMonsterController : ElementMonsterController
{
    [Header("Poison Skill")]
    [SerializeField] private GameObject sporePrefab;   // 독 포자 프리팹
    [SerializeField] private float sporeSpeed = 2.0f;  // 포자 속도 (아주 느리게)
    [SerializeField] private float sporeLife = 5.0f;   // 포자 유지 시간
    [SerializeField] private float explosionRadius = 2.0f; // 폭발 범위
    [SerializeField] private float explosionDamage = 10.0f; // 폭발 데미지

    protected override void PerformAttack()
    {
        if (sporePrefab == null || player == null) return;

        // 1. 포자 생성 (몬스터 위치에서)
        GameObject sporeObj = Instantiate(sporePrefab, transform.position, Quaternion.identity);

        // 2. 포자 셋팅 (타겟, 속도, 데미지, 수명, 범위 전달)
        PoisonSporeController spore = sporeObj.GetComponent<PoisonSporeController>();
        if (spore != null)
        {
            spore.Setup(player, sporeSpeed, explosionDamage, sporeLife, explosionRadius);
        }
    }
}