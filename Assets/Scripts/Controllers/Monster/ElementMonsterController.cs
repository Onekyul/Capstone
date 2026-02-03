using UnityEngine;

// MonsterController를 상속받아 기본 기능(HP, 피격 등)은 가져옵니다.
public class ElementMonsterController : MonsterController
{
    [Header("Element Monster Settings")]
    [SerializeField] protected float attackRange = 10f;
    [SerializeField] protected float attackCooldown = 3f;
    [SerializeField] private ElementChestController elementChestPrefab;

    protected float lastAttackTime;


    protected override void Start()
    {
        base.Start();
    }


    protected override void OnEnable()
    {
        // 1. 부모의 ResetStatus()를 먼저 실행 (타입이 Normal로 리셋됨)
        base.OnEnable();

        // 2. 속성 몬스터로 타입 재설정
        monsterType = MonsterType.Element;

        // 3. 스폰 즉시 공격이 가능하도록 쿨타임 초기화
        lastAttackTime = Time.time - attackCooldown;
    }

    protected override void Update()
    {
        // 죽음 체크는 frozen 상태에서도 해야 함
        if (IsDead())
        {
            ReturnToPool();
            return;
        }

        // 1. 상태이상(빙결 등) 체크
        if (isFrozen) return;

        player = GetClosestPlayer();
        if (player == null) return;

        // 2. 플레이어 바라보기 
        FlipSpriteTowardsPlayer();

        // 3. 거리 계산
        float distance = Vector2.Distance(transform.position, player.position);

        // 4. 사거리 안이고 + 쿨타임이 돌았으면 -> 공격 실행
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    // 자식들이 구체적인 공격
    protected virtual void PerformAttack()
    {
        Debug.Log("기본 원거리 공격 (설정 필요)");
    }

    protected override void ReturnToPool()
    {

        // 엘리멘탈 몬스터가 죽을 때 엘리멘탈 상자 드랍
        if (elementChestPrefab != null)
        {
            Instantiate(elementChestPrefab, transform.position + Vector3.down * 0.3f, Quaternion.identity);
        }
        base.ReturnToPool(); // 부모 클래스의 ReturnToPool 메서드 호출
    }
}