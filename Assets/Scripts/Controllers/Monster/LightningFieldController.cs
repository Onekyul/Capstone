using UnityEngine;
using System.Collections;

public class LightningFieldController : MonoBehaviour
{
    [Header("Field Settings")]
    [SerializeField] private float duration = 3.0f; // 필드 유지 시간 (3초)
    [SerializeField] private float fieldRadius = 3.0f; // 필드 반경 (고정, 3m)
    [SerializeField] private float damageInterval = 1.0f; // 데미지 간격 (1초마다)
    [SerializeField] private float damagePercentage = 0.3f; // 플레이어 공격력의 30%
    
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer fieldVisual; // 범위 시각화용 스프라이트
    [SerializeField] private float pulseSpeed = 2.0f; // 깜빡이는 속도
    [SerializeField] private float minAlpha = 0.2f; // 최소 투명도
    [SerializeField] private float maxAlpha = 0.5f; // 최대 투명도
    [SerializeField] private Color fieldColor = new Color(0.5f, 0.7f, 1f); // 필드 색상 (파란색)

    private float pulseTimer = 0f;
    private PlayerStats playerStats; // 플레이어 스탯 참조

    void Start()
    {
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("[LightningField] 플레이어에 PlayerStats가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[LightningField] 플레이어를 찾을 수 없습니다!");
        }
        
        // SpriteRenderer가 할당되지 않았다면 자동으로 찾기
        if (fieldVisual == null)
        {
            fieldVisual = GetComponent<SpriteRenderer>();
        }
        
        // 초기 색상 설정
        if (fieldVisual != null)
        {
            Color color = fieldColor;
            color.a = minAlpha;
            fieldVisual.color = color;
        }
        
        // 크기를 필드 반경에 맞게 설정 (직경 = 반경 * 2)
        transform.localScale = Vector3.one * fieldRadius * 2f;
        
        // duration 후 자동 삭제
        Destroy(gameObject, duration);
        
        // 데미지 코루틴 시작
        StartCoroutine(DealDamageRoutine());
        
        Debug.Log($"[LightningField] 생성됨 - 위치: {transform.position}, 반경: {fieldRadius}m, 지속시간: {duration}초, 데미지: 플레이어 공격력 × {damagePercentage * 100}%");
    }

    void Update()
    {
        // 시각적 효과: 알파값을 변경하여 깜빡이는 효과
        PulseEffect();
    }

    // 깜빡이는 시각 효과
    private void PulseEffect()
    {
        if (fieldVisual == null) return;
        
        pulseTimer += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(pulseTimer) + 1f) / 2f);
        
        Color color = fieldColor;
        color.a = alpha;
        fieldVisual.color = color;
    }

    // 범위 내 적에게 주기적으로 데미지
    IEnumerator DealDamageRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(damageInterval);
            
            // 플레이어의 현재 공격력 계산 (무기 공격력 포함)
            float currentPlayerDamage = CalculatePlayerDamage();
            
            // 필드 데미지 = 플레이어 공격력 × 30%
            float fieldDamage = currentPlayerDamage * damagePercentage;
            
            // 범위 내 모든 충돌체 검사
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, fieldRadius);

            int hitCount = 0;
            foreach (var hit in hits)
            {
                // 적 태그 확인 (Monster, Enemy 등)
                if (hit.CompareTag("Enemy"))
                {
                    MonsterController monsterCtrl = hit.GetComponent<MonsterController>();
                    if (monsterCtrl != null)
                    {
                        monsterCtrl.TakeDamage(fieldDamage);
                        hitCount++;
                        Debug.Log($"[LightningField] {hit.name}에게 {fieldDamage:F1} 데미지! (플레이어 공격력: {currentPlayerDamage:F1} × {damagePercentage * 100}%)");
                    }
                }
            }
            
            if (hitCount > 0)
            {
                Debug.Log($"[LightningField] 총 {hitCount}마리의 적에게 {fieldDamage:F1} 데미지 적용");
            }
        }
    }
    
    // 플레이어의 현재 총 공격력 계산 (무기 + 능력 배율)
    private float CalculatePlayerDamage()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("[LightningField] PlayerStats가 null입니다. 기본 데미지 10 사용");
            return 10f;
        }
        
        // 플레이어의 장착 무기 공격력 가져오기
        float weaponDamage = playerStats.GetEquippedWeaponBaseDamage();
        
        // 플레이어의 공격력 배율 적용
        float attackMultiplier = playerStats.GetAttackDamageMultiplier();
        
        float totalDamage = weaponDamage * attackMultiplier;
        
        return totalDamage;
    }
    
    // 외부에서 데미지 비율을 설정할 수 있도록 (선택사항)
    public void SetDamagePercentage(float percentage)
    {
        damagePercentage = percentage;
        Debug.Log($"[LightningField] 데미지 비율 설정: {percentage * 100}%");
    }
    
    // 외부에서 지속시간을 설정할 수 있도록
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }

    // 시각적 범위 확인용 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.3f); // 파란색
        Gizmos.DrawWireSphere(transform.position, fieldRadius);
    }
}

