using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

/// <summary>
/// 파티 목록/생성 UI 컨트롤러.
/// 페이지당 5개 슬롯 고정, 이전/다음 화살표로 페이지 이동.
/// 파티 생성은 버튼 클릭 즉시 자동 생성 (닉네임 기반 제목).
/// </summary>
public class PartyUI : MonoBehaviour
{
    private string serverUrl = "http://localhost:7200/api/Party";

    [Header("대기실 패널")]
    [SerializeField] private PartyWaitingRoomUI waitingRoomUI;

    [Header("파티 슬롯 (5개 고정, 순서대로 연결)")]
    [SerializeField] private GameObject[] partySlots;

    [Header("페이지네이션")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("상태 표시")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("버튼")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createButton;

    private const int SlotsPerPage = 5;
    private PartyDto[] _allParties = new PartyDto[0];
    private int _currentPage = 0;

    void OnEnable()
    {
        refreshButton.onClick.AddListener(OnRefreshClicked);
        createButton.onClick.AddListener(OnCreateClicked);
        prevButton.onClick.AddListener(OnPrevPage);
        nextButton.onClick.AddListener(OnNextPage);

        if (statusText != null) statusText.text = "";

        _currentPage = 0;
        OnRefreshClicked();
    }

    void OnDisable()
    {
        refreshButton.onClick.RemoveListener(OnRefreshClicked);
        createButton.onClick.RemoveListener(OnCreateClicked);
        prevButton.onClick.RemoveListener(OnPrevPage);
        nextButton.onClick.RemoveListener(OnNextPage);
    }

    // ============================
    //  새로고침
    // ============================

    private void OnRefreshClicked()
    {
        StartCoroutine(CoFetchPartyList());
    }

    private IEnumerator CoFetchPartyList()
    {
        using var req = UnityWebRequest.Get($"{serverUrl}/list");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"parties\":" + req.downloadHandler.text + "}";
            var wrapper = JsonUtility.FromJson<PartyListWrapper>(json);
            _allParties = wrapper.parties ?? new PartyDto[0];
            _currentPage = 0;
            RefreshUI();
        }
        else
        {
            Debug.LogWarning($"[PartyUI] 목록 조회 실패: {req.error}");
        }
    }

    // ============================
    //  페이지네이션
    // ============================

    private void OnPrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RefreshUI();
        }
    }

    private void OnNextPage()
    {
        if (_currentPage < GetTotalPages() - 1)
        {
            _currentPage++;
            RefreshUI();
        }
    }

    private int GetTotalPages()
    {
        if (_allParties.Length == 0) return 1;
        return Mathf.CeilToInt((float)_allParties.Length / SlotsPerPage);
    }

    // ============================
    //  UI 갱신
    // ============================

    private void RefreshUI()
    {
        int totalPages = GetTotalPages();
        int startIdx = _currentPage * SlotsPerPage;

        for (int i = 0; i < partySlots.Length; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx < _allParties.Length)
            {
                partySlots[i].SetActive(true);
                SetupSlot(partySlots[i], _allParties[dataIdx], dataIdx + 1);
            }
            else
            {
                partySlots[i].SetActive(false);
            }
        }

        if (pageText != null)
            pageText.text = $"{_currentPage + 1} / {totalPages}";

        prevButton.interactable = _currentPage > 0;
        nextButton.interactable = _currentPage < totalPages - 1;
    }

    private void SetupSlot(GameObject slot, PartyDto party, int displayIndex)
    {
        TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>();
        Button joinButton = slot.GetComponentInChildren<Button>();

        // texts[0]: 순번(1부터), [1]: 던전종류, [2]: 방장닉네임, [3]: 인원수
        if (texts.Length >= 4)
        {
            texts[0].text = displayIndex.ToString();
            texts[1].text = GetDungeonName(party.dungeonId);
            texts[2].text = party.leaderName;
            texts[3].text = $"{party.currentCount}/{party.maxCount}";
        }

        if (joinButton != null)
        {
            bool isFull = party.currentCount >= party.maxCount;
            joinButton.interactable = !isFull;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => OnJoinClicked(party));
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
    //  파티 가입
    // ============================

    private void OnJoinClicked(PartyDto party)
    {
        StartCoroutine(CoJoinParty(party));
    }

    private IEnumerator CoJoinParty(PartyDto party)
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("[PartyUI] SessionManager가 초기화되지 않았습니다.");
            yield break;
        }

        var body = new PartyJoinReq
        {
            partyId = party.partyId,
            userId = SessionManager.Instance.UserId,
            nickname = SessionManager.Instance.Nickname
        };

        string json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest($"{serverUrl}/join", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<PartyJoinRes>(req.downloadHandler.text);
            Debug.Log($"[PartyUI] 파티 가입 완료: partyId={res.partyId}");
            OpenWaitingRoom(res.partyId, res.leaderId);
        }
        else
        {
            string errorMsg = req.downloadHandler?.text ?? req.error;
            Debug.LogWarning($"[PartyUI] 파티 가입 실패: {errorMsg}");
            if (statusText != null) statusText.text = errorMsg;
        }
    }

    // ============================
    //  파티 생성 (즉시 생성)
    // ============================

    private void OnCreateClicked()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("[PartyUI] SessionManager가 초기화되지 않았습니다.");
            return;
        }
        StartCoroutine(CoCreateParty());
    }

    private IEnumerator CoCreateParty()
    {
        string nickname = SessionManager.Instance.Nickname;
        string autoTitle = $"{nickname}의 파티";

        var body = new PartyCreateReq
        {
            title = autoTitle,
            leaderId = SessionManager.Instance.UserId,
            leaderNickname = nickname,
            maxCount = 4,
            dungeonId = 3
        };

        string json = JsonUtility.ToJson(body);

        using var req = new UnityWebRequest($"{serverUrl}/create", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<PartyCreateRes>(req.downloadHandler.text);
            Debug.Log($"[PartyUI] 파티 생성 완료: partyId={res.partyId}");
            OpenWaitingRoom(res.partyId, res.leaderId);
        }
        else
        {
            Debug.LogError($"[PartyUI] 파티 생성 실패: {req.error}");
            if (statusText != null) statusText.text = "파티 생성에 실패했습니다.";
        }
    }

    // ============================
    //  대기실 전환
    // ============================

    private void OpenWaitingRoom(int partyId, int leaderId)
    {
        if (waitingRoomUI == null)
        {
            Debug.LogError("[PartyUI] waitingRoomUI가 연결되지 않았습니다.");
            return;
        }
        gameObject.SetActive(false);
        waitingRoomUI.Open(partyId, leaderId);
    }
}
