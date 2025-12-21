using UnityEngine;

public class ExpJewelController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // [수정 포인트]
            // Find 계열 함수를 써서 매니저를 찾을 필요가 없습니다.
            // LevelManager.instance를 통해 바로 접근합니다.

            if (LevelManager.instance != null)
            {
                LevelManager.instance.GainExperience();

                Debug.Log("Player gained experience from Exp Jewel!");

                Destroy(gameObject); // 보석 제거
            }
            else
            {
                Debug.LogError("LevelManager가 씬에 없거나 인스턴스화되지 않았습니다!");
            }
        }
    }
}