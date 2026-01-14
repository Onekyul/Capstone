using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    [Header("능력 데이터 목록")]
    [Tooltip("게임에 존재하는 모든 능력 데이터를 여기에 등록하세요.")]
    public List<AbilityData> allAbilities;

    [Header("Settings")]
    // 1.0f = 100% (기본), 1.5f = 150% (50% 증가)
    public float expMultiplier = 1.0f;

    // 0~39번 능력의 현재 레벨을 저장하는 배열
    private int[] currentAbilityLevels = new int[40];
    int curLevel = 0;
    float curExp = 0f;
    [SerializeField]
    [Tooltip("한 번 경험치 획득 시 얻는 경험치 양")]
    float gainExp = 10f;
    float[] expTable = { 100f, 125f, 150f, 200f, 250f, 300f, 400f, 500f, 650f, 800f, 1000f, 1300f, 1600f, 2000f }; // 레벨업에 필요한 경험치 테이블


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void GainExperience()
    {
        float finalExp = gainExp * expMultiplier;
        curExp += finalExp;
        // [추가] UI 갱신 호출
        DungeonUIManager.instance.UpdateExpBar(curExp, expTable[curLevel]);
        
        if (curLevel >= expTable.Length) return;

        if (curExp >= expTable[curLevel])
        {
            curLevel++;
            curExp -= expTable[curLevel - 1];
            DungeonUIManager.instance.UpdateExpBar(curExp, expTable[curLevel]);
            
            OnLevelUp();
        }
    }
    
    public void OnLevelUp()
    {
        Time.timeScale = 0f;
        
        List<AbilityData> validCandidates = new List<AbilityData>();
        foreach (var ability in allAbilities)
        {
            int id = ability.abilityID;
            if (currentAbilityLevels[id] < ability.maxLevel)
            {
                validCandidates.Add(ability);
            }
        }
        
        List<AbilityData> options = ChooseRandomAbilities(validCandidates, 3);
        
        if (options.Count > 0)
        {
            DungeonUIManager.instance.ShowLevelUpScreen(options);
        }
        else
        {
            Debug.Log("더 이상 찍을 수 있는 능력이 없습니다.");
            Time.timeScale = 1f;
        }
    }
    
    public void OnAbilitySelected(AbilityData chosenAbility)
    {
        int id = chosenAbility.abilityID;
        
        currentAbilityLevels[id]++;
        
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
        
        DungeonUIManager.instance.HideLevelUpScreen();
        Time.timeScale = 1f;
    }

    // 랜덤 뽑기 로직 (중복 방지)
    private List<AbilityData> ChooseRandomAbilities(List<AbilityData> candidates, int count)
    {
        List<AbilityData> results = new List<AbilityData>();
        
        if (candidates.Count <= count)
        {
            return new List<AbilityData>(candidates);
        }
        
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


    // ===== 랜덤 능력 초기화 (던전 종료 시 호출) =====
    public void ResetAbilities()
    {
        Debug.Log("[LevelManager] 능력 레벨 배열 초기화");
        currentAbilityLevels = new int[40];
        curLevel = 0;
        curExp = 0f;
        expMultiplier = 1.0f;
    }


    // 능력을 얻었을 때 호출할 함수 (외부에서 부르기 편하게)
    public void AddExpMultiplier(float amount)
    {
        // 예: 10% 증가면 1.1f를 넘겨줌 -> 1.1 (110%)
        expMultiplier = amount;
    }
}
