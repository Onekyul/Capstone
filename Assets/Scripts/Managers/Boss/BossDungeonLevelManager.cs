using UnityEngine;

public class BossDungeonLevelManager : MonoBehaviour
{
    public static BossDungeonLevelManager instance;

    [Header("Boss Mode Settings")]
    [SerializeField] private float maxExpPerStack = 50f; // 스택 1개 쌓는데 필요한 경험치 (고정값 추천)
    [SerializeField] private float damageBonusPerStack = 1.0f; // 스택당 공격력 배율 증가량 (1.0 = 100%)

    [Header("Current State")]
    public int currentStack = 0;
    public float currentExp = 0f;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // UI 초기화
        UpdateUI();
    }

    // 보스 몬스터가 떨군 보석이 이 함수를 호출함
    public void GainExperience(float amount)
    {
        currentExp += amount;

        // 경험치가 꽉 차면 스택 증가 (초과분은 다음 레벨로 이월)
        if (currentExp >= maxExpPerStack)
        {
            currentExp -= maxExpPerStack;
            currentStack++;

            // 스택 획득 효과음이나 이펙트 추가 가능
            Debug.Log($"[BossLevel] 스택 증가! 현재 스택: {currentStack}");
        }

        UpdateUI();
    }

    // 보스 컨트롤러(그로기 상태)에서 호출할 함수
    public float GetBossDamageMultiplier()
    {
        // 기본 1배 + (스택 * 보너스)
        // 예: 3스택 * 1.0 = +300% 추가 -> 총 4배 데미지
        return 1.0f + (currentStack * damageBonusPerStack);
    }

    // 그로기 모드가 끝날 때 호출할 함수
    public void ResetBossLevelStack()
    {
        currentStack = 0;
        currentExp = 0f;
        UpdateUI();
        Debug.Log("[BossLevel] 스택 초기화 완료");
    }

    private void UpdateUI()
    {
        if (BossDungeonUIManager.instance != null)
        {
            BossDungeonUIManager.instance.UpdateExpUI(currentExp, maxExpPerStack, currentStack);
        }
    }
}