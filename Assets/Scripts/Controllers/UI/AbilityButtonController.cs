using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트 사용을 위해 필수
using TMPro; // TextMeshPro를 쓴다면 필수 (그냥 Text라면 제외)

public class AbilityButtonController : MonoBehaviour
{
    [Header("UI 연결")]
    public Image iconImage;
    public TextMeshProUGUI nameText; // 혹은 public Text nameText;
    public TextMeshProUGUI descText; // 혹은 public Text descText;

    private AbilityData myAbilityData;

    // UIManager가 버튼을 만들면서 이 함수를 통해 데이터를 넣어줍니다.
    public void Setup(AbilityData data)
    {
        myAbilityData = data;

        // UI 기본 설정
        nameText.text = data.abilityName;
        iconImage.sprite = data.icon;

        // --- [핵심] 동적 설명 생성 로직 ---

        // 1. 현재 이 능력의 레벨을 가져옵니다.
        int currentLevel = LevelManager.instance.GetAbilityLevel(data.abilityID);

        // 2. 가져올 수치의 순번(Index)을 정합니다.
        // 현재 0레벨(미습득)이라면 -> 리스트의 0번(1레벨 수치)을 보여줘야 함.
        // 현재 1레벨이라면 -> 리스트의 1번(2레벨 수치)을 보여줘야 함.
        // 즉, index = currentLevel 입니다.
        int valueIndex = currentLevel;

        // 3. 데이터가 안전하게 있는지 확인하고 텍스트를 완성합니다.
        if (data.values != null && valueIndex < data.values.Count)
        {
            // string.Format을 사용해 {0} 자리에 숫자를 집어넣습니다.
            descText.text = string.Format(data.description, data.values[valueIndex]);
        }
        else
        {
            // 만약 수치 리스트를 안 채웠거나 레벨 범위를 넘었다면 그냥 원본 출력
            descText.text = data.description;
        }
    }

    // 버튼 컴포넌트의 OnClick 이벤트에 연결할 함수
    public void OnClick()
    {
        // 클릭되면 레벨매니저에게 "내가 선택됐어(myAbilityData)"라고 보고함
        LevelManager.instance.OnAbilitySelected(myAbilityData);
    }
}