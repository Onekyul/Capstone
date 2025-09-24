using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float health = 50f;
    // [SerializeField] private float moveSpeed = 2f; // 향후 이동 기능 구현 시 사용
    
    private float currentHealth;
    
    private void Start()
    {
        currentHealth = health;
    }
    
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // 적 사망 처리
        Destroy(gameObject);
    }
    
    // 프로퍼티로 damage 접근 가능하게 함
    public float damageValue => damage;
}