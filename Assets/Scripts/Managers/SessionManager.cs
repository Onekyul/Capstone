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
    
    private string BaseUrl = ServerConfig.BackendBaseUrl;
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
    // onComplete(isSuccess, isNotFound) — isNotFound=true 이면 404 미등록 기기
    public void Login(Action<bool, bool> onComplete)
    {
        string deviceId = PlayerPrefs.GetString(DEVICE_ID_KEY, SystemInfo.deviceUniqueIdentifier);
#if UNITY_EDITOR
        if (IsParrelSyncClone())
            deviceId += "_clone";
#endif
        StartCoroutine(CoGuestLogin(deviceId, onComplete));
    }

#if UNITY_EDITOR
    private bool IsParrelSyncClone()
    {
        try
        {
            var type = System.Type.GetType("ParrelSync.ClonesManager, ParrelSync");
            if (type == null) return false;
            var method = type.GetMethod("IsClone",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return method != null && (bool)method.Invoke(null, null);
        }
        catch { return false; }
    }
#endif

    IEnumerator CoGuestLogin(string deviceId, Action<bool, bool> onComplete)
    {
        Debug.Log("[Session] 게스트 로그인 시도...");

        string json = $"{{\"DeviceId\":\"{deviceId}\"}}";

        using (UnityWebRequest req = CreatePostRequest(BaseUrl + "/Auth/guest-login", json))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var res = JsonUtility.FromJson<LoginResponseDto>(req.downloadHandler.text);

                    UserId = res.userId;
                    Nickname = res.nickname;
                    MaxClearedStage = res.stage;

                    Debug.Log($" [Session] 로그인 성공! ID: {UserId}, Nick: {Nickname}, Stage: {MaxClearedStage}");

                    if (DataManager.instance != null) DataManager.instance.currentPlayer.nickname = Nickname;

                    onComplete?.Invoke(true, false);
                }
                catch (Exception e)
                {
                    Debug.LogError($" [Session] 응답 파싱 실패: {e.Message}\nResponse: {req.downloadHandler.text}");
                    onComplete?.Invoke(false, false);
                }
            }
            else if (req.responseCode == 404)
            {
#if UNITY_EDITOR
                // 파럴싱크 클론일 때만 자동 가입 (메인 에디터는 정상 가입 흐름)
                if (IsParrelSyncClone())
                {
                    Debug.Log($"[Session] ParrelSync 클론 — 자동 가입 시도 (DeviceId: {deviceId})");
                    string autoNickname = "Guest_" + deviceId.Substring(Mathf.Max(0, deviceId.Length - 6));
                    yield return StartCoroutine(CoRegisterAndLogin(deviceId, autoNickname, (ok) => onComplete?.Invoke(ok, false)));
                }
                else
                {
                    Debug.Log("[Session] 미등록 기기 — 회원가입 필요");
                    onComplete?.Invoke(false, true);
                }
#else
                Debug.Log("[Session] 미등록 기기 — 회원가입 필요");
                onComplete?.Invoke(false, true);
#endif
            }
            else
            {
                Debug.LogError($" [Session] 로그인 실패: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
                onComplete?.Invoke(false, false);
            }
        }
    }

    IEnumerator CoRegisterAndLogin(string deviceId, string nickname, Action<bool> onComplete)
    {
        string json = $"{{\"DeviceId\":\"{deviceId}\", \"Nickname\":\"{nickname}\"}}";

        using (UnityWebRequest req = CreatePostRequest(BaseUrl + "/Auth/register", json))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var res = JsonUtility.FromJson<LoginResponseDto>(req.downloadHandler.text);
                    UserId = res.userId;
                    Nickname = res.nickname;
                    MaxClearedStage = res.stage;

                    Debug.Log($"[Session] 자동 가입 성공! ID: {UserId}, Nick: {Nickname}");

                    if (DataManager.instance != null) DataManager.instance.currentPlayer.nickname = Nickname;

                    onComplete?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Session] 가입 응답 파싱 실패: {e.Message}");
                    onComplete?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"[Session] 자동 가입 실패: {req.error}\nResponse: {req.downloadHandler.text}");
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

