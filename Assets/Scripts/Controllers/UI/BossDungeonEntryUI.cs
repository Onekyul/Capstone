using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// 보스 던전 솔로 입장 UI 컨트롤러.
/// 버튼 클릭 → POST /Dungeon/enter → DungeonSessionManager로 접속.
/// </summary>
public class BossDungeonEntryUI : MonoBehaviour
{
    [Header("백엔드 설정")]
    [SerializeField] private string backendUrl = $"{ServerConfig.BackendBaseUrl}/Dungeon/enter";

    [Header("UI")]
    [SerializeField] private GameObject loadingIndicator;

    public void OnEnterBossDungeonClicked()
    {
        StartCoroutine(CoRequestBossSession());
    }

    private IEnumerator CoRequestBossSession()
    {
        if (DungeonSessionManager.Instance == null || SessionManager.Instance == null)
        {
            Debug.LogError("[BossEntry] 매니저 미초기화");
            yield break;
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        int userId = SessionManager.Instance.UserId;
        var body = new DungeonEnterReq
        {
            partyLeaderUserId = userId,
            memberUserIds = new int[] { userId }
        };

        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest(backendUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<DungeonEnterRes>(req.downloadHandler.text);
            if (res.status == "ok")
            {
                Debug.Log($"[BossEntry] 세션 할당: {res.sessionName}");
                DungeonSessionManager.Instance.EnterBossDungeon(res.sessionName);
            }
            else
            {
                Debug.LogWarning($"[BossEntry] 서버 혼잡: {res.message}");
            }
        }
        else
        {
            Debug.LogError($"[BossEntry] 입장 실패: {req.error}\n{req.downloadHandler.text}");
        }
    }
}
