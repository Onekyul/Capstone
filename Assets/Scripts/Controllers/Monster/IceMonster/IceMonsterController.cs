using UnityEngine;
using System.Collections;

public class IceMonsterController : ElementMonsterController
{
    [Header("Ice Skill")]
    [SerializeField] private GameObject iceClusterPrefab; // 아까 만든 얼음 덩어리 프리팹
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float bodyDuration = 2.0f;
    [SerializeField] private float iceContactDamage = 10f;
    [SerializeField] private float shardDamage = 5f;
    [SerializeField] private float shardSpeed = 5f;
    [SerializeField] private int shardCount = 6;
    [SerializeField] private float shardLifetime = 3.0f;

    private float attackTimer;

    protected override void Start()
    {
        base.Start();
        attackTimer = attackCooldown; // 시작하자마자 쏘지 않게 쿨타임 채워두기
    }

    protected override void Update()
    {
        base.Update();

        if (player == null) return;

        attackTimer -= Time.deltaTime;

        // 쿨타임이 찼고, 플레이어와의 거리가 적당하면 공격
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (attackTimer <= 0 && distToPlayer <= attackRange)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }

    protected override void PerformAttack()
    {
        // 플레이어의 현재 위치에 소환! (플레이어가 움직이게 강제함)
        // 약간의 오차를 주고 싶다면 Random.insideUnitCircle 사용
        Vector3 targetPos = player.position;
        GameObject clusterObj = Instantiate(iceClusterPrefab, targetPos, Quaternion.identity);
        IceClusterController cluster = clusterObj.GetComponent<IceClusterController>();
        if (cluster != null)
        {
            cluster.Setup(
                warningDuration,
                bodyDuration,
                iceContactDamage,
                shardDamage,
                shardSpeed,
                shardCount,
                shardLifetime
            );
        }
    }
}