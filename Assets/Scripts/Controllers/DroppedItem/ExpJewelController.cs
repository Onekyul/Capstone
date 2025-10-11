using UnityEngine;

public class ExpJewelController : MonoBehaviour
{   
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                //pc.GainExp(10); // 경험치 10 획득


                Debug.Log("Player gained experience from Exp Jewel!");


                Destroy(gameObject); // 보석 제거
            }
        }
    }
}
