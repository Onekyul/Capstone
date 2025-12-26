using UnityEngine;

public class TrackPlayerCam : MonoBehaviour
{
    public Transform target;      // 따라갈 플레이어의 Transform
    public float smoothSpeed = 0.125f; // 카메라가 따라가는 속도 (0에 가까울수록 부드럽고, 1이면 즉시 고정)
    public Vector3 offset = new Vector3(0, 0, -10); // 플레이어와 카메라 사이의 거리 (2D는 Z값을 -10으로 설정)

    void LateUpdate()
    {
        if (target == null) return;

        // 플레이어의 위치에 오프셋을 더한 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;
        
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}