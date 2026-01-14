using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float arrowSpeed = 15f;
    public float lifeTime = 0.3f;

    private Rigidbody2D rb;
    private float damageValue;
    private int[] enchantLevels; // 인챈트 레벨 배열 [불, 얼음, 번개, 독]

    //화살로부터 데미지 값 설정
    public void setDamage(float damage)
    {
        damageValue = damage;
    }

    // 화살에 인챈트 레벨 설정
    public void SetEnchants(int[] enchants)
    {
        enchantLevels = enchants;
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
            MonsterController monster = other.GetComponent<MonsterController>();
            if (monster != null)
            {
                // 인챈트 적용 (있는 경우)
                if (enchantLevels != null && enchantLevels.Length == 4)
                {
                    monster.TakeElement(enchantLevels);
                }
                
                // 데미지 적용
                monster.TakeDamage(damageValue);
                Debug.Log($"화살이 {other.name}에게 {damageValue} 데미지!");
            }

            Destroy(gameObject);
        }

    }
}