using UnityEngine;

public class ChasingMonsterController : MonsterController
{
    [SerializeField] private EliteChestController eliteChestPrefab;
    [Tooltip("이 몬스터가 노말인지 엘리트인지 여부. 체크 되면 엘리트임.")]
    [SerializeField] private bool IsElite = false;
    [Tooltip("몇 퍼센트 확률로 엘리트 몬스터가 나올게 할 건지. 0~100 사이 값.")]
    [Range(0, 100)]
    [SerializeField] private float eliteSpawnChance = 1;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 몬스터가 활성화될 때마다 엘리트 여부를 다시 결정
        IsElite = Random.Range(0f, 100f) < eliteSpawnChance;
        UpgradeEliteStats();
    }

    private void UpgradeEliteStats()
    {
        if (IsElite)
        {
            MaxHP *= 2.0f; // 엘리트 몬스터의 체력을 2배로 증가
            CurHP = MaxHP; // 현재 체력을 최대 체력으로 설정
            contactDamage *= 1.5f; // 엘리트 몬스터의 접촉 데미지를 1.5배로 증가
            defaultMoveSpeed *= 1.2f; // 엘리트 몬스터의 이동 속도를 20% 증가
            currentMoveSpeed = defaultMoveSpeed;
            if (hpSlider != null)
            {
                hpSlider.value = 1.0f;
                hpSlider.gameObject.SetActive(true); // 혹시 꺼져있으면 켜기
            }
        }
    }

    protected override void ReturnToPool()
    {
        base.ReturnToPool(); // 부모 클래스의 ReturnToPool 메서드 호출
                           // 엘리트 몬스터일 경우 엘리트 상자 드랍. 대신 보석보다 더 아래에서 나옴.
        if (IsElite && eliteChestPrefab != null)
        {
            Instantiate(eliteChestPrefab, transform.position + Vector3.down * 0.3f, Quaternion.identity);
        }
    }

}
