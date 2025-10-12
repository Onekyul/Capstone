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
            //데미지 부여 함수, 아직 MonsterController.cs 에 TakeDamage 함수 구현 안됨
            // other.GetComponent<MonsterController>()?.TakeDamage(damageValue);

            Destroy(gameObject);
        }

    }
}
