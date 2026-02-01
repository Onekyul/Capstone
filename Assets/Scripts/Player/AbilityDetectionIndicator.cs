using UnityEngine;
using System.Collections.Generic;


// 탐지 능력 - 속성 몬스터 방향을 가리키는 2D 화살표 시스템
// 최대 4개의 화살표가 플레이어 주변에서 각 속성 몬스터를 추적
public class DetectionIndicator : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private GameObject arrowPrefab; // 화살표 2D 스프라이트 프리펩
    [SerializeField] private float distanceFromPlayer = 2.5f; // 플레이어 중심에서 화살표까지 거리 (월드 좌표)
    [SerializeField] private Color arrowColor = Color.yellow; // 화살표 색상
    [SerializeField] private float arrowScale = 1.0f; // 화살표 크기
    
    [Header("Arrow Pool")]
    [SerializeField] private int maxArrows = 4; // 최대 화살표 개수 (속성 몬스터 최대 4마리)
    
    private Transform player;
    private List<ArrowTracker> arrowTrackers = new List<ArrowTracker>(); // 화살표-몬스터 매칭 정보
    private bool isActive;

    // 화살표와 추적 중인 몬스터를 연결하는 클래스
    private class ArrowTracker
    {
        public GameObject arrowObject;
        public MonsterController targetMonster;
        public System.Action onDeathCallback; // 이벤트 구독 해제를 위한 참조

        public ArrowTracker(GameObject arrow, MonsterController monster)
        {
            arrowObject = arrow;
            targetMonster = monster;
            onDeathCallback = null;
        }
        
        // 이벤트 구독 해제
        public void UnsubscribeFromMonster()
        {
            if (targetMonster != null && onDeathCallback != null)
            {
                targetMonster.OnDeath -= onDeathCallback;
                onDeathCallback = null;
            }
        }
    }

    private void Start()
    {
        // 플레이어 찾기
        if (PlayerStats.Instance != null)
        {
            player = PlayerStats.Instance.transform;
        }
        else
        {
            Debug.LogError("[DetectionIndicator] PlayerStats.Instance를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        if (!isActive || player == null) return;
        
        // 화살표 업데이트
        UpdateArrows();
    }
    
    // 탐지 기능 활성화 - 4개의 화살표 풀 생성
    public void ActivateDetection()
    {
        if (isActive) return;
        
        isActive = true;
        InitializeArrowPool();
        Debug.Log("[DetectionIndicator] 탐지 능력 활성화! 속성 몬스터 위치가 화살표로 표시됩니다.");
    }
    
    // 화살표 풀 초기화 - 4개의 화살표를 미리 생성 (비활성화 상태)
    private void InitializeArrowPool()
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("[DetectionIndicator] 화살표 프리펩이 할당되지 않았습니다!");
            return;
        }

        // 기존 화살표 정리
        ClearAllArrows();

        // 4개의 화살표 미리 생성
        for (int i = 0; i < maxArrows; i++)
        {
            GameObject arrow = Instantiate(arrowPrefab, transform);
            arrow.SetActive(false); // 초기에는 비활성화
            
            // 색상 및 크기 설정
            SpriteRenderer spriteRenderer = arrow.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = arrowColor;
                arrow.transform.localScale = Vector3.one * arrowScale;
            }

            arrowTrackers.Add(new ArrowTracker(arrow, null));
        }
    }
    
    // 화살표 업데이트 - 속성 몬스터 추적 및 방향 갱신
    private void UpdateArrows()
    {
        // 모든 속성 몬스터 찾기
        MonsterController[] allMonsters = FindObjectsByType<MonsterController>(FindObjectsSortMode.None);
        List<MonsterController> activeElementMonsters = new List<MonsterController>();

        foreach (var monster in allMonsters)
        {
            // 속성 몬스터이고 활성화된 경우만
            if (monster != null && monster.GetMonsterType() == MonsterType.Element && monster.gameObject.activeSelf)
            {
                activeElementMonsters.Add(monster);
            }
        }

        // 사라진 몬스터 처리 (화살표 비활성화)
        for (int i = 0; i < arrowTrackers.Count; i++)
        {
            var tracker = arrowTrackers[i];
            
            // 추적 중인 몬스터가 있는 경우
            if (tracker.targetMonster != null)
            {
                bool isMonsterGone = false;
                
                try
                {
                    // 몬스터가 파괴되었거나, 비활성화되었거나, 리스트에 없으면 화살표 비활성화
                    isMonsterGone = tracker.targetMonster == null || // Unity null (destroyed)
                                    !tracker.targetMonster || // C# null
                                    !tracker.targetMonster.gameObject || // GameObject destroyed
                                    !tracker.targetMonster.gameObject.activeSelf || // 비활성화
                                    !activeElementMonsters.Contains(tracker.targetMonster); // 리스트에서 제거됨
                }
                catch (System.Exception)
                {
                    // 오브젝트가 완전히 파괴된 경우 예외 발생 가능
                    isMonsterGone = true;
                }
                
                if (isMonsterGone)
                {
                    if (tracker.arrowObject != null)
                    {
                        tracker.arrowObject.SetActive(false);
                    }
                    
                    // 이벤트 구독 해제
                    tracker.UnsubscribeFromMonster();
                    tracker.targetMonster = null;
                    Debug.Log("[DetectionIndicator] 몬스터 사망/제거 감지 - 화살표 비활성화 및 이벤트 해제");
                }
            }
        }

        // 새로운 속성 몬스터에 화살표 할당
        foreach (var monster in activeElementMonsters)
        {
            // 이미 추적 중인 몬스터인지 확인
            bool isTracked = false;
            foreach (var tracker in arrowTrackers)
            {
                if (tracker.targetMonster == monster)
                {
                    isTracked = true;
                    break;
                }
            }

            // 새로운 몬스터면 빈 화살표에 할당
            if (!isTracked)
            {
                AssignArrowToMonster(monster);
            }
        }

        // 활성화된 화살표의 위치 및 회전 업데이트
        foreach (var tracker in arrowTrackers)
        {
            // 몬스터가 유효하고 화살표가 활성화된 경우만 업데이트
            if (tracker.targetMonster != null && !ReferenceEquals(tracker.targetMonster, null) && 
                tracker.arrowObject != null && tracker.arrowObject.activeSelf)
            {
                UpdateArrowTransform(tracker);
            }
        }
    }
    
    // 빈 화살표를 새로운 몬스터에 할당
    private void AssignArrowToMonster(MonsterController monster)
    {
        // 속성 몬스터인지 확인 (안전장치)
        if (monster == null || monster.GetMonsterType() != MonsterType.Element)
        {
            Debug.LogWarning("[DetectionIndicator] 속성 몬스터가 아닙니다!");
            return;
        }

        // 비활성화된 화살표 찾기
        foreach (var tracker in arrowTrackers)
        {
            if (tracker.targetMonster == null)
            {
                tracker.targetMonster = monster;
                tracker.arrowObject.SetActive(true);
                
                // OnDeath 이벤트 콜백 생성 및 구독
                tracker.onDeathCallback = () => OnMonsterDeath(tracker);
                monster.OnDeath += tracker.onDeathCallback;
                
                Debug.Log($"[DetectionIndicator] 속성 몬스터 추적 시작 및 OnDeath 이벤트 구독: {monster.name}");
                return;
            }
        }

        Debug.LogWarning("[DetectionIndicator] 사용 가능한 화살표가 없습니다! (최대 4개)");
    }

 
    // 몬스터가 죽었을 때 호출되는 콜백 (OnDeath 이벤트)
    private void OnMonsterDeath(ArrowTracker tracker)
    {
        Debug.Log("[DetectionIndicator] ★★★ OnDeath 이벤트 발동! 속성 몬스터 사망 감지 ★★★");
        
        if (tracker.arrowObject != null)
        {
            tracker.arrowObject.SetActive(false);
            Debug.Log("[DetectionIndicator] 화살표 즉시 비활성화 완료");
        }
        
        // 이벤트 구독 해제
        tracker.UnsubscribeFromMonster();
        tracker.targetMonster = null;
    }
    
    // 화살표의 위치와 회전을 몬스터 방향으로 업데이트
    private void UpdateArrowTransform(ArrowTracker tracker)
    {
        // 몬스터가 null이거나 파괴되었는지 체크
        if (tracker.targetMonster == null || !tracker.targetMonster || player == null) 
        {
            // 몬스터가 사라졌으면 화살표 비활성화
            if (tracker.arrowObject != null && tracker.arrowObject.activeSelf)
            {
                tracker.arrowObject.SetActive(false);
                Debug.Log("[DetectionIndicator] UpdateArrowTransform - 몬스터 null 감지, 화살표 비활성화");
            }
            return;
        }

        // GameObject도 활성화 상태인지 확인
        if (!tracker.targetMonster.gameObject.activeSelf)
        {
            tracker.arrowObject.SetActive(false);
            return;
        }

        // 플레이어 → 몬스터 방향 계산 (2D)
        Vector2 direction = ((Vector2)tracker.targetMonster.transform.position - (Vector2)player.position).normalized;
        
        // 화살표 위치: 플레이어 중심에서 일정 거리
        Vector3 arrowPosition = player.position + (Vector3)direction * distanceFromPlayer;
        arrowPosition.z = player.position.z; // Z축은 플레이어와 동일 (2D)
        tracker.arrowObject.transform.position = arrowPosition;

        // 화살표 회전: 몬스터 방향을 가리키도록
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        tracker.arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle); // 90도 회전 (스프라이트가 오른쪽을 가리킬 때)
        
        
    }
    
    // 탐지 기능 비활성화
    public void DeactivateDetection()
    {
        isActive = false;
        ClearAllArrows();
        Debug.Log("[DetectionIndicator] 탐지 능력 비활성화");
    }
    
    // 모든 화살표 제거
    private void ClearAllArrows()
    {
        foreach (var tracker in arrowTrackers)
        {
            // 이벤트 구독 해제
            tracker.UnsubscribeFromMonster();
            
            if (tracker.arrowObject != null)
            {
                Destroy(tracker.arrowObject);
            }
        }
        arrowTrackers.Clear();
    }

    private void OnDestroy()
    {
        ClearAllArrows();
    }

    // ===== 디버그용 기즈모 =====
    private void OnDrawGizmos()
    {
        if (!isActive || player == null) return;

        // 화살표 생성 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, distanceFromPlayer);
    }
}

