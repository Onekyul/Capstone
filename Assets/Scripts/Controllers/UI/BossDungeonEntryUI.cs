using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// 보스 던전 입장 UI 컨트롤러.
/// 버튼 클릭 → 백엔드에 세션 생성 요청 → DungeonSessionManager로 접속.
/// </summary>
public class BossDungeonEntryUI : MonoBehaviour
{
    [Header("백엔드 설정")]
    [SerializeField] private string backendUrl = "http://localhost:7200/api/Dungeon/create-boss-session";

    [Header("UI")]
    [SerializeField] private GameObject loadingIndicator; // 로딩 표시 (옵션)

    /// <summary>
    /// 보스 던전 입장 버튼에 연결.
    /// </summary>
    public void OnEnterBossDungeonClicked()
    {
        StartCoroutine(CoRequestBossSession());
    }

    private IEnumerator CoRequestBossSession()
    {
        if (DungeonSessionManager.Instance == null)
        {
            Debug.LogError("[BossEntry] DungeonSessionManager가 씬에 없습니다.");
            yield break;
        }

        if (SessionManager.Instance == null)
        {
            Debug.LogError("[BossEntry] SessionManager가 초기화되지 않았습니다.");
            yield break;
        }

        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // 백엔드에 세션 생성 요청
        int userId = SessionManager.Instance.UserId;
        string json = $"{{\"userId\":{userId}}}";

        using var req = new UnityWebRequest(backendUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<BossSessionResponse>(req.downloadHandler.text);
            Debug.Log($"[BossEntry] 세션 할당: {response.sessionName}");

            DungeonSessionManager.Instance.EnterBossDungeon(response.sessionName);
        }
        else
        {
            Debug.LogError($"[BossEntry] 세션 생성 실패: {req.error}");
            Debug.LogError($"[BossEntry] Response: {req.downloadHandler.text}");
        }
    }
}

[System.Serializable]
public class BossSessionResponse
{
    public string sessionName;
}
