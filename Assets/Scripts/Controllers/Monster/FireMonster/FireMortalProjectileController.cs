using UnityEngine;

public class FireMortarProjectile : MonoBehaviour
{
    [SerializeField] private GameObject fireFieldPrefab; // 착탄 시 생성할 장판
    [SerializeField] private float speed = 8f;

    private Vector3 targetPos;
    private bool isLaunched = false;
    private float impactDamage = 0f; // 직격 데미지

    // [수정] 데미지도 같이 받도록 변경
    public void Setup(Vector3 target, float damage)
    {
        targetPos = target;
        impactDamage = damage;
        isLaunched = true;

        // 투사체가 날아가는 방향을 보게 회전 (선택 사항)
        Vector3 dir = (target - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        if (!isLaunched) return;

        // 목표 지점으로 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 도착 감지 (바닥에 닿음)
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            Explode();
        }
    }

    // [추가] 날아가는 도중 플레이어와 충돌했는지 검사
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 도착해서 터지는 중이라면 무시
        if (!isLaunched) return;

        if (other.CompareTag("Player"))
        {
            PlayerStats pc = other.GetComponent<PlayerStats>();
            if (pc != null)
            {
                // 1. 직격 데미지 주기
                pc.TakeDamage(impactDamage);
                Debug.Log("투사체 직격! 데미지: " + impactDamage);
            }

            // 2. 플레이어 몸에서 즉시 폭발 (장판 생성)
            Explode();
        }
    }

    void Explode()
    {
        isLaunched = false; // 중복 폭발 방지

        // 장판 생성 (현재 투사체 위치에 생성됨)
        if (fireFieldPrefab != null)
        {
            Instantiate(fireFieldPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject); // 투사체 삭제
    }
}