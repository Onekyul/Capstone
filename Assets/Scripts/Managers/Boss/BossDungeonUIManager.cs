using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossDungeonUIManager : MonoBehaviour
{
    public static BossDungeonUIManager instance;

    [Header("UI Components")]
    [SerializeField] private Slider expSlider;          // 경험치 게이지
    [SerializeField] private TextMeshProUGUI stackText; // 현재 누적 스택 표시 (예: "x3 Damage!")
    [SerializeField] private GameObject resultPanel;    // 클리어 결과창
    [SerializeField] private TextMeshProUGUI resultText;    // 결과 텍스트 (성공/실패)
    [SerializeField] private TextMeshProUGUI clearTimeText; // 클리어 시간 표시

    [Header("보스 HP UI")]
    [SerializeField] private Slider bossHpSlider;       // 보스 HP 슬라이더
    [SerializeField] private TextMeshProUGUI bossHpText; // 보스 HP 수치 텍스트 (선택)

    [Header("로비 복귀")]
    [SerializeField] private Button returnToLobbyButton; // 결과창의 로비로 돌아가기 버튼
    [SerializeField] private float autoReturnDelay = 10f; // 버튼 없을 때 자동 복귀 시간 (초)

    [Header("사망 대기 오버레이")]
    [SerializeField] private GameObject deadOverlay; // 사망 시 표시되는 반투명 패널

    [Header("타이머")]
    [SerializeField] private TextMeshProUGUI timerText; // 남은 시간 표시
    [SerializeField] private float timeLimit = 180f;    // 3분

    private float _remainingTime;
    private bool _timerRunning;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void Start()
    {
        // 타이머는 서버 신호(RPC_StartPhaseTimer)를 받아야 시작됨
    }

    public void StartTimer()
    {
        _remainingTime = timeLimit;
        _timerRunning = true;
    }

    void Update()
    {
        if (!_timerRunning) return;

        _remainingTime -= Time.deltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _timerRunning = false;
            ShowResultPanel(false, 0f);
        }

        if (timerText != null)
        {
            int min = (int)(_remainingTime / 60f);
            int sec = (int)(_remainingTime % 60f);
            timerText.text = $"{min}:{sec:00}";
        }
    }

    public void StopTimer()
    {
        _timerRunning = false;
    }

    public void UpdateBossHP(float currentHp, float maxHp)
    {
        if (bossHpSlider != null)
        {
            bossHpSlider.maxValue = maxHp;
            bossHpSlider.value = currentHp;
        }
        if (bossHpText != null)
            bossHpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
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

    public void ShowResultPanel(bool isSuccess, float clearTime = 0f)
    {
        StopTimer();
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            if (resultText != null)
                resultText.text = isSuccess ? "보스 클리어!" : "실패...";

            if (clearTimeText != null && isSuccess)
            {
                int min = (int)(clearTime / 60f);
                float sec = clearTime % 60f;
                clearTimeText.text = min > 0 ? $"클리어 시간: {min}:{sec:00.0}" : $"클리어 시간: {sec:F1}초";
            }
            else if (clearTimeText != null)
            {
                clearTimeText.text = "";
            }
        }

        // 로비 복귀 버튼 연결
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.gameObject.SetActive(true);
            returnToLobbyButton.onClick.RemoveAllListeners();
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        }
        else
        {
            // 버튼 없으면 일정 시간 후 자동 복귀
            StartCoroutine(CoAutoReturn());
        }
    }

    private void ReturnToLobby()
    {
        if (DungeonSessionManager.Instance != null)
            DungeonSessionManager.Instance.ReturnToLobby();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("BaseArea 1 2");
    }

    private IEnumerator CoAutoReturn()
    {
        yield return new WaitForSeconds(autoReturnDelay);
        ReturnToLobby();
    }
}