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
    
    private string baseUrl = "http://localhost:7200/api"; 

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
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        StartCoroutine(CoGuestLogin(deviceId, onComplete));
    }

    IEnumerator CoGuestLogin(string deviceId, Action<bool> onComplete)
    {
        Debug.Log("[Session] 게스트 로그인 시도...");
        
        // 서버의 GuestLoginReq 모델에 맞춤
        string json = $"{{\"DeviceId\":\"{deviceId}\"}}";
        
        using (UnityWebRequest req = CreatePostRequest(baseUrl + "/Auth/guest-login", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<LoginResponseDto>(req.downloadHandler.text);
                
                UserId = res.userId;
                Nickname = res.nickname;
                MaxClearedStage = res.stage;
                
                Debug.Log($" [Session] 로그인 성공! ID: {UserId}, Nick: {Nickname}, Stage: {MaxClearedStage}");
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

