using UnityEngine;

/// <summary>
/// 서버 빌드 전용 부트스트랩.
/// BossTestScene에 배치. 서버 빌드일 때 BossDungeonServer, RedisSessionListener를 생성한다.
/// </summary>
public class ServerBootstrap : MonoBehaviour
{

    void Awake()
    {
#if UNITY_SERVER
        Debug.Log("[ServerBootstrap] 서버 빌드 감지 - 서버 컴포넌트 초기화");

        // BossDungeonServer
        if (BossDungeonServer.Instance == null)
        {
            GameObject serverObj = new GameObject("BossDungeonServer");
            serverObj.AddComponent<BossDungeonServer>();
            DontDestroyOnLoad(serverObj);
            Debug.Log("[ServerBootstrap] BossDungeonServer 생성 완료");
        }

        // RedisSessionListener
        if (RedisSessionListener.Instance == null)
        {
            GameObject redisObj = new GameObject("RedisSessionListener");
            redisObj.AddComponent<RedisSessionListener>();
            DontDestroyOnLoad(redisObj);
            Debug.Log("[ServerBootstrap] RedisSessionListener 생성 완료");
        }
#else
        // 클라이언트 빌드에서는 비활성화
        gameObject.SetActive(false);
#endif
    }
}
