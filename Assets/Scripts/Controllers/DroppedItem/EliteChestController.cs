using UnityEngine;

public class EliteChestController : MonoBehaviour
{

    //플레이어와 충돌 시 골드 획득
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                //pc.GainGold(50); // 골드 50 획득

                Debug.Log("Player gained gold from Elite Chest!");
                Destroy(gameObject); // 상자 제거
            }
        }
    }
}
