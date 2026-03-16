#if UNITY_SERVER
using System;
using System.Threading;
using UnityEngine;
using StackExchange.Redis;

/// <summary>
/// Redis Pub/Sub으로 백엔드의 세션 할당 메시지를 수신.
/// 채널: "boss-dungeon:assign:{serverId}" (서버 전용 채널)
/// 메시지 형식: { "sessionName": "boss-xxx", "memberUserIds": [123, 456] }
/// </summary>
public class RedisSessionListener : MonoBehaviour
{
    public static RedisSessionListener Instance { get; private set; }

    /// <summary>세션 할당 이벤트. (sessionName, memberUserIds)</summary>
    public event Action<string, int[], int> OnSessionRequested;

    [Header("Redis 설정")]
    [SerializeField] private string redisConnectionString = "localhost:6379";

    private ConnectionMultiplexer _redis;
    private ISubscriber _subscriber;
    private SynchronizationContext _mainThread;
    private string _assignChannel;

    [System.Serializable]
    private class AssignMessage
    {
        public string sessionName;
        public int[] memberUserIds;
        public int partyId;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Awake에서 파싱해야 BossDungeonServer.Start()의 Subscribe() 호출 전에 주소가 세팅됨
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-redis")
                    redisConnectionString = args[i + 1];
            }

            Debug.Log($"[RedisSession] Redis 주소: {redisConnectionString}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _mainThread = SynchronizationContext.Current;
        // serverId는 BossDungeonServer가 설정한 뒤 Subscribe() 호출
    }

    /// <summary>
    /// BossDungeonServer.Start()에서 serverId 확정 후 호출.
    /// </summary>
    public void Subscribe(string serverId)
    {
        _assignChannel = $"boss-dungeon:assign:{serverId}";
        ConnectRedis();
    }

    private async void ConnectRedis()
    {
        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            _subscriber = _redis.GetSubscriber();

            await _subscriber.SubscribeAsync(
                RedisChannel.Literal(_assignChannel),
                (channel, message) => ParseAndDispatch(message.ToString())
            );

            Debug.Log($"[RedisSession] Redis 연결 성공, '{_assignChannel}' 채널 구독 중");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RedisSession] Redis 연결 실패: {e.Message}");
        }
    }

    private void ParseAndDispatch(string message)
    {
        AssignMessage data = null;
        try { data = JsonUtility.FromJson<AssignMessage>(message); }
        catch (Exception e) { Debug.LogError($"[RedisSession] 메시지 파싱 실패: {e.Message}\n{message}"); return; }

        if (data == null || string.IsNullOrEmpty(data.sessionName)) return;

        _mainThread.Post(_ =>
        {
            Debug.Log($"[RedisSession] 세션 할당 수신: {data.sessionName} ({data.memberUserIds?.Length ?? 0}명)");
            OnSessionRequested?.Invoke(data.sessionName, data.memberUserIds ?? new int[0], data.partyId);
        }, null);
    }

    /// <summary>세션 준비 완료 알림 (기존 호환용)</summary>
    public void NotifySessionReady(string sessionName)
    {
        _subscriber?.PublishAsync(RedisChannel.Literal("boss-dungeon:ready"), sessionName);
    }

    void OnDestroy()
    {
        _subscriber?.UnsubscribeAll();
        _redis?.Close();
    }
}
#endif
