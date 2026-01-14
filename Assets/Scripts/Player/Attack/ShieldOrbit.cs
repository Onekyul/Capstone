using UnityEngine;

public class ShieldOrbit : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 150f; // 초당 회전 각도

    [Header("데미지 설정")]
    [SerializeField] private float baseShieldDamage = 1f; // 방패의 기본 데미지
    
    private PlayerStats playerStats;

    void Start()
    {
        // 플레이어 스탯 참조 (부모 객체가 플레이어이므로)
        playerStats = GetComponentInParent<PlayerStats>();
        
    }

    void Update()
    {
        float rotationThisFrame = rotationSpeed * Time.deltaTime;

        // 부모 회전 (공전)
        transform.Rotate(0, 0, rotationThisFrame);

        // 각 방패가 플레이어 중심에서 바깥쪽을 향하도록 회전
        foreach (Transform child in transform)
        {
            // 방패 위치에서 플레이어(부모)로 향하는 벡터 계산
            Vector3 directionFromPlayer = child.position - transform.position;

            // 방패가 바깥쪽을 향하도록 회전 (90도 조정 가능)
            float angle = Mathf.Atan2(directionFromPlayer.y, directionFromPlayer.x) * Mathf.Rad2Deg;
            child.rotation = Quaternion.Euler(0, 0, angle); // -90도는 스프라이트 방향에 따라 조정
        }
    }


    public float GetShieldDamage()
    {
        if (playerStats == null) return baseShieldDamage;

        // 설명서 3.1 스탯 계산 흐름에 따라 최종 공격력 배율 적용
        return baseShieldDamage * playerStats.GetAttackDamageMultiplier();
    }
}
