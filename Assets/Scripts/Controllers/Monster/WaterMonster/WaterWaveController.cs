using UnityEngine;

public class WaterWaveController : MonoBehaviour
{
    private Vector3 moveDir;
    private float moveSpeed;
    private float damage;
    private float lifetime = 2.0f; // 파도 유지 시간
    private float startScaleMultiplier = 0.5f;
    private float targetScaleMultiplier = 1.5f;

    // ★ 핵심: 한 번 맞은 대상은 다시 때리지 않기 위한 플래그
    private bool hasDamagedPlayer = false;

    // 파도가 점점 커지는 연출을 위한 변수
    private Vector3 startScale;
    private Vector3 targetScale;
    private float elapsed = 0f;

    public void Setup(
        Vector3 dir,
        float speed,
        float dmg,
        float life,
        float startScaleFactor,
        float targetScaleFactor
    )
    {
        moveDir = dir;
        moveSpeed = speed;
        damage = dmg;
        lifetime = life;
        startScaleMultiplier = startScaleFactor;
        targetScaleMultiplier = targetScaleFactor;

        // 회전 설정 (날아가는 방향 바라보기)
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 크기 연출 초기화 (작게 시작해서 커짐)
        startScale = transform.localScale * startScaleMultiplier;
        targetScale = transform.localScale * targetScaleMultiplier;
        transform.localScale = startScale;

        elapsed = 0f;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // 1. 앞으로 이동
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        // 2. 점점 커지는 연출 (파도가 퍼지는 느낌)
        elapsed += Time.deltaTime;
        if (elapsed < lifetime)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / lifetime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 이미 데미지를 줬다면 무시
        if (hasDamagedPlayer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 1. 데미지 처리
            collision.gameObject.GetComponent<PlayerStats>()?.TakeDamage(damage);
            Debug.Log($"플레이어가 파도에 휩쓸림! 데미지: {damage}");

            // 3. 플래그 설정 (이제 이 파도는 더 이상 플레이어를 아프게 하지 않음)
            hasDamagedPlayer = true;

            // 파도는 사라지지 않고 관통해서 지나감 (원한다면 여기서 Destroy해도 됨)
        }
    }
}