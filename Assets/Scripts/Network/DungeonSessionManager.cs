using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 클라이언트 측 세션 전환 매니저.
/// 로비(Shared) → 보스던전(Dedicated Server) 핸드오버를 담당.
/// INetworkRunnerCallbacks를 구현하여 OnInput으로 플레이어 입력을 Fusion에 전달.
/// </summary>
public class DungeonSessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static DungeonSessionManager Instance { get; private set; }

    [Header("프리팹")]
    [SerializeField] private NetworkRunner runnerPrefab;

    [Header("씬 이름")]
    [SerializeField] private string bossDungeonSceneName = "BossTestScene 1";
    [SerializeField] private string lobbySceneName = "BaseArea 1";

    private NetworkRunner _currentRunner;
    private bool _isTransitioning;

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

    // ============================
    //  로비 → 보스던전 전환
    // ============================

    /// <summary>
    /// 보스던전 데디서버 세션에 접속.
    /// 백엔드 API에서 받은 sessionName을 전달.
    /// </summary>
    public void EnterBossDungeon(string sessionName)
    {
        if (_isTransitioning) return;
        StartCoroutine(CoEnterBossDungeon(sessionName));
    }

    private IEnumerator CoEnterBossDungeon(string sessionName)
    {
        _isTransitioning = true;
        Debug.Log($"[DungeonSession] 보스던전 세션 접속 시작: {sessionName}");

        // 1. 기존 러너(로비) 모두 종료
        yield return CoShutdownAllRunners();

        // 2. 보스던전 씬 로드
        SceneManager.LoadScene(bossDungeonSceneName);
        yield return null; // 씬 로드 1프레임 대기

        // 3. 새 러너 생성 → GameMode.Client로 데디서버 접속
        yield return CoStartClientRunner(sessionName);

        _isTransitioning = false;
    }

    // ============================
    //  파밍 던전 진입 (Shared 세션 종료 → 로컬 씬 로드)
    // ============================

    /// <summary>
    /// 파밍 던전 진입: Shared 세션을 완전 종료한 뒤 로컬 던전 씬을 로드.
    /// DontDestroyOnLoad 오브젝트에서 실행하므로 씬 전환 중에도 코루틴이 유지됨.
    /// </summary>
    public void EnterFarmingDungeon(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(CoEnterFarmingDungeon(sceneName));
    }

    private IEnumerator CoEnterFarmingDungeon(string sceneName)
    {
        _isTransitioning = true;
        Debug.Log($"[DungeonSession] 파밍 던전 진입: {sceneName}");

        // 1. 로비 Shared 세션 완전 종료
        yield return CoShutdownAllRunners();

        // 2. 로컬 던전 씬 로드 (Fusion 비활성 상태이므로 SceneManager 직접 사용 가능)
        SceneManager.LoadScene(sceneName);

        _isTransitioning = false;
    }

    // ============================
    //  파밍/보스 던전 → 로비 복귀
    // ============================

    public void ReturnToLobby()
    {
        if (_isTransitioning) return;
        StartCoroutine(CoReturnToLobby());
    }

    private IEnumerator CoReturnToLobby()
    {
        _isTransitioning = true;
        Debug.Log("[DungeonSession] 로비로 복귀 중...");

        // 1. 활성 러너 모두 종료 + GameObject 제거
        yield return CoShutdownAllRunners();

        // 2. 로비 씬 로드
        //    "Prototype Network Start"의 FusionBootstrap이 Awake()에서 자동으로
        //    Shared 세션("LobbyRoom")에 재접속 → IPlayerJoined → LobbySpawner 스폰
        //    CoStartLobbyRunner()를 별도로 호출하면 Runner가 2개가 되어 플레이어 중복 스폰됨
        var loadOp = SceneManager.LoadSceneAsync(lobbySceneName);
        while (!loadOp.isDone)
            yield return null;

        _isTransitioning = false;
        Debug.Log("[DungeonSession] 로비 씬 로드 완료 - FusionBootstrap이 세션 재접속 처리");
    }

    // ============================
    //  러너 관리
    // ============================

    private IEnumerator CoShutdownAllRunners()
    {
        // NetworkRunner.Instances로 활성 러너 모두 종료
        var runners = new List<NetworkRunner>(NetworkRunner.Instances);
        foreach (var runner in runners)
        {
            if (runner != null && runner.IsRunning)
            {
                var task = runner.Shutdown();
                while (!task.IsCompleted)
                    yield return null;
            }
        }

        if (_currentRunner != null)
        {
            Destroy(_currentRunner.gameObject);
            _currentRunner = null;
        }
    }

    private IEnumerator CoStartClientRunner(string sessionName)
    {
        _currentRunner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(_currentRunner.gameObject);
        _currentRunner.AddCallbacks(this);

        // INetworkSceneManager 확보
        var sceneManager = _currentRunner.GetComponent<INetworkSceneManager>();
        if (sceneManager == null)
            sceneManager = _currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        // ConnectionToken으로 userId 전달 (서버가 플레이어 식별용)
        byte[] connectionToken = null;
        if (SessionManager.Instance != null)
        {
            connectionToken = Encoding.UTF8.GetBytes(SessionManager.Instance.UserId.ToString());
        }

        var startTask = _currentRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = sceneManager,
            ConnectionToken = connectionToken,
        });

        // Task 완료 대기
        while (!startTask.IsCompleted)
            yield return null;

        if (startTask.Result.Ok)
        {
            Debug.Log($"[DungeonSession] 보스던전 접속 성공: {sessionName}");
        }
        else
        {
            Debug.LogError($"[DungeonSession] 접속 실패: {startTask.Result.ShutdownReason}");
            ReturnToLobby();
        }
    }

    // ============================
    //  INetworkRunnerCallbacks
    // ============================

    /// <summary>
    /// 핵심: InputManager의 입력을 NetworkInputData로 변환하여 Fusion에 전달.
    /// 이것이 없으면 DungeonPlayerController.GetInput()이 동작하지 않음.
    /// </summary>
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (InputManager.instance != null)
        {
            data.direction = InputManager.instance.playerMoveInput;
            data.lookDirection = InputManager.instance.LookDir;
        }

        input.Set(data);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // 클라이언트: 스폰은 서버가 처리
        Debug.Log($"[DungeonSession] 플레이어 입장: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[DungeonSession] 플레이어 퇴장: {player}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[DungeonSession] 세션 종료: {reason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[DungeonSession] 서버 연결 끊김: {reason}");
        ReturnToLobby();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[DungeonSession] 서버 접속 완료");
    }

    // === 미사용 콜백 (빈 구현) ===
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[DungeonSession] 접속 실패: {reason}");
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
