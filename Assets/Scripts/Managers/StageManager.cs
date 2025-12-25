using UnityEngine;
using System.Collections.Generic;
using TMPro; // 텍스트 제어를 위해 필요

public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("--- [보상 설정] (데이터를 여기에 드래그하세요) ---")]
    [Tooltip("엘리트 상자 1개당 얻을 수 있는 아이템 목록 (기본 재료 + 조각)")]
    public List<RewardRule> eliteChestRewards;

    [Tooltip("엘리멘트 상자 1개당 얻을 수 있는 아이템 목록 (상위 재료)")]
    public List<RewardRule> elementChestRewards;

    // 보상 규칙 정의용 구조체 (인스펙터에서 보임)
    [System.Serializable]
    public struct RewardRule
    {
        public ItemData item;     // 획득할 아이템 데이터
        public int minAmount;     // 최소 개수
        public int maxAmount;     // 최대 개수
    }

    [Header("--- [게임 상태] ---")]
    public float stageTimeLimit = 300f; // 제한 시간 (5분 = 300초)
    private float currentTimer;
    public bool isGameEnded = false;

    // 상자 획득 개수
    public int eliteChestCount = 0;
    public int elementChestCount = 0;

    [Header("--- [스폰 & UI 참조] ---")]
    public Transform[] spawnPoints;          // 몬스터 스폰 위치들
    public GameObject[] elementMonsterPrefabs; // 속성 몬스터 프리팹
    public TextMeshProUGUI timerText;        // 타이머 UI (선택 사항)
    public TextMeshProUGUI objectiveText;    // 목표 텍스트 (선택 사항)

    // 목표 몬스터 처치 수 (필요 시 사용)
    private int targetMonstersCount = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // 초기화
        currentTimer = stageTimeLimit;
        isGameEnded = false;
        eliteChestCount = 0;
        elementChestCount = 0;

        // 시작 시 몬스터 스폰 (기존 로직 유지)
        SpawnElementalMonsters();
    }

    void Update()
    {
        if (isGameEnded) return;

        // 타이머 감소
        currentTimer -= Time.deltaTime;
        UpdateUIText();

        // 제한 시간 종료 = 생존 성공
        if (currentTimer <= 0)
        {
            FinishGame(true); // true = 클리어(생존)
        }
    }

    // --- [상자 획득 함수 (외부에서 호출)] ---
    public void CollectEliteChest()
    {
        eliteChestCount++;
        // Debug.Log($"은상자 획득! 현재: {eliteChestCount}");
    }

    public void CollectElementChest()
    {
        elementChestCount++;
        // Debug.Log($"금상자 획득! 현재: {elementChestCount}");
    }

    // --- [게임 종료 및 정산 처리] ---
    public void FinishGame(bool isClear)
    {
        if (isGameEnded) return;
        isGameEnded = true;

        // 1. 게임 정지
        Time.timeScale = 0f;
        if (MonsterPool.Instance != null) MonsterPool.Instance.StopSpawning();

        // 2. 최종 보상 계산
        Dictionary<ItemData, int> finalRewards = CalculateTotalRewards(isClear);

        // 3. 인벤토리 저장 (DataManager)
        foreach (var pair in finalRewards)
        {
            if (pair.Value > 0)
            {
                // ItemData에 있는 ID를 사용하여 저장
                DataManager.instance.AddInventory(pair.Key.itemId, pair.Value);
            }
        }

        // 4. UI 매니저에게 결과창 표시 요청
        // (성공여부, 은상자수, 금상자수, 보상목록 전달)
        DungeonUIManager.instance.ShowResultUI(isClear, eliteChestCount, elementChestCount, finalRewards);
    }

    // --- [보상 계산 핵심 로직] ---
    private Dictionary<ItemData, int> CalculateTotalRewards(bool isClear)
    {
        Dictionary<ItemData, int> totalRewards = new Dictionary<ItemData, int>();

        // 1. 엘리트 상자 정산 (설정된 모든 규칙 적용)
        for (int i = 0; i < eliteChestCount; i++)
        {
            foreach (var rule in eliteChestRewards)
            {
                // 최소~최대 사이 랜덤 개수
                int amount = Random.Range(rule.minAmount, rule.maxAmount + 1);
                AddItemToDict(totalRewards, rule.item, amount);
            }
        }

        // 2. 엘리멘트 상자 정산
        for (int i = 0; i < elementChestCount; i++)
        {
            foreach (var rule in elementChestRewards)
            {
                int amount = Random.Range(rule.minAmount, rule.maxAmount + 1);
                AddItemToDict(totalRewards, rule.item, amount);
            }
        }

        // 3. 실패(죽음) 시 패널티 적용 (0.7배)
        if (!isClear)
        {
            // 딕셔너리 키 복사 후 순회 (수정 중 에러 방지)
            List<ItemData> keys = new List<ItemData>(totalRewards.Keys);
            foreach (var key in keys)
            {
                int original = totalRewards[key];
                int penalized = Mathf.FloorToInt(original * 0.7f); // 0.7 곱하고 소수점 버림
                totalRewards[key] = penalized;
            }
        }

        return totalRewards;
    }

    // 딕셔너리 추가 도우미 함수
    void AddItemToDict(Dictionary<ItemData, int> dict, ItemData item, int amount)
    {
        if (item == null) return;
        if (dict.ContainsKey(item)) dict[item] += amount;
        else dict.Add(item, amount);
    }

    // --- [기타 유틸리티 (스폰, UI)] ---
    void SpawnElementalMonsters()
    {
        if (spawnPoints == null || elementMonsterPrefabs == null) return;

        // 여기에 기존 몬스터 스폰 로직 구현 (예시)
        // targetMonstersCount = ...;
    }

    // 외부에서 몬스터 죽었을 때 호출 (선택 사항)
    public void OnTargetMonsterDead()
    {
        if (isGameEnded) return;
        targetMonstersCount--;
        // 목표 몬스터를 다 잡으면 클리어 처리 가능
        // if (targetMonstersCount <= 0) FinishGame(true);
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
}