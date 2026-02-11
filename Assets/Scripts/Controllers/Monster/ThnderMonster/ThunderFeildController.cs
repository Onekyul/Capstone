using UnityEngine;
using System.Collections;

public class ThunderFieldController : MonoBehaviour
{
    [SerializeField] private float duration = 3.0f;      // 장판 유지 시간
    [SerializeField] private float damagePerTick = 2.0f; // 틱당 데미지

    private void Start()
    {
        StartCoroutine(LifeCycleRoutine());
    }

    IEnumerator LifeCycleRoutine()
    {
        // 유지 시간만큼 대기했다가 사라짐
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 플레이어 스크립트에 "틱 데미지"를 주는 방식이 있다면 호출
            collision.GetComponent<PlayerStats>()?.TakeDamage(damagePerTick);
            Debug.Log("찌릿! 전기 장판 데미지!");
        }
    }
}