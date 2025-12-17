using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트 사용을 위해 필수
using TMPro; // TextMeshPro를 쓴다면 필수 (그냥 Text라면 제외)

public class AbilityButton : MonoBehaviour
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

        // UI에 텍스트와 이미지 표시
        nameText.text = data.abilityName;
        descText.text = data.description;
        iconImage.sprite = data.icon;
    }

    // 버튼 컴포넌트의 OnClick 이벤트에 연결할 함수
    public void OnClick()
    {
        // 클릭되면 레벨매니저에게 "내가 선택됐어(myAbilityData)"라고 보고함
        LevelManager.instance.OnAbilitySelected(myAbilityData);
    }
}