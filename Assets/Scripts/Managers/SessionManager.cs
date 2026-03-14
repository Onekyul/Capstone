using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    [Header("User Info")]
    public int UserId { get; private set; }
    public string Nickname { get; private set; }
    public int MaxClearedStage { get; private set; }
    
    private string BaseUrl = "http://localhost:7200/api";
    private const string DEVICE_ID_KEY = "Capstone_DeviceID";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // DataManager가 호출할 로그인 함수
    public void Login(Action<bool> onComplete)
    {
        string deviceId = PlayerPrefs.GetString(DEVICE_ID_KEY, SystemInfo.deviceUniqueIdentifier);
        StartCoroutine(CoGuestLogin(deviceId, onComplete));
    }

    IEnumerator CoGuestLogin(string deviceId, Action<bool> onComplete)
    {
        Debug.Log("[Session] 게스트 로그인 시도...");
        
        // 서버의 GuestLoginReq 모델에 맞춤
        string json = $"{{\"DeviceId\":\"{deviceId}\"}}";
        
        using (UnityWebRequest req = CreatePostRequest(BaseUrl + "/Auth/guest-login", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<LoginResponseDto>(req.downloadHandler.text);
                
                UserId = res.userId;
                Nickname = res.nickname;
                MaxClearedStage = res.stage;
                
                Debug.Log($" [Session] 로그인 성공! ID: {UserId}, Nick: {Nickname}, Stage: {MaxClearedStage}");
                
                // 로그인 성공 시 DataManager에도 닉네임을 덮어씌워줍니다. (채팅 등에 사용)
                if (DataManager.instance != null) DataManager.instance.currentPlayer.nickname = Nickname;

                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($" [Session] 로그인 실패: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }
    }
    public void RegisterNewUser(string deviceId, string nickname, Action<bool> onComplete)
    {
        StartCoroutine(CoRegisterUser(deviceId, nickname, onComplete));
    }

    private IEnumerator CoRegisterUser(string deviceId, string nickname, Action<bool> onComplete)
    {
        Debug.Log($"[Session] 신규 유저 등록  Nickname: {nickname}");
        
        string json = $"{{\"DeviceId\":\"{deviceId}\", \"Nickname\":\"{nickname}\"}}";
        
        using (UnityWebRequest req = CreatePostRequest(BaseUrl + "/Auth/register", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // 가입 성공 시 서버에서 돌려주는 정보 받기
                var res = JsonUtility.FromJson<LoginResponseDto>(req.downloadHandler.text);
                
                UserId = res.userId;
                Nickname = res.nickname;
                MaxClearedStage = res.stage;
                
                Debug.Log($" [Session] 가입 및 로그인 성공 {Nickname}님!");
                
                // DataManager에 닉네임 동기화
                if (DataManager.instance != null && DataManager.instance.currentPlayer != null)
                {
                    DataManager.instance.currentPlayer.nickname = Nickname;
                }

                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($" [Session] 가입 실패: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }
    }
    
    public void CheckNicknameDuplicate(string nickname, Action<bool> onAvailable)
    {
        StartCoroutine(CoCheckNicknameDuplicate(nickname, onAvailable));
    }

    private IEnumerator CoCheckNicknameDuplicate(string nickname, Action<bool> onAvailable)
    {
        // GET 요청이므로 URL 파라미터로 닉네임을 보냅니다.
        string requestUrl = $"{BaseUrl}/Auth/check-nickname?nickname={UnityWebRequest.EscapeURL(nickname)}";

        using (UnityWebRequest req = UnityWebRequest.Get(requestUrl))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // 응답 예시: {"available":true}
                string responseText = req.downloadHandler.text;
                bool isAvailable = responseText.Contains("\"available\":true"); // 간단한 파싱
                
                onAvailable?.Invoke(isAvailable);
            }
            else
            {
                Debug.LogError($"[Session] 중복 확인 통신 실패: {req.error}");
                onAvailable?.Invoke(false); // 통신 에러 시 일단 중복(불가) 처리
            }
        }
    }
    private UnityWebRequest CreatePostRequest(string url, string json)
    {
        var req = UnityWebRequest.PostWwwForm(url, json);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }
}

