using UnityEngine;
using System.Collections;

public class IceClusterController : MonoBehaviour
{
    [Header("Settings")]
     private float warningDuration = 1.5f; // 경고 시간
     private float bodyDuration = 2.0f;    // 얼음 덩어리 유지 시간
     private float contactDamage = 10f;    // 얼음 덩어리에 닿았을 때 데미지
     private float shardDamage = 5f;       // 조각 데미지
     private float shardSpeed = 5f;        // 조각 날아가는 속도
     private int shardCount = 6;           // 조각 개수
     private float shardLifetime = 3.0f;   // 조각 수명

    [Header("References")]
    [SerializeField] private GameObject warningObject;     // 빨간 원
    [SerializeField] private GameObject iceBodyObject;     // 얼음 이미지
    [SerializeField] private GameObject iceShardPrefab;    // 날아갈 조각 프리팹
    [SerializeField] private Collider2D bodyCollider;      // 본체 콜라이더

    public void Setup(
        float warningSeconds,
        float bodySeconds,
        float contactDamageValue,
        float shardDamageValue,
        float shardSpeedValue,
        int shardCountValue,
        float shardLifetimeSeconds
    )
    {
        warningDuration = warningSeconds;
        bodyDuration = bodySeconds;
        contactDamage = contactDamageValue;
        shardDamage = shardDamageValue;
        shardSpeed = shardSpeedValue;
        shardCount = shardCountValue;
        shardLifetime = shardLifetimeSeconds;
    }

    private void Start()
    {
        // 시작하자마자 패턴 코루틴 실행
        StartCoroutine(AttackPatternRoutine());
    }

    IEnumerator AttackPatternRoutine()
    {
        // 1. 경고 단계
        warningObject.SetActive(true);
        iceBodyObject.SetActive(false);
        bodyCollider.enabled = false; // 아직은 안 아픔

        yield return new WaitForSeconds(warningDuration);

        // 2. 얼음 생성 단계 (공격 판정 시작)
        warningObject.SetActive(false);
        iceBodyObject.SetActive(true);
        bodyCollider.enabled = true; // 이제 닿으면 아픔

        // (선택) 꽝! 하는 소리나 이펙트 재생 위치

        yield return new WaitForSeconds(bodyDuration);

        // 3. 폭발 및 조각 발사
        Explode();
    }

    private void Explode()
    {
        if (shardCount <= 0) return;

        // N방향 발사 (360도 / shardCount 간격)
        float angleStep = 360f / shardCount;
        for (int i = 0; i < shardCount; i++)
        {
            float angle = i * angleStep;
            // 회전값 계산 (Z축 회전)
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            // 조각 생성
            GameObject shard = Instantiate(iceShardPrefab, transform.position, rotation);

            // 조각에게 데미지와 속도 정보 전달
            IceProjectile projectile = shard.GetComponent<IceProjectile>();
            if (projectile != null)
            {
                // 방향 벡터 구하기 (회전된 오른쪽 방향)
                Vector3 dir = rotation * Vector3.right;
                projectile.Setup(dir, shardSpeed, shardDamage, shardLifetime);
            }
        }

        // 자신은 파괴
        Destroy(gameObject);
    }

    // 얼음 덩어리 본체에 플레이어가 닿았을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerDamageHelper.TakeDamage(collision.gameObject, contactDamage);
            Debug.Log("플레이어한테 데미지 줌! 얼음 덩어리 접촉 데미지!");
        }
    }
}