using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 플레이어 전용 HP바. DungeonPlayerStats의 OnHealthChangedLocal 이벤트를 구독.
/// HasInputAuthority인 플레이어에서만 활성화됨.
/// </summary>
public class DungeonHpBarSlider : MonoBehaviour
{
    private Slider hpSlider;
    private DungeonPlayerStats dungeonStats;

    void Awake()
    {
        hpSlider = GetComponent<Slider>();
    }

    /// <summary>
    /// DungeonPlayerStats.Spawned()에서 HasInputAuthority일 때 호출.
    /// </summary>
    public void Init(DungeonPlayerStats stats)
    {
        dungeonStats = stats;
        dungeonStats.OnHealthChangedLocal += UpdateHealthBar;

        // 초기값 설정
        hpSlider.maxValue = dungeonStats.MaxHP;
        hpSlider.value = dungeonStats.NetCurHP;
    }

    void LateUpdate()
    {
        // 월드 스페이스 캔버스에서 회전 고정
        transform.rotation = Quaternion.identity;
    }

    void OnDestroy()
    {
        if (dungeonStats != null)
            dungeonStats.OnHealthChangedLocal -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float currentHp)
    {
        if (hpSlider == null) return;
        hpSlider.maxValue = dungeonStats.MaxHP;
        hpSlider.value = currentHp;
    }
}
