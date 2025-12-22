using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필수
using UnityEngine.UI;

public class DungeonUIManager : MonoBehaviour
{
    // 외부에서 접근하기 위한 싱글턴
    public static DungeonUIManager instance;

    // --- [레벨업 UI 전용] ---
    [Header("레벨업 UI")]
    [Tooltip("레벨업 시 켜질 전체 패널 (LevelUpPanel)")]
    public GameObject levelUpPanel;

    [Tooltip("능력 버튼들이 생성될 부모 오브젝트 (Container)")]
    public Transform abilityContainer;

    [Tooltip("생성할 능력 버튼의 프리팹 (AbilityButtonPrefab)")]
    public GameObject abilityButtonPrefab;

    [Header("HUD")]
    public Slider expSlider; // 인스펙터에서 경험치 슬라이더 연결

    void Awake()
    {
        // 싱글턴 설정
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 시작할 때 레벨업 패널은 꺼둡니다.
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }

    // --- 레벨업 화면 관련 함수들 ---

    // LevelManager가 호출하는 함수: 화면을 띄우고 버튼을 만듦
    public void ShowLevelUpScreen(List<AbilityData> options)
    {
        levelUpPanel.SetActive(true);

        // 1. 기존에 떠 있던 버튼들이 있다면 싹 지워줍니다. (초기화)
        // (이게 없으면 레벨업 할 때마다 버튼이 계속 쌓입니다)
        foreach (Transform child in abilityContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 받아온 옵션 개수만큼 버튼을 새로 만듭니다.
        foreach (AbilityData ability in options)
        {
            // 프리팹을 이용해 버튼 생성 (부모는 abilityContainer)
            GameObject newBtn = Instantiate(abilityButtonPrefab, abilityContainer);

            // 버튼에 붙어있는 AbilityButtonController 스크립트를 가져와서 데이터를 넣어줍니다.
            AbilityButtonController buttonScript = newBtn.GetComponent<AbilityButtonController>();

            if (buttonScript != null)
            {
                buttonScript.Setup(ability);
            }
        }
    }

    // LevelManager가 호출하는 함수: 화면 끄기
    public void HideLevelUpScreen()
    {
        levelUpPanel.SetActive(false);
    }

    // 경험치 비율(0.0 ~ 1.0)을 받아서 슬라이더에 반영
    public void UpdateExpBar(float currentExp, float maxExp)
    {
        if (expSlider != null)
        {
            expSlider.value = currentExp / maxExp;
        }
    }
}