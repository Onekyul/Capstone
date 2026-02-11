using UnityEngine;

public class WaterMonsterController : ElementMonsterController
{
    [Header("Water Skill")]
    [SerializeField] private GameObject waterWavePrefab; // 파도 프리팹
    [SerializeField] private float waveSpeed = 6.0f;     // 파도 속도
    [SerializeField] private float damage = 15.0f;        // 파도 공격력

    // ElementMonsterController의 PerformAttack을 구체화
    protected override void PerformAttack()
    {
        if (waterWavePrefab == null) return;

        // 1. 플레이어 방향 계산
        Vector3 targetDir = (player.position - transform.position).normalized;

        // 2. 파도 생성
        GameObject waveObj = Instantiate(waterWavePrefab, transform.position, Quaternion.identity);

        // 3. 파도 설정 (방향, 속도, 공격력 전달)
        // normalDamage는 부모(MonsterController)에 있는 변수 사용
        WaterWaveController wave = waveObj.GetComponent<WaterWaveController>();
        if (wave != null)
        {
            wave.Setup(targetDir, waveSpeed, damage);
        }
    }
}