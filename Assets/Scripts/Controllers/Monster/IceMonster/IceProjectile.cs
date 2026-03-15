using UnityEngine;

public class IceProjectile : MonoBehaviour
{
    private Vector3 moveDir;
    private float moveSpeed;
    private float damage;
    private float lifetime = 3.0f; // 3초 뒤 자동 삭제

    public void Setup(Vector3 dir, float speed, float dmg, float life)
    {
        moveDir = dir;
        moveSpeed = speed;
        damage = dmg;
        lifetime = life;

        // 투사체가 날아가는 방향을 바라보게 회전 (선택사항)
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime); // 안전장치
    }

    void Update()
    {
        // 설정된 방향으로 이동
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerDamageHelper.TakeDamage(collision.gameObject, damage);
            Debug.Log($"플레이어가 얼음 조각에 맞음! 데미지: {damage}");

            // 맞추면 사라짐
            Destroy(gameObject);
        }
    }
}