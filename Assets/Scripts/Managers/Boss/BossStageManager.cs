using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BossStageManager : MonoBehaviour
{
    public static BossStageManager instance;

    /// <summary>
    /// 보스 사망 시 발행. 데디서버(BossDungeonServer)가 구독하여 결과 전송.
    /// </summary>
    public event Action OnBossDefeated;

    [Header("Settings")]
    [SerializeField] private BossMonsterController boss;
    [SerializeField] private GameObject chasingMonsterPrefab;
    [SerializeField] private GameObject altarPrefab;

    // 잡몹 위치 동기화용 (MinionSyncManager가 읽어감)
    public List<MonsterController> ActiveMinions { get; private set; } = new List<MonsterController>();

    // 재단 동기화용 (AltarSyncManager가 읽어감)
    public AltarController ActiveAltar { get; private set; }
    [SerializeField] private float rageModeCycle = 120f; // 120초마다 레이지
    [SerializeField] private Transform[] spawnPoints; // 잡몹 스폰 위치들 
    [SerializeField] private float SpawnInterval = 2f;

    private float timer = 0f;
    private bool isRageMode = false;
    private bool _phaseStarted = false; // StartNormalPhase() 호출 전까지 타이머 차단
    private int _playerCount = 1;

    // 잡몹 스폰 코루틴 제어용
    private Coroutine spawnRoutine;

    void Awake() { instance = this; }

    void Start()
    {
#if !UNITY_SERVER
        // 클라이언트: 서버 전용 로직 실행 안 함
        enabled = false;
        return;
#endif
        // 보스 사망 이벤트 구독
        if (boss != null)
            boss.OnDeath += () => OnBossDefeated?.Invoke();

        // 통상 모드는 BossDungeonServer.CoInitBossSession()에서 플레이어 입장 후 호출
    }

    void Update()
    {
        if (!_phaseStarted) return; // 플레이어 입장 전에는 타이머 미동작

        if (!isRageMode)
        {
            timer += Time.deltaTime;
            // UI에 남은 시간 표시 가능 (120 - timer)

            if (timer >= rageModeCycle)
            {
                StartRagePhase();
            }
        }
    }

    /// <summary>서버 전용: 플레이어 수 설정 (HP 스케일링에 사용)</summary>
    public void SetPlayerCount(int count)
    {
        _playerCount = Mathf.Max(1, count);

        // 보스 HP 스케일링
        if (boss != null)
            boss.ScaleHP(_playerCount);

        Debug.Log($"[BossStageManager] 플레이어 {_playerCount}명 — 보스/재단 HP 스케일링 적용");
    }

    // --- 1. 통상 모드 ---
    public void StartNormalPhase()
    {
        _phaseStarted = true;
        isRageMode = false;
        timer = 0f;

        // 잡몹 스폰 시작
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnMinionRoutine());
    }

    IEnumerator SpawnMinionRoutine()
    {
        while (!isRageMode)
        {
            yield return new WaitForSeconds(SpawnInterval);

            if (MonsterPool.Instance == null) continue;
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[BossStageManager] spawnPoints가 비어있어 잡몹 스폰을 건너뜁니다.");
                continue;
            }

            Vector2 spawnPos = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;
            // 동기화 슬롯 한도 초과 시 스폰 생략
            if (ActiveMinions.Count >= MinionSyncManager.MaxMinions) continue;

            MonsterController minion = MonsterPool.Instance.GetFromPool("BossChasing", spawnPos, Quaternion.identity);
            if (minion == null) continue;

            ActiveMinions.Add(minion);
            minion.OnDeath += () => ActiveMinions.Remove(minion);
        }
    }

    // --- 2. 레이지 모드 ---
    public void StartRagePhase()
    {
        isRageMode = true;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine); // 스폰 중단

        if (boss == null)
        {
            Debug.LogError("[BossStageManager] Boss reference is missing. Assign BossMonsterController in the inspector.");
            return;
        }

        // 우선 보스 레이지 상태 전환 (아래 과정에서 예외가 나도 적용되도록)
        boss.SetRageMode(true);

        // A. 맵상의 모든 ChasingMonster 찾기
        GameObject[] minions = GameObject.FindGameObjectsWithTag("Enemy"); // 태그 주의
        int count = 0;

        foreach (var minion in minions)
        {
            // 보스는 제외하고 잡몹만
            if (minion != boss.gameObject)
            {
                count++;
                // 빨려들어가는 연출 후 삭제 (여기선 바로 삭제)
                // 풀링 반납: MonsterPool.Instance.ReturnToPool("ChasingMonster", minion.GetComponent<MonsterController>());
                MonsterController controller = minion.GetComponent<MonsterController>();
                if (controller == null)
                {
                    Debug.LogWarning($"[BossStageManager] Enemy object has no MonsterController: {minion.name}");
                    continue;
                }
                if (MonsterPool.Instance != null)
                    MonsterPool.Instance.ReturnToPool("BossChasing", controller);
            }
        }

        ActiveMinions.Clear(); // 풀 반납된 몬스터 stale 참조 제거
        Debug.Log($"보스가 {count}마리의 하수인을 흡수했습니다!");

        // B. 재단 설치 (보스 근처 랜덤 위치)
        if (altarPrefab == null)
        {
            Debug.LogError("[BossStageManager] Altar prefab is missing. Assign altarPrefab in the inspector.");
            return;
        }

        Vector3 altarPos = boss.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 5f;
        GameObject altarObj = Instantiate(altarPrefab, altarPos, Quaternion.identity);
        AltarController altarController = altarObj.GetComponent<AltarController>();
        if (altarController == null)
        {
            Debug.LogError("[BossStageManager] Altar prefab does not have AltarController component.");
            return;
        }
        altarController.Setup(count, _playerCount); // 잡몹 수 + 플레이어 수 기반 체력 설정
        ActiveAltar = altarController;
    }

    // --- 보스 사망 시 잡몹 전체 제거 ---
    public void ClearAllMinions()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            if (boss != null && enemy == boss.gameObject) continue;
            MonsterController mc = enemy.GetComponent<MonsterController>();
            if (mc != null && MonsterPool.Instance != null)
                MonsterPool.Instance.ReturnToPool("BossChasing", mc);
            else
                Destroy(enemy);
        }

        ActiveMinions.Clear();
        Debug.Log("[BossStageManager] 보스 사망 — 잡몹 전체 제거 완료");
    }

    // --- 3. 그로기 모드 (재단 파괴 시 호출) ---
    public void OnAltarDestroyed()
    {
        ActiveAltar = null;
        // 보스를 그로기 상태로 만듦
        boss.SetGroggyMode();

        // 그로기 시간(8초)이 보스 내부에서 끝나면 다시 StartNormalPhase 호출 필요?
        // BossMonsterController가 8초 뒤에 스스로 Normal로 돌아가므로
        // 여기서는 다시 통상 모드 로직(스폰 등)을 재개할 타이밍을 잡아야 함.

        StartCoroutine(WaitAndRestartNormalPhase(8f));
    }

    IEnumerator WaitAndRestartNormalPhase(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNormalPhase();
    }
}