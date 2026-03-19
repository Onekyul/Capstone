#if UNITY_SERVER
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 데디케이티드 서버 메인 로직.
/// Redis Pub/Sub 또는 커맨드라인 인자로 세션을 생성하고,
/// 접속한 플레이어를 스폰 + 백엔드에서 장비 조회 → 스탯 계산 후 적용.
///
/// Linux Headless 빌드 전용 (#if UNITY_SERVER).
/// </summary>
public class BossDungeonServer : MonoBehaviour, INetworkRunnerCallbacks
{
    public static BossDungeonServer Instance { get; private set; }

    [Header("프리팹")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkObject dungeonPlayerPrefab;

    [Header("서버 설정")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string backendBaseUrl = "http://localhost:7200/api";

    private NetworkRunner _runner;
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private static readonly HttpClient _httpClient = new();

    // 던전 결과 전송용
    private string _currentSessionName;
    private float _sessionStartTime;
    private readonly Dictionary<PlayerRef, int> _playerUserIds = new();
    private bool _resultSent;

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
        // Redis Pub/Sub 리스너가 있으면 연결
        if (RedisSessionListener.Instance != null)
        {
            RedisSessionListener.Instance.OnSessionRequested += OnSessionRequested;
        }

        // 커맨드라인 인자로 즉시 시작 (-session 세션이름)
        string sessionName = GetCommandLineArg("-session");
        if (!string.IsNullOrEmpty(sessionName))
        {
            string backendUrl = GetCommandLineArg("-backend");
            if (!string.IsNullOrEmpty(backendUrl))
                backendBaseUrl = backendUrl;

            CreateSession(sessionName);
        }
    }

    // ============================
    //  세션 생성
    // ============================

    private void OnSessionRequested(string sessionName, int playerCount)
    {
        maxPlayers = playerCount;
        CreateSession(sessionName);
    }

    public async void CreateSession(string sessionName)
    {
        Debug.Log($"[DediServer] 세션 생성 시작: {sessionName}");

        _currentSessionName = sessionName;
        _resultSent = false;

        _runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(_runner.gameObject);
        _runner.AddCallbacks(this);

        var sceneManager = _runner.GetComponent<INetworkSceneManager>();
        if (sceneManager == null)
            sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Server,
            SessionName = sessionName,
            PlayerCount = maxPlayers,
            SceneManager = sceneManager,
        });

        if (result.Ok)
        {
            Debug.Log($"[DediServer] 세션 시작 완료: {sessionName} (최대 {maxPlayers}명)");

            // Redis에 ready 상태 업데이트 (RedisSessionListener를 통해)
            RedisSessionListener.Instance?.NotifySessionReady(sessionName);
        }
        else
        {
            Debug.LogError($"[DediServer] 세션 시작 실패: {result.ShutdownReason}");
        }
    }

    // ============================
    //  플레이어 관리
    // ============================

    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[DediServer] 플레이어 접속: {player}");

        // 1. DungeonPlayer 스폰 (서버가 InputAuthority를 해당 플레이어에게 부여)
        Vector3 spawnPos = new Vector3(
            UnityEngine.Random.Range(-2f, 2f),
            UnityEngine.Random.Range(-2f, 2f),
            0
        );

        NetworkObject playerObj = runner.Spawn(
            dungeonPlayerPrefab,
            spawnPos,
            Quaternion.identity,
            player // 이 플레이어에게 InputAuthority 부여
        );

        _spawnedPlayers[player] = playerObj;

        // 2. ConnectionToken에서 userId 추출
        int userId = GetUserIdFromPlayer(runner, player);
        if (userId == 0)
        {
            Debug.LogWarning($"[DediServer] userId를 식별할 수 없음: {player}");
            return;
        }

        _playerUserIds[player] = userId;

        // 첫 플레이어 접속 시 타이머 시작
        if (_playerUserIds.Count == 1)
            _sessionStartTime = Time.time;

        // 3. 백엔드에서 플레이어 장비 데이터 조회
        PlayerStatsDto stats = await FetchPlayerStats(userId);
        if (stats == null)
        {
            Debug.LogError($"[DediServer] 스탯 조회 실패: userId={userId}");
            return;
        }

        // 4. 장비 데이터로 스탯 계산
        var calculated = CalculateStatsFromEquipment(stats);

        // 5. 서버 권한으로 스탯 직접 설정 (RPC 아님)
        var dungeonStats = playerObj.GetComponent<DungeonPlayerStats>();
        if (dungeonStats != null)
        {
            dungeonStats.InitFromServerData(calculated.maxHp, calculated.defense, calculated.moveSpeed, calculated.attackSpeed);
        }

        // 6. 서버 권한으로 무기 설정
        var attackManager = playerObj.GetComponent<DungeonAttackManager>();
        if (attackManager != null)
        {
            attackManager.InitWeaponFromServer(calculated.weaponType, stats.equippedWeapon ?? "");
        }

        Debug.Log($"[DediServer] 플레이어 초기화 완료: userId={userId}, weapon={stats.equippedWeapon}, HP={calculated.maxHp}, Def={calculated.defense}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[DediServer] 플레이어 퇴장: {player}");

        if (_spawnedPlayers.TryGetValue(player, out var playerObj))
        {
            runner.Despawn(playerObj);
            _spawnedPlayers.Remove(player);
        }

        _playerUserIds.Remove(player);

        // 모든 플레이어가 나가면 세션 종료
        if (_spawnedPlayers.Count == 0)
        {
            Debug.Log("[DediServer] 모든 플레이어 퇴장 — 세션 종료");
            runner.Shutdown();
        }
    }

    // ============================
    //  스탯 계산
    // ============================

    private struct CalculatedStats
    {
        public float maxHp;
        public float defense;
        public float moveSpeed;
        public float attackSpeed;
        public int weaponType;
    }

    private CalculatedStats CalculateStatsFromEquipment(PlayerStatsDto dto)
    {
        var result = new CalculatedStats
        {
            maxHp = 100f,       // DungeonPlayerStats.baseMaxHP 기본값
            defense = 0f,       // DungeonPlayerStats.baseDefense 기본값
            moveSpeed = 1.0f,
            attackSpeed = 1.0f,
            weaponType = 0
        };

        // 무기 타입 결정
        if (!string.IsNullOrEmpty(dto.equippedWeapon))
            result.weaponType = GetWeaponTypeFromId(dto.equippedWeapon);

        // 장비별 레벨 조회 헬퍼
        int GetLevel(string id)
        {
            if (string.IsNullOrEmpty(id) || dto.equipments == null) return 0;
            var item = dto.equipments.Find(e => e.id == id);
            return item?.level ?? 0;
        }

        // 갑옷 → HP 보너스
        if (!string.IsNullOrEmpty(dto.equippedArmor))
        {
            ArmorData armor = LoadArmorData(dto.equippedArmor);
            if (armor != null)
            {
                int level = GetLevel(dto.equippedArmor);
                result.maxHp += armor.bonusHp + (armor.hpPerLevel * level);
            }
        }

        // 헬멧 → 방어력 보너스
        if (!string.IsNullOrEmpty(dto.equippedHelmet))
        {
            ArmorData helmet = LoadArmorData(dto.equippedHelmet);
            if (helmet != null)
            {
                int level = GetLevel(dto.equippedHelmet);
                result.defense += helmet.bonusDef + (helmet.defPerLevel * level);
            }
        }

        // 신발 → 공격속도 보너스
        if (!string.IsNullOrEmpty(dto.equippedBoots))
        {
            ArmorData boots = LoadArmorData(dto.equippedBoots);
            if (boots != null)
            {
                int level = GetLevel(dto.equippedBoots);
                result.attackSpeed += boots.bonusAttackSpeed + (boots.attackSpeedPerLevel * level);
            }
        }

        return result;
    }

    private ArmorData LoadArmorData(string id)
    {
        ArmorData data = Resources.Load<ArmorData>($"Data/Items/Armor/Helmet/{id}");
        if (data != null) return data;
        data = Resources.Load<ArmorData>($"Data/Items/Armor/Armor/{id}");
        if (data != null) return data;
        data = Resources.Load<ArmorData>($"Data/Items/Armor/Boots/{id}");
        return data;
    }

    private int GetWeaponTypeFromId(string weaponId)
    {
        if (Resources.Load<WeaponData>($"Data/Items/Weapon/Sword/{weaponId}") != null) return 0;
        if (Resources.Load<WeaponData>($"Data/Items/Weapon/Spear/{weaponId}") != null) return 1;
        if (Resources.Load<WeaponData>($"Data/Items/Weapon/Bow/{weaponId}") != null) return 2;
        return 0;
    }

    // ============================
    //  백엔드 통신
    // ============================

    private async Task<PlayerStatsDto> FetchPlayerStats(int userId)
    {
        try
        {
            string url = $"{backendBaseUrl}/Game/player-stats?userId={userId}";
            string response = await _httpClient.GetStringAsync(url);
            return JsonUtility.FromJson<PlayerStatsDto>(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DediServer] 스탯 조회 오류 (userId={userId}): {e.Message}");
            return null;
        }
    }

    // ============================
    //  던전 결과 전송
    // ============================

    /// <summary>
    /// 보스 사망 시 BossStageManager에서 호출.
    /// </summary>
    public async void OnBossDefeated()
    {
        if (_resultSent) return;
        _resultSent = true;

        float clearTime = Time.time - _sessionStartTime;
        Debug.Log($"[DediServer] 보스 처치! 클리어 시간: {clearTime:F1}초");

        // 모든 클라이언트에게 결과 알림 RPC 전송
        foreach (var kvp in _spawnedPlayers)
        {
            var stats = kvp.Value.GetComponent<DungeonPlayerStats>();
            if (stats != null)
                stats.RPC_ShowBossResult(true, clearTime);
        }

        await SendDungeonResult(true, clearTime);
    }

    private async Task SendDungeonResult(bool cleared, float clearTime)
    {
        var resultDto = new DungeonResultDto
        {
            sessionName = _currentSessionName,
            results = new List<PlayerResultDto>()
        };

        foreach (var kvp in _playerUserIds)
        {
            resultDto.results.Add(new PlayerResultDto
            {
                userId = kvp.Value,
                cleared = cleared,
                clearTime = cleared ? clearTime : 0f
            });
        }

        try
        {
            string json = JsonUtility.ToJson(resultDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{backendBaseUrl}/Dungeon/result", content);
            Debug.Log($"[DediServer] 던전 결과 전송: {response.StatusCode}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DediServer] 결과 전송 실패: {e.Message}");
        }
    }

    // ============================
    //  유틸리티
    // ============================

    private int GetUserIdFromPlayer(NetworkRunner runner, PlayerRef player)
    {
        // Fusion의 ConnectionToken API로 userId 추출
        byte[] token = runner.GetPlayerConnectionToken(player);
        if (token != null && token.Length > 0)
        {
            string userIdStr = Encoding.UTF8.GetString(token);
            if (int.TryParse(userIdStr, out int userId))
                return userId;
        }
        return 0;
    }

    private string GetCommandLineArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }

    // ============================
    //  INetworkRunnerCallbacks (서버용)
    // ============================

    public void OnInput(NetworkRunner runner, NetworkInput input) { /* 서버: 입력 없음 */ }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[DediServer] 서버 종료: {reason}");
        _spawnedPlayers.Clear();
        _playerUserIds.Clear();
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // 보스 사망 이벤트 구독
        if (BossStageManager.instance != null)
        {
            BossStageManager.instance.OnBossDefeated += OnBossDefeated;
            Debug.Log("[DediServer] BossStageManager 보스 사망 이벤트 구독 완료");
        }
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}
#endif
