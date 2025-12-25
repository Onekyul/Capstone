using UnityEngine;

public class ElementChestController : MonoBehaviour
{
    //플레이어와 충돌 시
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                StageManager.instance.CollectElementChest();
                Debug.Log("Player gained Element Chest!");
                Destroy(gameObject); // 상자 제거
            }
        }
    }
}
