using UnityEngine;
using UnityEngine.UI; // Slider를 제어하기 위해 반드시 필요합니다.

public class HpBarSlider : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats; // PlayerStats 참조
    private Slider hpSlider;

    void Awake()
    {
        // 내 오브젝트에 붙어있는 Slider 컴포넌트를 가져옵니다.
        hpSlider = GetComponent<Slider>();

        // PlayerStats 자동 찾기 (Inspector에서 할당하지 않았을 경우)
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats를 찾을 수 없습니다!");
            }
        }
    }

    void Start()
    {
        // PlayerStats의 체력 변화 이벤트 구독
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthBar;
            
            // 초기 체력바 설정
            InitializeHealthBar();
        }
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
        }
    }

    // 초기 체력바 설정
    private void InitializeHealthBar()
    {
        float maxHealth = playerStats.GetMaxHP();
        float currentHealth = playerStats.GetCurrentHP();
        
        hpSlider.maxValue = maxHealth;
        hpSlider.value = currentHealth;
        
        Debug.Log($"체력바 초기화: {currentHealth}/{maxHealth}");
    }

    // 체력 변화 이벤트 핸들러
    private void UpdateHealthBar(float currentHealth)
    {
        // 최대 체력이 변경되었을 수 있으므로 매번 체크
        float maxHealth = playerStats.GetMaxHP();
        
        if (!Mathf.Approximately(hpSlider.maxValue, maxHealth))
        {
            hpSlider.maxValue = maxHealth;
            Debug.Log($"체력바 최대값 변경: {maxHealth}");
        }
        
        hpSlider.value = currentHealth;
    }
}