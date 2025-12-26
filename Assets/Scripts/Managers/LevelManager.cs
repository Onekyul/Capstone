using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    [Header("능력 데이터 목록")]
    [Tooltip("게임에 존재하는 모든 능력 데이터를 여기에 등록하세요.")]
    public List<AbilityData> allAbilities;

    [Header("통신 대상")]
    [Tooltip("능력 배열을 전달받을 플레이어 측 컴포넌트")]
    //public PlayerAbilityReceiver playerReceiver;

    // 0~39번 능력의 현재 레벨을 저장하는 배열
    private int[] currentAbilityLevels = new int[40];
    int curLevel = 0;
    int curExp = 0;
    [SerializeField]
    [Tooltip("한 번 경험치 획득 시 얻는 경험치 양")]
    int gainExp = 10;
    int[] expTable = { 100, 125, 150, 200, 250, 300, 400, 500, 650, 800, 1000, 1300, 1600, 2000 }; // 레벨업에 필요한 경험치 테이블


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void GainExperience()
    {
        curExp += gainExp;
        // [추가] UI 갱신 호출
        DungeonUIManager.instance.UpdateExpBar(curExp, expTable[curLevel]);

        // 배열 범위를 넘지 않도록 안전장치 추가 (최고 레벨 도달 시 경험치 획득 불가 처리 등)
        if (curLevel >= expTable.Length) return;

        if (curExp >= expTable[curLevel])
        {
            curLevel++;
            curExp -= expTable[curLevel - 1];
            DungeonUIManager.instance.UpdateExpBar(curExp, expTable[curLevel]);

            // [수정됨] 여기서 리턴만 하는 게 아니라, 레벨업 함수를 실행해야 합니다!
            OnLevelUp();
        }
    }

    // --- [1] 레벨업 시 호출: 랜덤 능력 3개 뽑기 ---
    public void OnLevelUp()
    {
        Time.timeScale = 0f;

        // 1. 뽑을 수 있는 후보군 추리기 (최대 레벨 도달하지 않은 것들만)
        List<AbilityData> validCandidates = new List<AbilityData>();
        foreach (var ability in allAbilities)
        {
            int id = ability.abilityID;
            // 현재 레벨이 최대 레벨보다 작을 때만 후보에 등록
            if (currentAbilityLevels[id] < ability.maxLevel)
            {
                validCandidates.Add(ability);
            }
        }

        // 2. 후보군 중에서 랜덤하게 3개(혹은 그 이하) 선택
        List<AbilityData> options = ChooseRandomAbilities(validCandidates, 3);

        // 3. UI에 선택지 표시 요청 (옵션이 없으면 바로 종료 처리 등 예외 처리 필요)
        if (options.Count > 0)
        {
            DungeonUIManager.instance.ShowLevelUpScreen(options);
        }
        else
        {
            // 더 이상 찍을 스킬이 없을 때의 처리 (골드를 주거나 회복을 시키거나 등)
            Debug.Log("더 이상 찍을 수 있는 능력이 없습니다.");
            Time.timeScale = 1f;
        }
    }

    // --- [2] 플레이어가 능력을 선택했을 때 호출 ---
    public void OnAbilitySelected(AbilityData chosenAbility)
    {
        int id = chosenAbility.abilityID;

        // 1. 해당 능력의 레벨(배열 값)을 1 증가
        currentAbilityLevels[id]++;

        // 2. 플레이어에게 선택한 능력 ID만 전달
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                Debug.Log($"[LevelManager] 선택한 능력: {chosenAbility.abilityName} (ID: {id}), UI 표시 레벨: {currentAbilityLevels[id]}");
                playerStats.AcquireAbility(id);
            }
        }

        // 3. UI 닫기 및 게임 재개
        DungeonUIManager.instance.HideLevelUpScreen();
        Time.timeScale = 1f;
    }

    // 랜덤 뽑기 로직 (중복 방지)
    private List<AbilityData> ChooseRandomAbilities(List<AbilityData> candidates, int count)
    {
        List<AbilityData> results = new List<AbilityData>();

        // 후보가 요청한 개수보다 적으면 전부 다 리턴
        if (candidates.Count <= count)
        {
            return new List<AbilityData>(candidates);
        }

        // 셔플 후 앞의 N개 가져오기 혹은 랜덤 인덱스 뽑기
        List<AbilityData> temp = new List<AbilityData>(candidates);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, temp.Count);
            results.Add(temp[randomIndex]);
            temp.RemoveAt(randomIndex);
        }

        return results;
    }

    // 특정 능력의 현재 레벨을 반환하는 함수
    public int GetAbilityLevel(int id)
    {
        if (id < 0 || id >= currentAbilityLevels.Length) return 0;
        return currentAbilityLevels[id];
    }
}
