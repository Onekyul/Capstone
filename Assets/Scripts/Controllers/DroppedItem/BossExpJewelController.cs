using UnityEngine;

public class BossExpJewelController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] public float expAmount = 1f; // 이 보석이 주는 경험치 양

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ★ 여기가 핵심 변경점: BossDungeonLevelManager를 호출
            if (BossDungeonLevelManager.instance != null)
            {
                BossDungeonLevelManager.instance.GainExperience(expAmount);
            }

            Destroy(gameObject);
        }
    }
}