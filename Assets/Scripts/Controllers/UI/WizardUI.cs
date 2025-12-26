using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnchantUI : MonoBehaviour
{
    public static EnchantUI instance;

    [Header("UI 연결 (좌측 탭)")] 
    public Button[] slotButtons;       // 탭 버튼들
    public GameObject[] highlights;    // 선택 테두리
    public string[] enchantIds;        // ID 매핑용 배열

    [Header("패널 루트 (상태별 전환)")]
    public GameObject openStonePanel;    // 0레벨용 (해금)
    public GameObject enhanceStonePanel; // 1레벨 이상용 (강화)

    // BlacksmithUI 처럼 '현재'와 '다음'으로 논리적 구분
    // 다만, 패널이 두 개라 인스펙터 연결을 위해 내부 클래스로 정리하지 않고 플랫하게 풀되,
    // UpdateUI 내부 로직에서 BlacksmithUI와 같은 흐름을 타도록 설계했습니다.

    [Header("1. 해금 패널 UI (Open Panel)")]
    public Image open_Icon;
    public TextMeshProUGUI open_LevelText; // "+0 이름"
    public TextMeshProUGUI open_DescText;  // "1레벨 효과 미리보기"
    public Image open_MaterialIcon;
    public TextMeshProUGUI open_MaterialText;
    public TextMeshProUGUI open_SuccessRateText;
    public Button open_Button;

    [Header("2. 강화 패널 UI (Enhance Panel)")]
    public Image enhance_CurIcon;
    public TextMeshProUGUI enhance_CurLevelText; // "+1 이름"
    public TextMeshProUGUI enhance_CurDescText;  // "현재 효과"
    
    public Image enhance_NextIcon;
    public TextMeshProUGUI enhance_NextLevelText; // "+2 이름"
    public TextMeshProUGUI enhance_NextDescText;  // "다음 효과"
    
    public Image enhance_MaterialIcon;
    public TextMeshProUGUI enhance_MaterialText;
    public TextMeshProUGUI enhance_SuccessRateText;
    public Button enhance_Button;

    private string currentSelectedId = "";
    private int currentIndex = 0;

    void Awake()
    {
        instance = this;
        
        // 버튼 리스너 연결
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => SelectTab(index));
        }
    }

    void OnEnable()
    {
        // 켜질 때 첫 번째 탭 선택
        if (enchantIds.Length > 0) SelectTab(0);
    }

    // 탭 선택 (BlacksmithUI와 구조 통일)
    public void SelectTab(int index)
    {
        if (index < 0 || index >= enchantIds.Length) return;

        currentIndex = index;
        currentSelectedId = enchantIds[index];

        // 하이라이트 갱신
        for (int i = 0; i < highlights.Length; i++)
        {
            if (highlights[i] != null)
                highlights[i].SetActive(i == index);
        }
            
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (DataManager.instance == null) return;

        // 1. 현재 선택된 인챈트 정보 가져오기
        EnchantData data = DataManager.instance.GetEnchantData(currentSelectedId);
        int currentLevel = DataManager.instance.GetEnchantLevel(currentSelectedId);

        if (data == null) return;
        
        string nameStr = data.enchantName;
        Sprite icon = data.icon;
        
        // 상태에 따른 패널 활성화
        bool isLocked = (currentLevel == 0);
        openStonePanel.SetActive(isLocked);
        enhanceStonePanel.SetActive(!isLocked);
        
        if (isLocked)
        {
            // 해금 모드: 현재 정보는 크게 중요하지 않음 (보통 비워두거나 0레벨 표시)
            // 해금 패널의 메인은 "다음 단계(1레벨)" 정보이므로 여기선 패스
        }
        else
        {
            // 강화 모드: 현재 레벨 정보 표시
            string curDesc = data.GetDescription(currentLevel);
            
            enhance_CurLevelText.text = $"+{currentLevel} {nameStr}";
            enhance_CurDescText.text = curDesc;
            if (enhance_CurIcon != null) enhance_CurIcon.sprite = icon;
        }
        // 해금(0->1)이든 강화(N->N+1)이든 '다음 단계'를 가져옴
        int nextLevel = currentLevel + 1;
        var nextStep = data.GetNextLevelInfo(nextLevel);

        if (nextStep != null)
        {
            // 변수 준비
            string nextNameStr = $"+{nextLevel} {nameStr}";
            string nextDescStr = data.GetDescription(nextLevel);
            
            // 재료 정보 준비
            string matName = "재료";
            string matId = "";
            int matCount = 0;
            Sprite matIcon = null;

            if (nextStep.requiredMaterials.Count > 0)
            {
                var matInfo = nextStep.requiredMaterials[0];
                matName = matInfo.material.itemName;
                matId = matInfo.material.itemId;
                matCount = matInfo.count;
                matIcon = matInfo.material.icon;
            }

            // 보유량 체크 (BlacksmithUI 로직 동일)
            bool hasMaterial = DataManager.instance.HasInventory(matId, matCount);
            string matTextStr = $"{matName} x {matCount}"; // "x 5" 등 포맷은 취향껏
            Color matColor = hasMaterial ? Color.white : Color.red;

            // UI 적용 (패널에 따라 분기)
            if (isLocked)
            {
                // [해금 패널 적용]
                if (open_Icon != null) open_Icon.sprite = icon;
                open_LevelText.text = $"+0 {nameStr}"; // 해금 대상 이름
                open_DescText.text = nextDescStr;      // 1레벨 효과 미리보기
                
                open_SuccessRateText.text = $"{nextStep.successRate}%";
                
                // 재료 및 버튼
                if (open_MaterialIcon != null) open_MaterialIcon.sprite = matIcon;
                open_MaterialText.text = matTextStr;
                open_MaterialText.color = matColor;
                open_Button.interactable = hasMaterial;
            }
            else
            {
                // [강화 패널 적용]
                if (enhance_NextIcon != null) enhance_NextIcon.sprite = icon;
                enhance_NextLevelText.text = nextNameStr;
                enhance_NextDescText.text = nextDescStr;

                enhance_SuccessRateText.text = $"{nextStep.successRate}%";

                // 재료 및 버튼
                if (enhance_MaterialIcon != null) enhance_MaterialIcon.sprite = matIcon;
                enhance_MaterialText.text = matTextStr;
                enhance_MaterialText.color = matColor;
                enhance_Button.interactable = hasMaterial;
            }
        }
        else
        {
            // MAX 레벨 처리 (강화 패널만 해당됨, 해금 패널은 MAX일 수 없음)
            if (!isLocked)
            {
                enhance_NextLevelText.text = "MAX";
                enhance_NextDescText.text = "최고 레벨";
                enhance_SuccessRateText.text = "-";
                enhance_MaterialText.text = "완료";
                enhance_Button.interactable = false;
            }
        }
    }

    // 강화/해금 버튼 클릭 (BlacksmithUI 구조와 동일)
    public void OnClickAction()
    {
        // 현재 선택된 ID로 강화 시도
        bool isSuccess = DataManager.instance.TryEnhanceEnchant(currentSelectedId);

        if (isSuccess)
        {
            // 성공 (UI 갱신)
            UpdateUI();
        }
        else
        {
            // 실패
            Debug.Log("재료가 부족하거나 강화할 수 없습니다.");
        }
    }
}