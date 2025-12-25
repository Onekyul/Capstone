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
        // 시작하자마자 바로 공격할 수 있게 하거나, 딜레이를 주거나 설정
        lastAttackTime = -attackCooldown;
    }

    protected override void Update()
    {
        // 1. 상태이상(빙결 등) 체크
        // (MonsterController의 변수들을 protected로 바꿔주셔야 접근 가능합니다)
        // if (isFrozen) return; 

        player = GetClosestPlayer();
        if (player == null) return;

        // 2. 플레이어 바라보기 (고정형이라도 쳐다는 봐야죠)
        FlipSpriteTowardsPlayer();

        // 3. 거리 계산
        float distance = Vector2.Distance(transform.position, player.position);

        // 4. 사거리 안이고 + 쿨타임이 돌았으면 -> 공격 실행
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack(); // 자식이 구현할 함수 호출
            lastAttackTime = Time.time;
        }

        // ★ 중요: 부모(MonsterController)의 Update를 부르지 않음으로써 이동 로직을 제거합니다.
        // 대신 사망 체크는 필요하므로 따로 호출하거나 직접 작성합니다.
        // if (IsDead()) ReturnToPool(); 
        // (IsDead가 private이면 protected로 바꾸거나 프로퍼티 사용)
    }

    // 자식들이 구체적인 공격을 작성할 빈 함수 (Virtual)
    protected virtual void PerformAttack()
    {
        // 자식 클래스에서 override해서 사용함
        Debug.Log("기본 원거리 공격 (설정 필요)");
    }

    protected override void ReturnToPool()
    {
        base.ReturnToPool(); // 부모 클래스의 ReturnToPool 메서드 호출
        // 엘리멘탈 몬스터가 죽을 때 엘리멘탈 상자 드랍
        if (elementChestPrefab != null)
        {
            Instantiate(elementChestPrefab, transform.position + Vector3.down * 0.3f, Quaternion.identity);
        }
    }
}