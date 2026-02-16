using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossDungeonUIManager : MonoBehaviour
{
    public static BossDungeonUIManager instance;

    [Header("UI Components")]
    [SerializeField] private Slider expSlider;          // 경험치 게이지
    [SerializeField] private TextMeshProUGUI stackText; // 현재 누적 스택 표시 (예: "x3 Damage!")
    [SerializeField] private GameObject resultPanel;    // 클리어 결과창

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    public void UpdateExpUI(float currentExp, float maxExp, int stack)
    {
        if (expSlider != null)
        {
            expSlider.value = currentExp / maxExp;
        }

        if (stackText != null)
        {
            // 스택이 0일 땐 그냥 레벨 표시, 쌓이면 배율 표시 등 연출 자유롭게
            stackText.text = $"BURST STACK: {stack}";

            // 스택이 쌓일 때 글자가 커졌다 작아지는 애니메이션 등을 넣으면 타격감이 좋음
        }
    }

    public void ShowResultPanel(bool isSuccess)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            // 결과창 텍스트 설정 등 추가 로직
        }
    }
}