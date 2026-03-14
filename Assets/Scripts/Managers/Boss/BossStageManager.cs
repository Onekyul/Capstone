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
    [SerializeField] private float rageModeCycle = 120f; // 120초마다 레이지
    [SerializeField] private Transform[] spawnPoints; // 잡몹 스폰 위치들 
    [SerializeField] private float SpawnInterval = 2f;

    private float timer = 0f;
    private bool isRageMode = false;

    // 잡몹 스폰 코루틴 제어용
    private Coroutine spawnRoutine;

    void Awake() { instance = this; }

    void Start()
    {
        // 보스 사망 이벤트 구독
        if (boss != null)
            boss.OnDeath += () => OnBossDefeated?.Invoke();

        // 통상 모드 시작
        StartNormalPhase();
    }

    void Update()
    {
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

    // --- 1. 통상 모드 ---
    public void StartNormalPhase()
    {
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
            yield return new WaitForSeconds(SpawnInterval); // SpawnInterval 초마다 스폰

            //잡몹 스폰 위치 중 랜덤 선택
            Vector2 spawnPos = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;

            // 풀링 매니저 사용
            MonsterPool.Instance.GetFromPool("BossChasing", spawnPos, Quaternion.identity);
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
        altarController.Setup(count); // 체력 설정
    }

    // --- 3. 그로기 모드 (재단 파괴 시 호출) ---
    public void OnAltarDestroyed()
    {
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