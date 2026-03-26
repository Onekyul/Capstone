using UnityEngine;
using TMPro;

/// <summary>
/// 보스던전 랭킹 UI.
/// DataManager.FetchBossRanking()으로 데이터를 받아 표시.
/// </summary>
public class RankingUI : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private int topCount = 10;

    [Header("UI")]
    [SerializeField] private Transform rankingListParent;   // ScrollView > Content
    [SerializeField] private GameObject rankingEntryPrefab;  // 한 줄 프리팹 (TMP_Text 3개: 순위, 닉네임, 시간)

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshRanking();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void RefreshRanking()
    {
        if (DataManager.instance == null) return;

        DataManager.instance.FetchBossRanking(topCount, OnRankingReceived);
    }

    private void OnRankingReceived(RankingResponseDto data)
    {
        // 기존 항목 제거
        foreach (Transform child in rankingListParent)
            Destroy(child.gameObject);

        if (data == null || data.rankings == null || data.rankings.Count == 0)
        {
            Debug.Log("[RankingUI] 랭킹 데이터 없음");
            return;
        }

        foreach (var entry in data.rankings)
        {
            GameObject go = Instantiate(rankingEntryPrefab, rankingListParent);
            TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 3)
            {
                texts[0].text = $"{entry.rank}";
                texts[1].text = entry.nickname;
                texts[2].text = FormatTime(entry.clearTime);
            }
        }
    }

    private string FormatTime(float seconds)
    {
        int min = (int)(seconds / 60f);
        float sec = seconds % 60f;
        return min > 0 ? $"{min}:{sec:00.0}" : $"{sec:F1}초";
    }
}
