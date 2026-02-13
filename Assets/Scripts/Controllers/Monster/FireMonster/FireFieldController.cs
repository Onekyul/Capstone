using UnityEngine;
using System.Collections;

public class FireFieldController : MonoBehaviour
{
    [Header("Field Settings")]
    [SerializeField] private float duration = 3.0f; // 장판 유지 시간 (3초)
    [SerializeField] private float maxScale = 3.0f; // 얼마나 커질지
    [SerializeField] private float expandSpeed = 2.0f; // 커지는 속도
    [SerializeField] private float damageInterval = 0.5f; // 데미지 주는 간격
    [SerializeField] private float damage = 5.0f; // 장판 데미지

    public void Setup(
        float durationSeconds,
        float maxScaleValue,
        float expandSpeedValue,
        float damageIntervalSeconds,
        float damageValue
    )
    {
        duration = durationSeconds;
        maxScale = maxScaleValue;
        expandSpeed = expandSpeedValue;
        damageInterval = damageIntervalSeconds;
        damage = damageValue;
    }

    void Start()
    {
        // 처음엔 크기가 0에서 시작 
        transform.localScale = Vector3.zero;

        // 3초 뒤에 자동 삭제
        Destroy(gameObject, duration);

        // 도트 데미지 코루틴 시작
        StartCoroutine(DealDamageRoutine());
    }

    void Update()
    {
        // 설정한 크기(maxScale)까지 부드럽게 커짐
        if (transform.localScale.x < maxScale)
        {
            transform.localScale += Vector3.one * expandSpeed * Time.deltaTime;
        }
    }

    // 범위 내에 있는 플레이어 감지 
    IEnumerator DealDamageRoutine()
    {
        while (true)
        {
            // 현재 장판의 콜라이더 안에 있는 모든 물체 검사
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, transform.localScale.x * 0.5f); // 원형 범위

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    PlayerStats player = hit.GetComponent<PlayerStats>();
                    if (player != null)
                    {
                        // 플레이어에게 데미지 주기 (구현된 함수 사용)
                        player.TakeDamage(damage); 
                        Debug.Log("앗 뜨거! 장판 데미지!");
                    }
                }
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }

    // 시각적 범위를 보기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);
    }
}