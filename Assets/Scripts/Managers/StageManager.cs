using UnityEngine;
using TMPro;
using System.Collections.Generic; // 리스트 사용

public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("Game Mode Settings")]
    public float stageTimeLimit = 300f; // 5분 (300초)
    public bool isClear = false;

    [Header("Elemental Monsters")]
    [Tooltip("맵 구석 4곳의 위치 (빈 오브젝트로 배치 후 연결)")]
    public Transform[] spawnPoints;

    [Tooltip("생성할 속성 몬스터 프리팹 4개 (순서대로 소환됨)")]
    public GameObject[] elementMonsterPrefabs;

    private int targetMonstersCount = 0; // 처치해야 할 남은 몬스터 수
    private float currentTimer;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI objectiveText; // 예: "남은 보스: 3" 표시용 (선택사항)
    public GameObject clearPanel;

    [Header("Collected Chests")]
    public int silverChestCount = 0; // 은상자 획득 수
    public int goldChestCount = 0;   // 금상자 획득 수

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentTimer = stageTimeLimit;
        isClear = false;

        // 게임 시작 시 몬스터 4마리 소환
        SpawnElementalMonsters();
    }

    void Update()
    {
        if (isClear) return;

        // 1. 타이머 체크
        currentTimer -= Time.deltaTime;
        UpdateUIText();

        // 2. 클리어 조건 A: 시간 종료 (생존 성공)
        if (currentTimer <= 0)
        {
            Debug.Log("시간 종료! 생존 성공!");
            GameClear();
        }

        // (클리어 조건 B는 몬스터가 죽을 때 OnMonsterDeath 함수에서 즉시 체크합니다)
    }

    // ★ 4마리 소환 및 감시 시작
    void SpawnElementalMonsters()
    {
        // 4마리라고 설정했으니 카운트 초기화
        targetMonstersCount = elementMonsterPrefabs.Length;
        UpdateObjectiveText(); // UI 갱신

        for (int i = 0; i < elementMonsterPrefabs.Length; i++)
        {
            // 스폰 포인트가 부족하면 0번 위치에 겹쳐서라도 소환 (에러 방지)
            Transform spawnPos = (i < spawnPoints.Length) ? spawnPoints[i] : spawnPoints[0];

            GameObject monsterObj = Instantiate(elementMonsterPrefabs[i], spawnPos.position, Quaternion.identity);

            // 몬스터 컨트롤러를 가져와서 죽음 이벤트를 연결(구독)합니다.
            MonsterController monster = monsterObj.GetComponent<MonsterController>();
            if (monster != null)
            {
                monster.OnDeath += OnTargetMonsterDead;
            }
        }
    }

    // ★ 몬스터가 죽었을 때 호출되는 함수
    void OnTargetMonsterDead()
    {
        if (isClear) return;

        targetMonstersCount--;
        UpdateObjectiveText();

        // 클리어 조건 B: 모든 타겟 몬스터 처치
        if (targetMonstersCount <= 0)
        {
            Debug.Log("모든 속성 몬스터 처치! 토벌 성공!");
            GameClear();
        }
    }

    public void GameClear()
    {
        isClear = true;
        currentTimer = 0;

        // 1. 스폰 중지 (일반 몬스터)
        if (MonsterPool.Instance != null)
            MonsterPool.Instance.StopSpawning(); // 이 함수가 구현되어 있어야 함

        // 2. 화면의 모든 적 제거 (선택사항)
        // DestroyAllEnemies(); 

        // 3. UI 띄우기
        if (clearPanel != null) clearPanel.SetActive(true);
        if (timerText != null) timerText.text = "STAGE CLEAR!";

        // 4. 시간 정지
        Time.timeScale = 0f;
    }

    // --- UI 갱신용 ---
    void UpdateUIText()
    {
        if (timerText != null)
        {
            int min = Mathf.FloorToInt(currentTimer / 60F);
            int sec = Mathf.FloorToInt(currentTimer % 60F);
            timerText.text = $"{min:00}:{sec:00}";
        }
    }

    void UpdateObjectiveText()
    {
        if (objectiveText != null)
        {
            objectiveText.text = $"Boss Left: {targetMonstersCount}";
        }
    }
}