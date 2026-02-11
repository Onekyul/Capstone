using UnityEngine;
using System.Collections;

public class ThunderMonsterController : ElementMonsterController
{
    [Header("Thunder Skill")]
    [SerializeField] private GameObject thunderPatternPrefab; // 위에서 만든 패턴 프리팹
    [SerializeField] private int burstCount = 3;               // 연속 번개 횟수
    [SerializeField] private float burstInterval = 0.1f;       // 번개 간격

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
            Instantiate(thunderPatternPrefab, targetPos, Quaternion.identity);

            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        isBursting = false;
    }
}