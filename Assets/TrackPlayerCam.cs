using UnityEngine;

public class TrackPlayerCam : MonoBehaviour
{
    public Transform target;
    [SerializeField] private float smoothSpeed = 5f; // 카메라가 따라오는 속도 (부드러움 조절)
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // 카메라와 플레이어 사이의 거리(Z축 고정)
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // "Player" 태그를 가진 오브젝트를 찾습니다.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target == null) return;

        // 플레이어 위치 + 오프셋 계산
        Vector3 desiredPosition = target.position + offset;
        
        // Lerp를 사용하여 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        transform.position = smoothedPosition;
    }
}
