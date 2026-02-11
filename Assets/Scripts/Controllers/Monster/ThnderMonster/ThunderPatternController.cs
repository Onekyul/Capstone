using UnityEngine;
using System.Collections;

public class ThunderPatternController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float warningDuration = 1.0f; // 경고 시간
    [SerializeField] private float strikeDamage = 10f;     // 번개 1발당 데미지
    [SerializeField] private float strikeRadius = 1.5f;    // 번개 피격 범위

    [Header("References")]
    [SerializeField] private GameObject warningObject;     // 경고 원
    [SerializeField] private GameObject strikeVfxObject;   // 번개 이펙트
    [SerializeField] private GameObject thunderFieldPrefab; // 끝난 뒤 생길 장판

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
        yield return new WaitForSeconds(0.1f);
        strikeVfxObject.SetActive(false);

        // 4. 번개 장판 생성 (현재 위치에)
        if (thunderFieldPrefab != null)
        {
            Instantiate(thunderFieldPrefab, transform.position, Quaternion.identity);
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