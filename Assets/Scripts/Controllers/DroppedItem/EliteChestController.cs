using UnityEngine;

public class EliteChestController : MonoBehaviour
{

    //플레이어와 충돌 시
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                StageManager.instance.CollectEliteChest();

                Debug.Log("Player gained Elite Chest!");
                Destroy(gameObject); // 상자 제거
            }
        }
    }
}
