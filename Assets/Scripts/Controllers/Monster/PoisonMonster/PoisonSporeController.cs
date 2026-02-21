using UnityEngine;

public class PoisonSporeController : MonoBehaviour
{
    private Transform target;       // 추적할 대상 (플레이어)
    private float moveSpeed;        // 이동 속도
    private float damage;           // 폭발 데미지
    private float explosionRadius;  // 폭발 범위
    private float lifetime;         // 수명

    private bool isExploded = false; // 중복 폭발 방지

    //본인 스프라이트 렌더러
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Effects")]
    [SerializeField] private GameObject explosionVFX; // 폭발 이펙트 프리팹 (선택)

    public void Setup(Transform targetInfo, float speed, float dmg, float life, float radius)
    {
        target = targetInfo;
        moveSpeed = speed;
        damage = dmg;
        lifetime = life;
        explosionRadius = radius;

        // 수명이 다하면 폭발하도록 예약
        Invoke(nameof(Explode), lifetime);
    }
    private void Start()
    {
        //폭발 이펙트 꺼두기
        explosionVFX?.SetActive(false);
    }

    void Update()
    {
        if (isExploded) return;

        // 1. 타겟(플레이어)이 있으면 쫓아감 (유도)
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );
        }
        else
        {
            // 타겟이 없으면 그냥 제자리에서 터지거나 사라짐
            Destroy(gameObject);
        }
    }

    // 플레이어와 닿았을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isExploded) return;

        if (collision.CompareTag("Player"))
        {
            Explode(); // 닿자마자 폭발
        }
    }

    private void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        // 1. 폭발 이펙트 생성하면서 본인 스프라이트 숨기기
        spriteRenderer.enabled = false;
        if (explosionVFX != null)
        {
            explosionVFX.SetActive(true);
        }

        // 2. 범위 데미지 판정 (OverlapCircle)
        // 포자 중심에서 반지름(explosionRadius)만큼 원을 그려 닿는 적 확인
        Collider2D hit = Physics2D.OverlapCircle(transform.position, explosionRadius, LayerMask.GetMask("Player"));

        if (hit != null && hit.CompareTag("Player"))
        {
            // 플레이어에게 데미지 전달
            hit.GetComponent<PlayerStats>()?.TakeDamage(damage);
            Debug.Log($"독 포자 폭발! 플레이어에게 {damage} 데미지");
        }

        // 1초 정도 시간 지난 뒤에 오브젝트 삭제
        Invoke(nameof(DestroySelf), 0.5f);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    // 에디터에서 폭발 범위 미리보기
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}