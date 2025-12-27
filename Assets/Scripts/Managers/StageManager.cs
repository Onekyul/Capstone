using UnityEngine;
using TMPro;
using System.Collections;     // IEnumerator 사용
using System.Collections.Generic; // List 사용
using UnityEngine.SceneManagement; // 씬 전환용

public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    // =========================================================
    // [1] 기존 스폰 시스템 (StageSO 웨이브 패턴)
    // =========================================================
    [Header("--- [Wave Spawn Settings] ---")]
    [Tooltip("실행할 스테이지의 설계도 (StageSO 파일을 여기에 연결하세요)")]
    public StageSO currentStage;

    [Tooltip("스폰의 기준이 될 플레이어 또는 카메라")]
    public Transform spawnCenter;

    private float elapsedTime = 0f;       // 경과 시간 (스폰 타이밍 체크용)
    private int currentPhaseIndex = 0;    // 현재 진행 중인 웨이브 단계
    private Camera mainCamera;

    // 4개의 대각선 꼭짓점 방향 (대각선 스폰용)
    private readonly Vector2[] diagonalSpawnPoints =
    {
        new Vector2(1, 1).normalized,   // 오른쪽 위
        new Vector2(1, -1).normalized,  // 오른쪽 아래
        new Vector2(-1, -1).normalized, // 왼쪽 아래
        new Vector2(-1, 1).normalized   // 왼쪽 위
    };

    // =========================================================
    // [2] 신규 보상 및 게임 모드 시스템
    // =========================================================
    [Header("--- [Reward Settings] ---")]
    public List<RewardRule> eliteChestRewards;
    public List<RewardRule> elementChestRewards;

    [System.Serializable]
    public struct RewardRule
    {
        public ItemData item;
        public int minAmount;
        public int maxAmount;
    }

    [Header("--- [Game Mode Settings] ---")]
    public float stageTimeLimit = 300f; // 제한 시간 (5분)
    private float currentTimer;         // 남은 시간
    public bool isGameEnded = false;

    // 상자 획득 카운트
    public int eliteChestCount = 0;
    public int elementChestCount = 0;

    [Header("--- [Boss & UI References] ---")]
    public Transform[] spawnPoints;            // 보스/속성 몬스터 고정 스폰 위치
    public GameObject[] elementMonsterPrefabs; // 속성 몬스터 프리팹
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI objectiveText;

    private int targetMonstersCount = 0; // 보스 처치 수

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // 1. 초기화
        mainCamera = Camera.main;
        if (spawnCenter == null && mainCamera != null) spawnCenter = mainCamera.transform;

        currentTimer = stageTimeLimit;
        elapsedTime = 0f;
        currentPhaseIndex = 0;
        isGameEnded = false;
        eliteChestCount = 0;
        elementChestCount = 0;

        // 2. 고정형 속성 몬스터(보스급) 4마리 소환 (기존 로직 유지)
        SpawnElementalMonsters();
    }

    void Update()
    {
        if (isGameEnded) return;

        // --- 타이머 처리 ---
        float deltaTime = Time.deltaTime;
        currentTimer -= deltaTime;
        elapsedTime += deltaTime; // 웨이브 스폰용 경과 시간 증가

        UpdateUIText();

        // --- 승리 조건 A: 제한 시간 생존 ---
        if (currentTimer <= 0)
        {
            FinishGame(true); // 생존 성공
            return;
        }

        // --- 웨이브 스폰 로직 (복구됨!) ---
        CheckWaveSpawn();
    }

    // =========================================================
    // [기능 1] 웨이브 스폰 로직 (복구된 부분)
    // =========================================================
    void CheckWaveSpawn()
    {
        // 스테이지 정보가 없거나 모든 페이즈가 끝났으면 패스
        if (currentStage == null || currentPhaseIndex >= currentStage.phases.Count) return;

        // 현재 페이즈의 시작 시간이 되었는지 확인
        if (elapsedTime >= currentStage.phases[currentPhaseIndex].timestamp)
        {
            StartPhase(currentStage.phases[currentPhaseIndex]);
            currentPhaseIndex++;
        }
    }

    void StartPhase(Phase phase)
    {
        foreach (SpawnData group in phase.spawnGroups)
        {
            StartCoroutine(SpawnMonsterGroup(group));
        }
    }

    IEnumerator SpawnMonsterGroup(SpawnData data)
    {
        Vector2 spawnDirection = Vector2.zero;
        Vector3 baseSpawnPosition = Vector3.zero;

        // 그룹의 기준 위치 계산
        switch (data.spawnPattern)
        {
            case SpawnPattern.DiagonalEntrance:
                spawnDirection = diagonalSpawnPoints[Random.Range(0, diagonalSpawnPoints.Length)];
                baseSpawnPosition = (Vector2)spawnCenter.position + spawnDirection * data.spawnRadius;
                break;
                // Circle, Random은 개별 위치 계산
        }

        for (int i = 0; i < data.count; i++)
        {
            if (isGameEnded) yield break; // 게임 끝나면 스폰 중단

            Vector3 finalSpawnPosition = Vector3.zero;

            switch (data.spawnPattern)
            {
                case SpawnPattern.Circle:
                    float angle = i * (360f / data.count);
                    Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                    finalSpawnPosition = spawnCenter.position + dir * (data.spawnRadius + Random.Range(-data.groupSpread / 2, data.groupSpread / 2));
                    break;

                case SpawnPattern.RandomOutsideCamera:
                    Vector3 screenPoint = Vector3.zero;
                    int edge = Random.Range(0, 4);
                    // 화면 밖 랜덤 좌표 계산
                    if (edge == 0) screenPoint = new Vector3(Random.Range(0, Screen.width), -50f, 10f);
                    else if (edge == 1) screenPoint = new Vector3(Random.Range(0, Screen.width), Screen.height + 50f, 10f);
                    else if (edge == 2) screenPoint = new Vector3(-50f, Random.Range(0, Screen.height), 10f);
                    else screenPoint = new Vector3(Screen.width + 50f, Random.Range(0, Screen.height), 10f);

                    finalSpawnPosition = mainCamera.ScreenToWorldPoint(screenPoint);
                    finalSpawnPosition.z = 0;
                    break;

                case SpawnPattern.DiagonalEntrance:
                    Vector2 randomOffset = Random.insideUnitCircle * data.groupSpread;
                    finalSpawnPosition = baseSpawnPosition + (Vector3)randomOffset;
                    break;
            }

            // ★ 중요: MonsterPool 이름 확인 (MonsterPool vs MonsterPoolingManager)
            if (MonsterPool.Instance != null)
            {
                MonsterController monster = MonsterPool.Instance.GetFromPool(data.monsterTag, finalSpawnPosition, Quaternion.identity);

                // 대각선 이동 몬스터인 경우 방향 설정
                if (monster != null && monster is DiagonalMoveMonsterController diagonalMover)
                {
                    diagonalMover.SetDirection(-spawnDirection);
                }
            }

            yield return new WaitForSeconds(data.spawnInterval);
        }
    }

    // =========================================================
    // [기능 2] 보상 및 결과 시스템 (새로 만든 부분)
    // =========================================================
    public void CollectEliteChest() { eliteChestCount++; }
    public void CollectElementChest() { elementChestCount++; }

    public void FinishGame(bool isClear)
    {
        if (isGameEnded) return;
        isGameEnded = true;

        // 1. 게임 정지 및 스폰 중단
        Time.timeScale = 0f;
        if (MonsterPool.Instance != null) MonsterPool.Instance.StopSpawning();

        // 2. 보상 계산
        Dictionary<ItemData, int> finalRewards = CalculateTotalRewards(isClear);

        // 3. 인벤토리 저장
        foreach (var pair in finalRewards)
        {
            if (pair.Value > 0) DataManager.instance.AddInventory(pair.Key.itemId, pair.Value);
        }

        // 4. UI 표시
        DungeonUIManager.instance.ShowResultUI(isClear, eliteChestCount, elementChestCount, finalRewards);

        DataManager.instance.SaveGame();
    }

    private Dictionary<ItemData, int> CalculateTotalRewards(bool isClear)
    {
        Dictionary<ItemData, int> totalRewards = new Dictionary<ItemData, int>();

        // 엘리트 상자 정산
        for (int i = 0; i < eliteChestCount; i++)
        {
            foreach (var rule in eliteChestRewards)
            {
                int amount = Random.Range(rule.minAmount, rule.maxAmount + 1);
                AddItemToDict(totalRewards, rule.item, amount);
            }
        }

        // 엘리멘트 상자 정산
        for (int i = 0; i < elementChestCount; i++)
        {
            foreach (var rule in elementChestRewards)
            {
                int amount = Random.Range(rule.minAmount, rule.maxAmount + 1);
                AddItemToDict(totalRewards, rule.item, amount);
            }
        }

        // 죽음 패널티 (0.7배)
        if (!isClear)
        {
            List<ItemData> keys = new List<ItemData>(totalRewards.Keys);
            foreach (var key in keys)
            {
                totalRewards[key] = Mathf.FloorToInt(totalRewards[key] * 0.7f);
            }
        }

        return totalRewards;
    }

    void AddItemToDict(Dictionary<ItemData, int> dict, ItemData item, int amount)
    {
        if (item == null) return;
        if (dict.ContainsKey(item)) dict[item] += amount;
        else dict.Add(item, amount);
    }

    // =========================================================
    // [기능 3] 고정형 속성 몬스터 (보스) 스폰
    // =========================================================
    void SpawnElementalMonsters()
    {
        if (spawnPoints == null || elementMonsterPrefabs == null) return;

        targetMonstersCount = elementMonsterPrefabs.Length;
        UpdateObjectiveText();

        for (int i = 0; i < elementMonsterPrefabs.Length; i++)
        {
            Transform spawnPos = (i < spawnPoints.Length) ? spawnPoints[i] : spawnPoints[0];
            if (spawnPos == null) continue;

            GameObject monsterObj = Instantiate(elementMonsterPrefabs[i], spawnPos.position, Quaternion.identity);
            MonsterController monster = monsterObj.GetComponent<MonsterController>();

            if (monster != null)
            {
                monster.OnDeath += OnTargetMonsterDead;
            }
        }
    }

    void OnTargetMonsterDead()
    {
        if (isGameEnded) return;
        targetMonstersCount--;
        UpdateObjectiveText();

        if (targetMonstersCount <= 0)
        {
            FinishGame(true); // 토벌 성공
        }
    }

    void UpdateUIText()
    {
        if (timerText != null)
        {
            int min = Mathf.FloorToInt(currentTimer / 60);
            int sec = Mathf.FloorToInt(currentTimer % 60);
            timerText.text = $"{min:D2}:{sec:D2}";
        }
    }

    void UpdateObjectiveText()
    {
        if (objectiveText != null) objectiveText.text = $"Boss Left: {targetMonstersCount}";
    }


    // ★ 버튼에 연결할 함수
    public void ReturnToBase()
    {
        // 1. 멈췄던 시간을 다시 흐르게 함 (이거 안 하면 다음 씬에서도 멈춰있음!)
        Time.timeScale = 1.0f;

        // 2. 'BaseArea' 씬으로 이동
        // (만약 로딩 화면을 띄워야 한다면 LoadingManager.LoadScene("BaseArea") 등을 사용)
        SceneManager.LoadScene("BaseArea");
    }
}