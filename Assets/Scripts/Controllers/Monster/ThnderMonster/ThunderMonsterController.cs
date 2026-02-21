using UnityEngine;
using System.Collections;

public class ThunderMonsterController : ElementMonsterController
{
    [Header("Thunder Skill")]
    [SerializeField] private GameObject thunderPatternPrefab; // 위에서 만든 패턴 프리팹
    [SerializeField] private int burstCount = 3;               // 연속 번개 횟수
    [SerializeField] private float burstInterval = 0.1f;       // 번개 간격
    [SerializeField] private float warningDuration = 1.0f;      // 경고 시간
    [SerializeField] private float strikeDamage = 10f;          // 번개 1발당 데미지
    [SerializeField] private float strikeRadius = 1.5f;         // 번개 피격 범위
    [SerializeField] private float strikeVfxDuration = 0.1f;    // 번개 이펙트 노출 시간
    [SerializeField] private float fieldDuration = 3.0f;        // 장판 유지 시간
    [SerializeField] private float fieldDamagePerTick = 2.0f;   // 장판 틱 데미지

    private bool isBursting;

    protected override void PerformAttack()
    {
        if (thunderPatternPrefab == null) return;
        if (isBursting) return;

        StartCoroutine(SpawnBurstAtPlayerPositions());
    }

    private IEnumerator SpawnBurstAtPlayerPositions()
    {
        isBursting = true;

        for (int i = 0; i < burstCount; i++)
        {
            Vector3 targetPos = player.position;
            GameObject patternObj = Instantiate(thunderPatternPrefab, targetPos, Quaternion.identity);
            ThunderPatternController pattern = patternObj.GetComponent<ThunderPatternController>();
            if (pattern != null)
            {
                pattern.Setup(
                    warningDuration,
                    strikeDamage,
                    strikeRadius,
                    strikeVfxDuration,
                    fieldDuration,
                    fieldDamagePerTick
                );
            }

            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        isBursting = false;
    }
}