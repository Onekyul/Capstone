using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("이동 및 소멸 설정")]
    public float speed = 15f;      // 1. 날아가는 속도
    public float lifeTime = 0.5f;  // 2. 생성된 후 사라지기까지의 시간 (초 단위)

    void Start()
    {
        // 3. 이 오브젝트(검기)가 생성되자마자 타이머를 켬
        // "나 자신(gameObject)을 lifeTime(0.5초) 뒤에 파괴해라!"
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 매 프레임마다 지정된 속도로 앞(오른쪽)을 향해 날아감
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }
}