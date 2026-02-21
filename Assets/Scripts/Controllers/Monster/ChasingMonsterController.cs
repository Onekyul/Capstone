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

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        // FollowPlayer()는 Move()를 오버라이드하여 처리하므로 여기서 호출하지 않음
    }

    protected override void OnEnable()
    {
        // 1. 여기서 부모의 ResetStatus()가 실행되어 모든 스탯이 원본(100%)으로 돌아감
        base.OnEnable();

        // 2. 깨끗한 상태에서 확률에 따라 엘리트 여부 결정
        IsElite = UnityEngine.Random.Range(0f, 100f) < eliteSpawnChance;
        UpgradeEliteStats();
    }

    private void UpgradeEliteStats()
    {
        if (IsElite)
        {
            monsterType = MonsterType.Elite; // 타입 갱신 추가
            // 원본에서 리셋된 값에 곱하기를 수행하므로 누적되지 않음
            // 5. 무조건 (1.5, 1.5, 1)이 아니라 원래 크기의 1.5배가 되도록 수정
            transform.localScale = defaultScale * 1.5f;
            //스탯 업그레이드
            MaxHP *= 2.0f;
            CurHP = MaxHP;
            contactDamage *= 1.5f;
            baseMoveSpeed *= 1.2f;
            currentMoveSpeed = baseMoveSpeed;

            if (hpSlider != null)
            {
                hpSlider.value = 1.0f;
                hpSlider.gameObject.SetActive(true);
            }
        }
    }

    // 부모의 Move()를 오버라이드하여 플레이어 추적 로직 구현
    // ★ currentMoveSpeed를 사용해야 슬로우/빙결 효과가 정상 적용됨!
    protected override void Move()
    {
        if (player == null) return;
        Vector2 targetPosition = new Vector2(player.position.x, player.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, currentMoveSpeed * Time.deltaTime);
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
