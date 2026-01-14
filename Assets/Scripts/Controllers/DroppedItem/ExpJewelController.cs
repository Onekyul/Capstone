using UnityEngine;

public class ExpJewelController : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float attractionSpeed = 10f; // 끌어당기는 속도
    [SerializeField] private float magnetRange = 5f; // 자석 범위 (기본값, 능력으로 확장 가능)
    
    private Transform playerTransform;
    private bool isBeingAttracted = false;

    private void Start()
    {
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null || !isBeingAttracted) return;

        // 플레이어 방향으로 이동
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * attractionSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        // 플레이어와의 거리 체크
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        
        // 자석 범위 내에 있고 플레이어가 자석 능력을 가지고 있는지 확인
        if (distance <= magnetRange && HasMagnetAbility())
        {
            isBeingAttracted = true;
        }
    }

    private bool HasMagnetAbility()
    {
        // PlayerStats를 통해 AbilitySystem에 접근
        if (PlayerStats.Instance != null)
        {
            AbilitySystem abilitySystem = PlayerStats.Instance.GetComponent<AbilitySystem>();
            if (abilitySystem != null)
            {
                return abilitySystem.HasAbility(16); // 자석 능력 ID = 17
            }
        }
        return false;
    }

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