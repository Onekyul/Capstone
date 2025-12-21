using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float arrowSpeed = 15f;
    public float lifeTime = 3f;

    private Rigidbody2D rb;
    private float damageValue;

    //화살로부터 데미지 값 설정
    public void setDamage(float damage)
    {
        damageValue = damage;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(transform.up * arrowSpeed, ForceMode2D.Impulse);

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 몬스터에게 데미지 적용
            other.GetComponent<MonsterController>()?.TakeDamage(damageValue);
            Debug.Log($"화살이 {other.name}에게 {damageValue} 데미지!");

            Destroy(gameObject);
        }

    }
}