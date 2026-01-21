using UnityEngine;
using UnityEngine.UI;
using StackExchange.Redis;
using System.Collections.Concurrent;
using TMPro;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.EventSystems;

public class ChatManager : MonoBehaviour
{
    [Header("UI connect")]
    public TMP_InputField inputField;
    public TextMeshProUGUI chatDisplay;
    public ScrollRect scrollRect;
    
    [Header("Redis Setting")]
    private ConnectionMultiplexer redis;
    private ISubscriber sub;
    private string myNickname = "Player_" + Random.Range(1000, 9999);// 임시용 코드 나중에 닉네임 가져오기
    
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    private readonly HttpClient client = new HttpClient();
    
    private string serverUrl = "http://localhost:7000/api/chat/send";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ConnectRedis();
        inputField.DeactivateInputField();
    }

    // Update is called once per frame
    void ConnectRedis()
    {
        try
        {
            string connectingString = "localhost:6379";
            redis = ConnectionMultiplexer.Connect(connectingString);
            sub = redis.GetSubscriber();
    
            //메세지 수신 (백그라운드 스레드 -> 큐)
            sub.Subscribe("chat:global", (channel, message) => { messageQueue.Enqueue(message); });
            Debug.Log("Redis 채팅 연결");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Redis 연결 실패 : {e.Message}");
        }
    }

    void Update()
    {
        //메세지 수신 처리
        while (messageQueue.TryDequeue(out string message))
        {
            chatDisplay.text += message+"\n";
            if(scrollRect!=null) scrollRect.verticalNormalizedPosition = 0f;
        }

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (inputField.isFocused)
            {  // 입력창이 켜져 있을시 전송
                if (!string.IsNullOrWhiteSpace(inputField.text))
                {
                    SendMessageToServer(inputField.text);
                }
                
                inputField.text = "";
                inputField.DeactivateInputField();
                
                EventSystem.current.SetSelectedGameObject(null);
            }
            else
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inputField.isFocused)
            {
                inputField.text = "";
                inputField.DeactivateInputField();
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    //서버로 메세지 전송 (비동기)
    async void SendMessageToServer(string msg)
    {
        var req = new {Nickname = myNickname, msg = msg};
        string json = JsonConvert.SerializeObject(req);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            await client.PostAsync(serverUrl, content);
        }
        catch (System.Exception e)
        {
            Debug.LogError("전송 실패 :"+e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if(redis!=null)redis.Close();
    }
}
