using UnityEngine;

public class ExpJewelController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
           

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