using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class DungeonUIManager : MonoBehaviour
{
    public static DungeonUIManager instance;

    [Header("--- [패널 참조] ---")]
    public GameObject levelUpPanel; // 레벨업 화면
    public GameObject resultPanel;  //  결과창 화면 

    [Header("--- [결과창 UI 컴포넌트] ---")]
    public TextMeshProUGUI resultTitleText;       

    public TextMeshProUGUI eliteChestCountText;   // 은상자 개수 텍스트 
    public TextMeshProUGUI elementChestCountText; // 금상자 개수 텍스트 

    public Transform rewardContentArea;           // 보상 아이콘들이 생성될 부모 
    public GameObject rewardSlotPrefab;           // 보상 아이콘 프리팹 

    [Header("--- [레벨업 UI 컴포넌트] ---")]
    public Transform abilityContainer;
    public GameObject abilityButtonPrefab;

    [Header("--- [HUD] ---")]
    public Slider expSlider;

    void Awake()
    {
        // 싱글턴 설정
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 시작 시 패널들은 모두 꺼두기
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    
    // 파라미터 4개: (성공여부, 은상자수, 금상자수, 보상목록)
    public void ShowResultUI(bool isClear, int eliteCount, int elementCount, Dictionary<ItemData, int> rewards)
    {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);

        // 1. 타이틀 설정
        if (resultTitleText != null)
        {
            resultTitleText.text = isClear ? "<color=yellow>STAGE CLEAR!</color>" : "<color=red>GAME OVER...</color>";
        }

        // 2. 상자 개수 표시
        if (eliteChestCountText != null) eliteChestCountText.text = $"x {eliteCount}";
        if (elementChestCountText != null) elementChestCountText.text = $"x {elementCount}";

        // 3. 기존 보상 슬롯 초기화 (지우기)
        foreach (Transform child in rewardContentArea)
        {
            Destroy(child.gameObject);
        }

        // 4. 보상 아이콘 생성 (아이템 종류만큼 반복)
        foreach (var pair in rewards)
        {
            ItemData item = pair.Key;
            int count = pair.Value;

            if (count <= 0) continue; // 0개는 표시 안 함

            // 프리팹 생성 (복사)
            GameObject slot = Instantiate(rewardSlotPrefab, rewardContentArea);

            // 프리팹 내부의 UI 찾기
            Image iconImg = slot.transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI countTxt = slot.transform.Find("CountText").GetComponent<TextMeshProUGUI>();

            // 값 넣기
            if (iconImg != null) iconImg.sprite = item.icon;
            if (countTxt != null) countTxt.text = $"x{count}";
        }
    }
    // --- [레벨업 관련 함수들 (기존 유지)] ---
    public void ShowLevelUpScreen(List<AbilityData> options)
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f; // 레벨업 시 일시정지

        foreach (Transform child in abilityContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (AbilityData ability in options)
        {
            GameObject newBtn = Instantiate(abilityButtonPrefab, abilityContainer);
            AbilityButtonController buttonScript = newBtn.GetComponent<AbilityButtonController>();
            if (buttonScript != null)
            {
                buttonScript.Setup(ability);
            }
        }
    }

    public void HideLevelUpScreen()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f; // 일시정지 해제
    }

    public void UpdateExpBar(float currentExp, float maxExp)
    {
        if (expSlider != null)
        {
            expSlider.value = currentExp / maxExp;
        }
    }
}