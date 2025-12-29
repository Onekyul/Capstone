using UnityEngine;

public class FireMortarProjectile : MonoBehaviour
{
    [SerializeField] private GameObject fireFieldPrefab; // 착탄 시 생성할 장판
    [SerializeField] private float speed = 8f;

    private Vector3 targetPos;
    private bool isLaunched = false;
    private float impactDamage = 0f; // 직격 데미지
    
    public void Setup(Vector3 target, float damage)
    {
        targetPos = target;
        impactDamage = damage;
        isLaunched = true;

        // 투사체가 날아가는 방향을 보게 회전 
        Vector3 dir = (target - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        if (!isLaunched) return;

        // 목표 지점으로 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // 도착 감지 
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            Explode();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 도착해서 터지는 중이라면 무시
        if (!isLaunched) return;

        if (other.CompareTag("Player"))
        {
            PlayerStats pc = other.GetComponent<PlayerStats>();
            if (pc != null)
            {
                pc.TakeDamage(impactDamage);
                Debug.Log("투사체 직격! 데미지: " + impactDamage);
            }
            
            Explode();
        }
    }

    void Explode()
    {
        isLaunched = false; // 중복 폭발 방지
        
        if (fireFieldPrefab != null)
        {
            Instantiate(fireFieldPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject); // 투사체 삭제
    }
}