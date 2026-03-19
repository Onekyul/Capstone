using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class WizardUI : MonoBehaviour
{
    public static WizardUI instance;
    
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
    public Image enhance_NextIcon;
    public TextMeshProUGUI enhance_NextLevelText; // "+2 이름"
    public TextMeshProUGUI enhance_DescText; // "다음 효과"
    
    public Image enhance_MaterialIcon;
    public TextMeshProUGUI enhance_MaterialText;
    public TextMeshProUGUI enhance_SuccessRateText;
    public Button enhance_Button;

    private string currentSelectedId = "";
    private int currentIndex = 0;

   void Awake()
    {
        instance = this;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => SelectTab(index));
        }
    }

    void OnEnable()
    {
        if (enchantIds.Length > 0) SelectTab(0);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= enchantIds.Length) return;

        currentIndex = index;
        currentSelectedId = enchantIds[index];

        for (int i = 0; i < highlights.Length; i++)
        {
            if (highlights[i] != null) highlights[i].SetActive(i == index);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (DataManager.instance == null) return;

        // 1. 데이터 가져오기
        EnchantData data = DataManager.instance.GetEnchantData(currentSelectedId);
        int currentLevel = DataManager.instance.GetEnchantLevel(currentSelectedId); // 0, 1, 2...

        if (data == null) return;

        // 2. 패널 활성화 분기
        bool isLocked = (currentLevel == 0);
        openStonePanel.SetActive(isLocked);
        enhanceStonePanel.SetActive(!isLocked);

        // 3. 레벨 데이터 확인
        // levels 리스트는 0번 인덱스가 1레벨 데이터입니다.
        // 따라서 Next Level Index는 현재 레벨 숫자와 같습니다. (Lv.0 -> Index 0(Lv.1데이터))
        int nextLevelIndex = currentLevel; 
        
        // 만렙 체크
        if (nextLevelIndex >= data.levels.Count)
        {
            SetMaxLevelUI(data, currentLevel);
            return;
        }

        // 데이터 객체 가져오기
        var nextLevelInfo = data.levels[nextLevelIndex];
        // 현재 레벨 데이터 (0레벨이면 null)
        var curLevelInfo = (currentLevel > 0) ? data.levels[currentLevel - 1] : null;

        // -------------------------------------------------------------
        // ★ 파라미터 비교 및 하이라이팅 로직
        // -------------------------------------------------------------
        // 파라미터 리스트의 순서가 템플릿의 {0}, {1}... 순서와 같다고 가정합니다.
        int paramCount = nextLevelInfo.parameters.Count;
        string[] formatArgs = new string[paramCount];

        for (int i = 0; i < paramCount; i++)
        {
            // A. 다음 레벨 값
            float nextVal = nextLevelInfo.parameters[i].value;
            
            // B. 현재 레벨 값 (없거나 인덱스 초과시 0 처리)
            float curVal = 0;
            if (curLevelInfo != null && i < curLevelInfo.parameters.Count)
            {
                curVal = curLevelInfo.parameters[i].value;
            }

            // C. 값 변화 체크 (부동소수점 오차 고려)
            bool isChanged = Mathf.Abs(nextVal - curVal) > 0.001f;

            if (isChanged)
            {
                // 값이 다르면 -> 초록색으로 숫자만 강조 (예: <color>15</color>)
                formatArgs[i] = $"<color=#00FF00>{nextVal}</color>";
            }
            else
            {
                // 값이 같으면 -> 그냥 흰색 출력 (예: 15)
                formatArgs[i] = $"{nextVal}";
            }
        }

        // 템플릿에 적용: "공격력이 {0} 증가하고..." -> "공격력이 15 증가하고..."
        string finalDesc = string.Format(data.descriptionTemplate, formatArgs);


        // -------------------------------------------------------------
        // 4. 재료 정보 처리
        // -------------------------------------------------------------
        string matName = "";
        int requiredCount = 0;
        int ownedCount = 0;
        Sprite matIconSprite = null;

        // 첫 번째 재료 기준 (여러 개라면 루프 필요)
        if (nextLevelInfo.requiredMaterials.Count > 0)
        {
            var matItem = nextLevelInfo.requiredMaterials[0];
            
            matName = matItem.material.itemName;      // 아이템 이름
            requiredCount = matItem.count;            // 필요 개수
            matIconSprite = matItem.material.icon;    // 아이콘
            
            // 현재 보유량 가져오기
            ownedCount = DataManager.instance.GetInventoryCount(matItem.material.itemId);
        }

        // 재료 충분 여부 판단
        bool isSufficient = ownedCount >= requiredCount;

        // 텍스트 포맷: "나무 (0 / 5)"
        string materialTextStr = $"{matName} ({ownedCount} / {requiredCount})";

        // 색상 결정: 부족하면 빨강, 충분하면 흰색
        Color materialTextColor = isSufficient ? Color.white : Color.red;


        // -------------------------------------------------------------
        // 5. UI 적용
        // -------------------------------------------------------------
        if (isLocked)
        {
            // [해금 패널]
            // 잠금 상태 아이콘이 따로 설정되어 있다면 사용, 없으면 기본 아이콘
            Sprite iconToShow = (data.lockedIcon != null) ? data.lockedIcon : data.icon;
            
            if (open_Icon) open_Icon.sprite = iconToShow;
            open_LevelText.text = $"+0 {data.enchantName}"; 
            
            // 해금 시에는 1레벨 스펙을 보여줌 (하이라이팅 로직 그대로 적용됨 -> 0에서 변했으므로 보통 다 초록색)
            open_DescText.text = finalDesc; 

            if (open_MaterialIcon) open_MaterialIcon.sprite = matIconSprite;
            open_MaterialText.text = materialTextStr;
            open_MaterialText.color = materialTextColor;
            
            open_SuccessRateText.text = $"{nextLevelInfo.successRate}%";
            open_Button.interactable =isSufficient;
        }
        else
        {
            // [강화 패널]
            // Current Info
            if (enhance_CurIcon) enhance_CurIcon.sprite = data.icon;
            enhance_CurLevelText.text = $"+{currentLevel} {data.enchantName}";

            // Next Info
            if (enhance_NextIcon) enhance_NextIcon.sprite = data.icon;
            enhance_NextLevelText.text = $"+{currentLevel + 1} {data.enchantName}";

            // Description (수치가 바뀐 부분만 초록색)
            enhance_DescText.text = finalDesc;

            // Material & Button
            if (enhance_MaterialIcon) enhance_MaterialIcon.sprite = matIconSprite;
            enhance_MaterialText.text = materialTextStr;
            enhance_MaterialText.color = materialTextColor;

            enhance_SuccessRateText.text = $"{nextLevelInfo.successRate}%";
            enhance_Button.interactable = isSufficient;
        }
    }

    // 만렙일 때 UI 처리
    void SetMaxLevelUI(EnchantData data, int currentLevel)
    {
        if (enhance_CurIcon) enhance_CurIcon.sprite = data.icon;
        enhance_CurLevelText.text = $"+{currentLevel} {data.enchantName}";
        
        if (enhance_NextIcon) enhance_NextIcon.sprite = data.icon;
        enhance_NextLevelText.text = "MAX";
        
        // 마지막 레벨의 파라미터를 가져와서 그냥 흰색으로 출력
        var lastLevelInfo = data.levels[data.levels.Count - 1];
        string[] args = new string[lastLevelInfo.parameters.Count];
        for(int i=0; i<args.Length; i++) 
        {
            args[i] = lastLevelInfo.parameters[i].value.ToString();
        }
        
        enhance_DescText.text = string.Format(data.descriptionTemplate, args);

        enhance_SuccessRateText.text = "-";
        enhance_MaterialText.text = "완료";
        enhance_Button.interactable = false;
    }

    // 해금/강화 버튼 클릭 시 실행
    public void OnClickAction()
    {
        // ★ [핵심 1] 연타 방지: 해금 버튼과 강화 버튼을 둘 다 잠시 끕니다.
        if (open_Button != null) open_Button.interactable = false;
        if (enhance_Button != null) enhance_Button.interactable = false;

        Debug.Log("[WizardUI] 서버에 인챈트 해금/강화 요청 중...");

        // ★ [핵심 2] 비동기 콜백 형식으로 변경
        DataManager.instance.TryEnhanceEnchant(currentSelectedId, (isSuccess, message) => 
        {
            // -----------------------------------------------------------------
            // 이 중괄호 안의 코드는 서버에서 주사위를 굴리고 응답이 오면 실행됩니다!
            // -----------------------------------------------------------------
            if (isSuccess) 
            {
                Debug.Log($"[WizardUI] 성공! 서버 메시지: {message}");

                // 플레이어 무기에 즉시 반영
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    WeaponBase[] weapons = player.GetComponentsInChildren<WeaponBase>(true);
                    foreach (var weapon in weapons)
                    {
                        weapon.UpgradeEnchantLevels();
                        Debug.Log($"[WizardUI] {weapon.gameObject.name}의 인챈트 수치를 갱신했습니다.");
                    }
                }
            }
            else
            {
                // 실패 처리
                Debug.Log($"[WizardUI] 실패: {message}");
            }
            
            UpdateUI();
        });
    }
}
