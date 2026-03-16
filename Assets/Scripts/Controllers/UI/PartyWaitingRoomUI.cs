using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

/// <summary>
/// 파티 대기실 UI.
/// 방장: 입장 버튼 / 파티원: 준비 버튼.
/// 1초마다 detail API를 폴링해 멤버 목록과 준비 상태를 갱신한다.
/// </summary>
public class PartyWaitingRoomUI : MonoBehaviour
{
    private string serverUrl = $"{ServerConfig.BackendBaseUrl}/Party";

    [Header("파티 목록 패널 (뒤로가기용)")]
    [SerializeField] private PartyUI partyListUI;

    [Header("정보 표시")]
    [SerializeField] private TextMeshProUGUI partyInfoText;     // "보스던전 | 파티장: 닉네임"
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("멤버 슬롯 (최대 4개, 순서대로 연결)")]
    [SerializeField] private PartyMemberSlot[] memberSlots;     // 4개 고정

    [Header("버튼")]
    [SerializeField] private Button enterButton;    // 방장 전용
    [SerializeField] private Button readyButton;    // 파티원 전용
    [SerializeField] private Button closeButton;    // X 버튼

    [Header("준비 버튼 텍스트")]
    [SerializeField] private TextMeshProUGUI readyButtonText;

    private int _partyId;
    private int _leaderId;
    private bool _isReady;
    private bool _isLeader;
    private Coroutine _pollCoroutine;
    private PartyDetailRes _lastDetail; // 최신 파티 정보 캐시 (입장 시 memberUserIds 사용)

    private static readonly float PollInterval = 1.5f;

    // ============================
    //  열기 / 닫기
    // ============================

    public void Open(int partyId, int leaderId)
    {
        _partyId = partyId;
        _leaderId = leaderId;
        _isReady = false;
        _isLeader = SessionManager.Instance != null && SessionManager.Instance.UserId == leaderId;

        gameObject.SetActive(true);

        enterButton.gameObject.SetActive(_isLeader);
        readyButton.gameObject.SetActive(!_isLeader);

        enterButton.onClick.RemoveAllListeners();
        enterButton.onClick.AddListener(OnEnterClicked);
        readyButton.onClick.RemoveAllListeners();
        readyButton.onClick.AddListener(OnReadyClicked);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClicked);

        UpdateReadyButtonText();
        _pollCoroutine = StartCoroutine(CoPollDetail());
    }

    private void Close()
    {
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }
        gameObject.SetActive(false);
    }

    // ============================
    //  폴링
    // ============================

    private IEnumerator CoPollDetail()
    {
        while (true)
        {
            yield return CoFetchDetail();
            yield return new WaitForSeconds(PollInterval);
        }
    }

    private IEnumerator CoFetchDetail()
    {
        using var req = UnityWebRequest.Get($"{serverUrl}/detail/{_partyId}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[WaitingRoom] detail 응답: {req.downloadHandler.text}");
            var res = JsonUtility.FromJson<PartyDetailRes>(req.downloadHandler.text);
            if (res == null)
            {
                Debug.LogWarning("[WaitingRoom] detail 파싱 결과가 null입니다.");
                yield break;
            }
            if (res.members == null) res.members = new System.Collections.Generic.List<PartyMemberDto>();

            // 파티원: 방장이 입장 확정하면 sessionName이 내려옴 → 자동 입장
            if (!_isLeader && res.status == "InGame" && !string.IsNullOrEmpty(res.sessionName))
            {
                Debug.Log($"[WaitingRoom] 파티원 자동 입장: sessionName={res.sessionName}");
                Close();
                DungeonSessionManager.Instance.EnterBossDungeon(res.sessionName);
                yield break;
            }

            _lastDetail = res;
            RefreshUI(res);
        }
        else
        {
            Debug.LogWarning($"[WaitingRoom] detail 조회 실패: {req.error}");
        }
    }

    // ============================
    //  UI 갱신
    // ============================

    private void RefreshUI(PartyDetailRes res)
    {
        // 상단 정보
        string dungeonName = GetDungeonName(res.dungeonId);
        if (partyInfoText != null)
            partyInfoText.text = $"{dungeonName} | {res.members.Count}/{res.maxCount}명";

        // 멤버 슬롯 — 데이터 있으면 BindData, 없으면 명시적으로 SetEmpty
        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < res.members.Count)
            {
                var m = res.members[i];
                bool isThisLeader = m.userId == res.leaderId;
                memberSlots[i].BindData(m, isThisLeader);
            }
            else
            {
                memberSlots[i].SetEmpty();
            }
        }

        // 입장 버튼 활성화 조건
        if (_isLeader)
        {
            bool onlyLeader = res.members.Count == 1;
            bool allReady = true;
            foreach (var m in res.members)
            {
                if (m.userId == res.leaderId) continue;
                if (!m.isReady) { allReady = false; break; }
            }
            enterButton.interactable = onlyLeader || allReady;
        }
    }

    private string GetDungeonName(int dungeonId)
    {
        return dungeonId switch
        {
            1 => "심층던전(1층)",
            2 => "심층던전(2층)",
            3 => "보스던전",
            _ => $"던전 {dungeonId}"
        };
    }

    // ============================
    //  버튼 핸들러
    // ============================

    private void OnReadyClicked()
    {
        _isReady = !_isReady;
        UpdateReadyButtonText();
        StartCoroutine(CoSendReady());
    }

    private void UpdateReadyButtonText()
    {
        if (readyButtonText != null)
            readyButtonText.text = _isReady ? "준비 취소" : "준비";
    }

    private IEnumerator CoSendReady()
    {
        var body = new PartyReadyReq
        {
            partyId = _partyId,
            userId = SessionManager.Instance.UserId,
            isReady = _isReady
        };

        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest($"{serverUrl}/ready", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[WaitingRoom] 준비 요청 실패: {req.error}");
            _isReady = !_isReady; // 롤백
            UpdateReadyButtonText();
        }
    }

    private void OnEnterClicked()
    {
        StartCoroutine(CoEnterDungeon());
    }

    private IEnumerator CoEnterDungeon()
    {
        enterButton.interactable = false;
        if (statusText != null) statusText.text = "던전 입장 중...";

        // 파티 멤버 userId 목록 수집
        var memberIds = new System.Collections.Generic.List<int>();
        if (_lastDetail?.members != null)
            foreach (var m in _lastDetail.members) memberIds.Add(m.userId);
        else
            memberIds.Add(SessionManager.Instance.UserId);

        var body = new DungeonEnterReq
        {
            partyId = _partyId,
            partyLeaderUserId = SessionManager.Instance.UserId,
            memberUserIds = memberIds.ToArray()
        };

        string json = JsonUtility.ToJson(body);
        string dungeonEnterUrl = serverUrl.Replace("/Party", "/Dungeon") + "/enter";
        using var req = new UnityWebRequest(dungeonEnterUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<DungeonEnterRes>(req.downloadHandler.text);
            if (res.status == "ok")
            {
                Debug.Log($"[WaitingRoom] 던전 입장: sessionName={res.sessionName}");
                Close();
                DungeonSessionManager.Instance.EnterBossDungeon(res.sessionName);
            }
            else
            {
                Debug.LogWarning($"[WaitingRoom] 서버 혼잡: {res.message}");
                if (statusText != null) statusText.text = res.message ?? "서버가 혼잡합니다. 잠시 후 다시 시도해주세요.";
                enterButton.interactable = true;
            }
        }
        else
        {
            string errorMsg = req.downloadHandler?.text ?? req.error;
            Debug.LogWarning($"[WaitingRoom] 입장 실패: {errorMsg}");
            if (statusText != null) statusText.text = "입장 실패. 다시 시도해주세요.";
            enterButton.interactable = true;
        }
    }

    private void OnCloseClicked()
    {
        StartCoroutine(CoLeaveParty());
    }

    private IEnumerator CoLeaveParty()
    {
        var body = new PartyLeaveReq
        {
            partyId = _partyId,
            userId = SessionManager.Instance.UserId
        };

        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest($"{serverUrl}/leave", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[WaitingRoom] 탈퇴 요청 실패: {req.error}");

        // 성공/실패 무관하게 파티 목록으로 돌아감
        Close();
        if (partyListUI != null)
            partyListUI.gameObject.SetActive(true);
    }
}
