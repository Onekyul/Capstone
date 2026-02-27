#if UNITY_SERVER
using System;
using System.Threading;
using UnityEngine;
using StackExchange.Redis;

/// <summary>
/// Redis Pub/Sub으로 백엔드의 세션 생성 요청을 수신하는 리스너.
/// 채널 "boss-dungeon:create"를 구독하며,
/// 메시지 형식: "세션이름|최대인원" (예: "boss_party_abc123|4")
///
/// 세션이 준비되면 "boss-dungeon:ready" 채널로 알림.
///
/// Linux Headless 빌드 전용 (#if UNITY_SERVER).
/// </summary>
public class RedisSessionListener : MonoBehaviour
{
    public static RedisSessionListener Instance { get; private set; }

    /// <summary>세션 생성 요청 이벤트. (sessionName, maxPlayers)</summary>
    public event Action<string, int> OnSessionRequested;

    [Header("Redis 설정")]
    [SerializeField] private string redisConnectionString = "localhost:6379";

    private ConnectionMultiplexer _redis;
    private ISubscriber _subscriber;
    private SynchronizationContext _mainThread;

    private const string CHANNEL_CREATE = "boss-dungeon:create";
    private const string CHANNEL_READY = "boss-dungeon:ready";

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

    void Start()
    {
        _mainThread = SynchronizationContext.Current;

        // 커맨드라인에서 Redis 주소 오버라이드 가능
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-redis")
            {
                redisConnectionString = args[i + 1];
                break;
            }
        }

        ConnectRedis();
    }

    private async void ConnectRedis()
    {
        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            _subscriber = _redis.GetSubscriber();

            await _subscriber.SubscribeAsync(
                RedisChannel.Literal(CHANNEL_CREATE),
                (channel, message) =>
                {
                    string msg = message.ToString();
                    ParseAndDispatch(msg);
                }
            );

            Debug.Log($"[RedisSession] Redis 연결 성공, '{CHANNEL_CREATE}' 채널 구독 중 ({redisConnectionString})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RedisSession] Redis 연결 실패: {e.Message}");
        }
    }

    private void ParseAndDispatch(string message)
    {
        // 메시지 형식: "sessionName|maxPlayers"
        string[] parts = message.Split('|');
        string sessionName = parts[0];
        int maxPlayers = parts.Length > 1 && int.TryParse(parts[1], out int mp) ? mp : 4;

        // Unity 메인 스레드로 마셜링
        _mainThread.Post(_ =>
        {
            Debug.Log($"[RedisSession] 세션 생성 요청 수신: {sessionName} (최대 {maxPlayers}명)");
            OnSessionRequested?.Invoke(sessionName, maxPlayers);
        }, null);
    }

    /// <summary>
    /// 세션이 준비되었음을 Redis를 통해 백엔드에 알림.
    /// </summary>
    public void NotifySessionReady(string sessionName)
    {
        if (_subscriber == null)
        {
            Debug.LogWarning("[RedisSession] Redis 미연결 — ready 알림 불가");
            return;
        }

        _subscriber.PublishAsync(RedisChannel.Literal(CHANNEL_READY), sessionName);
        Debug.Log($"[RedisSession] 세션 준비 알림 전송: {sessionName}");
    }

    void OnDestroy()
    {
        _subscriber?.UnsubscribeAll();
        _redis?.Close();
    }
}
#endif
