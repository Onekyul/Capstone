using UnityEngine;
using System.Collections;

public class ThunderPatternController : MonoBehaviour
{
    [Header("Settings")]
    private float warningDuration = 1.0f; // 경고 시간
    private float strikeDamage = 10f;     // 번개 1발당 데미지
    private float strikeRadius = 1.5f;    // 번개 피격 범위
    private float strikeVfxDuration = 0.1f; // 번개 이펙트 노출 시간
    private float fieldDuration = 3.0f;    // 장판 유지 시간
    private float fieldDamagePerTick = 2.0f; // 장판 틱 데미지

    [Header("References")]
    [SerializeField] private GameObject warningObject;     // 경고 원
    [SerializeField] private GameObject strikeVfxObject;   // 번개 이펙트
    [SerializeField] private GameObject thunderFieldPrefab; // 끝난 뒤 생길 장판

    public void Setup(
        float warningSeconds,
        float damage,
        float radius,
        float vfxDurationSeconds,
        float fieldDurationSeconds,
        float fieldDamageTick
    )
    {
        warningDuration = warningSeconds;
        strikeDamage = damage;
        strikeRadius = radius;
        strikeVfxDuration = vfxDurationSeconds;
        fieldDuration = fieldDurationSeconds;
        fieldDamagePerTick = fieldDamageTick;
    }

    private void Start()
    {
        StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        // 1. 경고 표시
        warningObject.SetActive(true);
        strikeVfxObject.SetActive(false);

        yield return new WaitForSeconds(warningDuration);

        // 2. 경고 끄고 공격 시작
        warningObject.SetActive(false);

        // 3. 번개 1회 타격
        strikeVfxObject.SetActive(true);
        CheckDamage();
        yield return new WaitForSeconds(strikeVfxDuration);
        strikeVfxObject.SetActive(false);

        // 4. 번개 장판 생성 (현재 위치에)
        if (thunderFieldPrefab != null)
        {
            GameObject fieldObj = Instantiate(thunderFieldPrefab, transform.position, Quaternion.identity);
            ThunderFieldController field = fieldObj.GetComponent<ThunderFieldController>();
            if (field != null)
            {
                field.Setup(fieldDuration, fieldDamagePerTick);
            }
        }

        // 5. 할 일 다 했으니 퇴장
        Destroy(gameObject);
    }

    private void CheckDamage()
    {
        // Physics2D.OverlapCircle로 범위 내 플레이어 검출
        Collider2D hit = Physics2D.OverlapCircle(transform.position, strikeRadius, LayerMask.GetMask("Player"));

        if (hit != null && hit.CompareTag("Player"))
        {
            hit.GetComponent<PlayerStats>()?.TakeDamage(strikeDamage);
            Debug.Log($"콰광! 번개 {strikeDamage} 데미지!");
        }
    }

    // 에디터에서 범위 확인용 기즈모
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, strikeRadius);
    }
}