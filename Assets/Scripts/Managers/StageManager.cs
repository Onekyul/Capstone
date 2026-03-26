using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager instance;


    [Header("--- [Wave Spawn Settings] ---")]
    [Tooltip("실행할 스테이지의 설계도 (StageSO 파일을 여기에 연결하세요)")]
    public StageSO currentStage;

    [Tooltip("스폰의 기준이 될 플레이어 또는 카메라")]
    public Transform spawnCenter;

    private float elapsedTime = 0f;       // 경과 시간 
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

    [Header("--- [보상 배율 확인용] ---")]
    [SerializeField]
    private float rewardMultiplier = 1; // 보상 배수 (향후 난이도에 따라 변경 가능)

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

        // 2. 고정형 속성 몬스터 4마리 소환 
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

        // --- 제한 시간 생존 ---
        if (currentTimer <= 0)
        {
            FinishGame(true); // 생존 성공
            return;
        }

        // --- 웨이브 스폰 로직  ---
        CheckWaveSpawn();
    }


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

        // 보상 배수 적용 (클리어 시에만 - 보석 사냥꾼 능력)
        if (isClear)
        {
            List<ItemData> keysForMultiplier = new List<ItemData>(totalRewards.Keys);
            foreach (var key in keysForMultiplier)
            {
                totalRewards[key] = Mathf.FloorToInt(totalRewards[key] * rewardMultiplier);
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
            // ★ 바로 게임을 멈추지 않고, 3초 대기 후 종료
            StartCoroutine(WaitBeforeFinalizeStage());
        }
    }

    // ★ 게임 완료 전 대기 코루틴 (보상 수집 시간 제공)
    IEnumerator WaitBeforeFinalizeStage()
    {
        // 1. 플레이어에게 3초 무적 시간 부여 (보상 수집용)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.SetVictoryInvincibility(3f);
            Debug.Log("[StageManager] 플레이어에게 3초 무적 시간 부여");
        }

        // 2. 3초 동안 플레이어가 움직일 수 있도록 대기
        yield return new WaitForSeconds(3f);

        // 3. 3초 후 게임 종료
        FinishGame(true); // 토벌 성공
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
        Time.timeScale = 1.0f;
        // 씬 전환 코루틴은 DontDestroyOnLoad인 DungeonSessionManager에서 실행
        // (씬 언로드 시 StageManager가 파괴되면 코루틴이 중단되는 것을 방지)
        DungeonSessionManager.Instance.ReturnToLobby();
        Debug.Log("[StageManager] 거점으로 이동");
    }

    public void SetRewardMultiplier(float multiplier)
    {
        rewardMultiplier = multiplier;
    }
}